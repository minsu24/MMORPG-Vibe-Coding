using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EasternFantasy.EditorTools
{
    public static class InventoryEquipmentInteractiveValidation
    {
        private const string Folder = "Artifacts/InventoryEquipmentInteractive";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly List<string> results = new List<string>();
        private static InventoryEquipmentTutorialUI tutorial;

        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run in Play mode with isolated save storage.");
            tutorial = UnityEngine.Object.FindFirstObjectByType<InventoryEquipmentTutorialUI>();
            tutorial.StartCoroutine(RunRoutine());
        }

        private static void Check(bool ok, string message)
        {
            results.Add((ok ? "PASS " : "FAIL ") + message);
            File.WriteAllLines(Folder + "/Validation.txt", results);
            if (!ok) throw new InvalidOperationException(message);
        }

        private static void Call(string method) => typeof(InventoryEquipmentTutorialUI).GetMethod(method, Hidden).Invoke(tutorial, null);
        private static void Restore(bool completed) => typeof(InventoryEquipmentTutorialUI).GetMethod("RestoreSave", Hidden)
            .Invoke(null, new object[] { new SaveGameData { inventoryTutorial = completed } });

        private static void Press(Key key)
        {
            typeof(InventoryEquipmentTutorialUI).GetField("openedFrame", Hidden).SetValue(tutorial, -1);
            if (key == Key.I || key == Key.J)
                typeof(InventoryEquipmentTutorialUI).GetMethod("HandleShortcut", Hidden).Invoke(tutorial, new object[] { key == Key.J });
            else if (key == Key.Space) tutorial.Advance();
            Call("EvaluateProgress");
        }

        private static void Recover()
        {
            typeof(InventoryEquipmentTutorialUI).GetField("nextRecoveryCheck", Hidden).SetValue(tutorial, 0f);
            Call("RecoverUnfinishedTutorial");
            if ((bool)typeof(InventoryEquipmentTutorialUI).GetField("pending", Hidden).GetValue(tutorial)) Call("Begin");
        }

        private static void CheckRaycast(RectTransform target, Type expected, string label)
        {
            var guide = (PetCompanionTutorialUI)typeof(InventoryEquipmentTutorialUI).GetField("guide", Hidden).GetValue(tutorial);
            typeof(PetCompanionTutorialUI).GetMethod("RefreshLayout", Hidden).Invoke(guide, null);
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center))
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent(expected) != null,
                label + (hits.Count == 0 ? " (no hits)" : " (hit: " + hits[0].gameObject.name + ")"));
        }

        private static IEnumerator RunRoutine()
        {
            Directory.CreateDirectory(Folder);
            results.Clear();
            while (SaveGameService.IsRestoring) yield return null;
            SaveGameService.BeginAdminSession();
            typeof(MapTutorialUI).GetMethod("RestoreSave", Hidden).Invoke(null, new object[] { new SaveGameData { mapTutorial = true } });
            typeof(StatsSkillTutorialUI).GetMethod("RestoreSave", Hidden).Invoke(null, new object[] { new SaveGameData { statsTutorial = true } });
            DialogueManager.Instance?.EndDialogue();
            SceneLoadingScreen.Cancel();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            player.GetComponent<PlayerAdminController>().enabled = false;
            bool inputEnabled = player.GetComponent<PlayerInputReader>().enabled;
            var inventory = player.GetComponent<PlayerInventory>();
            var window = InventoryWindowUI.Instance;
            var equipment = EquipmentWindowUI.Instance;
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ClothTop.asset");
            var other = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/OldHat.asset");
            try
            {
                Restore(false);
                typeof(PlayerInventory).GetMethod("RestoreSave", Hidden).Invoke(inventory, new object[] { new SaveGameData() });
                inventory.AddItem(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/HealthPotion.asset"));
                inventory.AddItem(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/GoblinClub.asset"));
                var wrongWeapon = AssetDatabase.LoadAssetAtPath<ItemDefinition>(inventory.CurrentClass == EasternFantasy.CharacterSelection.CharacterClassId.Monk
                    ? "Assets/_Project/Data/Items/OldFan.asset" : "Assets/_Project/Data/Items/MonkGauntlets.asset");
                inventory.AddItem(wrongWeapon);
                Call("Update");
                Check(!tutorial.IsShowing, "Consumables, materials and another class's weapon do not start the tutorial");
                inventory.AddItem(item);
                Check((bool)typeof(InventoryEquipmentTutorialUI).GetField("pending", Hidden).GetValue(tutorial),
                    "Acquiring usable equipment queues the tutorial even if a dialogue delays it");
                Call("Begin");
                Check(tutorial.IsShowing && tutorial.CurrentStep == 0 && !window.IsOpen && !equipment.IsOpen,
                    "Acquiring usable equipment waits for I with both windows closed");
                Press(Key.Space); Press(Key.J);
                Check(tutorial.CurrentStep == 0 && !window.IsOpen && !equipment.IsOpen, "Space, next and J cannot bypass the I action");
                Press(Key.I);
                Check(tutorial.CurrentStep == 1 && window.IsOpen && !equipment.IsOpen, "I action opens only the large inventory");
                float referenceScale = window.GetComponent<Canvas>().scaleFactor;
                Check(window.TutorialTarget.rect.width * window.TutorialTarget.lossyScale.x / referenceScale > 650,
                    "Inventory uses a large readable panel rather than a shrunken two-window layout");
                tutorial.Advance();
                Check(tutorial.CurrentStep == 1, "Required equip action cannot be skipped");
                inventory.AddItem(other); inventory.TryEquip(other); Call("EvaluateProgress");
                tutorial.Advance();
                Check(tutorial.CurrentStep == 1, "Another item and next do not complete the required equip action");
                Canvas.ForceUpdateCanvases();
                yield return new WaitForSecondsRealtime(.15f); // Let newly enabled canvases register their graphic depths.
                var slot = window.TutorialItemTargetFor(item).GetComponent<InventorySlotUI>();
                CheckRaycast((RectTransform)slot.transform, typeof(InventorySlotUI), "Guide permits clicks on the acquired inventory item");
                var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, clickCount = 2 };
                ExecuteEvents.Execute<IPointerClickHandler>(slot.gameObject, click, ExecuteEvents.pointerClickHandler);
                Call("EvaluateProgress");
                Check(inventory.IsEquipped(item) && tutorial.CurrentStep == 2, "Actual inventory double-click equips the acquired item and advances");
                Check(!equipment.IsOpen && window.IsOpen, "Equipping waits for J rather than automatically opening equipment");
                Press(Key.Space); Press(Key.I);
                Check(tutorial.CurrentStep == 2 && !equipment.IsOpen, "Space, next and I cannot bypass the J action");
                Press(Key.J);
                Check(tutorial.CurrentStep == 3 && equipment.IsOpen && !window.IsOpen, "J action opens only the large equipment window");
                Check(equipment.TutorialTarget.rect.width * equipment.TutorialTarget.lossyScale.x / equipment.GetComponent<Canvas>().scaleFactor > 650,
                    "Equipment confirmation also uses a large readable panel");
                tutorial.Advance();
                Check(!tutorial.IsShowing && InventoryEquipmentTutorialUI.HasFinishedForCurrentClass && inventory.IsEquipped(item), "Completion preserves equipped gear and unlocks both windows");
                Check(player.GetComponent<PlayerInputReader>().enabled == inputEnabled && !window.IsOpen && !equipment.IsOpen, "Player input and closed window layout are restored");
                var saved = new SaveGameData();
                typeof(InventoryEquipmentTutorialUI).GetMethod("CaptureSave", Hidden).Invoke(null, new object[] { saved });
                Check(saved.inventoryTutorial, "Completion is captured by the existing save format");
                Restore(true); Recover();
                Check(!tutorial.IsShowing && window.IsUnlocked && equipment.IsUnlocked, "Completed saves do not replay the guide");
                inventory.TryUnequip(item.EquipmentSlot);
                Restore(false); Recover();
                Check(tutorial.IsShowing && tutorial.CurrentStep == 0, "Incomplete saves recover using owned gear without a new drop");
                // Recovery may select any owned usable equipment; use the actual selected item.
                var resumed = (ItemDefinition)typeof(InventoryEquipmentTutorialUI).GetField("practiceItem", Hidden).GetValue(tutorial);
                inventory.TryUnequip(resumed.EquipmentSlot);
                Press(Key.I);
                var source = window.TutorialItemTargetFor(resumed).gameObject;
                ExecuteEvents.Execute<IPointerClickHandler>(source, click, ExecuteEvents.pointerClickHandler);
                Call("EvaluateProgress");
                Check(tutorial.CurrentStep == 2 && inventory.IsEquipped(resumed), "Resumed guide can complete the same direct equip flow");
                Press(Key.J);
                tutorial.Advance();
                Restore(false); Recover();
                tutorial.Finish();
                Check(!InventoryEquipmentTutorialUI.HasFinishedForCurrentClass && player.GetComponent<PlayerInputReader>().enabled == inputEnabled,
                    "Interrupted guide restores input without falsely recording completion");
                Recover();
                Check(tutorial.IsShowing, "Interrupted guide can restart in the same session");
                tutorial.Finish(); Restore(true);
                results.Add("COMPLETE"); File.WriteAllLines(Folder + "/Validation.txt", results);
            }
            finally
            {
                if (tutorial != null && tutorial.IsShowing) tutorial.Finish();
            }
        }
    }
}
