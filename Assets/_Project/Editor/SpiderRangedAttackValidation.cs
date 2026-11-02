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
    public static class SpiderRangedAttackValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly MethodInfo Ability = typeof(MON_Spider).GetMethod("TryBeginSpitAttack", Hidden);
        private static readonly MethodInfo Fixed = typeof(EnemyController).GetMethod("FixedUpdate", Hidden);
        private static readonly MethodInfo ProjectileFixed = typeof(SpiderSpitProjectile).GetMethod("FixedUpdate", Hidden);
        private static readonly List<string> checks = new List<string>();
        private static GameObject fixture;

        public static string Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run at character selection in Play mode.");
            checks.Clear();
            var previous = Physics2D.simulationMode;
            var scene = SceneManager.CreateScene("Spider Ranged Validation");
            fixture = new GameObject("Spider Fixtures"); SceneManager.MoveGameObjectToScene(fixture, scene);
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                var floor = new GameObject("Floor", typeof(BoxCollider2D)); floor.transform.SetParent(fixture.transform);
                floor.transform.position = new Vector3(0, -.94f); floor.GetComponent<BoxCollider2D>().size = new Vector2(70, 1);
                var player = Player(7f); var spider = Spider(player);
                Ability.Invoke(spider, null); Fixed.Invoke(spider, null);
                Check(!spider.IsSpitting && spider.GetComponent<Rigidbody2D>().linearVelocity.x > 0, "Detected target outside attack range is chased");
                player.transform.position = new Vector2(4, 0); Physics2D.SyncTransforms();
                Ability.Invoke(spider, null);
                Check(spider.IsSpitting, "Entering separate attack range starts attack" +
                    (spider.IsSpitting ? "" : " (target=" + player.transform.position + ", spider=" + spider.GetComponent<Rigidbody2D>().position + ", timeScale=" + Time.timeScale + ")"));
                float before = player.HP; int hits = 0; player.Damaged += _ => hits++;
                bool released = false, impact = false;
                for (int i = 0; i < 75; i++)
                {
                    Step(spider);
                    var projectiles = Projectiles();
                    if (!released && projectiles.Length > 0)
                    {
                        released = true;
                        Check(spider.GetComponent<SpriteRenderer>().sprite.name == "Spider_AttackFrame5", "Projectile is released on open-mouth frame 5");
                        Check(SoundCount("SpiderAttack") == 1, "Release sound plays at projectile launch");
                        spider.EmitSpiderProjectile();
                        Check(Projectiles().Length == 1, "Repeated event cannot emit duplicate projectile");
                    }
                    foreach (var p in projectiles) impact |= p.HasImpacted;
                    if (i < 19) Check(projectiles.Length == 0, "No projectile before the open-mouth frame");
                }
                Check(released && impact && hits == 1, "Projectile travels to player and hits exactly once");
                float expected = Mathf.Max(1f, 10f * 100f / (100f + Mathf.Max(0f, player.Defense)));
                Check(Mathf.Abs(before - player.HP - expected) < .01f, "Ranged damage matches original 10 damage and defense handling");
                Check(SoundCount("SpiderProjectileImpact") == 1, "Player collision plays impact sound once");
                Check(Mathf.Abs(spider.GetComponent<Rigidbody2D>().linearVelocity.x) < .001f && !spider.IsSpitting,
                    "Spider waits stationary during attack cooldown");
                player.transform.position = new Vector2(8, 0); Physics2D.SyncTransforms(); Step(spider); Step(spider);
                Check(spider.GetComponent<Rigidbody2D>().linearVelocity.x > 0, "Leaving attack range resumes chasing");
                ClearActors(player, spider);

                player = Player(-4); spider = Spider(player); Ability.Invoke(spider, null);
                for (int i = 0; i < 21; i++) Step(spider);
                var left = Projectiles();
                Check(left.Length == 1 && left[0].transform.right.x < 0 && spider.transform.localScale.x > 0, "Left attack faces and fires left");
                float x = left[0].transform.position.x;
                player.transform.position = new Vector2(-4, 3); Physics2D.SyncTransforms();
                before = player.HP;
                for (int i = 0; i < 50; i++) Step(spider);
                Check(left[0].transform.position.x < x - 2 && player.HP == before, "Projectile has committed aim and can be dodged");
                ClearActors(player, spider);

                player = Player(4); spider = Spider(player);
                var wall = new GameObject("Wall", typeof(BoxCollider2D)); wall.transform.SetParent(fixture.transform);
                wall.transform.position = new Vector3(2, 0); wall.GetComponent<BoxCollider2D>().size = new Vector2(.08f, 3);
                Physics2D.SyncTransforms(); Ability.Invoke(spider, null); before = player.HP;
                int impactBefore = SoundCount("SpiderProjectileImpact");
                for (int i = 0; i < 55; i++) Step(spider);
                Check(player.HP == before && SoundCount("SpiderProjectileImpact") == impactBefore + 1, "Thin wall intercepts projectile and plays impact sound");
                Object.DestroyImmediate(wall); ClearActors(player, spider);

                player = Player(4); spider = Spider(player); Ability.Invoke(spider, null);
                spider.ApplyKnockback(-1); spider.EmitSpiderProjectile();
                Check(!spider.IsSpitting && Projectiles().Length == 0, "Knockback cancels attack before release");
                ClearActors(player, spider);
                player = Player(4); spider = Spider(player); Ability.Invoke(spider, null);
                spider.HP = 0; spider.EmitSpiderProjectile();
                Check(Projectiles().Length == 0, "Dead spider cannot release projectile");
                spider.enabled = false; Check(!spider.IsSpitting, "Disable clears attack state");
                ClearActors(player, spider);

                player = Player(25); spider = Spider(player); Ability.Invoke(spider, null);
                Check(!spider.IsSpitting, "Outside detection range does not attack");
                player.transform.position = new Vector2(4, 0); Physics2D.SyncTransforms();
                spider.BeginSpawnContactGrace(2); Ability.Invoke(spider, null);
                Check(!spider.IsSpitting, "Spawn protection prevents attacks");
                ClearActors(player, spider);
                Directory.CreateDirectory("Artifacts/SpiderRangedAttack");
                File.WriteAllLines("Artifacts/SpiderRangedAttack/Validation.txt", checks);
                return "Spider ranged attack validation passed: " + checks.Count;
            }
            finally
            {
                Physics2D.simulationMode = previous;
                foreach (var p in Projectiles()) Object.DestroyImmediate(p.gameObject);
                Object.DestroyImmediate(fixture); SceneManager.UnloadSceneAsync(scene);
            }
        }

        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            if (!checks.Contains("PASS: " + message)) checks.Add("PASS: " + message);
        }

        private static SpiderSpitProjectile[] Projectiles()
        {
            var result = new List<SpiderSpitProjectile>();
            foreach (var p in Object.FindObjectsByType<SpiderSpitProjectile>(FindObjectsSortMode.None))
                if (p.gameObject.scene == fixture.scene) result.Add(p);
            return result.ToArray();
        }

        private static int SoundCount(string name)
        {
            int count = 0;
            foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (source.gameObject.scene == fixture.scene && source.clip != null && source.clip.name == name) count++;
            return count;
        }

        private static PlayerEntity Player(float x)
        {
            var root = new GameObject("Spit Test Player"); root.SetActive(false);
            root.transform.SetParent(fixture.transform); root.transform.position = new Vector3(x, 0);
            root.tag = "Player"; root.layer = LayerMask.NameToLayer("Player");
            var entity = root.AddComponent<PlayerEntity>(); var movement = root.GetComponent<PlayerMovement2D>();
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            var data = new SerializedObject(movement);
            data.FindProperty("settings").objectReferenceValue = new SerializedObject(template.GetComponent<PlayerMovement2D>()).FindProperty("settings").objectReferenceValue;
            data.ApplyModifiedPropertiesWithoutUndo(); movement.enabled = false;
            root.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var collider = root.GetComponent<CapsuleCollider2D>(); collider.size = new Vector2(.55f, 1.2f); collider.offset = Vector2.zero;
            root.SetActive(true); entity.HP = entity.maxHP; return entity;
        }

        private static MON_Spider Spider(PlayerEntity player)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Spider.prefab");
            var root = Object.Instantiate(prefab, fixture.transform); root.transform.position = Vector3.zero;
            var spider = root.GetComponent<MON_Spider>();
            root.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            typeof(EnemyController).GetField("playerEntity", Hidden).SetValue(spider, player);
            typeof(EnemyController).GetField("player", Hidden).SetValue(spider, player.gameObject);
            Physics2D.SyncTransforms(); return spider;
        }

        private static void Step(MON_Spider spider)
        {
            Fixed.Invoke(spider, null);
            foreach (var p in Projectiles()) ProjectileFixed.Invoke(p, null);
            Physics2D.Simulate(Time.fixedDeltaTime);
            spider.GetComponent<Animator>().Update(Time.fixedDeltaTime);
        }

        private static void ClearActors(PlayerEntity player, MON_Spider spider)
        {
            foreach (var p in Projectiles()) Object.DestroyImmediate(p.gameObject);
            Object.DestroyImmediate(spider.gameObject); Object.DestroyImmediate(player.gameObject);
        }
    }
}
