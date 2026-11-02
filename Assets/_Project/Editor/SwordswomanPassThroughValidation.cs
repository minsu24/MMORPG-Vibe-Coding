using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SwordswomanPassThroughValidation
    {
        private const string Folder = "Artifacts/SwordswomanPassThrough";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Check(bool value, string message)
        {
            File.AppendAllText(Folder + "/Validation.txt", (value ? "PASS " : "FAIL ") + message + "\n");
            if (!value) throw new InvalidOperationException(message);
        }
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/Validation.txt", "Sword rhythm and pass-through regression checks\n");
            EditorApplication.LockReloadAssemblies();
            var scene = SceneManager.CreateScene("Sword Pass Through Validation", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            try { Validate(scene); }
            finally { SceneManager.UnloadSceneAsync(scene); EditorApplication.UnlockReloadAssemblies(); }
        }
        private static void Validate(Scene scene)
        {
            var holder = new GameObject("Inactive test holder"); holder.SetActive(false);
            SceneManager.MoveGameObjectToScene(holder, scene);
            var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/SwordswomanPlayer.prefab"), holder.transform);
            UnityEngine.Object.DestroyImmediate(actor.GetComponent<PlayerClassRuntime>());
            UnityEngine.Object.DestroyImmediate(actor.GetComponent<PlayerAdminController>());
            UnityEngine.Object.DestroyImmediate(actor.GetComponent<PlayerLocationSetter>());
            actor.transform.position = Vector3.zero; actor.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(actor, scene);
            UnityEngine.Object.Destroy(holder);
            actor.GetComponent<PlayerInputReader>().enabled = false;
            var animation = actor.GetComponent<PlayerIdleAnimation>();
            typeof(PlayerIdleAnimation).GetField("inputReader", Hidden).SetValue(animation, null);
            var movement = actor.GetComponent<PlayerMovement2D>();
            var body = actor.GetComponent<Rigidbody2D>(); body.gravityScale = 0;
            var collider = actor.GetComponent<CapsuleCollider2D>();
            var entity = actor.GetComponent<PlayerEntity>(); entity.isInvincible = true;
            var physics = scene.GetPhysicsScene2D();
            var enemy = SpawnEnemy(scene, new Vector2(2, 0));
            var enemyBody = enemy.GetComponent<Collider2D>();
            var child = new GameObject("Enemy body on Default layer"); child.layer = 0;
            child.transform.SetParent(enemy.transform, false);
            var extra = child.AddComponent<BoxCollider2D>(); extra.size = new Vector2(.7f, 4);
            var enemyWall = Box(scene, "Invisible monster boundary", new Vector2(.9f, 0), 8);
            Physics2D.SyncTransforms();
            physics.Simulate(.0001f);
            Check(actor.scene == scene && body.simulated, "test player is in isolated physics scene");
            Check((movement.PassThroughObstacleMask & (1 << 8)) == 0, "normal player collision matrix excludes EnemyWall");
            var hits = new RaycastHit2D[32];
            int oldCount = collider.Cast(Vector2.right, new ContactFilter2D { useLayerMask = true, layerMask = ~(1 << 7), useTriggers = false }, hits, 4);
            File.AppendAllText(Folder + "/Validation.txt", "INFO legacy cast obstacles: " + string.Join(", ", hits.Take(oldCount).Select(h => h.collider != null ? h.collider.name : "null")) + "\n");

            Check(movement.BeginDash(1, 4, .25f), "dash starts");
            SimulateDash(movement, physics);
            Check(Mathf.Abs(body.position.x - 4) < .06f, "dash travels four units through enemy, default-layer child, and EnemyWall");
            Reset(movement);
            Check(!Physics2D.GetIgnoreCollision(collider, extra), "enemy child collision restores once clear");

            var wall = Box(scene, "Solid terrain", new Vector2(3, 0), 0);
            Physics2D.SyncTransforms();
            Check(movement.BeginDash(1, 4, .25f), "dash with wall starts");
            SimulateDash(movement, physics);
            Check(body.position.x > 2.1f && collider.bounds.max.x <= wall.bounds.min.x + .04f, "dash passes enemy but stops before real terrain");
            UnityEngine.Object.DestroyImmediate(wall.gameObject);
            Reset(movement);
            Check(movement.BeginDash(1, 2, .25f), "dash ending inside enemy starts");
            SimulateDash(movement, physics);
            Check(Mathf.Abs(body.position.x - 2) < .06f && Physics2D.GetIgnoreCollision(collider, extra), "ending inside enemy does not push player back");
            Reset(movement);
            Check(!Physics2D.GetIgnoreCollision(collider, extra), "temporary overlap collision restores after leaving enemy");
            Physics2D.IgnoreCollision(collider, enemyBody, true);
            movement.BeginDash(1, 4, .25f); SimulateDash(movement, physics); Reset(movement);
            Check(Physics2D.GetIgnoreCollision(collider, enemyBody), "pre-existing ignored pair is preserved");
            Physics2D.IgnoreCollision(collider, enemyBody, false);

            var second = SpawnEnemy(scene, new Vector2(4, 0));
            var drawing = actor.GetComponent<SwordswomanDrawingAttack>();
            typeof(SwordswomanDrawingAttack).GetField("direction", Hidden).SetValue(drawing, 1f);
            Physics2D.SyncTransforms();
            Check(Mathf.Abs((float)typeof(SwordswomanDrawingAttack).GetMethod("GetUnblockedDistance", Hidden).Invoke(drawing, null) - 8) < .01f,
                "drawing path ignores enemy bodies and invisible monster boundary");
            typeof(SwordswomanDrawingAttack).GetField("damageMultiplier", Hidden).SetValue(drawing, .01f);
            float firstHealth = enemy.HP, secondHealth = second.HP;
            StartDrawing(actor, drawing); drawing.Release();
            Check(Mathf.Abs(body.position.x - 8) < .01f, "drawing passes through both enemies for full distance");
            Check(enemy.HP < firstHealth && second.HP < secondHealth, "drawing still damages every passed enemy");
            drawing.enabled = false; drawing.enabled = true;
            Reset(movement);
            wall = Box(scene, "Drawing solid terrain", new Vector2(3, 0), 0);
            Physics2D.SyncTransforms(); secondHealth = second.HP;
            StartDrawing(actor, drawing); drawing.Release();
            Check(body.position.x > 2.1f && collider.bounds.max.x <= wall.bounds.min.x + .04f, "drawing stops at real terrain after passing nearer enemy");
            Check(second.HP == secondHealth, "drawing does not hit enemy behind solid terrain");
            drawing.enabled = false; drawing.enabled = true;
            UnityEngine.Object.DestroyImmediate(wall.gameObject);
            Reset(movement);
            movement.PrepareEnemyPassThrough(); movement.enabled = false;
            Check(!Physics2D.GetIgnoreCollision(collider, extra), "disabling movement restores temporary collision pairs");
            movement.enabled = true;

            ValidateRhythm(actor);
            File.AppendAllText(Folder + "/Validation.txt", "COMPLETE\n");
        }
        private static void SimulateDash(PlayerMovement2D movement, PhysicsScene2D physics)
        {
            for (int i = 0; i < 20 && movement.IsDashing; i++)
            {
                typeof(PlayerMovement2D).GetMethod("FixedUpdate", Hidden).Invoke(movement, null);
                physics.Simulate(Time.fixedDeltaTime);
            }
        }
        private static void Reset(PlayerMovement2D movement)
        {
            movement.EndDash(); movement.SetMovementLocked(false);
            movement.TeleportTo(Vector3.zero, Quaternion.identity);
            movement.body.gravityScale = 0;
            typeof(PlayerMovement2D).GetMethod("RestoreEnemyPassThrough", Hidden).Invoke(movement, new object[] { false });
            Time.timeScale = 1;
        }
        private static BoxCollider2D Box(Scene scene, string name, Vector2 position, int layer)
        {
            var root = new GameObject(name); SceneManager.MoveGameObjectToScene(root, scene);
            root.layer = layer; root.transform.position = position;
            var collider = root.AddComponent<BoxCollider2D>(); collider.size = new Vector2(.2f, 8);
            return collider;
        }
        private static EnemyController SpawnEnemy(Scene scene, Vector2 position)
        {
            var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Spider.prefab"));
            SceneManager.MoveGameObjectToScene(go, scene); go.transform.position = position;
            var enemy = go.GetComponent<EnemyController>(); enemy.enabled = false;
            go.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            typeof(EnemyController).GetField("_reward_EXP", Hidden).SetValue(enemy, 0f);
            typeof(EnemyController).GetField("_rewardYeopjeon", Hidden).SetValue(enemy, 0);
            return enemy;
        }
        private static void StartDrawing(GameObject actor, SwordswomanDrawingAttack drawing)
        {
            var caster = actor.GetComponent<SwordswomanSkillCaster>();
            var skill = (SkillDefinition)typeof(SwordswomanSkillCaster).GetField("drawingSkill", Hidden).GetValue(caster);
            actor.GetComponent<PlayerEntity>().MP = actor.GetComponent<PlayerEntity>().maxMP;
            Check(drawing.TryStart(skill), "drawing starts");
            typeof(SwordswomanDrawingAttack).GetField("direction", Hidden).SetValue(drawing, 1f);
        }
        private static void ValidateRhythm(GameObject actor)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Character/SwordWoman/SwordDoubleSlash.anim");
            var events = AnimationUtility.GetAnimationEvents(clip);
            Check(events.Length == 4 && events.Select(e => e.functionName).SequenceEqual(new[] { "DoubleSlashSwing", "DoubleSlashHit", "DoubleSlashSwing", "DoubleSlashHit" }),
                "animation order is swish, hit, swish, hit");
            var caster = actor.GetComponent<SwordswomanSkillCaster>();
            foreach (var name in new[] { "firstSlashSound", "secondSlashSound" })
                Check(((AudioClip)typeof(SwordswomanSkillCaster).GetField(name, Hidden).GetValue(caster)).length < .065f, name + " is sixty milliseconds");
            var hit = (AudioClip)typeof(SwordswomanSkillCaster).GetField("doubleSlashHitSound", Hidden).GetValue(caster);
            Check(hit.length < .095f, "impact is ninety milliseconds");
            Check(events[0].time / 2 + .06f <= events[1].time / 2 && events[1].time / 2 + hit.length < events[2].time / 2,
                "each short swish and hit completes before the next sound");
        }
    }
}
