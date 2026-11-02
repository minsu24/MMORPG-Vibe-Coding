using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class BasicAttackMovementValidation
    {
        private const string Folder = "Artifacts/BasicAttackMovement";
        private const string Key = "EasternFantasy.BasicAttackMovementValidation";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void Prepare()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Prepare the isolated test before entering Play Mode.");
            Directory.CreateDirectory(Folder);
            foreach (CharacterClassId id in Enum.GetValues(typeof(CharacterClassId)))
            {
                string path = SaveGameService.PathFor(id);
                SessionState.SetString(Key + ".Path." + id, path);
                SessionState.SetString(Key + ".Contents." + id, File.Exists(path) ? File.ReadAllText(path) : "");
            }
            MonkChargeAudioValidation.Prepare();
            var seed = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(SaveGameService.PathFor(CharacterClassId.Monk)));
            foreach (CharacterClassId id in Enum.GetValues(typeof(CharacterClassId)))
            {
                seed.characterClass = (int)id;
                seed.scene = "MovementPrototype";
                seed.mana = seed.mental = 100f;
                File.WriteAllText(SaveGameService.PathFor(id), JsonUtility.ToJson(seed, true));
            }
            File.WriteAllText(Folder + "/Validation.txt", "Native three-class basic attack movement validation\n");
        }

        public static void Cleanup()
        {
            foreach (CharacterClassId id in Enum.GetValues(typeof(CharacterClassId)))
            {
                string path = SessionState.GetString(Key + ".Path." + id, "");
                Check((File.Exists(path) ? File.ReadAllText(path) : "") == SessionState.GetString(Key + ".Contents." + id, ""), id + ": original save unchanged");
            }
            MonkChargeAudioValidation.Cleanup();
        }

        public static void Run()
        {
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(Validate());
        }

        private static IEnumerator Validate()
        {
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            foreach (CharacterClassId id in Enum.GetValues(typeof(CharacterClassId)))
            {
                SaveGameService.PrepareSelection();
                PlayerPrefs.SetInt("Character.SelectedClass", (int)id);
                yield return SceneManager.LoadSceneAsync("MovementPrototype");
                yield return null;
                while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
                SaveGameService.BeginAdminSession();
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
                Check(player.GetComponent<PlayerClassRuntime>().RepresentedClass == id, id + ": real class prefab loaded");
                player.isInvincible = true;
                player.GetComponent<PlayerInputReader>().enabled = false;
                var animation = player.GetComponent<PlayerIdleAnimation>();
                // Supply deterministic movement directly, without requiring keyboard focus.
                typeof(PlayerIdleAnimation).GetField("inputReader", Hidden).SetValue(animation, null);
                var movement = player.GetComponent<PlayerMovement2D>();
                var body = movement.body;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.simulated = true;
                var attack = player.GetComponents<MonoBehaviour>().OfType<IPlayerBasicAttack>()
                    .First(a => ((MonoBehaviour)a).isActiveAndEnabled);
                var platform = new GameObject("Basic Attack Test Platform");
                try
                {
                    int mask = ((LayerMask)typeof(PlayerMovement2D).GetField("groundLayers", Hidden).GetValue(movement)).value;
                    int layer = Enumerable.Range(0, 32).First(i => (mask & (1 << i)) != 0);
                    platform.layer = layer;
                    player.GetComponent<PlayerLocationSetter>().TeleportToAndRetarget(new Vector3(10000f, 30f, 0f), Quaternion.identity);
                    float feet = player.GetComponent<CapsuleCollider2D>().bounds.min.y;
                    platform.transform.position = new Vector3(10000f, feet - 0.52f, 0f);
                    platform.AddComponent<BoxCollider2D>().size = new Vector2(50f, 1f);
                    Physics2D.SyncTransforms();
                    yield return new WaitForSeconds(0.12f);
                    Check(movement.IsGrounded, id + ": grounded on test platform");
                    movement.SetMoveInput(1f);
                    yield return new WaitForFixedUpdate();
                    Check(body.linearVelocity.x > 0.1f, id + ": normal movement works before attacking");
                    Check(attack.TryAttack(), id + ": grounded basic attack accepted");
                    Check(movement.IsBasicAttackMovementLocked && Mathf.Abs(body.linearVelocity.x) < 0.001f,
                        id + ": attack immediately stops existing horizontal motion");
                    Vector2 start = body.position;
                    int fixedFrames = 0;
                    float deadline = Time.realtimeSinceStartup + 4f;
                    while (animation.IsBasicAttackActive && Time.realtimeSinceStartup < deadline)
                    {
                        movement.SetMoveInput(-1f);
                        movement.RequestJump();
                        yield return new WaitForFixedUpdate();
                        if (!animation.IsBasicAttackActive) break;
                        Check(Mathf.Abs(body.linearVelocity.x) < 0.001f && Mathf.Abs(body.position.x - start.x) < 0.02f,
                            id + ": no horizontal movement during the full attack", false);
                        Check(body.linearVelocity.y <= 0.1f, id + ": cannot start jumping out of a grounded attack", false);
                        fixedFrames++;
                    }
                    Check(!animation.IsBasicAttackActive && fixedFrames > 2, id + ": grounded attack completes normally while movement stays blocked");
                    yield return new WaitForFixedUpdate();
                    Check(body.linearVelocity.x < -0.1f && !movement.IsBasicAttackMovementLocked,
                        id + ": held movement resumes automatically after recovery");
                    yield return new WaitForSeconds(0.05f);

                    movement.SetMoveInput(1f);
                    movement.RequestJump();
                    yield return new WaitForFixedUpdate();
                    Check(body.linearVelocity.y > 0f && !movement.IsGrounded, id + ": normal jump remains available");
                    float verticalSpeed = body.linearVelocity.y;
                    float horizontalSpeed = body.linearVelocity.x;
                    Check(attack.TryAttack(), id + ": basic attack can start during a real jump");
                    Check(!movement.IsBasicAttackMovementLocked && Mathf.Abs(body.linearVelocity.x - horizontalSpeed) < 0.001f
                        && Mathf.Abs(body.linearVelocity.y - verticalSpeed) < 0.001f,
                        id + ": airborne attack preserves horizontal and vertical velocity");
                    float airY = body.position.y;
                    int airFrames = 0;
                    bool movedLeft = false, movedRight = false;
                    deadline = Time.realtimeSinceStartup + 4f;
                    while (animation.IsBasicAttackActive && Time.realtimeSinceStartup < deadline)
                    {
                        float direction = airFrames < 5 ? -1f : 1f;
                        movement.SetMoveInput(direction);
                        yield return new WaitForFixedUpdate();
                        if (animation.IsBasicAttackActive && !movement.IsGrounded)
                        {
                            Check(body.linearVelocity.x * direction > 0.1f && !movement.IsBasicAttackMovementLocked,
                                id + ": directional input freely controls jump attacks", false);
                            movedLeft |= body.linearVelocity.x < -0.1f;
                            movedRight |= body.linearVelocity.x > 0.1f;
                        }
                        else if (animation.IsBasicAttackActive)
                            Check(Mathf.Abs(body.linearVelocity.x) < 0.001f, id + ": landing restores the grounded attack movement restriction", false);
                        airFrames++;
                    }
                    Check(movedLeft && movedRight, id + ": can steer both left and right during the same jump attack");
                    Check(!animation.IsBasicAttackActive && body.gravityScale > 0f
                        && (Mathf.Abs(body.position.y - airY) > 0.05f || body.linearVelocity.y < verticalSpeed - 1f),
                        id + ": gravity and vertical movement continue during the airborne attack");
                    movement.SetMoveInput(-1f);
                    yield return new WaitForFixedUpdate();
                    Check(body.linearVelocity.x < -0.1f, id + ": airborne movement resumes after the attack");
                    yield return new WaitForSeconds(0.05f);

                    Check(attack.TryAttack(), id + ": next attack accepted for cancellation test");
                    animation.CancelBasicAttack();
                    movement.SetMoveInput(1f);
                    yield return new WaitForFixedUpdate();
                    Check(!movement.IsBasicAttackMovementLocked && body.linearVelocity.x > 0.1f,
                        id + ": cancellation releases movement immediately");
                    movement.SetMovementLocked(true);
                    Check(!attack.TryAttack() && movement.IsMovementLocked, id + ": existing full movement locks still block combat");
                    movement.SetMovementLocked(false);
                }
                finally
                {
                    movement.ClearInput();
                    body.constraints = RigidbodyConstraints2D.FreezeAll;
                    UnityEngine.Object.Destroy(platform);
                }
            }
            File.AppendAllText(Folder + "/Validation.txt", "COMPLETE\n");
        }

        private static void Check(bool ok, string message, bool logSuccess = true)
        {
            if (logSuccess || !ok) File.AppendAllText(Folder + "/Validation.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
