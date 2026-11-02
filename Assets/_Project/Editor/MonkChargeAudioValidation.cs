using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.Skill;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class MonkChargeAudioValidation
    {
        private const string Folder = "Artifacts/MonkChargeAudioValidation";
        private const string Key = "EasternFantasy.MonkChargeAudioValidation";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Hidden).GetValue(target);

        public static void Prepare()
        {
            Directory.CreateDirectory(Folder + "/Saves");
            SessionState.SetString(Key + ".Directory", SessionState.GetString("EasternFantasy.SaveTestDirectory", ""));
            SessionState.SetInt(Key + ".Class", PlayerPrefs.GetInt("Character.SelectedClass", 0));
            SessionState.SetBool(Key + ".Background", Application.runInBackground);
            string original = SaveGameService.PathFor(CharacterClassId.Monk);
            string contents = File.Exists(original) ? File.ReadAllText(original) : "";
            SessionState.SetString(Key + ".OriginalPath", original);
            SessionState.SetString(Key + ".OriginalContents", contents);
            var data = string.IsNullOrEmpty(contents) ? new SaveGameData() : JsonUtility.FromJson<SaveGameData>(contents);
            data.characterClass = (int)CharacterClassId.Monk;
            data.scene = "PrologueVilage";
            data.position = new Vector3(0f, 1f, 0f);
            data.health = 100f;
            data.inventoryTutorial = data.statsTutorial = data.mapTutorial = data.shopTutorial = true;
            data.dungeonTutorial = data.upgradeTutorial = data.companionTutorial = data.companionGuide = true;
            data.playedDialogues = Resources.Load<SaveAssetCatalog>("SaveAssetCatalog").entries
                .Where(e => e.asset is DialogueSequence).Select(e => e.id).ToArray();
            SessionState.SetString("EasternFantasy.SaveTestDirectory", Path.GetFullPath(Folder + "/Saves"));
            PlayerPrefs.SetInt("Character.SelectedClass", (int)CharacterClassId.Monk);
            File.WriteAllText(SaveGameService.PathFor(CharacterClassId.Monk), JsonUtility.ToJson(data, true));
            File.WriteAllText(Folder + "/Validation.txt", "Native Unity charge audio validation\n");
        }

        public static void Cleanup()
        {
            string path = SessionState.GetString(Key + ".OriginalPath", "");
            string contents = File.Exists(path) ? File.ReadAllText(path) : "";
            Check(contents == SessionState.GetString(Key + ".OriginalContents", ""), "original Monk save unchanged");
            SessionState.SetString("EasternFantasy.SaveTestDirectory", SessionState.GetString(Key + ".Directory", ""));
            PlayerPrefs.SetInt("Character.SelectedClass", SessionState.GetInt(Key + ".Class", 0));
            PlayerPrefs.Save();
            Application.runInBackground = SessionState.GetBool(Key + ".Background", false);
        }

        public static void Run()
        {
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(Validate());
        }

        private static IEnumerator Validate()
        {
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            SaveGameService.BeginAdminSession();
            player.isInvincible = true;
            var input = player.GetComponent<PlayerInputReader>();
            input.enabled = true;
            typeof(PlayerInputReader).GetField("hasFocus", Hidden).SetValue(input, true);
            player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            var caster = player.GetComponent<MonkSkillCaster>();
            var energy = player.GetComponent<MonkEnergy>();
            var animation = player.GetComponent<PlayerIdleAnimation>();
            var skill = Field<SkillDefinition>(caster, "energyWaveSkill");
            var levels = Field<Dictionary<SkillDefinition, int>>(player.GetComponent<PlayerSkillSystem>(), "skillLevels");
            levels[skill] = 1;
            var cooldowns = Field<Dictionary<SkillDefinition, float>>(caster, "cooldownEnds");
            var pauseOwner = new object();
            try
            {
                animation.FinishSkillAnimation();
                energy.Gain(energy.Maximum);
                Check(caster.TryCast(skill), "real energy-wave skill cast accepted");
                var audio = Field<AudioSource>(caster, "waveChargeAudio");
                Check(audio != null && audio.isPlaying && !audio.loop, "charge sound starts once with the charging animation");
                yield return new WaitForSecondsRealtime(0.15f);
                GameTimeController.SetPaused(pauseOwner, true);
                yield return null;
                int pausedSample = audio.timeSamples;
                yield return new WaitForSecondsRealtime(0.15f);
                Check(Field<bool>(caster, "waveChargePaused") && Mathf.Abs(audio.timeSamples - pausedSample) < 256,
                    "game pause freezes charge audio instead of letting it drift ahead");
                GameTimeController.SetPaused(pauseOwner, false);
                yield return null;
                Check(audio.isPlaying, "charge audio resumes with the animation");
                float deadline = Time.realtimeSinceStartup + 4f;
                while (Field<bool>(caster, "wavePending") && Time.realtimeSinceStartup < deadline) yield return null;
                Check(Field<MonkEnergyWave>(caster, "activeWave") != null, "actual animation release frame launches the wave");
                yield return new WaitForSecondsRealtime(0.12f);
                Check(!audio.isPlaying, "charge sound fades out at release, before the recovery motion ends");
                while (Field<Coroutine>(caster, "waveRoutine") != null && Time.realtimeSinceStartup < deadline) yield return null;
                Check(Field<Coroutine>(caster, "waveRoutine") == null, "normal cast fully recovers");
                yield return new WaitForSecondsRealtime(0.1f);

                cooldowns.Clear();
                animation.FinishSkillAnimation();
                typeof(PlayerInputReader).GetField("hasFocus", Hidden).SetValue(input, true);
                energy.Gain(energy.Maximum);
                Check(caster.TryCast(skill), "second cast starts for cancellation verification");
                yield return new WaitForSecondsRealtime(0.12f);
                animation.FinishSkillAnimation();
                yield return new WaitForSecondsRealtime(0.15f);
                Check(!audio.isPlaying && !Field<bool>(caster, "wavePending") && Field<MonkEnergyWave>(caster, "activeWave") == null,
                    "cancelled animation stops charge audio and never launches a wave");

                cooldowns.Clear();
                animation.FinishSkillAnimation();
                typeof(PlayerInputReader).GetField("hasFocus", Hidden).SetValue(input, true);
                energy.Gain(energy.Maximum);
                Check(caster.TryCast(skill), "third cast starts for disable verification");
                yield return new WaitForSecondsRealtime(0.1f);
                caster.enabled = false;
                Check(!audio.isPlaying, "disabling the caster immediately stops charging audio");
                caster.enabled = true;
                energy.ResetEnergy();
                Check(!caster.TryCast(skill) && !audio.isPlaying, "insufficient energy rejects the cast without playing audio");
                File.AppendAllText(Folder + "/Validation.txt", "COMPLETE\n");
            }
            finally { GameTimeController.SetPaused(pauseOwner, false); }
        }

        private static void Check(bool ok, string message)
        {
            File.AppendAllText(Folder + "/Validation.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
