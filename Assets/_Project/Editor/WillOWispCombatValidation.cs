using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class WillOWispCombatValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        public static string Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run this validation in Play Mode.");
            var report = new List<string>();
            Action<bool, string> check = (ok, message) =>
            {
                if (!ok) throw new InvalidOperationException(message);
                report.Add("PASS " + message);
            };
            var previous = SceneManager.GetActiveScene();
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            var scene = SceneManager.CreateScene("WillOWispCombatValidation", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            var physics = scene.GetPhysicsScene2D();
            var root = new GameObject("Wisp validation fixture");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.SetActive(false);
            try
            {
                var playerObject = new GameObject("Wisp validation player");
                playerObject.transform.SetParent(root.transform);
                playerObject.layer = LayerMask.NameToLayer("Player");
                var player = playerObject.AddComponent<PlayerEntity>();
                player.enabled = false;
                var movement = playerObject.GetComponent<PlayerMovement2D>();
                movement.enabled = false;
                var settings = new SerializedObject(movement);
                settings.FindProperty("settings").objectReferenceValue = new SerializedObject(
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab").GetComponent<PlayerMovement2D>())
                    .FindProperty("settings").objectReferenceValue;
                settings.ApplyModifiedPropertiesWithoutUndo();
                var playerBody = playerObject.GetComponent<Rigidbody2D>();
                playerBody.bodyType = RigidbodyType2D.Kinematic;
                playerBody.gravityScale = 0f;
                var playerCollider = playerObject.GetComponent<CapsuleCollider2D>();
                playerCollider.size = new Vector2(.8f, 1.7f);
                playerObject.transform.position = new Vector3(4f, 1f, 0f);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WillOWispCombatSetup.PrefabPath);
                var actor = UnityEngine.Object.Instantiate(prefab, root.transform);
                actor.transform.position = new Vector3(0f, 2f, 0f);
                var monster = actor.GetComponent<MON_WillOWisp>();
                foreach (var behaviour in actor.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
                root.SetActive(true);
                // No automatic Update runs during this synchronous fixture. Drive the real AI and Animator explicitly.
                typeof(EnemyController).GetField("playerEntity", Hidden).SetValue(monster, player);
                typeof(EnemyController).GetField("player", Hidden).SetValue(monster, playerObject);
                var animator = actor.GetComponent<Animator>();
                animator.Rebind(); animator.Update(0f);
                check(actor.GetComponent<MON_Ginseng>() == null, "Prefab uses dedicated wisp AI");
                check(actor.GetComponent<Rigidbody2D>().gravityScale == 0f, "Wisp hovers without gravity");
                foreach (string name in new[] { "Idle", "Move", "Attack", "Dead" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WillOWispCombatSetup.AnimationFolder + "Will-o " + name + ".anim");
                    var keys = AnimationUtility.GetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
                    check(keys.Length == 8 && keys.All(k => k.value != null), name + " has eight valid frames");
                    check(clip.isLooping == (name == "Idle" || name == "Move"), name + " looping configured correctly");
                }
                check(animator.GetCurrentAnimatorStateInfo(0).IsName("Will-o Idle"), "Animator starts idle");
                animator.SetBool("isMoving", true); animator.Update(.02f); animator.Update(.02f);
                check(animator.GetCurrentAnimatorStateInfo(0).IsName("Will-o Move"), "isMoving enters movement");
                animator.SetBool("isMoving", false); animator.Update(.02f); animator.Update(.02f);
                check(animator.GetCurrentAnimatorStateInfo(0).IsName("Will-o Idle"), "Clearing isMoving returns to idle");

                var ability = typeof(MON_WillOWisp).GetMethod("MonsterAbility", Hidden);
                Func<WillOWispProjectile[]> projectiles = () => UnityEngine.Object.FindObjectsByType<WillOWispProjectile>(FindObjectsSortMode.None)
                    .Where(p => p.gameObject.scene == scene).ToArray();
                ability.Invoke(monster, null);
                monster.EmitWispVolley();
                check(!monster.IsProvoked && !monster.IsAttacking && projectiles().Length == 0,
                    "Passing a neutral wisp never starts an attack or emits flames");
                check(monster.ContactDamage == 0f && (actor.GetComponent<Collider2D>().excludeLayers.value & LayerMask.GetMask("Player")) != 0,
                    "Player can pass through the wisp without contact damage or blocking");
                var locomotion = typeof(MON_WillOWisp).GetMethod("LocomotionFixedUpdate", Hidden);
                var body = actor.GetComponent<Rigidbody2D>();
                float fastest = 0f;
                bool waited = false;
                float untouchedHealth = player.HP;
                for (int frame = 0; frame < 300; frame++)
                {
                    locomotion.Invoke(monster, null);
                    fastest = Mathf.Max(fastest, body.linearVelocity.magnitude);
                    waited |= body.linearVelocity.sqrMagnitude < .001f;
                    playerBody.position = new Vector2(-5f + frame / 30f, 2f);
                    physics.Simulate(.02f);
                    check(Mathf.Abs(body.position.x) <= 3.55f && Mathf.Abs(body.position.y - 2f) <= .95f,
                        "Neutral blink roaming remains bounded");
                }
                check(fastest > 10f && waited, "Neutral movement alternates near-instant bursts with pauses");
                check(player.HP == untouchedHealth && !monster.IsProvoked && projectiles().Length == 0,
                    "Walking through a roaming wisp is harmless and never provokes it");
                body.position = new Vector2(0f, 2f);
                playerBody.position = new Vector2(4f, 1f);
                typeof(MON_WillOWisp).GetMethod("StopBlink", Hidden).Invoke(monster, null);
                Physics2D.SyncTransforms();
                monster.TakeDamage(0f, true, false);
                check(!monster.IsProvoked, "Zero damage cannot provoke a wisp");
                // Skip only the global hit-stop effect in this synchronous simulation.
                Time.timeScale = 0f;
                monster.TakeDamage(1f, true, false);
                Time.timeScale = 1f;
                check(monster.IsProvoked, "Damage overload used by player skills provokes retaliation");
                ability.Invoke(monster, null);
                animator.Update(.02f); animator.Update(.02f);
                check(monster.IsAttacking && animator.GetCurrentAnimatorStateInfo(0).IsName("Will-o Attack"), "AI enters attack through isAttacking");
                animator.Update(.55f);
                check(projectiles().Length == 0, "Inflated-cheek windup emits no early projectile");
                animator.Update(.15f);
                var volley = projectiles();
                check(volley.Length == 3, "Open-mouth animation event emits exactly three flames");
                monster.EmitWispVolley();
                check(projectiles().Length == 3, "Repeated emission event cannot duplicate volley");
                check(volley.Select(p => p.Destination).Distinct().Count() == 3, "All three destinations differ");
                check(volley.All(p => p.transform.position.x > actor.transform.position.x), "Right-facing volley starts at the mouth");
                var step = typeof(WillOWispProjectile).GetMethod("FixedUpdate", Hidden);
                playerCollider.enabled = false;
                for (int frame = 0; frame < 25; frame++)
                {
                    foreach (var projectile in volley) step.Invoke(projectile, null);
                    physics.Simulate(.02f);
                }
                check(volley.All(p => p.GetComponent<Rigidbody2D>().position.y > 2.5f), "All flights bow above the direct path");
                for (int frame = 0; frame < 40; frame++)
                {
                    foreach (var projectile in volley) if (!projectile.HasImpacted) step.Invoke(projectile, null);
                    physics.Simulate(.02f);
                }
                check(volley.All(p => p.HasImpacted && Vector2.Distance(p.GetComponent<Rigidbody2D>().position, p.Destination) < .02f),
                    "Unobstructed flames reach their own endpoints and begin impact animation");
                foreach (var projectile in volley) UnityEngine.Object.DestroyImmediate(projectile.gameObject);
                animator.Update(.7f); animator.Update(.02f);
                check(!monster.IsAttacking && animator.GetCurrentAnimatorStateInfo(0).IsName("Will-o Idle"), "Attack end event returns to idle");

                var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WillOWispCombatSetup.ProjectilePath).GetComponent<WillOWispProjectile>();
                playerCollider.enabled = true;
                playerBody.position = new Vector2(4f, 1f);
                var shot = UnityEngine.Object.Instantiate(projectilePrefab, new Vector3(0f, 1f, 0f), Quaternion.identity);
                SceneManager.MoveGameObjectToScene(shot.gameObject, scene);
                shot.Launch(playerBody.position, .5f, .1f, 10f, monster);
                float health = player.HP;
                Physics2D.SyncTransforms();
                for (int frame = 0; frame < 30 && !shot.HasImpacted; frame++) { step.Invoke(shot, null); physics.Simulate(.02f); }
                check(shot.HasImpacted && player.HP < health, "Swept projectile collision damages the player");
                float afterHit = player.HP;
                shot.SendMessage("OnTriggerEnter2D", playerCollider);
                check(Mathf.Approximately(player.HP, afterHit), "An impacted flame cannot deal repeated damage");
                UnityEngine.Object.DestroyImmediate(shot.gameObject);

                playerCollider.enabled = false;
                var wall = new GameObject("Thin terrain validation wall", typeof(BoxCollider2D));
                SceneManager.MoveGameObjectToScene(wall, scene);
                wall.transform.position = new Vector3(2f, 1f, 0f);
                wall.GetComponent<BoxCollider2D>().size = new Vector2(.03f, 4f);
                shot = UnityEngine.Object.Instantiate(projectilePrefab, new Vector3(0f, 1f, 0f), Quaternion.identity);
                SceneManager.MoveGameObjectToScene(shot.gameObject, scene);
                shot.Launch(new Vector2(4f, 1f), .1f, .1f, 10f, monster);
                Physics2D.SyncTransforms();
                for (int frame = 0; frame < 8 && !shot.HasImpacted; frame++) { step.Invoke(shot, null); physics.Simulate(.02f); }
                check(shot.HasImpacted && shot.GetComponent<Rigidbody2D>().position.x < 2.1f, "Fast flames collide with thin terrain");
                UnityEngine.Object.DestroyImmediate(shot.gameObject);
                UnityEngine.Object.DestroyImmediate(wall);

                playerBody.position = new Vector2(-4f, 1f);
                playerCollider.enabled = true;
                Physics2D.SyncTransforms();
                typeof(MON_WillOWisp).GetField("nextAttackAt", Hidden).SetValue(monster, float.NegativeInfinity);
                ability.Invoke(monster, null);
                monster.EmitWispVolley();
                volley = projectiles();
                check(volley.Length == 3 && volley.All(p => p.transform.position.x < actor.transform.position.x), "Left-facing volley mirrors the mouth and keeps three destinations");
                foreach (var projectile in volley) UnityEngine.Object.DestroyImmediate(projectile.gameObject);
                monster.FinishWispAttack();
                ability.Invoke(monster, null);
                check(!monster.IsAttacking, "Cooldown prevents immediate repeat attacks");
                typeof(MON_WillOWisp).GetField("nextAttackAt", Hidden).SetValue(monster, float.NegativeInfinity);
                ability.Invoke(monster, null);
                monster.ApplyKnockback(1f);
                check(!monster.IsAttacking && Mathf.Abs(actor.GetComponent<Rigidbody2D>().linearVelocity.y) < .001f, "Knockback cancels windup without vertical drift");
                monster.EmitWispVolley();
                check(projectiles().Length == 0, "Interrupted windup cannot emit flames");
                animator.SetTrigger("isDead"); animator.Update(.02f); animator.Update(.02f);
                check(animator.GetCurrentAnimatorStateInfo(0).IsName("Will-o Dead"), "isDead enters death from any state");
                monster.HP = 0f;
                monster.EmitWispVolley();
                check(projectiles().Length == 0, "Defeated wisp cannot emit a volley");
                Directory.CreateDirectory("Artifacts/WillOWispCombat");
                File.WriteAllLines("Artifacts/WillOWispCombat/Validation.txt", report.Concat(new[] { "COMPLETE " + report.Count + " checks" }));
                return string.Join("\n", report);
            }
            finally
            {
                foreach (var projectile in UnityEngine.Object.FindObjectsByType<WillOWispProjectile>(FindObjectsSortMode.None))
                    if (projectile.gameObject.scene == scene) UnityEngine.Object.DestroyImmediate(projectile.gameObject);
                UnityEngine.Object.DestroyImmediate(root);
                SceneManager.SetActiveScene(previous);
                SceneManager.UnloadSceneAsync(scene);
                Time.timeScale = previousTimeScale;
            }
        }
    }
}
