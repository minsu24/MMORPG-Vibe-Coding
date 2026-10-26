using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Quest;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    public static class ToadHuntValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly List<string> results = new List<string>();
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            results.Add("PASS: " + message);
        }

        [MenuItem("Eastern Fantasy/Quest/Validate Toad Hunt and Accessories")]
        public static void RunMenu()
        {
            Directory.CreateDirectory("Artifacts/ToadHunt");
            results.Clear();
            try
            {
                if (Application.isPlaying) throw new InvalidOperationException("Run outside Play mode.");
                var definition = AssetDatabase.LoadAssetAtPath<ToadHuntDefinition>(ToadHuntSetup.DefinitionPath);
                Check(definition != null && definition.necklace != null && definition.relicPrefab != null, "All runtime flow assets are connected");
                Check(definition.necklace.Category == ItemCategory.Accessory && definition.necklace.EquipmentSlot == EquipmentSlot.Necklace,
                    "Reward belongs to the accessory category and necklace slot");
                Check(definition.necklace.Icon != null && !definition.necklace.CanSell, "Supplied necklace artwork is assigned and unique reward is protected from sale");
                Check(definition.accessoryExplanation.Lines[0].Text == "장신구들은 고유한 특수능력을 지닙니다. 게임을 진행하며 여러 장신구를 모아보세요.", "Requested accessory explanation is preserved verbatim");
                TestResident(definition);
                foreach (string prefab in new[] { "Player", "MonkPlayer", "SwordswomanPlayer" }) TestEquipment(definition.necklace, prefab);
                TestFlow(definition, false);
                TestFlow(definition, true);
                File.WriteAllText("Artifacts/ToadHunt/Validation.txt", string.Join("\n", results) + "\nAll " + results.Count + " checks passed.");
                Debug.Log("Toad hunt validation: " + results.Count + " checks passed. See Artifacts/ToadHunt.");
            }
            catch (Exception exception)
            {
                File.WriteAllText("Artifacts/ToadHunt/Validation.txt", string.Join("\n", results) + "\nFAIL: " + exception);
                Debug.LogException(exception);
            }
        }
        private static void TestResident(ToadHuntDefinition definition)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath("Assets/_Project/Scenes/SpiderCave.unity");
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/SpiderCave.unity", OpenSceneMode.Additive);
            try
            {
                var givers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<QuestGiver>(true));
                var giver = givers.Single(candidate => Get<QuestDefinition>(candidate, "nextQuest") == definition.quest);
                Check(Get<QuestDefinition>(giver, "quest") == definition.prerequisite, "Wounded resident continues the existing spider quest");
                Check(Get<DialogueSequence>(giver, "completionDialogue") == definition.prerequisiteCompletion, "Existing completion dialogue and class-specific lines remain connected");
                Check(!Get<bool>(giver, "allowNextQuestTurnIn"), "Toad quest cannot be turned in to the resident instead of the relic");
                Check(Get<DialogueSequence>(giver, "nextOfferDialogue") != null, "Resident can offer and re-offer the follow-up quest");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
        }

        private static void TestEquipment(ItemDefinition necklace, string prefabName)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var temporary = new List<Object>();
            try
            {
                var physics = scene.GetPhysicsScene2D();
                var floor = new GameObject("Floor", typeof(BoxCollider2D));
                SceneManager.MoveGameObjectToScene(floor, scene);
                floor.transform.position = new Vector3(0, -.5f); floor.GetComponent<BoxCollider2D>().size = new Vector2(40, 1);
                var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/" + prefabName + ".prefab"), scene);
                var entity = player.GetComponent<PlayerEntity>();
                var movement = player.GetComponent<PlayerMovement2D>();
                var inventory = player.GetComponent<PlayerInventory>();
                Call(entity, "Awake"); Call(inventory, "Awake"); Call(movement, "Awake");
                var body = player.GetComponent<Rigidbody2D>();
                body.position = new Vector2(0, 2); player.transform.position = body.position;
                for (int i = 0; i < 80; i++) physics.Simulate(.02f);
                Call(movement, "FixedUpdate");
                Vector2 ground = body.position;
                float originalHeight = JumpHeight(movement, physics, ground);
                Check(inventory.AddItem(necklace) && inventory.EquippedJumpHeightBonus == 0f, prefabName + ": owning the necklace does not apply an unequipped effect");
                var equipment = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/EquipmentWindow.prefab"), scene);
                var slots = equipment.GetComponentsInChildren<EquipmentSlotUI>(true);
                foreach (var slot in slots) slot.Initialize(inventory, null);
                var source = new GameObject("Drag source", typeof(InventorySlotUI)); SceneManager.MoveGameObjectToScene(source, scene);
                Set(source.GetComponent<InventorySlotUI>(), "item", necklace);
                var pointer = new PointerEventData(null) { pointerDrag = source };
                slots.Single(slot => slot.Slot == EquipmentSlot.Ring).OnDrop(pointer);
                Check(!inventory.IsEquipped(necklace), prefabName + ": necklace cannot be dragged onto the ring slot");
                var necklaceSlot = slots.Single(slot => slot.Slot == EquipmentSlot.Necklace);
                necklaceSlot.OnDrop(pointer);
                Check(inventory.GetEquipped(EquipmentSlot.Necklace) == necklace, prefabName + ": real inventory-to-necklace-slot drop equips the accessory");
                Check(Mathf.Abs(inventory.EquippedJumpHeightBonus - .2f) < .001f, prefabName + ": equipped necklace grants its 20% height bonus");
                inventory.TryEquip(necklace);
                Check(Mathf.Abs(inventory.EquippedJumpHeightBonus - .2f) < .001f, prefabName + ": repeated equip does not stack the same item");
                float boostedHeight = JumpHeight(movement, physics, ground);
                Check(boostedHeight > originalHeight * 1.16f && boostedHeight < originalHeight * 1.24f,
                    prefabName + ": isolated physics measures about 20% higher jumps (ratio " + (boostedHeight / originalHeight).ToString("F3") + ")");
                Check(inventory.TryUnequip(EquipmentSlot.Necklace) && inventory.EquippedJumpHeightBonus == 0f, prefabName + ": unequip removes the special ability immediately");
                foreach (var type in new[] { EquipmentSlot.Bracelet, EquipmentSlot.Ring })
                {
                    var item = Object.Instantiate(necklace); temporary.Add(item);
                    var data = new SerializedObject(item); data.FindProperty("equipmentSlot").enumValueIndex = (int)type; data.ApplyModifiedPropertiesWithoutUndo();
                    Check(inventory.AddItem(item) && inventory.TryEquip(item) && inventory.GetEquipped(type) == item,
                        prefabName + ": accessory model also supports " + type);
                    inventory.TryUnequip(type);
                }
                var inventoryRoot = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/InventoryWindow.prefab"), scene);
                var ui = inventoryRoot.GetComponent<InventoryWindowUI>();
                Set(ui, "inventory", inventory);
                foreach (var slot in Get<InventorySlotUI[]>(ui, "slots")) slot.Initialize(inventory, ui.Tooltip);
                ui.SelectCategory(ItemCategory.Accessory);
                Check(Get<Button>(ui, "accessoryTab") != null && Get<InventorySlotUI[]>(ui, "slots").Count(slot => slot.Definition == necklace) == 1,
                    prefabName + ": accessory tab displays the reward in its own inventory category");
                if (prefabName == "Player")
                {
                    foreach (var temporaryItem in temporary.OfType<ItemDefinition>()) inventory.RemoveItem(temporaryItem);
                    ui.Refresh();
                    inventory.TryEquip(necklace); necklaceSlot.Refresh();
                    CaptureEquipment(scene, ui, equipment.GetComponent<EquipmentWindowUI>(), "Artifacts/ToadHunt/AccessoryEquipment.png");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var item in temporary) Object.DestroyImmediate(item);
            }
        }
        private static float JumpHeight(PlayerMovement2D movement, PhysicsScene2D physics, Vector2 ground)
        {
            var body = movement.GetComponent<Rigidbody2D>();
            body.position = ground; body.transform.position = ground; body.linearVelocity = Vector2.zero;
            movement.ClearInput(); Physics2D.SyncTransforms(); Call(movement, "FixedUpdate");
            movement.RequestJump(); Call(movement, "FixedUpdate");
            float peak = ground.y;
            for (int i = 0; i < 100; i++) { physics.Simulate(.02f); peak = Mathf.Max(peak, body.position.y); }
            return peak - ground.y;
        }

        private static void TestFlow(ToadHuntDefinition source, bool earlyKill)
        {
            var previousScene = SceneManager.GetActiveScene();
            var managerField = typeof(QuestManager).GetField("<Instance>k__BackingField", Static);
            var controllerField = typeof(ToadHuntQuestController).GetField("<Instance>k__BackingField", Static);
            object previousManager = managerField.GetValue(null), previousController = controllerField.GetValue(null);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var objects = new List<Object>();
            try
            {
                SceneManager.SetActiveScene(scene);
                var definition = Object.Instantiate(source); objects.Add(definition); definition.bossScene = scene.name;
                var prerequisite = Object.Instantiate(source.prerequisite); objects.Add(prerequisite);
                var q = new SerializedObject(prerequisite); q.FindProperty("prerequisiteQuest").objectReferenceValue = null;
                q.FindProperty("rewardEXP").intValue = 0; q.FindProperty("rewardYeopjeon").intValue = 0; q.ApplyModifiedPropertiesWithoutUndo();
                var quest = Object.Instantiate(source.quest); objects.Add(quest); definition.quest = quest; definition.prerequisite = prerequisite;
                q = new SerializedObject(quest); q.FindProperty("prerequisiteQuest").objectReferenceValue = prerequisite; q.ApplyModifiedPropertiesWithoutUndo();
                var manager = new GameObject("Fixture quests", typeof(QuestManager)).GetComponent<QuestManager>(); managerField.SetValue(null, manager);
                var flow = new GameObject("Fixture toad flow", typeof(ToadHuntQuestController)).GetComponent<ToadHuntQuestController>(); controllerField.SetValue(null, flow);
                Set(flow, "<Definition>k__BackingField", definition); Call(flow, "RefreshBindings");
                var enemy = new GameObject("Defeated toad", typeof(MON_Ghost)).GetComponent<MON_Ghost>(); enemy.transform.position = new Vector3(6, 1);
                var inventory = new GameObject("Reward inventory", typeof(PlayerInventory)).GetComponent<PlayerInventory>();
                Check(!manager.CanAccept(quest), "Flow " + earlyKill + ": toad quest stays locked before spider completion");
                manager.AcceptQuest(prerequisite); manager.AddProgress(prerequisite, prerequisite.RequiredAmount); manager.CompleteQuest(prerequisite);
                var giver = new GameObject("Wounded resident fixture", typeof(QuestGiver)).GetComponent<QuestGiver>();
                Set(giver, "quest", prerequisite); Set(giver, "nextQuest", quest); Set(giver, "allowQuestOffer", false);
                Set(giver, "nextOfferDialogue", source.prerequisiteCompletion);
                Check(giver.CanOfferQuest, "Flow " + earlyKill + ": resident can offer next quest while initial quest offers are disabled");
                Call(flow, "DialogueCompleted", source.prerequisiteCompletion);
                Check(Get<bool>(flow, "offerPending"), "Flow " + earlyKill + ": completing the existing dialogue queues the follow-up offer");
                if (earlyKill) flow.BossDefeated(enemy);
                manager.AcceptQuest(quest);
                if (!earlyKill) flow.BossDefeated(enemy);
                Check(manager.FindProgress(quest).Status == QuestStatus.ReadyToTurnIn, "Flow " + earlyKill + ": toad death advances the quest, including pre-acceptance defeats");
                Check(flow.Relic != null && flow.Relic.GetComponent<WorldItemPickup>() == null && inventory.GetQuantity(source.necklace) == 0,
                    "Flow " + earlyKill + ": death leaves an uncollectable necklace relic without granting the reward");
                Check(Vector2.Distance(flow.Relic.transform.position, enemy.transform.position) < 1f, "Flow " + earlyKill + ": completion marker is placed at the toad's death position");
                if (!earlyKill) CaptureRelic(scene, flow.Relic);
                Object.DestroyImmediate(flow.Relic.gameObject);
                Set(flow, "<Relic>k__BackingField", null);
                Call(flow, "RefreshProgress");
                Check(flow.Relic != null, "Flow " + earlyKill + ": unclaimed discovery relic can be restored after a scene revisit");
                Call(flow, "DialogueCompleted", source.discoveryMonologue);
                Check(inventory.GetQuantity(source.necklace) == 0, "Flow " + earlyKill + ": a non-inspected or interrupted monologue cannot grant the reward");
                for (int i = 0; i < PlayerInventory.SlotsPerCategory; i++)
                {
                    var filler = Object.Instantiate(source.necklace); objects.Add(filler); inventory.AddItem(filler);
                }
                Set(flow, "inspectingInventory", inventory); Set(flow, "inspecting", true);
                Call(flow, "DialogueCompleted", source.discoveryMonologue);
                Check(manager.FindProgress(quest).Status == QuestStatus.ReadyToTurnIn && flow.Relic != null
                    && inventory.GetQuantity(source.necklace) == 0, "Flow " + earlyKill + ": full accessory inventory preserves both quest and relic for retry");
                foreach (var filler in inventory.Items.Select(stack => stack.Item).ToArray()) inventory.RemoveItem(filler);
                Set(flow, "inspectingInventory", inventory); Set(flow, "inspecting", true);
                Call(flow, "DialogueCompleted", source.discoveryMonologue);
                Check(inventory.GetQuantity(source.necklace) == 1 && manager.FindProgress(quest).Status == QuestStatus.Completed && flow.Relic == null,
                    "Flow " + earlyKill + ": finishing inspection grants exactly one necklace and completes the quest");
                Check(Get<bool>(flow, "explanationPending"), "Flow " + earlyKill + ": accessory explanation is queued after acquisition");
                Set(flow, "inspecting", true); Set(flow, "inspectingInventory", inventory);
                Call(flow, "DialogueCompleted", source.discoveryMonologue); flow.BossDefeated(enemy);
                Check(inventory.GetQuantity(source.necklace) == 1 && flow.Relic == null, "Flow " + earlyKill + ": repeated callbacks or defeats cannot duplicate the unique reward");
            }
            finally
            {
                SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
                managerField.SetValue(null, previousManager); controllerField.SetValue(null, previousController);
                foreach (var item in objects) Object.DestroyImmediate(item);
            }
        }

        private static Camera CameraFor(Scene scene)
        {
            var camera = new GameObject("Validation Camera", typeof(Camera)).GetComponent<Camera>();
            SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
            camera.orthographic = true; camera.orthographicSize = 4;
            camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .045f, .045f); camera.cullingMask = 1 << 31;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            var light = new GameObject("Validation Light", typeof(Light)); SceneManager.MoveGameObjectToScene(light, scene);
            light.GetComponent<Light>().type = LightType.Directional; light.GetComponent<Light>().cullingMask = 1 << 31;
            return camera;
        }
        private static void Layer(Transform root) { root.gameObject.layer = 31; foreach (Transform child in root) Layer(child); }
        private static void CaptureEquipment(Scene scene, InventoryWindowUI inventory, EquipmentWindowUI equipment, string path)
        {
            var camera = CameraFor(scene);
            foreach (var root in new[] { inventory.gameObject, equipment.gameObject })
            {
                Layer(root.transform);
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
            }
            var left = inventory.TutorialTarget; left.gameObject.SetActive(true);
            left.anchorMin = left.anchorMax = new Vector2(.5f, .5f); left.pivot = new Vector2(.5f, .5f);
            left.anchoredPosition = new Vector2(-440, 0); left.localScale = Vector3.one * .9f;
            var right = equipment.TutorialTarget; right.gameObject.SetActive(true);
            right.anchorMin = right.anchorMax = new Vector2(.5f, .5f); right.pivot = new Vector2(.5f, .5f);
            right.anchoredPosition = new Vector2(440, 0); right.localScale = Vector3.one * .9f;
            Canvas.ForceUpdateCanvases();
            SaveCamera(camera, path, 1920, 1080);
        }
        private static void CaptureRelic(Scene scene, QuestRewardRelic relic)
        {
            var camera = CameraFor(scene); Layer(relic.transform);
            camera.orthographicSize = 2.3f; camera.transform.position = relic.transform.position + new Vector3(0, 1.1f, -10);
            Canvas.ForceUpdateCanvases(); SaveCamera(camera, "Artifacts/ToadHunt/QuestRelic.png", 1280, 720);
            Object.DestroyImmediate(camera.gameObject);
        }
        private static void SaveCamera(Camera camera, string path, int width, int height)
        {
            var target = new RenderTexture(width, height, 24); var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }
    }
}
