using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EasternFantasy.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    public static class GoblinLeapAttackValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<string> checks = new List<string>();
        private static GameObject fixture;
        private static PhysicsMaterial2D frictionless;
        private static readonly MethodInfo Ability = typeof(MON_Goblin).GetMethod("TryBeginLeapAttack", Hidden);
        private static readonly MethodInfo Fixed = typeof(EnemyController).GetMethod("FixedUpdate", Hidden);

        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run in Play mode from character selection.");
            checks.Clear();
            Directory.CreateDirectory("Artifacts/GoblinLeapAttack");
            File.WriteAllText("Artifacts/GoblinLeapAttack/Validation.txt", "");
            var previous = Physics2D.simulationMode;
            var scene = SceneManager.CreateScene("Goblin Leap Validation");
            fixture = new GameObject("Goblin Leap Fixtures");
            SceneManager.MoveGameObjectToScene(fixture, scene);
            frictionless = new PhysicsMaterial2D("Goblin test friction") { friction = 0, bounciness = 0 };
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                Floor(30f);
                var player = Player(3.5f);
                var goblin = Goblin(player);
                Settle(goblin, 5);
                Check(goblin.ContactDamage == 5f, "Original contact damage remains 5 outside an attack");
                Ability.Invoke(goblin, null);
                Check(goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.Approach, "Player in range starts approach");
                float startX = goblin.transform.position.x, maxY = goblin.transform.position.y;
                float before = player.HP;
                bool windup = false, leap = false, recovery = false;
                int damageEvents = 0;
                player.Damaged += _ => damageEvents++;
                var swingSources = new HashSet<int>();
                var impactSources = new HashSet<int>();
                for (int i = 0; i < 130; i++)
                {
                    Step(goblin);
                    foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                    {
                        if (source.gameObject.scene != goblin.gameObject.scene || source.clip == null) continue;
                        if (source.clip.name == "GoblinSwing" && swingSources.Add(source.GetInstanceID()))
                            Check(goblin.GetComponent<SpriteRenderer>().sprite.name == "Goblin_AttackFrame6", "Swing sound starts on airborne swing frame 6");
                        if (source.clip.name == "GoblinAttackImpact" && impactSources.Add(source.GetInstanceID()))
                            Check(goblin.GetComponent<SpriteRenderer>().sprite.name == "Goblin_AttackFrame7", "Impact sound starts on ground impact frame 7");
                    }
                    if (goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.Windup) windup = true;
                    if (goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.Leaping) leap = true;
                    if (goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.Recovery) recovery = true;
                    maxY = Mathf.Max(maxY, goblin.transform.position.y);
                    if (recovery && goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.None) break;
                }
                Check(goblin.transform.position.x > startX + 1f, "Goblin approaches then travels toward player");
                Check(windup && leap && recovery, "Windup, leap and recovery phases all execute");
                Check(maxY > .35f, "Physical body leaves the ground during leap");
                Check(damageEvents == 1, "One attack causes exactly one damage event");
                Check(swingSources.Count == 1 && impactSources.Count == 1, "Each goblin attack sound plays exactly once");
                float expected = Mathf.Max(1f, 5f * 100f / (100f + Mathf.Max(0f, player.Defense)));
                Check(Mathf.Abs((before - player.HP) - expected) < .01f, "Leap damage matches existing contact damage and defenses");
                Check(!Physics2D.GetIgnoreCollision(goblin.GetComponent<Collider2D>(), player.GetComponent<Collider2D>()), "Player collision restored after attack");
                Check(Mathf.Approximately(goblin.GetComponent<Rigidbody2D>().gravityScale, 1f), "Gravity restored after landing");
                Ability.Invoke(goblin, null);
                Check(goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.None, "Cooldown prevents immediate repeat attack");
                ClearActors(player, goblin);

                player = Player(1.7f); goblin = Goblin(player); Settle(goblin, 5);
                Ability.Invoke(goblin, null);
                for (int i = 0; i < 20 && goblin.CurrentAttackPhase != MON_Goblin.LeapAttackPhase.Windup; i++) Step(goblin);
                Check(goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.Windup, "Close target enters windup");
                player.GetComponent<Rigidbody2D>().position = new Vector2(-3f, -.05f);
                Physics2D.SyncTransforms();
                before = player.HP;
                bool dodgedRecovery = false;
                for (int i = 0; i < 90; i++)
                {
                    Step(goblin);
                    dodgedRecovery |= goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.Recovery;
                    if (dodgedRecovery && goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.None) break;
                }
                Check(Mathf.Approximately(player.HP, before), "Dodging behind committed jump avoids damage");
                Check(goblin.transform.position.x > .8f, "Jump does not home toward a dodging player");
                ClearActors(player, goblin);

                player = Player(1.7f); goblin = Goblin(player); Settle(goblin, 5); Ability.Invoke(goblin, null);
                for (int i = 0; i < 40 && goblin.CurrentAttackPhase != MON_Goblin.LeapAttackPhase.Leaping; i++) Step(goblin);
                Check(goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.Leaping, "Interruption fixture starts in flight");
                goblin.ApplyKnockback(-1f);
                Check(goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.None && goblin.isKnockback, "Player knockback interrupts leap attack");
                Check(Mathf.Approximately(goblin.GetComponent<Rigidbody2D>().gravityScale, 1f), "Interrupt restores gravity");
                Check(!Physics2D.GetIgnoreCollision(goblin.GetComponent<Collider2D>(), player.GetComponent<Collider2D>()), "Interrupt restores collision pairs");
                ClearActors(player, goblin);

                player = Player(1.7f); goblin = Goblin(player); Settle(goblin, 5); Ability.Invoke(goblin, null);
                for (int i = 0; i < 40 && goblin.CurrentAttackPhase != MON_Goblin.LeapAttackPhase.Leaping; i++) Step(goblin);
                goblin.HP = 0;
                typeof(MON_Goblin).GetMethod("OnDefeated", Hidden).Invoke(goblin, null);
                Check(goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.None, "Defeat cancels in-flight attack");
                Check(!goblin.GetComponent<Rigidbody2D>().simulated, "Defeat retains existing corpse handling");
                Check(!Physics2D.GetIgnoreCollision(goblin.GetComponent<Collider2D>(), player.GetComponent<Collider2D>()), "Death clears ignored collision pairs");
                ClearActors(player, goblin);

                player = Player(12f); goblin = Goblin(player); Settle(goblin, 5); Ability.Invoke(goblin, null);
                Check(goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.None, "Out-of-range player does not start leap attack");
                player.HP = 0; player.GetComponent<Rigidbody2D>().position = new Vector2(1.7f, -.05f);
                Physics2D.SyncTransforms(); Ability.Invoke(goblin, null);
                Check(goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.None, "Dead player cannot trigger attack");
                ClearActors(player, goblin);

                player = Player(1.7f); goblin = Goblin(player); Settle(goblin, 5);
                goblin.BeginSpawnContactGrace(2f); Ability.Invoke(goblin, null);
                Check(goblin.CurrentAttackPhase == MON_Goblin.LeapAttackPhase.None, "Spawn contact grace also prevents leap attack");
                ClearActors(player, goblin);
                Directory.CreateDirectory("Artifacts/GoblinLeapAttack");
                File.WriteAllLines("Artifacts/GoblinLeapAttack/Validation.txt", checks);
                Debug.Log("Goblin leap validation passed: " + checks.Count);
            }
            finally
            {
                Physics2D.simulationMode = previous;
                Object.DestroyImmediate(fixture);
                Object.DestroyImmediate(frictionless);
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        private static void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
            checks.Add("PASS: " + message);
            File.AppendAllText("Artifacts/GoblinLeapAttack/Validation.txt", "PASS: " + message + "\n");
        }

        private static void Floor(float width)
        {
            var floor = new GameObject("Floor", typeof(BoxCollider2D));
            floor.transform.SetParent(fixture.transform);
            floor.transform.position = new Vector3(0, -1.15f);
            floor.GetComponent<BoxCollider2D>().size = new Vector2(width, 1);
            floor.GetComponent<BoxCollider2D>().sharedMaterial = frictionless;
        }

        private static PlayerEntity Player(float x)
        {
            var root = new GameObject("Leap Test Player"); root.SetActive(false);
            root.transform.SetParent(fixture.transform); root.transform.position = new Vector3(x, -.05f);
            root.tag = "Player"; root.layer = LayerMask.NameToLayer("Player");
            var entity = root.AddComponent<PlayerEntity>();
            var movement = root.GetComponent<PlayerMovement2D>();
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            var data = new SerializedObject(movement);
            data.FindProperty("settings").objectReferenceValue = new SerializedObject(template.GetComponent<PlayerMovement2D>()).FindProperty("settings").objectReferenceValue;
            data.ApplyModifiedPropertiesWithoutUndo(); movement.enabled = false;
            var body = root.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            var collider = root.GetComponent<CapsuleCollider2D>(); collider.size = new Vector2(.55f, 1.2f); collider.offset = Vector2.zero;
            root.SetActive(true); entity.HP = entity.maxHP;
            return entity;
        }

        private static MON_Goblin Goblin(PlayerEntity player)
        {
            var root = new GameObject("Leap Test Goblin"); root.SetActive(false);
            root.transform.SetParent(fixture.transform); root.layer = LayerMask.NameToLayer("Enemy");
            var body = root.AddComponent<Rigidbody2D>(); body.gravityScale = 1; body.freezeRotation = true;
            var collider = root.AddComponent<CapsuleCollider2D>(); collider.size = new Vector2(.59f, 1.03f); collider.offset = new Vector2(-.06f, -.1f); collider.sharedMaterial = frictionless;
            var renderer = root.AddComponent<SpriteRenderer>(); renderer.flipX = true;
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animations/TutoMonster/Goblin/Goblin.controller");
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var goblin = root.AddComponent<MON_Goblin>(); var data = new SerializedObject(goblin);
            data.FindProperty("_maxHP").floatValue = 100;
            data.FindProperty("_attackPower").floatValue = 5;
            data.FindProperty("_moveSpeed").floatValue = 3;
            data.FindProperty("_detectRange").floatValue = 6;
            data.FindProperty("_playerLayer").intValue = LayerMask.GetMask("Player");
            data.FindProperty("_rewardYeopjeon").intValue = 0;
            data.ApplyModifiedPropertiesWithoutUndo(); root.SetActive(true);
            typeof(EnemyController).GetField("playerEntity", Hidden).SetValue(goblin, player);
            typeof(EnemyController).GetField("player", Hidden).SetValue(goblin, player.gameObject);
            Physics2D.SyncTransforms();
            return goblin;
        }

        private static void Step(MON_Goblin goblin)
        {
            Fixed.Invoke(goblin, null);
            Physics2D.Simulate(Time.fixedDeltaTime);
            goblin.GetComponent<Animator>().Update(Time.fixedDeltaTime);
        }

        private static void Settle(MON_Goblin goblin, int steps)
        {
            Physics2D.SyncTransforms();
            for (int i = 0; i < steps; i++) Physics2D.Simulate(Time.fixedDeltaTime);
        }

        private static void ClearActors(PlayerEntity player, MON_Goblin goblin)
        {
            Object.DestroyImmediate(goblin.gameObject); Object.DestroyImmediate(player.gameObject);
        }
    }
}
