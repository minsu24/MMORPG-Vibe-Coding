using System;
using System.IO;
using System.Linq;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Quest;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    public static class ToadHuntSetup
    {
        public const string ItemPath = "Assets/_Project/Data/Items/ToadBellNecklace.asset";
        public const string QuestPath = "Assets/_Project/Data/Quest/ToadHunt.asset";
        public const string DefinitionPath = "Assets/_Project/Resources/ToadHunt.asset";
        private const string DialogueFolder = "Assets/_Project/Data/Tutorial_DIalogue/";
        private const string RelicPath = "Assets/_Project/Prefabs/ToadBellQuestRelic.prefab";
        private static TMP_FontAsset font;

        private static void Set(SerializedObject data, string name, Object value) => data.FindProperty(name).objectReferenceValue = value;
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T result = AssetDatabase.LoadAssetAtPath<T>(path);
            if (result != null) return result;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.Refresh();
            result = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        [MenuItem("Eastern Fantasy/Quest/Setup Toad Hunt and Accessories")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play mode.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/_Project/Art/Items/ToadBellNecklace.png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1000f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(importer.assetPath);
            var item = LoadOrCreate<ItemDefinition>(ItemPath);
            var itemData = new SerializedObject(item);
            itemData.FindProperty("itemId").stringValue = "toad_bell_necklace";
            itemData.FindProperty("displayName").stringValue = "두꺼비 방울 목걸이";
            itemData.FindProperty("description").stringValue = "두꺼비가 지니고 있던 방울 목걸이. 낡은 부적과 방울에 남은 기운이 몸을 가볍게 해 줍니다.";
            Set(itemData, "icon", icon);
            itemData.FindProperty("category").enumValueIndex = (int)ItemCategory.Accessory;
            itemData.FindProperty("grade").intValue = (int)ItemGrade.Rare;
            itemData.FindProperty("equipmentSlot").enumValueIndex = (int)EquipmentSlot.Necklace;
            itemData.FindProperty("specialAbility").enumValueIndex = (int)AccessoryAbility.JumpHeight;
            itemData.FindProperty("specialAbilityValue").floatValue = .2f;
            itemData.FindProperty("maximumStack").intValue = 1;
            itemData.FindProperty("cannotSell").boolValue = true;
            itemData.FindProperty("allowedClasses").arraySize = 0;
            itemData.ApplyModifiedPropertiesWithoutUndo();

            var spider = AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/_Project/Data/Quest/SpiderHunt.asset");
            var quest = LoadOrCreate<QuestDefinition>(QuestPath);
            var q = new SerializedObject(quest);
            q.FindProperty("questId").stringValue = "prologue_toad_hunt";
            q.FindProperty("questType").enumValueIndex = (int)QuestType.Main;
            q.FindProperty("title").stringValue = "늪지대의 두꺼비";
            q.FindProperty("objectiveDescription").stringValue = "늪지대의 두꺼비 처치";
            q.FindProperty("requiredAmount").intValue = 1;
            q.FindProperty("rewardYeopjeon").intValue = 0;
            q.FindProperty("rewardEXP").intValue = 0;
            q.FindProperty("turnInDescription").stringValue = "두꺼비가 쓰러진 자리의 목걸이를 T키로 조사";
            Set(q, "prerequisiteQuest", spider);
            q.FindProperty("mapOfferScene").stringValue = "SpiderCave";
            q.FindProperty("mapObjectiveScene").stringValue = "Marshland";
            q.FindProperty("mapTurnInScene").stringValue = "Marshland";
            var revealed = q.FindProperty("revealedMapScenes"); revealed.arraySize = 1; revealed.GetArrayElementAtIndex(0).stringValue = "Marshland";
            q.ApplyModifiedPropertiesWithoutUndo();

            var player = AssetDatabase.LoadAssetAtPath<DialogueCharacter>(DialogueFolder + "DialoguePlayer.asset");
            var resident = AssetDatabase.LoadAssetAtPath<DialogueCharacter>(DialogueFolder + "DialogueHurtPeople.asset");
            var offer = Dialogue("ToadHuntOfferDialogue", player, resident,
                (DialogueSpeakerSide.Partner, "그 두꺼비가 늪지대의 길을 막고 있습니다. 부디 물리쳐 주세요."));
            var accepted = Dialogue("ToadHuntAcceptedDialogue", player, resident,
                (DialogueSpeakerSide.Partner, "거미동굴을 지나면 늪지대가 나옵니다. 두꺼비의 공격을 조심하세요."));
            var declined = Dialogue("ToadHuntDeclinedDialogue", player, resident,
                (DialogueSpeakerSide.Partner, "준비가 되면 다시 말씀해 주세요. 저는 여기서 기다리겠습니다."));
            var progress = Dialogue("ToadHuntProgressDialogue", player, resident,
                (DialogueSpeakerSide.Partner, "늪지대의 두꺼비는 아직 그대로인가요? 부디 조심해 주세요."));
            var after = Dialogue("ToadHuntAfterDialogue", player, resident,
                (DialogueSpeakerSide.Partner, "두꺼비를 물리쳐 주셨군요. 이제 안심하고 마을로 돌아갈 수 있겠습니다. 고맙습니다."));
            var discovery = Dialogue("ToadNecklaceMonologue", player, null,
                (DialogueSpeakerSide.Player, "두꺼비가 있던 자리에 목걸이가 떨어져 있다. 방울과 부적에서 묘한 기운이 느껴진다…"),
                (DialogueSpeakerSide.Player, "몸을 가볍게 해 주는 물건인 것 같다. 챙겨 두자."));
            var explanation = Dialogue("AccessoryIntroduction", player, null,
                (DialogueSpeakerSide.Player, "장신구들은 고유한 특수능력을 지닙니다. 게임을 진행하며 여러 장신구를 모아보세요."),
                (DialogueSpeakerSide.Player, "I키로 인벤토리의 장신구 탭을 열고, J키로 장비창을 여세요. 두꺼비 방울 목걸이를 목걸이 칸에 드래그하면 점프 높이가 증가합니다."));
            var config = LoadOrCreate<ToadHuntDefinition>(DefinitionPath);
            config.prerequisite = spider;
            config.prerequisiteCompletion = AssetDatabase.LoadAssetAtPath<DialogueSequence>(DialogueFolder + "SpiderHuntCompletionDialogue.asset");
            config.quest = quest; config.necklace = item;
            config.discoveryMonologue = discovery; config.accessoryExplanation = explanation;
            config.relicPrefab = CreateRelic(icon);
            config.bossScene = "Marshland";
            EditorUtility.SetDirty(config);
            UpdateInventory();
            UpdateMap(quest);
            UpdateResident(spider, quest, offer, accepted, declined, progress, after);
            AssetDatabase.SaveAssets();
            Debug.Log("Toad hunt, uncollectable discovery relic, accessory inventory and +20% jump-height necklace are ready.");
        }

        [MenuItem("Eastern Fantasy/Quest/Improve Toad Reward Visibility")]
        public static void ImproveRewardVisibility()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play mode.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
            var definition = AssetDatabase.LoadAssetAtPath<ToadHuntDefinition>(DefinitionPath);
            definition.relicPrefab = CreateRelic(definition.necklace.Icon);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssetIfDirty(definition);
        }

        private static DialogueSequence Dialogue(string name, DialogueCharacter player, DialogueCharacter partner,
            params (DialogueSpeakerSide side, string text)[] lines)
        {
            var sequence = LoadOrCreate<DialogueSequence>(DialogueFolder + name + ".asset");
            var data = new SerializedObject(sequence);
            Set(data, "playerCharacter", player); Set(data, "partnerCharacter", partner);
            var target = data.FindProperty("lines"); target.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = target.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speaker").enumValueIndex = (int)lines[i].side;
                line.FindPropertyRelative("text").stringValue = lines[i].text;
                line.FindPropertyRelative("classOverrides").arraySize = 0;
                line.FindPropertyRelative("firstAdvancementText").stringValue = string.Empty;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            return sequence;
        }

        private static QuestRewardRelic CreateRelic(Sprite icon)
        {
            var root = new GameObject("Toad Bell Quest Relic", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(QuestRewardRelic));
            try
            {
                var renderer = root.GetComponent<SpriteRenderer>(); renderer.sprite = icon; renderer.sortingOrder = 202;
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
                root.GetComponent<CircleCollider2D>().isTrigger = true;
                root.GetComponent<CircleCollider2D>().radius = .55f;
                root.transform.localScale = Vector3.one;
                var itemPlate = new GameObject("Item Contrast Plate", typeof(RectTransform), typeof(Canvas), typeof(Image));
                itemPlate.transform.SetParent(root.transform, false);
                itemPlate.transform.localScale = Vector3.one * .01f;
                ((RectTransform)itemPlate.transform).sizeDelta = new Vector2(120, 110);
                var plateCanvas = itemPlate.GetComponent<Canvas>(); plateCanvas.renderMode = RenderMode.WorldSpace;
                plateCanvas.overrideSorting = true; plateCanvas.sortingOrder = 201;
                itemPlate.GetComponent<Image>().color = new Color(1f, .75f, .25f, .95f);
                itemPlate.GetComponent<Image>().raycastTarget = false;
                DarkInset(itemPlate.transform);
                var marker = new GameObject("Quest Completion Marker", typeof(RectTransform), typeof(Canvas));
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition = Vector3.up * 1.75f;
                marker.transform.localScale = Vector3.one * .012f;
                var canvas = marker.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true; canvas.sortingOrder = 300;
                ((RectTransform)marker.transform).sizeDelta = new Vector2(300f, 170f);
                var backing = new GameObject("Completion Contrast Panel", typeof(RectTransform), typeof(Image));
                backing.transform.SetParent(marker.transform, false);
                ((RectTransform)backing.transform).sizeDelta = new Vector2(300, 150);
                ((RectTransform)backing.transform).anchoredPosition = new Vector2(0, 8);
                backing.GetComponent<Image>().color = new Color(.85f, .65f, .25f, .95f);
                backing.GetComponent<Image>().raycastTarget = false;
                DarkInset(backing.transform);
                var map = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/UI/MapWindow.prefab");
                Sprite ready;
                try { ready = (Sprite)new SerializedObject(map.GetComponent<MapMainQuestUI>()).FindProperty("readySprite").objectReferenceValue; }
                finally { PrefabUtility.UnloadPrefabContents(map); }
                var image = new GameObject("Complete Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(marker.transform, false); image.sprite = ready; image.preserveAspect = true; image.raycastTarget = false;
                image.rectTransform.sizeDelta = new Vector2(48, 48); image.rectTransform.anchoredPosition = new Vector2(0, 50);
                Text(marker.transform, "퀘스트 완료", new Vector2(0, 4), new Vector2(290, 44), 34);
                Text(marker.transform, "T키 · 목걸이 조사", new Vector2(0, -42), new Vector2(290, 34), 25);
                Text(marker.transform, "▼", new Vector2(0, -94), new Vector2(70, 40), 38);
                var data = new SerializedObject(root.GetComponent<QuestRewardRelic>());
                Set(data, "completionMarker", marker.transform); data.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, RelicPath).GetComponent<QuestRewardRelic>();
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void DarkInset(Transform parent)
        {
            var fill = new GameObject("Dark Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(parent, false); fill.color = new Color(.01f, .02f, .025f, .98f); fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.one * 2; fill.rectTransform.offsetMax = Vector2.one * -2;
        }

        private static void Text(Transform parent, string value, Vector2 position, Vector2 size, float pointSize)
        {
            var text = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            text.transform.SetParent(parent, false); text.font = font; text.text = value;
            text.fontSize = pointSize; text.color = new Color(1f, .85f, .4f); text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; text.outlineWidth = .22f;
            text.rectTransform.sizeDelta = size; text.rectTransform.anchoredPosition = position;
        }

        private static void UpdateInventory()
        {
            const string path = "Assets/_Project/Prefabs/UI/InventoryWindow.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var data = new SerializedObject(root.GetComponent<InventoryWindowUI>());
                var equipment = (Button)data.FindProperty("equipmentTab").objectReferenceValue;
                var consumable = (Button)data.FindProperty("consumableTab").objectReferenceValue;
                var misc = (Button)data.FindProperty("miscellaneousTab").objectReferenceValue;
                var accessory = (Button)data.FindProperty("accessoryTab").objectReferenceValue;
                bool hadAccessory = accessory != null;
                if (accessory == null)
                {
                    accessory = Object.Instantiate(equipment, equipment.transform.parent);
                    accessory.name = "장신구Tab";
                    accessory.onClick = new Button.ButtonClickedEvent();
                }
                Button[] tabs = { equipment, accessory, consumable, misc };
                string[] titles = { "장비", "장신구", "소비", "기타" };
                float center = (((RectTransform)equipment.transform).anchoredPosition.x + ((RectTransform)misc.transform).anchoredPosition.x) * .5f;
                float y = ((RectTransform)equipment.transform).anchoredPosition.y;
                float width = (Mathf.Abs(((RectTransform)misc.transform).anchoredPosition.x - ((RectTransform)equipment.transform).anchoredPosition.x) + ((RectTransform)equipment.transform).sizeDelta.x) / 4f;
                if (hadAccessory) width = Mathf.Abs(((RectTransform)misc.transform).anchoredPosition.x - ((RectTransform)equipment.transform).anchoredPosition.x) / 3f;
                // Re-running setup keeps the same total width rather than shrinking the row.
                width = Mathf.Max(140f, width);
                for (int i = 0; i < tabs.Length; i++)
                {
                    var rect = (RectTransform)tabs[i].transform;
                    rect.anchoredPosition = new Vector2(center + (i - 1.5f) * width, y);
                    rect.sizeDelta = new Vector2(width - 8f, rect.sizeDelta.y);
                    var label = tabs[i].GetComponentInChildren<TMP_Text>();
                    label.text = titles[i]; label.fontSize = 25;
                }
                Set(data, "accessoryTab", accessory); data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void UpdateMap(QuestDefinition quest)
        {
            const string path = "Assets/_Project/Prefabs/UI/MapWindow.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var data = new SerializedObject(root.GetComponent<MapMainQuestUI>());
                var array = data.FindProperty("mainQuests");
                bool exists = false; for (int i = 0; i < array.arraySize; i++) exists |= array.GetArrayElementAtIndex(i).objectReferenceValue == quest;
                if (!exists) { int index = array.arraySize++; array.GetArrayElementAtIndex(index).objectReferenceValue = quest; }
                data.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void UpdateResident(QuestDefinition spider, QuestDefinition quest, DialogueSequence offer,
            DialogueSequence accepted, DialogueSequence declined, DialogueSequence progress, DialogueSequence after)
        {
            const string path = "Assets/_Project/Scenes/SpiderCave.unity";
            Scene scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save existing SpiderCave edits before running setup.");
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                int count = 0;
                foreach (var giver in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<QuestGiver>(true)))
                {
                    var data = new SerializedObject(giver);
                    if (data.FindProperty("quest").objectReferenceValue != spider || data.FindProperty("allowQuestOffer").boolValue) continue;
                    Set(data, "nextQuest", quest); Set(data, "nextOfferDialogue", offer); Set(data, "nextAcceptedDialogue", accepted);
                    Set(data, "nextDeclinedDialogue", declined); Set(data, "nextProgressDialogue", progress); Set(data, "nextRegularDialogue", after);
                    data.FindProperty("allowNextQuestOffer").boolValue = true;
                    data.FindProperty("allowNextQuestTurnIn").boolValue = false;
                    data.ApplyModifiedPropertiesWithoutUndo(); count++;
                }
                if (count != 1) throw new InvalidOperationException("Expected exactly one wounded resident, found " + count);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
