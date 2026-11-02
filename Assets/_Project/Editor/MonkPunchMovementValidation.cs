using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.Skill;
using EasternFantasy.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class MonkPunchMovementValidation
    {
        private const string Report = "Artifacts/MonkPunchMovement/Validation.txt";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        public static void Run()
        {
            Directory.CreateDirectory("Artifacts/MonkPunchMovement");
            File.WriteAllText(Report, "Native straight punch movement validation\n");
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(Validate());
        }

        private static IEnumerator Validate()
        {
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            SaveGameService.BeginAdminSession();
            yield return SceneManager.LoadSceneAsync("MovementPrototype");
            yield return null;
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            var movement = player.GetComponent<PlayerMovement2D>();
            var animation = player.GetComponent<PlayerIdleAnimation>();
            var caster = player.GetComponent<MonkSkillCaster>();
            player.isInvincible = true;
            player.GetComponent<PlayerInputReader>().enabled = false;
            typeof(PlayerIdleAnimation).GetField("inputReader", Hidden).SetValue(animation, null);
            var skill = (SkillDefinition)typeof(MonkSkillCaster).GetField("straightPunchSkill", Hidden).GetValue(caster);
            var levels = (Dictionary<SkillDefinition, int>)typeof(PlayerSkillSystem).GetField("skillLevels", Hidden).GetValue(player.GetComponent<PlayerSkillSystem>());
            levels[skill] = 1;
            var cooldowns = (Dictionary<SkillDefinition, float>)typeof(MonkSkillCaster).GetField("cooldownEnds", Hidden).GetValue(caster);
            var body = movement.body;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var platform = new GameObject("Straight Punch Test Platform");
            try
            {
                int mask = ((LayerMask)typeof(PlayerMovement2D).GetField("groundLayers", Hidden).GetValue(movement)).value;
                platform.layer = Enumerable.Range(0, 32).First(i => (mask & (1 << i)) != 0);
                player.GetComponent<PlayerLocationSetter>().TeleportToAndRetarget(new Vector3(10000f, 30f, 0f), Quaternion.identity);
                float feet = player.GetComponent<CapsuleCollider2D>().bounds.min.y;
                platform.transform.position = new Vector3(10000f, feet - 0.52f, 0f);
                platform.AddComponent<BoxCollider2D>().size = new Vector2(50f, 1f);
                Physics2D.SyncTransforms();
                yield return new WaitForSeconds(0.15f);
                Check(movement.IsGrounded, "grounded on test platform");
                movement.SetMoveInput(1f);
                yield return new WaitForFixedUpdate();
                Check(body.linearVelocity.x > 0.1f, "movement available before cast");
                player.GetComponent<MonkEnergy>().Gain(1000f);
                Check(caster.TryCast(skill), "real grounded straight punch accepted");
                Check(movement.IsGroundedAttackMovementLocked && Mathf.Abs(body.linearVelocity.x) < 0.001f, "cast stops horizontal movement immediately");
                float x = body.position.x;
                float deadline = Time.realtimeSinceStartup + 4f;
                int frames = 0;
                while (animation.IsAttacking && Time.realtimeSinceStartup < deadline)
                {
                    movement.SetMoveInput(-1f);
                    movement.RequestJump();
                    yield return new WaitForFixedUpdate();
                    if (!animation.IsAttacking) break;
                    Check(Mathf.Abs(body.linearVelocity.x) < 0.001f && Mathf.Abs(body.position.x - x) < 0.02f && body.linearVelocity.y <= 0.1f, "grounded cast blocks movement and jump", false);
                    frames++;
                }
                yield return new WaitForFixedUpdate();
                Check(frames > 2 && !animation.IsAttacking && !movement.IsGroundedAttackMovementLocked && body.linearVelocity.x < -0.1f, "movement resumes after full animation recovery");
                yield return null;
                cooldowns.Clear();
                movement.RequestJump();
                yield return new WaitForFixedUpdate();
                float vy = body.linearVelocity.y;
                float vx = body.linearVelocity.x;
                Check(!movement.IsGrounded && vy > 0f, "jump remains available after cast");
                player.GetComponent<MonkEnergy>().Gain(1000f);
                Check(caster.TryCast(skill), "real airborne straight punch accepted");
                Check(!movement.IsGroundedAttackMovementLocked && body.linearVelocity.y == vy && body.linearVelocity.x == vx, "air cast preserves jump velocity");
                bool left = false, right = false, landed = false;
                frames = 0;
                deadline = Time.realtimeSinceStartup + 4f;
                while (animation.IsAttacking && Time.realtimeSinceStartup < deadline)
                {
                    float direction = frames++ < 4 ? -1f : 1f;
                    movement.SetMoveInput(direction);
                    yield return new WaitForFixedUpdate();
                    if (!animation.IsAttacking) break;
                    if (!movement.IsGrounded)
                    {
                        Check(body.linearVelocity.x * direction > 0.1f && !movement.IsMovementLocked, "airborne steering and gravity remain active", false);
                        left |= body.linearVelocity.x < -0.1f;
                        right |= body.linearVelocity.x > 0.1f;
                    }
                    else
                    {
                        landed = true;
                        Check(Mathf.Abs(body.linearVelocity.x) < 0.001f, "landing during cast stops movement", false);
                    }
                }
                Check(left && right && body.gravityScale > 0f, "airborne cast allows both steering directions and gravity");
                Check(!animation.IsAttacking, "airborne cast recovers normally");
                File.AppendAllText(Report, "Landing observed: " + landed + "\n");
                yield return null;
                cooldowns.Clear();
                player.GetComponent<MonkEnergy>().Gain(1000f);
                Check(caster.TryCast(skill), "cast accepted for cancellation test");
                animation.FinishSkillAnimation();
                movement.SetMoveInput(1f);
                yield return new WaitForFixedUpdate();
                Check(!movement.IsGroundedAttackMovementLocked && body.linearVelocity.x > 0.1f, "cancellation releases movement");
                File.AppendAllText(Report, "COMPLETE\n");
            }
            finally
            {
                movement.ClearInput();
                body.constraints = RigidbodyConstraints2D.FreezeAll;
                UnityEngine.Object.Destroy(platform);
            }
        }

        private static void Check(bool ok, string message, bool logSuccess = true)
        {
            if (logSuccess || !ok) File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
