using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DG.Tweening;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class MapTutorialInteractiveValidation
    {
        private const string Folder = "Artifacts/MapTutorialInteractive";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly List<string> results = new List<string>();
        private static MapTutorialUI tutorial;
        private static MapWindowUI map;
        private static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Hidden).Invoke(owner, args);
        private static RectTransform Rect(string name) => (RectTransform)typeof(MapWindowUI).GetProperty(name, Hidden).GetValue(map);
        private static PetCompanionTutorialUI Guide => (PetCompanionTutorialUI)typeof(MapTutorialUI).GetField("guide", Hidden).GetValue(tutorial);

        public static void Run()
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("EasternFantasy.SaveTestDirectory", "")))
                throw new InvalidOperationException("Use Play mode with isolated save storage.");
            map = MapWindowUI.Instance;
            tutorial = map.GetComponent<MapTutorialUI>();
            tutorial.StartCoroutine(Routine());
        }

        private static void Check(bool ok, string message)
        {
            results.Add((ok ? "PASS " : "FAIL ") + message);
            File.WriteAllLines(Folder + "/Validation.txt", results);
            if (!ok) throw new InvalidOperationException(message);
        }

        private static void Restore(Type type, SaveGameData data) => type.GetMethod("RestoreSave", Hidden).Invoke(null, new object[] { data });

        private static void Click(RectTransform target)
        {
            Call(Guide, "RefreshLayout");
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == target.GetComponent<Button>(),
                "Highlighted map button receives pointer input: " + target.name);
            ExecuteEvents.Execute<IPointerClickHandler>(target.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Call(tutorial, "EvaluateProgress");
        }

        private static IEnumerator Routine()
        {
            Directory.CreateDirectory(Folder); results.Clear();
            while (SaveGameService.IsRestoring) yield return null;
            SaveGameService.BeginAdminSession();
            Restore(typeof(InventoryEquipmentTutorialUI), new SaveGameData { inventoryTutorial = true });
            Restore(typeof(StatsSkillTutorialUI), new SaveGameData { statsTutorial = true });
            DialogueManager.Instance?.EndDialogue();
            SceneLoadingScreen.Cancel();
            var input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
            input.GetComponent<PlayerAdminController>().enabled = false;
            Restore(typeof(MapTutorialUI), new SaveGameData());
            var panel = Rect("TutorialPanel");
            Vector3 position = panel.localPosition, scale = panel.localScale;
            Call(tutorial, "Begin", input);
            try
            {
                Check(tutorial.CurrentStep == 0 && !map.IsOpen, "Guide starts with map closed and requires M");
                Guide.Advance();
                Check(tutorial.CurrentStep == 0 && !map.IsOpen, "Next cannot bypass opening the map");
                Call(tutorial, "HandleOpenMap");
                Check(tutorial.CurrentStep == 1 && map.IsOpen && !map.IsShowingDetail, "M action opens the real overview");
                Guide.Advance();
                Check(tutorial.CurrentStep == 1 && !map.IsShowingDetail, "Next does not automatically select a region");
                yield return new WaitForSecondsRealtime(1.4f);
                Call(tutorial, "PlaceMap");
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4]; Rect("TutorialOverview").GetWorldCorners(corners);
                Check(corners[2].x - corners[0].x > Screen.width * .65f, "Overview spans most of the screen instead of a small side panel");
                ScreenCapture.CaptureScreenshot(Folder + "/Overview.png");
                Click(Rect("TutorialIsland"));
                Check(tutorial.CurrentStep == 2 && map.IsShowingDetail, "Clicking current island opens its actual detailed map");
                Guide.Advance();
                Check(tutorial.CurrentStep == 2 && map.IsShowingDetail, "Detail step requires the real back button");
                yield return new WaitForSecondsRealtime(1.3f);
                Call(tutorial, "PlaceMap");
                Canvas.ForceUpdateCanvases();
                ScreenCapture.CaptureScreenshot(Folder + "/Detail.png");
                Click(Rect("TutorialBackButton"));
                Check(tutorial.CurrentStep == 3 && !map.IsShowingDetail, "Clicking back returns to overview and advances");
                Guide.Advance();
                var saved = new SaveGameData();
                typeof(MapTutorialUI).GetMethod("CaptureSave", Hidden).Invoke(null, new object[] { saved });
                Check(saved.mapTutorial && !MapTutorialUI.IsRunning && !map.IsOpen, "Completion saves progress and closes map");
                Check(panel.localPosition == position && panel.localScale == scale, "Original map layout is restored after completion");
                Check(!input.IsInputBlocked, "Gameplay input is released after guide completion");
                Restore(typeof(MapTutorialUI), new SaveGameData { mapTutorial = true });
                Call(tutorial, "Update");
                Check(!MapTutorialUI.IsRunning, "Previously completed characters do not replay tutorial");
                Restore(typeof(MapTutorialUI), new SaveGameData());
                Call(tutorial, "Begin", input); Call(tutorial, "End");
                saved = new SaveGameData(); typeof(MapTutorialUI).GetMethod("CaptureSave", Hidden).Invoke(null, new object[] { saved });
                Check(!saved.mapTutorial && !input.IsInputBlocked, "Interrupted tutorial does not falsely complete or leave input blocked");
                Restore(typeof(MapTutorialUI), new SaveGameData { mapTutorial = true });
                results.Add("COMPLETE"); File.WriteAllLines(Folder + "/Validation.txt", results);
            }
            finally { if (MapTutorialUI.IsRunning) Call(tutorial, "End"); }
        }
    }
}
