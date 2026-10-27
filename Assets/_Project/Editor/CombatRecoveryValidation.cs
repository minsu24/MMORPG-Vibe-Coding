using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EasternFantasy.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.EditorTools
{
    public static class CombatRecoveryValidation
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static void Set(object instance, string field, object value) => instance.GetType().GetField(field, Private).SetValue(instance, value);
        private static T Get<T>(object instance, string field) => (T)instance.GetType().GetField(field, Private).GetValue(instance);
        private static void Call(object instance, string method) => instance.GetType().GetMethod(method, Private).Invoke(instance, null);

        [MenuItem("Eastern Fantasy/Controls/Validate Hit Recovery")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play mode.");
            var results = new List<string>();
            Action<bool, string> check = (condition, message) =>
            { if (!condition) throw new InvalidOperationException(message); results.Add("PASS: " + message); };
            Directory.CreateDirectory("Artifacts/CombatRecovery");
            try
            {
                foreach (string name in new[] { "Player", "MonkPlayer", "SwordswomanPlayer" })
                {
                    var scene = EditorSceneManager.NewPreviewScene();
                    try
                    {
                        var physics = scene.GetPhysicsScene2D();
                        var floor = new GameObject("Floor", typeof(BoxCollider2D)); SceneManager.MoveGameObjectToScene(floor, scene);
                        floor.transform.position = new Vector3(0, -.5f); floor.GetComponent<BoxCollider2D>().size = new Vector2(40, 1);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/" + name + ".prefab");
                        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                        var entity = player.GetComponent<PlayerEntity>(); var movement = player.GetComponent<PlayerMovement2D>();
                        var animation = player.GetComponent<PlayerIdleAnimation>(); var animator = player.GetComponentInChildren<Animator>();
                        var monk = player.GetComponent<MonkJumpAnimation>(); var body = player.GetComponent<Rigidbody2D>();
                        Call(entity, "Awake"); Call(movement, "Awake"); animator.Rebind(); animator.Update(0); Call(animation, "Awake");
                        if (monk != null) Call(monk, "Awake");
                        body.position = new Vector2(0, 2); player.transform.position = body.position;
                        for (int i = 0; i < 80; i++) physics.Simulate(.02f);
                        Call(movement, "FixedUpdate"); Vector2 ground = body.position;
                        body.position = ground + Vector2.up * 3; player.transform.position = body.position;
                        body.linearVelocity = Vector2.down; physics.Simulate(.02f); Call(movement, "FixedUpdate");
                        string jump = name == "Player" ? "PlayerJump" : name == "MonkPlayer" ? "MonkJump" : "SwordJump";
                        animation.SetJumping(true); animator.Play("Base Layer." + jump, 0, .6f); animator.Update(0);
                        entity.isKnockback = true;
                        if (monk != null) { Set(monk, "wasAirborne", true); Call(monk, "Update"); }
                        Call(animation, "LateUpdate"); animator.Update(0);
                        string idle = Get<string>(animation, "idleStateName");
                        check(animator.GetCurrentAnimatorStateInfo(0).IsName(idle), name + ": hit interrupts the jump pose immediately");
                        check(!animator.GetBool("isJumping"), name + ": jump parameter stays clear during hit stun");
                        body.position = ground; player.transform.position = ground; body.linearVelocity = Vector2.zero;
                        for (int i = 0; i < 8; i++) physics.Simulate(.02f);
                        Call(movement, "FixedUpdate");
                        if (monk != null)
                        {
                            Call(monk, "CompleteLanding");
                            check(Get<bool>(monk, "wasAirborne"), "Monk: landing during hit stun remains pending until animation control returns");
                        }
                        entity.isKnockback = false;
                        if (monk != null) Call(monk, "Update");
                        Call(animation, "LateUpdate"); animator.Update(0);
                        check(movement.IsGrounded && animator.GetCurrentAnimatorStateInfo(0).IsName(idle),
                            name + ": landing during knockback returns to idle after recovery");
                    }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                }
                File.WriteAllText("Artifacts/CombatRecovery/Validation.txt", string.Join("\n", results) + "\nAll " + results.Count + " checks passed.");
                Debug.Log("Hit recovery: " + results.Count + " checks passed.");
            }
            catch (Exception error)
            {
                File.WriteAllText("Artifacts/CombatRecovery/Validation.txt", string.Join("\n", results) + "\nFAIL: " + error);
                Debug.LogException(error);
            }
        }

        public static IEnumerator RunSpawnContactGrace(Action<bool, string> check)
        {
            var entity = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            var movement = entity.GetComponent<PlayerMovement2D>(); var input = entity.GetComponent<PlayerInputReader>();
            var body = movement.body;
            Vector3 savedPosition = entity.transform.position; var savedConstraints = body.constraints;
            var savedSleep = body.sleepMode; bool savedInput = input.enabled;
            float savedHealth = entity.HP;
            int enemyLayer = LayerMask.NameToLayer("Enemy"), playerLayer = entity.gameObject.layer;
            bool ignored = Physics2D.GetIgnoreLayerCollision(enemyLayer, playerLayer);
            var root = new GameObject("Spawn Contact Grace Regression"); root.SetActive(false);
            try
            {
                input.enabled = false; movement.TeleportTo(new Vector3(10000, 1000), Quaternion.identity);
                body.constraints = RigidbodyConstraints2D.FreezeAll; body.sleepMode = RigidbodySleepMode2D.NeverSleep;
                entity.isInvincible = false; entity.isKnockback = false;
                Physics2D.IgnoreLayerCollision(enemyLayer, playerLayer, false);
                var point = new GameObject("Spawn Point").transform; point.SetParent(root.transform);
                point.position = entity.transform.position + Vector3.right * .2f;
                var spawner = root.AddComponent<MonsterSpawner>();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Ghost.prefab").GetComponent<EnemyController>();
                Set(spawner, "monsterPrefab", prefab); Set(spawner, "spawnPoints", new[] { point });
                Set(spawner, "spawnedMonsterRoot", root.transform); Set(spawner, "maximumAlive", 1); Set(spawner, "initialSpawnDelay", 100f);
                root.SetActive(true); spawner.RefillMissingMonsters();
                var enemy = root.GetComponentInChildren<EnemyController>();
                var enemyBody = enemy.GetComponent<Rigidbody2D>(); enemyBody.constraints = RigidbodyConstraints2D.FreezeAll;
                var enemyCollider = enemy.GetComponent<CapsuleCollider2D>();
                var playerCollider = entity.GetComponent<CapsuleCollider2D>();
                var renderer = enemy.GetComponent<SpriteRenderer>();
                var originalMask = prefab.GetComponent<CapsuleCollider2D>().excludeLayers;
                float originalAlpha = prefab.GetComponent<SpriteRenderer>().color.a;
                enemyBody.sleepMode = RigidbodySleepMode2D.NeverSleep;
                float started = Time.time;
                check(enemy != null && !enemy.CanDealContactDamage, "Actual spawner gives the new monster contact protection immediately");
                check((enemyCollider.excludeLayers.value & (1 << playerLayer)) != 0
                    && (enemyCollider.excludeLayers.value & ~(1 << playerLayer)) == (originalMask.value & ~(1 << playerLayer)),
                    "Spawn protection excludes player collision while preserving terrain collision settings");
                check(Mathf.Approximately(renderer.color.a, originalAlpha * .45f), "The new monster appears translucent during spawn protection");
                Physics2D.SyncTransforms();
                while (Time.time - started < .55f) yield return null;
                check(entity.HP == savedHealth && !enemy.CanDealContactDamage,
                    "Overlapping the respawned monster during its first 0.55 seconds causes no damage or knockback");
                check(!enemyCollider.IsTouching(playerCollider), "Player and monster physically pass through each other during spawn protection");
                while (Time.time - started < .8f) yield return null;
                check(enemy.CanDealContactDamage && enemyCollider.excludeLayers == originalMask,
                    "Contact protection and player collision exclusion expire after 0.7 gameplay seconds");
                check(Mathf.Approximately(renderer.color.a, originalAlpha), "The monster returns to its original opacity after spawn protection");
                check(entity.HP < savedHealth, "Continuing contact damages the player after the grace period without requiring re-entry");
                while (entity.isInvincible || entity.isKnockback) yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
                Physics2D.IgnoreLayerCollision(enemyLayer, playerLayer, ignored);
                body.constraints = savedConstraints; body.sleepMode = savedSleep;
                movement.TeleportTo(savedPosition, Quaternion.identity); entity.HP = savedHealth; input.enabled = savedInput;
            }
        }
    }
}
