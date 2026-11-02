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
    public static class GhostScareAttackValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly MethodInfo Ability = typeof(MON_Ghost).GetMethod("TryBeginScareAttack", Hidden);
        private static readonly MethodInfo Fixed = typeof(EnemyController).GetMethod("FixedUpdate", Hidden);
        private static readonly List<string> checks = new List<string>();
        private static GameObject fixture;

        public static string Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run in Play mode at character selection.");
            checks.Clear();
            Directory.CreateDirectory("Artifacts/GhostScareAttack");
            var previous = Physics2D.simulationMode;
            var scene = SceneManager.CreateScene("Ghost Scare Validation");
            fixture = new GameObject("Ghost Scare Fixtures");
            SceneManager.MoveGameObjectToScene(fixture, scene);
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                var floor = new GameObject("Floor", typeof(BoxCollider2D));
                floor.transform.SetParent(fixture.transform);
                floor.transform.position = new Vector3(0, -1.92f);
                floor.GetComponent<BoxCollider2D>().size = new Vector2(40, 1);
                var player = Player(4f);
                var ghost = Ghost(player);
                Check(ghost.ContactDamage == 5f, "Original contact damage remains 5");
                Ability.Invoke(ghost, null);
                Check(ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.Flying, "Detection starts flight");
                Check(ghost.ContactDamage == 0f, "Flight prevents duplicate contact damage");
                float before = player.HP;
                int hits = 0; player.Damaged += _ => hits++;
                bool scaring = false, effect = false, frameSeven = false;
                for (int i = 0; i < 220; i++)
                {
                    Step(ghost);
                    scaring |= ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.Scaring;
                    if (hits > 0 && !frameSeven)
                    {
                        frameSeven = ghost.GetComponent<SpriteRenderer>().sprite.name == "Ghost_AttackFrame7";
                        effect = typeof(MON_Ghost).GetField("scareEffect", Hidden).GetValue(ghost) != null;
                    }
                    if (!scaring) Check(player.HP == before, "Flight has no early damage");
                    if (scaring && ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.None) break;
                }
                Check(scaring && ghost.transform.position.x > 2f, "Ghost flies toward player then scares");
                Check(hits == 1 && frameSeven && effect, "Frame 7 emits effect and exactly one damage event");
                int attackSounds = 0;
                foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                    if (source.gameObject.scene == ghost.gameObject.scene && source.clip != null && source.clip.name == "GhostAttack")
                    {
                        attackSounds++;
                        Check(!source.loop && !source.ignoreListenerPause && Mathf.Approximately(source.volume, .3f), "Ghost sound plays once at 30 percent and respects loading mute");
                    }
                Check(attackSounds == 1, "Scare emits exactly one attack sound");
                float expected = Mathf.Max(1f, 5f * 100f / (100f + Mathf.Max(0f, player.Defense)));
                Check(Mathf.Abs(before - player.HP - expected) < .01f, "Attack uses original contact damage and player defenses");
                Check(Mathf.Approximately(ghost.GetComponent<Rigidbody2D>().gravityScale, 1f), "Attack restores gravity");
                Check(!Physics2D.GetIgnoreCollision(ghost.GetComponent<Collider2D>(), player.GetComponent<Collider2D>()), "Attack restores player collision");
                Ability.Invoke(ghost, null);
                Check(ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.None, "Cooldown prevents immediate repeat");
                Clear(player, ghost);

                player = Player(1.3f); ghost = Ghost(player); Ability.Invoke(ghost, null); Step(ghost);
                Check(ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.Scaring, "Close player starts scare pose");
                before = player.HP;
                player.GetComponent<Rigidbody2D>().position = new Vector2(-4f, 0);
                Physics2D.SyncTransforms();
                for (int i = 0; i < 40; i++) Step(ghost);
                Check(player.HP == before, "Moving away before frame 7 avoids damage");
                Clear(player, ghost);

                player = Player(-4f); ghost = Ghost(player); Ability.Invoke(ghost, null); Step(ghost);
                Check(ghost.GetComponent<Rigidbody2D>().linearVelocity.x < 0 && ghost.transform.localScale.x > 0,
                    "Left target reverses flight and facing");
                ghost.ApplyKnockback(1f);
                Check(ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.None && ghost.isKnockback,
                    "Knockback interrupts attack");
                Check(ghost.GetComponent<Rigidbody2D>().gravityScale == 1f &&
                    !Physics2D.GetIgnoreCollision(ghost.GetComponent<Collider2D>(), player.GetComponent<Collider2D>()),
                    "Interruption restores gravity and collision");
                Clear(player, ghost);

                player = Player(1.3f); ghost = Ghost(player); Ability.Invoke(ghost, null); Step(ghost);
                ghost.HP = 0; ghost.EmitScareAttack();
                Check(typeof(MON_Ghost).GetField("scareEffect", Hidden).GetValue(ghost) == null,
                    "Dead ghost cannot emit a scare");
                ghost.enabled = false;
                Check(ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.None && ghost.GetComponent<Rigidbody2D>().gravityScale == 1f,
                    "Disable cancels attack and restores physics");
                Clear(player, ghost);

                player = Player(12f); ghost = Ghost(player); Ability.Invoke(ghost, null);
                Check(ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.None, "Outside detection cannot attack");
                player.GetComponent<Rigidbody2D>().position = new Vector2(2, 0); player.HP = 0;
                Physics2D.SyncTransforms(); Ability.Invoke(ghost, null);
                Check(ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.None, "Dead player cannot trigger attack");
                player.HP = player.maxHP; ghost.BeginSpawnContactGrace(2f); Ability.Invoke(ghost, null);
                Check(ghost.CurrentAttackPhase == MON_Ghost.ScareAttackPhase.None, "Spawn grace prevents attacks");
                Clear(player, ghost);
                File.WriteAllLines("Artifacts/GhostScareAttack/Validation.txt", checks);
                return "Ghost scare attack validation passed: " + checks.Count;
            }
            finally
            {
                Physics2D.simulationMode = previous;
                Object.DestroyImmediate(fixture);
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        private static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException(message);
            // Repeated flight steps need only one line in the report.
            if (!checks.Contains("PASS: " + message)) checks.Add("PASS: " + message);
        }

        private static PlayerEntity Player(float x)
        {
            var root = new GameObject("Scare Test Player"); root.SetActive(false);
            root.transform.SetParent(fixture.transform); root.transform.position = new Vector3(x, 0);
            root.tag = "Player"; root.layer = LayerMask.NameToLayer("Player");
            var entity = root.AddComponent<PlayerEntity>();
            var movement = root.GetComponent<PlayerMovement2D>();
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            var data = new SerializedObject(movement);
            data.FindProperty("settings").objectReferenceValue = new SerializedObject(template.GetComponent<PlayerMovement2D>()).FindProperty("settings").objectReferenceValue;
            data.ApplyModifiedPropertiesWithoutUndo(); movement.enabled = false;
            root.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var collider = root.GetComponent<CapsuleCollider2D>(); collider.size = new Vector2(.55f, 1.2f); collider.offset = Vector2.zero;
            root.SetActive(true); entity.HP = entity.maxHP;
            return entity;
        }

        private static MON_Ghost Ghost(PlayerEntity player)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Ghost.prefab");
            var root = Object.Instantiate(prefab, fixture.transform);
            root.transform.position = Vector3.zero;
            var ghost = root.GetComponent<MON_Ghost>();
            ghost.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            typeof(EnemyController).GetField("playerEntity", Hidden).SetValue(ghost, player);
            typeof(EnemyController).GetField("player", Hidden).SetValue(ghost, player.gameObject);
            Physics2D.SyncTransforms();
            return ghost;
        }

        private static void Step(MON_Ghost ghost)
        {
            Fixed.Invoke(ghost, null);
            Physics2D.Simulate(Time.fixedDeltaTime);
            ghost.GetComponent<Animator>().Update(Time.fixedDeltaTime);
        }

        private static void Clear(PlayerEntity player, MON_Ghost ghost)
        {
            Object.DestroyImmediate(ghost.gameObject); Object.DestroyImmediate(player.gameObject);
        }
    }
}
