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
    public static class MonkPunchImpactAudioValidation
    {
        private const string Folder = "Artifacts/MonkPunchImpactAudioValidation";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Hidden).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);

        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/Validation.txt", "Native Monk punch impact audio validation\n");
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(Validate());
        }

        private static IEnumerator Validate()
        {
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            SaveGameService.BeginAdminSession();
            yield return SceneManager.LoadSceneAsync("MovementPrototype");
            yield return null;
            yield return null;
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            var animation = player.GetComponent<PlayerIdleAnimation>();
            var movement = player.GetComponent<PlayerMovement2D>();
            var basic = player.GetComponent<MonkBasicAttack>();
            var caster = player.GetComponent<MonkSkillCaster>();
            var setter = player.GetComponent<PlayerLocationSetter>();
            player.isInvincible = true;
            player.GetComponent<PlayerInputReader>().enabled = false;
            Set(animation, "inputReader", null);
            movement.body.constraints = RigidbodyConstraints2D.FreezeAll;
            player.GetComponent<CapsuleCollider2D>().enabled = false;
            Set(basic, "damageMultiplier", 0.001f);
            Check(Field<AudioClip>(basic, "hitSound").name == "Monk_BasicPunch_Hit", "basic attack references the edited hit16 clip");
            Check(Field<AudioClip>(caster, "punchHitSound").name == "Monk_StraightPunch_Hit", "straight punch references the edited hit10 clip");
            Check(Field<AudioClip>(player.GetComponent<PlayerBasicAttackAudio>(), "attackSound").name == "cloth1", "movement Foley remains cloth1");
            var skill = Field<SkillDefinition>(caster, "straightPunchSkill");
            Field<Dictionary<SkillDefinition, int>>(player.GetComponent<PlayerSkillSystem>(), "skillLevels")[skill] = 1;
            var enemy = UnityEngine.Object.FindObjectsByType<MON_Goblin>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            enemy.gameObject.SetActive(true);
            enemy.enabled = false;
            enemy.HP = enemy.maxHP;
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            foreach (var collider in enemy.GetComponents<Collider2D>()) collider.enabled = true;

            setter.TeleportToAndRetarget(new Vector3(10000f, 10000f, 0f), Quaternion.identity);
            Physics2D.SyncTransforms();
            var feedback = UnityEngine.Object.FindFirstObjectByType<CombatImpactFeedback>();
            if (feedback != null) Field<AudioSource>(feedback, "impactAudio").Stop();
            Check(basic.TryAttack(), "real basic attack starts on a miss");
            yield return WaitForRecovery(animation, basic, caster);
            feedback = UnityEngine.Object.FindFirstObjectByType<CombatImpactFeedback>();
            Check(feedback == null || !Field<AudioSource>(feedback, "impactAudio").isPlaying, "missed basic attack has no impact audio");

            movement.SetMoveInput(1f);
            animation.RefreshAttackFacing();
            movement.ClearInput();
            float direction = (float)typeof(MonkBasicAttack).GetMethod("GetAttackDirection", Hidden).Invoke(basic, null);
            Vector2 offset = Field<Vector2>(basic, "attackOffset");
            Vector3 center = enemy.GetComponent<CapsuleCollider2D>().bounds.center;
            setter.TeleportToAndRetarget(center - new Vector3(offset.x * direction, offset.y, 0f), Quaternion.identity);
            Physics2D.SyncTransforms();
            float health = enemy.HP;
            int previousIndex = feedback != null ? Field<int>(feedback, "lastSoundIndex") : -1;
            Check(basic.TryAttack(), "real basic attack starts against the target");
            float deadline = Time.realtimeSinceStartup + 3f;
            while (enemy.HP >= health && Time.realtimeSinceStartup < deadline) yield return null;
            feedback = UnityEngine.Object.FindFirstObjectByType<CombatImpactFeedback>();
            Check(enemy.HP < health && feedback != null && Field<AudioSource>(feedback, "impactAudio").isPlaying,
                "basic punch impact plays exactly when actual damage is dealt");
            Check(Field<int>(feedback, "lastSoundIndex") == previousIndex, "basic hit does not layer the old generic punch sound");
            yield return WaitForRecovery(animation, basic, caster);

            offset = Field<Vector2>(caster, "punchOffset");
            direction = (float)typeof(MonkSkillCaster).GetMethod("GetDirection", Hidden).Invoke(caster, null);
            setter.TeleportToAndRetarget(center - new Vector3(offset.x * direction, offset.y, 0f), Quaternion.identity);
            Physics2D.SyncTransforms();
            player.GetComponent<MonkEnergy>().Gain(1000f);
            health = enemy.HP;
            Check(caster.TryCast(skill), "real two-strike straight punch starts");
            Set(caster, "punchDamage", 0.2f);
            Set(caster, "punchCritical", false);
            for (int strike = 1; strike <= 2; strike++)
            {
                deadline = Time.realtimeSinceStartup + 3f;
                while (Field<int>(caster, "lastStrike") < strike && Time.realtimeSinceStartup < deadline) yield return null;
                Check(enemy.HP < health - (strike - 1) * 0.2f - 0.05f && Field<AudioSource>(feedback, "impactAudio").isPlaying,
                    "straight punch strike " + strike + " deals damage and plays its impact audio");
                Check(Field<int>(feedback, "lastSoundIndex") == previousIndex, "strike " + strike + " replaces the generic impact without overlap");
            }
            yield return WaitForRecovery(animation, basic, caster);
            var audio = Field<AudioSource>(feedback, "impactAudio");
            audio.Stop();
            setter.TeleportToAndRetarget(new Vector3(10000f, 10000f, 0f), Quaternion.identity);
            Physics2D.SyncTransforms();
            Field<Dictionary<SkillDefinition, float>>(caster, "cooldownEnds").Clear();
            player.GetComponent<MonkEnergy>().Gain(1000f);
            Check(caster.TryCast(skill), "straight punch starts on a miss");
            yield return WaitForRecovery(animation, basic, caster);
            Check(!audio.isPlaying, "both missed straight-punch strikes are silent on the impact channel");
            Set(feedback, "nextSoundTime", float.NegativeInfinity);
            enemy.TakeDamage(0.01f, false, false);
            Check(audio.isPlaying && Field<int>(feedback, "lastSoundIndex") != previousIndex, "ordinary enemy damage keeps the existing shared impact library");
            Check(Field<AudioClip>(Field<MonkEnergyWave>(caster, "wavePrefab"), "hitSound").name == "Monk_EnergyWave_WindHit", "energy-wave wind impact remains unchanged");
            File.AppendAllText(Folder + "/Validation.txt", "COMPLETE\n");
        }

        private static IEnumerator WaitForRecovery(PlayerIdleAnimation animation, MonkBasicAttack basic, MonkSkillCaster caster)
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while ((animation.IsAttacking || Field<Coroutine>(basic, "attackRoutine") != null || Field<Coroutine>(caster, "punchRoutine") != null)
                && Time.realtimeSinceStartup < deadline) yield return null;
            Check(!animation.IsAttacking, "attack recovery completed");
            yield return new WaitForSecondsRealtime(0.08f);
        }

        private static void Check(bool ok, string message)
        {
            File.AppendAllText(Folder + "/Validation.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
