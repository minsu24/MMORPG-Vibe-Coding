using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Advancement;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Quest;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EasternFantasy.EditorTools
{
    public static class ToadHuntPlayValidation
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static IEnumerator routine;
        private static double deadline;
        private static readonly List<string> results = new List<string>();
        private static bool enteredPlayMode;
        private static bool stepping, savedPause;
        private static int lastFrame;
        private static double lastFrameAt;
        [InitializeOnLoadMethod]
        private static void ObservePlayMode()
        {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }
        private static void PlayModeChanged(PlayModeStateChange state) => enteredPlayMode = state == PlayModeStateChange.EnteredPlayMode;
        private static T Get<T>(object instance, string field) => (T)instance.GetType().GetField(field, Private).GetValue(instance);
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            results.Add("PASS: " + message);
            File.WriteAllText("Artifacts/ToadHunt/PlayValidation.txt", string.Join("\n", results) + "\nRUNNING");
        }

        [MenuItem("Eastern Fantasy/Quest/Run Toad Hunt Play Validation")]
        public static void Begin()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode before running this validation.");
            if (routine != null) return;
            savedPause = EditorApplication.isPaused;
            lastFrame = Time.frameCount; lastFrameAt = EditorApplication.timeSinceStartup;
            results.Clear(); deadline = EditorApplication.timeSinceStartup + 120;
            routine = Run(); EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            if (stepping) return;
            try
            {
                if (!Application.isPlaying || EditorApplication.timeSinceStartup > deadline) throw new InvalidOperationException("Play validation interrupted or timed out.");
                // Background editors can stop repainting their Game view. Step the real player loop
                // only when it stalls, so lifecycle, physics, collisions and coroutine tests still run.
                if (enteredPlayMode && (EditorApplication.isPaused ||
                    (Time.frameCount == lastFrame && EditorApplication.timeSinceStartup - lastFrameAt > .1)))
                {
                    stepping = true;
                    try { EditorApplication.Step(); }
                    finally { stepping = false; }
                }
                if (Time.frameCount != lastFrame) { lastFrame = Time.frameCount; lastFrameAt = EditorApplication.timeSinceStartup; }
                if (routine.MoveNext()) return;
                File.WriteAllText("Artifacts/ToadHunt/PlayValidation.txt", string.Join("\n", results) + "\nAll play-mode checks passed.");
                Debug.Log("Toad hunt end-to-end play validation passed."); Finish();
            }
            catch (Exception error)
            {
                File.WriteAllText("Artifacts/ToadHunt/PlayValidation.txt", string.Join("\n", results) + "\nFAIL: " + error);
                Debug.LogException(error); Finish();
            }
        }
        private static void Finish()
        {
            EditorApplication.update -= Tick; (routine as IDisposable)?.Dispose(); routine = null;
            if (Application.isPlaying) EditorApplication.isPaused = savedPause;
        }
        private static void DismissGuides()
        {
            foreach (var guide in UnityEngine.Object.FindObjectsByType<MapTutorialUI>(FindObjectsSortMode.None)) guide.enabled = false;
            foreach (var guide in UnityEngine.Object.FindObjectsByType<StatsSkillTutorialUI>(FindObjectsSortMode.None)) guide.enabled = false;
            foreach (var guide in UnityEngine.Object.FindObjectsByType<InventoryEquipmentTutorialUI>(FindObjectsSortMode.None)) guide.enabled = false;
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            var advancement = player != null ? player.GetComponent<PlayerAdvancementSystem>() : null;
            if (advancement != null) advancement.enabled = false;
        }
        private static void Drain(DialogueManager manager)
        {
            int limit = 40;
            while (manager.IsDialogueActive && !Get<bool>(manager, "isWaitingForQuestDecision") && limit-- > 0)
            {
                typeof(DialogueManager).GetMethod("CompleteCurrentLineImmediately", Private).Invoke(manager, null);
                manager.Advance();
            }
        }
        private static IEnumerator Run()
        {
            while (!enteredPlayMode) yield return null;
            float bootTime = Time.unscaledTime;
            while (Time.unscaledTime < bootTime + .15f) yield return null;
            while (ToadHuntQuestController.Instance == null || QuestManager.Instance == null || DialogueManager.Instance == null) yield return null;
            DismissGuides(); DialogueManager.Instance.EndDialogue();
            var flow = ToadHuntQuestController.Instance; var definition = flow.Definition;
            var quests = QuestManager.Instance;
            QuestDefinition first = definition.prerequisite.PrerequisiteQuest;
            if (first != null && quests.FindProgress(first)?.Status != QuestStatus.Completed)
            { quests.AcceptQuest(first); quests.AddProgress(first, first.RequiredAmount); quests.CompleteQuest(first); }
            var spiderLoad = SceneManager.LoadSceneAsync("SpiderCave");
            while (!spiderLoad.isDone) yield return null;
            yield return null;
            DismissGuides(); var dialogue = DialogueManager.Instance; dialogue.EndDialogue();
            Check(flow != null, "Quest flow survives scene transition");
            Check(Get<DialogueManager>(flow, "dialogue") == dialogue, "Quest flow observes the current dialogue manager");
            quests.AcceptQuest(definition.prerequisite); quests.AddProgress(definition.prerequisite, definition.prerequisite.RequiredAmount);
            var resident = UnityEngine.Object.FindObjectsByType<QuestGiver>(FindObjectsSortMode.None).Single(giver => giver.Quest == definition.prerequisite);
            resident.Interact();
            Check(Get<DialogueSequence>(dialogue, "currentSequence") == definition.prerequisiteCompletion, "Real resident interaction starts the existing spider completion dialogue");
            Drain(dialogue);
            Check(!dialogue.IsDialogueActive, "The spider completion dialogue finishes normally");
            Check(Get<bool>(flow, "offerPending"), "The completed dialogue reaches the persistent quest flow");
            var offer = Get<DialogueSequence>(resident, "nextOfferDialogue");
            while (Get<DialogueSequence>(dialogue, "currentSequence") != offer) yield return null;
            Check(quests.FindProgress(definition.quest) == null, "Follow-up offer appears after the existing dialogue, before acceptance");
            Drain(dialogue); Check(Get<bool>(dialogue, "isWaitingForQuestDecision"), "The real accept/decline choice is shown");
            dialogue.AcceptQuestChoice(); Drain(dialogue);
            Check(quests.FindProgress(definition.quest)?.Status == QuestStatus.Active, "Accepting the resident's request starts the toad quest");
            var marshLoad = SceneManager.LoadSceneAsync("Marshland");
            while (!marshLoad.isDone) yield return null;
            yield return null;
            DismissGuides(); dialogue.EndDialogue();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            var inventory = player.GetComponent<PlayerInventory>();
            var frog = UnityEngine.Object.FindFirstObjectByType<MON_Frog>();
            Check(frog != null && frog.HP > 0f, "Marshland contains the living toad boss");
            frog.TakeDamage(frog.maxHP * 10f);
            while (flow.Relic == null) yield return null;
            Check(quests.FindProgress(definition.quest).Status == QuestStatus.ReadyToTurnIn && inventory.GetQuantity(definition.necklace) == 0,
                "An actual boss death leaves the marked relic and does not auto-grant the necklace");
            player.GetComponent<PlayerMovement2D>().TeleportTo(flow.Relic.transform.position + Vector3.right, Quaternion.identity);
            var input = player.GetComponent<PlayerInputReader>(); input.enabled = true;
            typeof(PlayerInputReader).GetMethod("OnApplicationFocus", Private).Invoke(input, new object[] { true });
            Check(player.GetComponent<PlayerQuestInteraction>().TryInteract(), "The player's real T interaction can investigate the nearby relic");
            Check(Get<DialogueSequence>(dialogue, "currentSequence") == definition.discoveryMonologue && inventory.GetQuantity(definition.necklace) == 0,
                "Investigation opens the monologue before granting any item");
            Drain(dialogue);
            Check(inventory.GetQuantity(definition.necklace) == 1 && quests.FindProgress(definition.quest).Status == QuestStatus.Completed,
                "Completing the real monologue grants one necklace and completes the quest");
            while (Get<DialogueSequence>(dialogue, "currentSequence") != definition.accessoryExplanation) yield return null;
            Check(flow.Relic == null, "The relic disappears before the accessory explanation");
            Drain(dialogue);
            var finished = (HashSet<CharacterClassId>)typeof(InventoryEquipmentTutorialUI).GetField("completedClasses", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            finished.Add(CharacterSelectionState.SelectedClass);
            while (!EquipmentWindowUI.Instance.IsOpen) yield return null;
            Check(InventoryWindowUI.Instance.IsOpen && InventoryWindowUI.Instance.SelectedCategory == ItemCategory.Accessory,
                "Finishing the accessory explanation automatically opens the equipment window and accessory inventory tab");
            int equipmentOpenedFrame = Time.frameCount;
            while (Time.frameCount <= equipmentOpenedFrame) yield return null;
            var slots = Get<InventorySlotUI[]>(InventoryWindowUI.Instance, "slots");
            var sourceSlot = slots.Single(slot => slot.Definition == definition.necklace);
            var target = Get<EquipmentSlotUI[]>(EquipmentWindowUI.Instance, "slots").Single(slot => slot.Slot == EquipmentSlot.Necklace);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)target.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                pointerDrag = sourceSlot.gameObject,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            // A stepped/background Game view may not build native Graphic depths; the rendered
            // EventSystem raycast is checked separately in EquipmentValidation.txt.
            target.OnDrop(pointer);
            Check(inventory.GetEquipped(EquipmentSlot.Necklace) == definition.necklace && Mathf.Abs(inventory.EquippedJumpHeightBonus - .2f) < .001f,
                "The real accessory tab and necklace-slot drag apply the jump ability");
            Check(Get<UnityEngine.UI.Image>(target, "icon").enabled && Get<UnityEngine.UI.Image>(target, "icon").sprite == definition.necklace.Icon,
                "The equipped necklace artwork is visible in the equipment window");
            EquipmentWindowUI.Instance.SetOpen(false);
            var contact = CombatRecoveryValidation.RunSpawnContactGrace(Check);
            try { while (contact.MoveNext()) yield return contact.Current; }
            finally { (contact as IDisposable)?.Dispose(); }
        }
    }
}
