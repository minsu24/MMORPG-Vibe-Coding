using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.Skill;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class StatsSkillInteractiveValidation
    {
        private const string Folder = "Artifacts/StatsSkillInteractive";
        private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static readonly List<string> results = new List<string>();
        private static StatsSkillTutorialUI tutorial;
        private static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Hidden).Invoke(owner, args);
        private static void RestoreGuide(Type type, SaveGameData data) => type.GetMethod("RestoreSave", Hidden).Invoke(null, new object[] { data });
        private static PetCompanionTutorialUI Guide => (PetCompanionTutorialUI)typeof(StatsSkillTutorialUI).GetField("guide", Hidden).GetValue(tutorial);
        private static RectTransform[] Targets => (RectTransform[])typeof(StatsSkillTutorialUI).GetField("targets", Hidden).GetValue(tutorial);
        private static void Check(bool ok, string message)
        {
            results.Add((ok ? "PASS " : "FAIL ") + message);
            File.WriteAllLines(Folder + "/Validation.txt", results);
            if (!ok) throw new InvalidOperationException(message);
        }

        public static void Run()
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("EasternFantasy.SaveTestDirectory", "")))
                throw new InvalidOperationException("Run in Play mode with isolated save storage.");
            tutorial = UnityEngine.Object.FindFirstObjectByType<StatsSkillTutorialUI>();
            tutorial.StartCoroutine(Routine());
        }

        private static IEnumerator Routine()
        {
            Directory.CreateDirectory(Folder); results.Clear();
            while (SaveGameService.IsRestoring) yield return null;
            SaveGameService.BeginAdminSession();
            RestoreGuide(typeof(MapTutorialUI), new SaveGameData { mapTutorial = true });
            RestoreGuide(typeof(InventoryEquipmentTutorialUI), new SaveGameData { inventoryTutorial = true });
            RestoreGuide(typeof(StatsSkillTutorialUI), new SaveGameData { statsTutorial = true });
            DialogueManager.Instance?.EndDialogue();
            SceneLoadingScreen.Cancel();
            var input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
            input.GetComponent<PlayerAdminController>().enabled = false;
            var system = input.GetComponent<PlayerSkillSystem>();
            Call(input.GetComponent<PlayerProgression>(), "RestoreSave", new SaveGameData { level = 2 });
            Call(system, "RestoreSave", new SaveGameData { skillPoints = 1 });
            bool inputEnabled = input.enabled;
            var stats = PlayerStatsWindowUI.Instance;
            var window = SkillWindowUI.Instance;
            RestoreGuide(typeof(StatsSkillTutorialUI), new SaveGameData());
            Call(tutorial, "Begin");
            try
            {
                Check(tutorial.CurrentStep == 0 && !stats.IsOpen && !window.IsOpen, "Guide waits for C with both windows closed");
                tutorial.Advance(); Call(tutorial, "HandleShortcut", true);
                Check(tutorial.CurrentStep == 0 && !window.IsOpen, "Next and K cannot bypass the C action");
                Call(tutorial, "HandleShortcut", false);
                Check(tutorial.CurrentStep == 1 && stats.IsOpen && !window.IsOpen, "C action opens actual stats window");
                tutorial.Advance();
                Check(tutorial.CurrentStep == 1 && !window.IsOpen, "Next cannot bypass the K action");
                Call(tutorial, "HandleShortcut", true);
                Check(tutorial.CurrentStep == 2 && window.IsOpen && !stats.IsOpen, "K action opens actual skill window");
                Check(Targets[2].GetComponent<SkillSlotUI>().Definition.ActivationType == SkillActivationType.Passive, "Passive explanation highlights a real passive skill");
                tutorial.Advance();
                Check(tutorial.CurrentStep == 3 && Targets[3].GetComponent<SkillSlotUI>().Definition.ActivationType == SkillActivationType.Active, "Active explanation highlights a real active skill");
                tutorial.Advance(); tutorial.Advance();
                Check(tutorial.CurrentStep == 4 && system.SkillPoints == 1, "Learning step cannot be skipped and has not spent or granted points");
                yield return new WaitForSecondsRealtime(1.4f);
                // The fixture skips the opening story; dismiss its delayed portrait before testing UI raycasts.
                DialogueManager.Instance?.EndDialogue();
                Call(Guide, "RefreshLayout"); Canvas.ForceUpdateCanvases();
                Check(window.TutorialTarget.rect.width * window.TutorialTarget.lossyScale.x / window.GetComponent<Canvas>().scaleFactor > 1100,
                    "Actual skill scroll uses most of the screen above the compact guide");
                var definition = system.AvailableSkills.First(s => s.ActivationType == SkillActivationType.Passive && system.CanLearn(s));
                var slot = window.TutorialSlots.First(s => s != null && s.Definition == definition);
                var button = (Button)typeof(SkillSlotUI).GetField("learnButton", Hidden).GetValue(slot);
                var rect = slot.transform.Find("LearnPointBadge") as RectTransform;
                Check(rect != null && rect.gameObject.activeInHierarchy, "Learnable skill displays a visible plus badge");
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                    button = PointerEventData.InputButton.Left
                };
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
                    "Guide allows pointer input to an actual learn button" + (hits.Count == 0 ? " (no hits)" : " (hit: " + hits[0].gameObject.name + ")"));
                int before = system.GetSkillLevel(definition);
                ExecuteEvents.Execute<IPointerClickHandler>(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Check(system.SkillPoints == 0 && system.GetSkillLevel(definition) == before + 1, "Real plus button spends one point and increases chosen skill level");
                Check(!rect.gameObject.activeSelf, "Plus badge disappears when no points remain");
                var partial = new SaveGameData();
                typeof(StatsSkillTutorialUI).GetMethod("CaptureSave", Hidden).Invoke(null, new object[] { partial });
                Call(system, "CaptureSave", partial);
                partial = JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(partial));
                Check(partial.statsSkillPracticed && !partial.statsTutorial && partial.skillPoints == 0, "Partial progress persists the genuine point spend before guide completion");
                Call(tutorial, "EvaluateProgress");
                Check(tutorial.CurrentStep == 5 && Targets[5] == QuickSlotBarUI.Instance.TutorialTarget, "Learning advances to the actual quick-slot bar explanation");
                Check(!system.CanPlaceInQuickSlot(definition), "The first passive learned is not assignable as an active shortcut");
                tutorial.Advance();
                Check(StatsSkillTutorialUI.HasFinishedForCurrentClass && !tutorial.IsShowing && system.SkillPoints == 0
                    && system.GetSkillLevel(definition) == before + 1, "Completion preserves the learned skill and spent point");
                Check(input.enabled == inputEnabled && !window.IsOpen && !stats.IsOpen, "Input and closed windows are restored");
                RestoreGuide(typeof(StatsSkillTutorialUI), partial); Call(system, "RestoreSave", partial); Call(tutorial, "Begin");
                Call(tutorial, "HandleShortcut", false); Call(tutorial, "HandleShortcut", true);
                tutorial.Advance(); tutorial.Advance(); Call(tutorial, "EvaluateProgress");
                Check(tutorial.CurrentStep == 5 && system.SkillPoints == 0, "Reloading an unfinished lesson does not require spending a second point");
                tutorial.Advance();
                RestoreGuide(typeof(StatsSkillTutorialUI), new SaveGameData { statsTutorial = true });
                Call(tutorial, "Update");
                Check(!tutorial.IsShowing, "Previously completed saves remain unlocked without replay");
                Call(system, "RestoreSave", new SaveGameData { skillPoints = 1 });
                RestoreGuide(typeof(StatsSkillTutorialUI), new SaveGameData()); Call(tutorial, "Begin"); tutorial.Finish();
                var cancelled = new SaveGameData(); typeof(StatsSkillTutorialUI).GetMethod("CaptureSave", Hidden).Invoke(null, new object[] { cancelled });
                Check(!cancelled.statsTutorial && !cancelled.statsSkillPracticed && system.SkillPoints == 1 && input.enabled == inputEnabled,
                    "Cancelling before learning does not spend points or falsely complete practice");
                Call(tutorial, "Update");
                Check(tutorial.IsShowing, "Interrupted lesson can restart without repeating the boss dialogue");
                tutorial.Finish(); RestoreGuide(typeof(StatsSkillTutorialUI), new SaveGameData { statsTutorial = true });
                results.Add("COMPLETE"); File.WriteAllLines(Folder + "/Validation.txt", results);
            }
            finally { if (tutorial.IsShowing) tutorial.Finish(); }
        }
    }
}
