using System;
using System.Collections;
using System.IO;
using System.Reflection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.Skill;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    public static class SaveLoadLevelEffectValidation
    {
        private const string Report = "Artifacts/SaveLoadLevelEffect/Validation.txt";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Hidden).GetValue(target);
        private static void Check(bool valid, string message)
        {
            File.AppendAllText(Report, (valid ? "PASS " : "FAIL ") + message + Environment.NewLine);
            if (!valid) throw new InvalidOperationException(message);
        }
        public static void Run()
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("EasternFantasy.SaveTestDirectory", "")))
                throw new InvalidOperationException("Use Play mode and isolated save storage.");
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report, "Native Unity Play mode: save load and real level-up effects" + Environment.NewLine);
            SaveGameService.Instance.StartCoroutine(Validate());
        }
        private static IEnumerator Validate()
        {
            GameTimeController.SetPaused("SaveLoadLevelEffectValidation", true);
            while (SaveGameService.IsRestoring) yield return null;
            // Also exercise a fresh restore when the Editor starts Play in the bootstrap scene.
            typeof(SaveGameService).GetField("ready", Hidden).SetValue(SaveGameService.Instance, false);
            SaveGameService.PrepareSelection();
            yield return SceneManager.LoadSceneAsync("MovementPrototype");
            while (SaveGameService.IsRestoring) yield return null;
            var player = Object.FindFirstObjectByType<PlayerEntity>();
            var progression = player.GetComponent<PlayerProgression>();
            var skills = player.GetComponent<PlayerSkillSystem>();
            var effect = LevelUpEffectUI.Instance;
            Check(effect != null, "level-up effect is present and bound");
            Check(SceneManager.GetActiveScene().name == "PrologueVilage" && progression.CurrentLevel == 4
                && progression.CurrentExperience == 13 && skills.SkillPoints == 7, "actual save load restores scene, level, EXP and unspent points");
            var group = Get<CanvasGroup>(effect, "effectGroup");
            float maximumAlpha = group.alpha;
            int maximumPending = Get<int>(effect, "pendingEffects");
            float deadline = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < deadline)
            {
                maximumAlpha = Mathf.Max(maximumAlpha, group.alpha);
                maximumPending = Mathf.Max(maximumPending, Get<int>(effect, "pendingEffects"));
                yield return null;
            }
            Check(maximumAlpha == 0f && maximumPending == 0 && Get<Coroutine>(effect, "effectQueueRoutine") == null,
                "entering the saved map never displays or queues a level-up effect");
            int stateChanges = 0, realLevelUps = 0;
            Action<int> stateChanged = level => stateChanges++;
            Action<int> leveledUp = level => realLevelUps++;
            progression.LevelChanged += stateChanged;
            progression.LeveledUp += leveledUp;
            try
            {
                typeof(PlayerProgression).GetMethod("RestoreSave", Hidden).Invoke(progression,
                    new object[] { new SaveGameData { level = 4, experience = 13 } });
                Check(stateChanges == 1 && realLevelUps == 0 && skills.SkillPoints == 7
                    && Get<int>(effect, "pendingEffects") == 0, "state restoration refreshes listeners without rewards or effects even outside the load guard");
                progression.AddExperience(progression.RequiredExperience - progression.CurrentExperience);
                Check(progression.CurrentLevel == 5 && realLevelUps == 1 && skills.SkillPoints == 8,
                    "real EXP level-up still grants exactly one skill point");
                deadline = Time.realtimeSinceStartup + .8f;
                while (group.alpha == 0f && Time.realtimeSinceStartup < deadline) yield return null;
                Check(group.alpha > 0f, "real EXP level-up still plays the visible effect");
                while (Get<Coroutine>(effect, "effectQueueRoutine") != null) yield return null;

                var dialogue = ScriptableObject.CreateInstance<DialogueSequence>();
                var serialized = new SerializedObject(dialogue);
                serialized.FindProperty("lines").arraySize = 1;
                serialized.FindProperty("lines").GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue = "Level-up effect regression validation";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Check(DialogueManager.Instance.StartDialogue(dialogue), "dialogue starts for deferred-effect regression check");
                progression.AddExperience(progression.RequiredExperience - progression.CurrentExperience);
                yield return new WaitForSecondsRealtime(.2f);
                Check(group.alpha == 0f && Get<int>(effect, "pendingEffects") == 1,
                    "genuine level-up during dialogue still waits in the effect queue");
                DialogueManager.Instance.EndDialogue();
                deadline = Time.realtimeSinceStartup + .8f;
                while (group.alpha == 0f && Time.realtimeSinceStartup < deadline) yield return null;
                Check(group.alpha > 0f && skills.SkillPoints == 9 && realLevelUps == 2,
                    "deferred effect plays after dialogue and rewards stay correct");
                while (Get<Coroutine>(effect, "effectQueueRoutine") != null) yield return null;
                Object.Destroy(dialogue);

                typeof(PlayerProgression).GetMethod("RestoreSave", Hidden).Invoke(progression,
                    new object[] { new SaveGameData { level = 1 } });
                yield return null;
                Check(progression.CurrentLevel == 1 && realLevelUps == 2 && skills.SkillPoints == 9
                    && group.alpha == 0f && Get<int>(effect, "pendingEffects") == 0,
                    "restoring level 1 also remains silent and grants no points");
            }
            finally
            {
                progression.LevelChanged -= stateChanged;
                progression.LeveledUp -= leveledUp;
                GameTimeController.SetPaused("SaveLoadLevelEffectValidation", false);
            }
            File.AppendAllText(Report, "All checks passed." + Environment.NewLine);
        }
    }
}
