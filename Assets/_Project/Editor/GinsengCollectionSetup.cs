using System;
using System.Linq;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Quest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class GinsengCollectionSetup
    {
        public const string QuestPath = "Assets/_Project/Data/Quest/GinsengCollection/GinsengCollection.asset";
        public const string TemplePath = "Assets/_Project/Scenes/OldTemple/TempleGate.unity";
        public const string StemPath = "Assets/_Project/Data/Items/Ginseng Stem.asset";
        public const string RingPath = "Assets/_Project/Data/Items/Ginseng Stem Ring.asset";
        private const string Folder = "Assets/_Project/Data/Quest/GinsengCollection";

        private static void Edit(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            var data = new SerializedObject(target);
            edit(data);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value != null) return value;
            value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        [MenuItem("Eastern Fantasy/Quest/Setup Ginseng Collection")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play Mode.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Data/Quest", "GinsengCollection");
            var stem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(StemPath);
            var ring = AssetDatabase.LoadAssetAtPath<ItemDefinition>(RingPath);
            if (stem == null || ring == null) throw new InvalidOperationException("The existing Ginseng Stem and Ginseng Stem Ring assets are required.");
            Edit(stem, data => data.FindProperty("category").enumValueIndex = (int)ItemCategory.Miscellaneous);
            Edit(ring, data =>
            {
                data.FindProperty("itemId").stringValue = "ginseng_stem_ring";
                data.FindProperty("displayName").stringValue = "인삼 줄기 반지";
                data.FindProperty("description").stringValue = "백 년 묵은 인삼 줄기를 엮어 만든 반지. 장착하면 1초마다 체력을 1 회복합니다.";
                data.FindProperty("category").enumValueIndex = (int)ItemCategory.Accessory;
                data.FindProperty("equipmentSlot").enumValueIndex = (int)EquipmentSlot.Ring;
                data.FindProperty("specialAbility").enumValueIndex = (int)AccessoryAbility.HealthRegeneration;
                data.FindProperty("specialAbilityValue").floatValue = 1f;
                data.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Items/인삼 줄기 반지.png").OfType<Sprite>().First();
            });

            var quest = Asset<QuestDefinition>(QuestPath);
            Edit(quest, data =>
            {
                data.FindProperty("questId").stringValue = "temple_gate_ginseng_stems";
                data.FindProperty("questType").enumValueIndex = (int)QuestType.Sub;
                data.FindProperty("title").stringValue = "나그네의 약초 부탁";
                data.FindProperty("objectiveDescription").stringValue = "인삼 몬스터에게서 인삼 줄기 15개를 모아 나그네에게 가져가기";
                data.FindProperty("turnInDescription").stringValue = "사찰 입구의 나그네에게 인삼 줄기 15개 전달";
                data.FindProperty("requiredAmount").intValue = 15;
                data.FindProperty("requiredItem").objectReferenceValue = stem;
                data.FindProperty("rewardYeopjeon").intValue = 0;
                data.FindProperty("rewardEXP").intValue = 0;
                var rewards = data.FindProperty("rewardItems"); rewards.arraySize = 1;
                rewards.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = ring;
                rewards.GetArrayElementAtIndex(0).FindPropertyRelative("quantity").intValue = 1;
                foreach (string field in new[] { "mapOfferScene", "mapObjectiveScene", "mapTurnInScene" }) data.FindProperty(field).stringValue = "TempleGate";
            });

            var enemy = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/Enemy/Ginseng.prefab");
            try
            {
                var drop = enemy.GetComponent<EnemyLootDrop>() ?? enemy.AddComponent<EnemyLootDrop>();
                Edit(drop, data =>
                {
                    data.FindProperty("pickupPrefab").objectReferenceValue = Resources.Load<CommonMonsterLoot>("CommonMonsterLoot").PickupPrefab;
                    var drops = data.FindProperty("drops");
                    int index = -1;
                    for (int i = 0; i < drops.arraySize; i++) if (drops.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue == stem) index = i;
                    if (index < 0) { index = drops.arraySize; drops.arraySize++; }
                    var entry = drops.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("item").objectReferenceValue = stem;
                    entry.FindPropertyRelative("dropChance").floatValue = .7f;
                    entry.FindPropertyRelative("minimumQuantity").intValue = 1;
                    entry.FindPropertyRelative("maximumQuantity").intValue = 1;
                });
                PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/_Project/Prefabs/Enemy/Ginseng.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(enemy); }

            Scene previous = SceneManager.GetActiveScene();
            Scene temple = SceneManager.GetSceneByPath(TemplePath);
            bool openedTemple = !temple.isLoaded;
            if (openedTemple) temple = EditorSceneManager.OpenScene(TemplePath, OpenSceneMode.Additive);
            const string villagePath = "Assets/_Project/Scenes/TutorialMap/PrologueVilage.unity";
            Scene village = SceneManager.GetSceneByPath(villagePath);
            bool openedVillage = !village.isLoaded;
            if (openedVillage) village = EditorSceneManager.OpenScene(villagePath, OpenSceneMode.Additive);
            try
            {
                var traveler = temple.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).First(t => t.name == "Traveler").gameObject;
                var character = Asset<DialogueCharacter>(Folder + "/TravelerCharacter.asset");
                Edit(character, data =>
                {
                    data.FindProperty("displayName").stringValue = "나그네";
                    data.FindProperty("portrait").objectReferenceValue = traveler.GetComponent<SpriteRenderer>().sprite;
                });
                var giver = traveler.GetComponent<QuestGiver>() ?? traveler.AddComponent<QuestGiver>();
                Edit(giver, data =>
                {
                    data.FindProperty("quest").objectReferenceValue = quest;
                    data.FindProperty("offerDialogue").objectReferenceValue = Dialogue("Offer", character, "이 사찰의 인삼 줄기는 약효가 뛰어나다네. 인삼 몬스터에게서 줄기 15개만 구해 주겠나? 가져오면 몸을 회복시켜 주는 인삼 줄기 반지를 주겠네.");
                    data.FindProperty("acceptedDialogue").objectReferenceValue = Dialogue("Accepted", character, "고맙네. 인삼 줄기 15개를 모으면 내게 돌아오게. 줄기는 내가 받아 약으로 쓰겠네.");
                    data.FindProperty("declinedDialogue").objectReferenceValue = Dialogue("Declined", character, "괜찮네. 마음이 바뀌면 다시 찾아오게.");
                    data.FindProperty("progressDialogue").objectReferenceValue = Dialogue("Progress", character, "인삼 줄기 15개가 필요하네. 인삼 몬스터를 쓰러뜨리고 떨어진 줄기를 챙겨 오게.");
                    data.FindProperty("completionDialogue").objectReferenceValue = Dialogue("Completion", character, "줄기 15개를 모두 가져왔군! 약속한 인삼 줄기 반지일세. 장착하면 1초마다 체력을 1 회복시켜 줄 걸세.");
                    data.FindProperty("RegularDialogue").objectReferenceValue = Dialogue("Regular", character, "덕분에 약을 만들 수 있게 되었네. 반지가 자네의 여정에 도움이 되길 바라네.");
                    data.FindProperty("allowQuestOffer").boolValue = true;
                    data.FindProperty("allowQuestTurnIn").boolValue = true;
                });
                if (traveler.GetComponent<Collider2D>() == null)
                {
                    var collider = traveler.AddComponent<BoxCollider2D>();
                    var sprite = traveler.GetComponent<SpriteRenderer>().sprite;
                    collider.isTrigger = true;
                    collider.offset = sprite.bounds.center;
                    collider.size = sprite.bounds.size;
                }
                var reference = village.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<QuestAvailabilityIndicator>(true)).First();
                var source = new SerializedObject(reference);
                var indicator = traveler.GetComponent<QuestAvailabilityIndicator>() ?? traveler.AddComponent<QuestAvailabilityIndicator>();
                var existingRoot = new SerializedObject(indicator).FindProperty("indicatorRoot").objectReferenceValue as GameObject;
                var root = existingRoot ?? (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/QuestStatusIndicator.prefab"), traveler.transform);
                Edit(indicator, data =>
                {
                    data.FindProperty("questGiver").objectReferenceValue = giver;
                    data.FindProperty("indicatorRoot").objectReferenceValue = root;
                    data.FindProperty("indicatorImage").objectReferenceValue = root.GetComponentInChildren<Image>(true);
                    foreach (string field in new[] { "availableSprite", "activeSprite", "readyToTurnInSprite" }) data.FindProperty(field).objectReferenceValue = source.FindProperty(field).objectReferenceValue;
                    data.FindProperty("heightOffset").floatValue = source.FindProperty("heightOffset").floatValue;
                });
                root.transform.position = traveler.transform.position + Vector3.up * (traveler.GetComponent<SpriteRenderer>().bounds.extents.y + .9f);
                EditorSceneManager.MarkSceneDirty(temple);
                EditorSceneManager.SaveScene(temple);
                AssetDatabase.SaveAssets();
                SaveAssetCatalogBuilder.Rebuild();
            }
            finally
            {
                if (openedVillage) EditorSceneManager.CloseScene(village, true);
                if (openedTemple) EditorSceneManager.CloseScene(temple, true);
                SceneManager.SetActiveScene(previous);
            }
        }

        private static DialogueSequence Dialogue(string name, DialogueCharacter character, string text)
        {
            var sequence = Asset<DialogueSequence>(Folder + "/Traveler" + name + ".asset");
            Edit(sequence, data =>
            {
                data.FindProperty("partnerCharacter").objectReferenceValue = character;
                var lines = data.FindProperty("lines"); lines.arraySize = 1;
                lines.GetArrayElementAtIndex(0).FindPropertyRelative("speaker").enumValueIndex = (int)DialogueSpeakerSide.Partner;
                lines.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue = text;
            });
            return sequence;
        }
    }
}
