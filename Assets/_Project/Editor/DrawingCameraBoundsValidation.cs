using System;
using System.Collections;
using System.IO;
using System.Reflection;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.Skill;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class DrawingCameraBoundsValidation
    {
        private const string Folder = "Artifacts/DrawingCameraBounds";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Check(bool condition, string message)
        {
            File.AppendAllText(Folder + "/Validation.txt", (condition ? "PASS " : "FAIL ") + message + "\n");
            if (!condition) throw new InvalidOperationException(message);
        }
        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/Validation.txt", "Drawing camera final viewport confinement\n");
            EditorApplication.LockReloadAssemblies();
            SaveGameService.Instance.StartCoroutine(Validate());
        }
        private static IEnumerator Validate()
        {
            var scene = SceneManager.CreateScene("Drawing Camera Bounds Validation");
            GameObject actor = null;
            try
            {
                Application.runInBackground = true;
                var holder = new GameObject("Inactive camera test actor"); holder.SetActive(false);
                SceneManager.MoveGameObjectToScene(holder, scene);
                actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/SwordswomanPlayer.prefab"), holder.transform);
                UnityEngine.Object.DestroyImmediate(actor.GetComponent<PlayerClassRuntime>());
                UnityEngine.Object.DestroyImmediate(actor.GetComponent<PlayerAdminController>());
                UnityEngine.Object.DestroyImmediate(actor.GetComponent<PlayerLocationSetter>());
                actor.transform.position = Vector3.zero; actor.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(actor, scene); UnityEngine.Object.Destroy(holder);
                actor.GetComponent<PlayerInputReader>().enabled = false;
                var animation = actor.GetComponent<PlayerIdleAnimation>();
                typeof(PlayerIdleAnimation).GetField("inputReader", Hidden).SetValue(animation, null);
                var movement = actor.GetComponent<PlayerMovement2D>();
                movement.body.constraints = RigidbodyConstraints2D.FreezeAll; movement.body.gravityScale = 0;
                var entity = actor.GetComponent<PlayerEntity>(); entity.isInvincible = true;
                var drawing = actor.GetComponent<SwordswomanDrawingAttack>();
                typeof(SwordswomanDrawingAttack).GetField("damageMultiplier", Hidden).SetValue(drawing, .001f);
                var skill = (SkillDefinition)typeof(SwordswomanSkillCaster).GetField("drawingSkill", Hidden).GetValue(actor.GetComponent<SwordswomanSkillCaster>());

                var boundsRoot = Root(scene, "Camera boundary");
                var bounds = boundsRoot.AddComponent<PolygonCollider2D>(); bounds.isTrigger = true;
                bounds.points = new[] { new Vector2(-15,-8), new Vector2(-15,8), new Vector2(15,8), new Vector2(15,-8) };
                foreach (float x in new[] { -14.5f, 14.5f })
                {
                    var wall = Root(scene, "Solid map edge"); wall.transform.position = new Vector3(x, 0, 0);
                    wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 15);
                }
                var outputRoot = Root(scene, "Camera validation output");
                var output = outputRoot.AddComponent<Camera>(); output.orthographic = true; output.orthographicSize = 3;
                output.aspect = 16f/9f; output.transform.position = new Vector3(0, 0, -10);
                var brain = outputRoot.AddComponent<CinemachineBrain>();
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate; brain.IgnoreTimeScale = true;
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
                var cameraRoot = Root(scene, "Camera validation virtual camera");
                var camera = cameraRoot.AddComponent<CinemachineCamera>();
                camera.Priority = 1000; camera.Follow = actor.transform;
                var lens = camera.Lens; lens.OrthographicSize = 3; lens.ModeOverride = LensSettings.OverrideModes.Orthographic; camera.Lens = lens;
                var composer = cameraRoot.AddComponent<CinemachinePositionComposer>(); composer.CameraDistance = 10;
                var confiner = cameraRoot.AddComponent<CinemachineConfiner2D>(); confiner.BoundingShape2D = bounds;
                cameraRoot.AddComponent<CameraFollowDetail>();
                var finalizer = cameraRoot.GetComponent<CameraBoundaryFinalizer>();
                Check(finalizer != null, "tracking camera automatically installs final confinement");
                for (int i = 0; i < 5; i++) { brain.ManualUpdate(Time.frameCount, 1f/60); yield return null; }
                Check(Fits(output, bounds.bounds), "initial camera viewport fits boundary");

                movement.TeleportTo(new Vector3(14, 0, 0), Quaternion.identity);
                brain.ManualUpdate(Time.frameCount, 1f/60);
                var state = camera.State; state.PositionCorrection += Vector3.right * .75f;
                Check(!Fits(state.GetFinalPosition(), state.Lens.OrthographicSize, state.Lens.Aspect, bounds.bounds),
                    "reproduced boundary escape when shake is added after normal confinement");
                finalizer.InvokePostPipelineStageCallback(camera, CinemachineCore.Stage.Finalize, ref state, 1f/60);
                Check(Fits(state.GetFinalPosition(), state.Lens.OrthographicSize, state.Lens.Aspect, bounds.bounds),
                    "final confinement corrects outward shake with the baked polygon");
                yield return null;

                var enemies = new EnemyController[2];
                for (int i = 0; i < enemies.Length; i++)
                {
                    var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Spider.prefab"));
                    SceneManager.MoveGameObjectToScene(go, scene);
                    var enemy = go.GetComponent<EnemyController>(); enemy.enabled = false;
                    go.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                    typeof(EnemyController).GetField("_reward_EXP", Hidden).SetValue(enemy, 0f);
                    typeof(EnemyController).GetField("_rewardYeopjeon", Hidden).SetValue(enemy, 0);
                    enemies[i] = enemy;
                }
                int caseId = 0;
                foreach (float aspect in new[] { 16f/9f, 4f/3f })
                    foreach (int direction in new[] { 1, -1 })
                    {
                        caseId++; output.aspect = aspect;
                        lens = camera.Lens; lens.OrthographicSize = caseId > 2 ? 4 : 3; camera.Lens = lens;
                        confiner.InvalidateLensCache();
                        drawing.enabled = false; drawing.enabled = true;
                        actor.GetComponentInChildren<Animator>().speed = 1;
                        movement.TeleportTo(new Vector3(direction*7, 0, 0), Quaternion.identity);
                        foreach (var enemy in enemies) enemy.HP = enemy.maxHP;
                        enemies[0].transform.position = new Vector3(direction*11, .15f, 0);
                        enemies[1].transform.position = new Vector3(direction*13, .15f, 0);
                        Physics2D.SyncTransforms(); Time.timeScale = 1;
                        brain.ManualUpdate(Time.frameCount, 1f/60); yield return null;
                        entity.MP = entity.maxMP;
                        Check(drawing.TryStart(skill), "drawing begins near map edge, case " + caseId);
                        actor.GetComponentInChildren<Animator>().speed = 0;
                        typeof(SwordswomanDrawingAttack).GetField("direction", Hidden).SetValue(drawing, (float)direction);
                        drawing.Release();
                        Check(!camera.PreviousStateIsValid, "teleport invalidates previous follow/damping state, case " + caseId);
                        Check(Fits(output, bounds.bounds), "output remains inside before next camera update, case " + caseId);
                        Check(Mathf.Abs(movement.body.position.x) > 13, "drawing reaches wall-side destination, case " + caseId);
                        for (int frame = 0; frame < 20; frame++)
                        {
                            brain.ManualUpdate(Time.frameCount, 1f/60);
                            Check(Fits(output, bounds.bounds), "render viewport stays inside after drawing and impact shake, case " + caseId + ", frame " + frame);
                            yield return null;
                        }
                        drawing.enabled = false; drawing.enabled = true;
                    }
                File.AppendAllText(Folder + "/Validation.txt", "COMPLETE\n");
            }
            finally
            {
                if (actor != null) UnityEngine.Object.Destroy(actor);
                SceneManager.UnloadSceneAsync(scene);
                Time.timeScale = 1;
                EditorApplication.UnlockReloadAssemblies();
            }
        }
        private static GameObject Root(Scene scene, string name)
        {
            var root = new GameObject(name); SceneManager.MoveGameObjectToScene(root, scene); return root;
        }
        private static bool Fits(Camera camera, Bounds bounds) => Fits(camera.transform.position, camera.orthographicSize, camera.aspect, bounds);
        private static bool Fits(Vector3 center, float halfHeight, float aspect, Bounds bounds)
        {
            float halfWidth = halfHeight*aspect;
            return center.x-halfWidth >= bounds.min.x-.01f && center.x+halfWidth <= bounds.max.x+.01f
                && center.y-halfHeight >= bounds.min.y-.01f && center.y+halfHeight <= bounds.max.y+.01f;
        }
    }
}
