using System;
using System.Collections;
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
    public static class MonkWaveImpactAudioValidation
    {
        private const string Folder = "Artifacts/MonkWaveImpactAudioValidation";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Hidden).GetValue(target);

        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/Validation.txt", "Native wind-impact audio validation\n");
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
            player.isInvincible = true;
            player.GetComponent<PlayerInputReader>().enabled = false;
            player.GetComponent<PlayerMovement2D>().enabled = false;
            player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            player.GetComponent<CapsuleCollider2D>().enabled = false;
            var prefab = Field<MonkEnergyWave>(player.GetComponent<MonkSkillCaster>(), "wavePrefab");
            var clip = Field<AudioClip>(prefab, "hitSound");
            Check(clip != null && clip.name == "Monk_EnergyWave_WindHit", "energy-wave prefab references the edited wind impact");
            clip.LoadAudioData();
            var setter = player.GetComponent<PlayerLocationSetter>();
            setter.TeleportToAndRetarget(new Vector3(10000f, 10000f), Quaternion.identity);
            Physics2D.SyncTransforms();
            var feedback = UnityEngine.Object.FindFirstObjectByType<CombatImpactFeedback>();
            if (feedback != null) Field<AudioSource>(feedback, "impactAudio").Stop();
            var empty = MonkEnergyWave.Launch(prefab, player, player.GetComponent<MonkEnergy>(), 1f, 4f, 0.001f, LayerMask.GetMask("Enemy"));
            Check(UnityEngine.Object.FindFirstObjectByType<CombatImpactFeedback>() == null
                || !Field<AudioSource>(UnityEngine.Object.FindFirstObjectByType<CombatImpactFeedback>(), "impactAudio").isPlaying,
                "a wave that misses all enemies does not play an impact sound");
            UnityEngine.Object.Destroy(empty.gameObject);
            yield return null;

            var enemy = UnityEngine.Object.FindObjectsByType<MON_Goblin>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            enemy.gameObject.SetActive(true);
            Freeze(enemy);
            var second = UnityEngine.Object.Instantiate(enemy, enemy.transform.position + Vector3.right * 1.2f, enemy.transform.rotation);
            Freeze(second);
            try
            {
                Vector2 offset = Field<Vector2>(prefab, "spawnOffset");
                Vector3 center = enemy.GetComponent<CapsuleCollider2D>().bounds.center;
                setter.TeleportToAndRetarget(center - new Vector3(offset.x + 0.5f, offset.y), Quaternion.identity);
                Physics2D.SyncTransforms();
                feedback = UnityEngine.Object.FindFirstObjectByType<CombatImpactFeedback>();
                int previousIndex = feedback != null ? Field<int>(feedback, "lastSoundIndex") : -1;
                float firstHealth = enemy.HP, secondHealth = second.HP;
                var wave = MonkEnergyWave.Launch(prefab, player, player.GetComponent<MonkEnergy>(), 1f, 4f, 0.001f, LayerMask.GetMask("Enemy"));
                feedback = UnityEngine.Object.FindFirstObjectByType<CombatImpactFeedback>();
                Check(enemy.HP < firstHealth && second.HP < secondHealth, "actual wave overlap damages both targets");
                var audio = Field<AudioSource>(feedback, "impactAudio");
                Check(audio.isPlaying && Field<int>(feedback, "lastSoundIndex") == previousIndex,
                    "wind impact plays without selecting or layering the generic punch sound");
                Check(feedback.GetComponents<AudioSource>().Length == 1, "simultaneous hits share the persistent impact source");
                UnityEngine.Object.Destroy(wave.gameObject);
                yield return null;
                Check(audio.isPlaying, "wind audio tail survives destruction of the short-lived wave visual");
                yield return new WaitForSecondsRealtime(0.45f);
                Check(!audio.isPlaying, "wind impact ends on its own without looping");

                typeof(CombatImpactFeedback).GetField("nextSoundTime", Hidden).SetValue(feedback, float.NegativeInfinity);
                previousIndex = Field<int>(feedback, "lastSoundIndex");
                enemy.TakeDamage(0.1f, false, false);
                Check(audio.isPlaying && Field<int>(feedback, "lastSoundIndex") != previousIndex,
                    "ordinary attacks retain their existing generic impact sound");
                yield return new WaitForSecondsRealtime(0.45f);
                audio.Stop();
                var zeroDamage = MonkEnergyWave.Launch(prefab, player, player.GetComponent<MonkEnergy>(), 1f, 4f, 0f, LayerMask.GetMask("Enemy"));
                Check(!audio.isPlaying, "a zero-damage wave never plays the hit sound");
                UnityEngine.Object.Destroy(zeroDamage.gameObject);
                File.AppendAllText(Folder + "/Validation.txt", "COMPLETE\n");
            }
            finally { if (second != null) UnityEngine.Object.Destroy(second.gameObject); }
        }

        private static void Freeze(EnemyController enemy)
        {
            enemy.enabled = false;
            enemy.HP = enemy.maxHP;
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            foreach (var collider in enemy.GetComponents<Collider2D>()) collider.enabled = true;
        }

        private static void Check(bool ok, string message)
        {
            File.AppendAllText(Folder + "/Validation.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
