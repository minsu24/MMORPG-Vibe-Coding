using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    // Runs real prefab casts and damage callbacks in an isolated Play Mode physics scene.
    public static class SkillBalanceValidation
    {
        private const string Report = "Artifacts/SkillBalance/Validation.txt";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static Scene scene;
        private static int checks;

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Hidden).SetValue(target, value);
        private static object Get(object target, string field) => target.GetType().GetField(field, Hidden).GetValue(target);
        private static void Check(bool condition, string message)
        {
            File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + message + "\n");
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
        private static void Near(float actual, float expected, string message) =>
            Check(Mathf.Abs(actual - expected) < .015f, message + " (actual " + actual + ", expected " + expected + ")");
        private static SkillDefinition Skill(string path) => AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/_Project/Data/Skills/" + path + ".asset");

        public static string Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
            Directory.CreateDirectory("Artifacts/SkillBalance");
            File.WriteAllText(Report, "Native Unity Play Mode skill balance validation\n");
            checks = 0;
            EasternFantasy.Saving.SaveGameService.BeginAdminSession();
            scene = SceneManager.CreateScene("Skill Balance Validation", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            float previousScale = Time.timeScale;
            var errors = new List<string>();
            Application.LogCallback captureErrors = (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    errors.Add(message);
            };
            Application.logMessageReceived += captureErrors;
            try
            {
                ValidateLevels();
                ValidateTeleport();
                ValidateShield();
                ValidateSword();
                Check(errors.Count == 0, "No Unity runtime errors during validation: " + string.Join("; ", errors));
                File.AppendAllText(Report, "COMPLETE " + checks + " checks passed\n");
                return checks + " native Play Mode checks passed. " + Report;
            }
            catch (Exception error)
            {
                File.AppendAllText(Report, "ERROR " + error + "\n");
                throw;
            }
            finally
            {
                Application.logMessageReceived -= captureErrors;
                SceneManager.UnloadSceneAsync(scene);
                Time.timeScale = previousScale;
            }
        }

        private static GameObject Player(string prefabName)
        {
            Time.timeScale = 1f;
            var holder = new GameObject("Inactive balance fixture");
            holder.SetActive(false);
            SceneManager.MoveGameObjectToScene(holder, scene);
            var player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Player/" + prefabName + ".prefab"), holder.transform);
            UnityEngine.Object.DestroyImmediate(player.GetComponent<PlayerClassRuntime>());
            UnityEngine.Object.DestroyImmediate(player.GetComponent<PlayerAdminController>());
            UnityEngine.Object.DestroyImmediate(player.GetComponent<PlayerLocationSetter>());
            foreach (var behaviour in player.GetComponents<MonoBehaviour>())
                if (!(behaviour is PlayerEntity || behaviour is PlayerMovement2D || behaviour is PlayerIdleAnimation
                    || behaviour is PlayerProgression || behaviour is PlayerSkillSystem || behaviour is PlayerActiveSkillCaster
                    || behaviour is SwordswomanSkillCaster || behaviour is SwordswomanDrawingAttack
                    || behaviour is MonkSkillCaster || behaviour is MonkEnergy || behaviour is PlayerProjectileShooter))
                    behaviour.enabled = false;
            holder.SetActive(true);
            player.transform.position = Vector3.zero;
            var entity = player.GetComponent<PlayerEntity>();
            entity.Attack_Power = 100f;
            Set(entity, "criticalChance", 0f);
            Set(player.GetComponent<PlayerIdleAnimation>(), "inputReader", null);
            player.GetComponent<Rigidbody2D>().gravityScale = 0f;
            return player;
        }

        private static void Learn(GameObject player, SkillDefinition skill, int level)
        {
            var levels = (Dictionary<SkillDefinition, int>)Get(player.GetComponent<PlayerSkillSystem>(), "skillLevels");
            levels[skill] = level;
        }
        private static void Remove(GameObject player) => UnityEngine.Object.DestroyImmediate(player.transform.parent.gameObject);
        private static EnemyController Target(Vector2 position)
        {
            var target = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Spider.prefab"));
            SceneManager.MoveGameObjectToScene(target, scene);
            target.transform.position = position;
            var enemy = target.GetComponent<EnemyController>();
            enemy.enabled = false;
            typeof(EnemyController).GetField("_maxHP", Hidden).SetValue(enemy, 10000f);
            typeof(EnemyController).GetField("hitFlashMaterial", Hidden).SetValue(enemy, null);
            enemy.HP = enemy.maxHP;
            enemy.damageTextPrefab = null;
            target.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            Physics2D.SyncTransforms();
            scene.GetPhysicsScene2D().Simulate(.0001f);
            return enemy;
        }

        private static void ValidateLevels()
        {
            var table = AssetDatabase.LoadAssetAtPath<PlayerLevelTable>("Assets/_Project/Data/Player/DefaultLevelTable.asset");
            Near(table.GetRequiredExperience(14), 5500, "Original 14 to 15 experience preserved");
            for (int level = 15; level < 30; level++) Check(table.GetRequiredExperience(level) > 0, "Experience exists at level " + level);
            foreach (string name in new[] { "Player", "MonkPlayer", "SwordswomanPlayer" })
            {
                var player = Player(name);
                try
                {
                    var progression = player.GetComponent<PlayerProgression>();
                    Set(progression, "currentLevel", 15);
                    Set(progression, "currentExperience", 0);
                    int rewards = 0;
                    progression.LeveledUp += _ => rewards++;
                    progression.AddExperience(table.GetRequiredExperience(15));
                    Check(progression.CurrentLevel == 16 && !progression.IsMaximumLevel, name + " progresses beyond 15");
                    progression.AddExperience(1000000);
                    Check(progression.CurrentLevel == 30 && progression.IsMaximumLevel && progression.CurrentExperience == 0, name + " stops at level 30");
                    Check(rewards == 15, name + " emits one level-up reward per gained level");
                }
                finally { Remove(player); }
            }
        }

        private static void ValidateTeleport()
        {
            var skill = Skill("Dosa/FirstTeleport");
            Check(skill.MaximumLevel == 5, "Teleport maximum level 5");
            for (int level = 1; level <= 5; level++)
            {
                var player = Player("Player");
                try
                {
                    var caster = player.GetComponent<PlayerActiveSkillCaster>();
                    Check(!caster.TryCast(skill), "Unlearned teleport rejected");
                    Learn(player, skill, level);
                    var entity = player.GetComponent<PlayerEntity>();
                    float mana = entity.MP;
                    Check(caster.TryCast(skill), "Teleport casts at level " + level);
                    Near(Mathf.Abs(player.transform.position.x), 4f + .5f * (level - 1), "Teleport actual distance level " + level);
                    Near(caster.GetCooldownRemaining(skill), 6f - .5f * (level - 1), "Teleport actual cooldown level " + level);
                    Near(mana - entity.MP, 5f, "Teleport MP cost");
                    Check(!caster.TryCast(skill), "Teleport cooldown blocks repeat cast");
                    Check(skill.GetLevelEffectDescription(level).Contains("순간이동 거리"), "Teleport tooltip includes distance");
                }
                finally { Remove(player); }
            }
        }

        private static void ValidateShield()
        {
            var skill = Skill("Dosa/FirstTalismanShield");
            Check(skill.MaximumLevel == 9, "Shield maximum level 9");
            for (int level = 1; level <= 9; level++)
            {
                var player = Player("Player");
                try
                {
                    Learn(player, skill, level);
                    var entity = player.GetComponent<PlayerEntity>();
                    float hp = entity.HP, mana = entity.MP;
                    var caster = player.GetComponent<PlayerActiveSkillCaster>();
                    Check(caster.TryCast(skill), "Shield casts at level " + level);
                    float reduction = .3f + .05f * (level - 1);
                    Near(entity.ActiveDamageReduction, reduction, "Shield applied reduction level " + level);
                    entity.TakeDamage(50f);
                    Near(hp - entity.HP, 50f * 100f / (100f + entity.Defense) * (1f - reduction), "Shield actual damage mitigation level " + level);
                    Near(mana - entity.MP, 30f, "Shield MP cost");
                    Near(caster.GetCooldownRemaining(skill), 10f, "Shield cooldown");
                    Check(skill.GetLevelEffectDescription(level).Contains((reduction * 100f).ToString("0") + "%"), "Shield tooltip matches reduction");
                }
                finally { Remove(player); }
            }
        }

        private static void ValidateSword()
        {
            foreach (string name in new[] { "SwordDoubleSlash", "SwordDrawing", "SwordDash" })
            {
                var skill = Skill("Swordswoman/" + name);
                int max = name == "SwordDoubleSlash" ? 10 : name == "SwordDrawing" ? 9 : 5;
                Check(skill.MaximumLevel == max, name + " maximum level " + max);
                for (int level = 1; level <= max; level++)
                {
                    var player = Player("SwordswomanPlayer");
                    EnemyController target = null;
                    try
                    {
                        Learn(player, skill, level);
                        var caster = player.GetComponent<SwordswomanSkillCaster>();
                        var entity = player.GetComponent<PlayerEntity>();
                        float mana = entity.MP;
                        var animation = player.GetComponent<PlayerIdleAnimation>();
                        float direction = animation.FacingDirectionX;
                        if (name != "SwordDash") target = Target(new Vector2((name == "SwordDrawing" ? 4f : 1.5f) * direction, .15f));
                        Time.timeScale = 1f;
                        Check(caster.TryCast(skill), name + " actual cast level " + level);
                        Near(caster.GetCooldownRemaining(skill), name == "SwordDash" ? 5f - .5f * (level - 1) : name == "SwordDrawing" ? 20f : 8f, name + " actual cooldown level " + level);
                        if (name == "SwordDoubleSlash")
                        {
                            caster.OnDoubleSlashHit(1);
                            Time.timeScale = 1f;
                            caster.OnDoubleSlashHit(2);
                            Near(10000f - target.HP, 2f * (120f + 6f * (level - 1)), "Double slash two actual hits level " + level);
                            float hp = target.HP;
                            caster.OnDoubleSlashHit(2);
                            Near(target.HP, hp, "Duplicate slash event cannot hit twice");
                        }
                        else if (name == "SwordDrawing")
                        {
                            player.GetComponent<SwordswomanDrawingAttack>().Release();
                            Near(10000f - target.HP, 500f + 25f * (level - 1), "Drawing actual path damage level " + level);
                            Near(Mathf.Abs(player.transform.position.x), 8f, "Drawing distance remains 8");
                        }
                        else
                        {
                            var movement = player.GetComponent<PlayerMovement2D>();
                            Check(entity.IsDamageImmune && entity.IsCrowdControlImmune, "Dash retains damage and CC immunity");
                            for (int step = 0; step < 20 && movement.IsDashing; step++)
                            {
                                typeof(PlayerMovement2D).GetMethod("UpdateDash", Hidden).Invoke(movement, null);
                                if (movement.IsDashing) scene.GetPhysicsScene2D().Simulate(Time.fixedDeltaTime);
                            }
                            Near(Mathf.Abs(player.transform.position.x), 4f, "Dash distance remains 4");
                        }
                        Near(mana - entity.MP, name == "SwordDash" ? 0f : name == "SwordDrawing" ? 25f : 15f, name + " MP cost");
                        Check(!string.IsNullOrEmpty(skill.GetLevelEffectDescription(level)), name + " level tooltip exists");
                    }
                    finally
                    {
                        if (target != null) UnityEngine.Object.DestroyImmediate(target.gameObject);
                        Remove(player);
                        Time.timeScale = 1f;
                    }
                }
            }
        }
    }
}
