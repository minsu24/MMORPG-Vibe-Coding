using System;
using System.Reflection;
using EasternFantasy.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GiantSmashJumpValidation
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    public static string Validate()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        int checks = 0;
        try
        {
            var physics = scene.GetPhysicsScene2D();
            if (physics == Physics2D.defaultPhysicsScene) throw new Exception("Physics must be isolated.");
            var floor = new GameObject("Jump validation floor", typeof(BoxCollider2D));
            SceneManager.MoveGameObjectToScene(floor, scene);
            floor.transform.position = new Vector3(0, -0.5f);
            floor.GetComponent<BoxCollider2D>().size = new Vector2(100, 1);
            var bossObject = new GameObject("Timing validation giant");
            SceneManager.MoveGameObjectToScene(bossObject, scene);
            var boss = bossObject.AddComponent<MON_Giant>();
            var evade = typeof(MON_Giant).GetMethod("CanEvadeSmash", Hidden);
            foreach (string prefabName in new[] { "Player", "MonkPlayer", "SwordswomanPlayer" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/" + prefabName + ".prefab");
                var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var entity = player.GetComponent<PlayerEntity>();
                var movement = player.GetComponent<PlayerMovement2D>();
                var body = player.GetComponent<Rigidbody2D>();
                var collider = player.GetComponent<CapsuleCollider2D>();
                typeof(PlayerEntity).GetMethod("Awake", Hidden).Invoke(entity, null);
                typeof(PlayerMovement2D).GetMethod("Awake", Hidden).Invoke(movement, null);
                var tick = typeof(PlayerMovement2D).GetMethod("FixedUpdate", Hidden);
                var started = typeof(PlayerMovement2D).GetField("lastJumpStartedAt", Hidden);
                Action<bool, string> check = (condition, label) => {
                    if (!condition) throw new Exception(prefabName + ": " + label);
                    checks++;
                };
                Func<bool> canEvade = () => (bool)evade.Invoke(boss, new object[] { entity });
                body.position = new Vector2(0, 4);
                player.transform.position = body.position;
                Physics2D.SyncTransforms();
                for (int i = 0; i < 150; i++) { tick.Invoke(movement, null); physics.Simulate(0.02f); }
                tick.Invoke(movement, null);
                check(movement.IsGrounded, "Player settles on floor");
                check(!canEvade(), "Standing does not evade");
                movement.RequestJump();
                check(!canEvade(), "Buffered input alone does not evade");
                tick.Invoke(movement, null);
                check(!movement.IsGrounded && body.linearVelocity.y > 0f, "Actual jump launches");
                check(Mathf.Abs((float)started.GetValue(movement) - Time.time) < 0.001f, "Timestamp tracks executed jump");
                check(!canEvade(), "Jump at impact is too late");
                foreach (float age in new[] { 0.04f, 0.1f, 0.16f, 0.22f, 0.28f, 0.45f })
                {
                    started.SetValue(movement, Time.time - age);
                    check(canEvade() == (age >= 0.08f && age <= 0.24f), "Impact with jump age " + age);
                }
                started.SetValue(movement, Time.time - 0.16f);
                body.linearVelocity = Vector2.down;
                check(!canEvade(), "Falling does not evade");
                body.linearVelocity = Vector2.up * 8f;
                started.SetValue(movement, float.NegativeInfinity);
                check(!canEvade(), "External upward motion does not evade");
                started.SetValue(movement, Time.time - 0.16f);
                movement.SetMovementLocked(true);
                check(!canEvade(), "Locked movement does not evade");
                movement.SetMovementLocked(false);
                started.SetValue(movement, Time.time - 0.16f);
                player.SetActive(false);
                // Preview-scene fixtures do not receive play-mode MonoBehaviour callbacks.
                typeof(PlayerMovement2D).GetMethod("OnDisable", Hidden).Invoke(movement, null);
                check(float.IsNegativeInfinity((float)started.GetValue(movement)), "Disabling clears timing");
                UnityEngine.Object.DestroyImmediate(player);
            }
            string result = checks + " checks passed across Dosa, Monk and Swordswoman: executed jump, timing, standing, falling, external motion and cancellation.";
            System.IO.Directory.CreateDirectory("Artifacts/GiantSmashJump");
            System.IO.File.WriteAllText("Artifacts/GiantSmashJump/Validation.txt", result);
            return result;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
