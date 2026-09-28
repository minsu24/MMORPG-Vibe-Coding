using System;
using System.Linq;
using EasternFantasy.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class FrogLootSetup
    {
        private const string ItemFolder = "Assets/_Project/Data/Items";
        private const string ArtPath =
            "Assets/_Project/Art/Item/두꺼비 구슬과 봉인의 흔적.png";
        private const string PickupPrefabPath =
            "Assets/_Project/Prefabs/WorldItemPickup.prefab";
        private const string MarshlandScenePath =
            "Assets/_Project/Scenes/Marshland.unity";

        [MenuItem("Eastern Fantasy/Setup Marshland Frog Loot")]
        public static void Apply()
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(ArtPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
            if (sprites.Length < 2)
                throw new MissingReferenceException(
                    "The frog loot texture needs two sliced sprites.");

            ItemDefinition frogOrb = GetOrCreateItem(
                "FrogOrb",
                "두꺼비 구슬",
                "두꺼비의 기운이 응축된 구슬입니다. 장비 제작 재료로 사용할 수 있습니다.",
                sprites[0]);
            ItemDefinition brokenSealTrace = GetOrCreateItem(
                "BrokenSealTrace",
                "깨진 봉인의 흔적",
                "깨진 봉인에서 흘러나온 기운이 굳어 남은 흔적입니다.",
                sprites[1]);
            WorldItemPickup pickupPrefab = GetOrCreatePickupPrefab();

            Scene marshland = SceneManager.GetSceneByPath(MarshlandScenePath);
            bool openedForSetup = !marshland.IsValid() || !marshland.isLoaded;
            if (openedForSetup)
                marshland = EditorSceneManager.OpenScene(
                    MarshlandScenePath,
                    OpenSceneMode.Additive);

            try
            {
                MON_Frog frog = marshland.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MON_Frog>(true))
                    .FirstOrDefault();
                if (frog == null)
                    throw new MissingReferenceException(
                        "Marshland scene needs one MON_Frog.");

                EnemyLootDrop loot = frog.GetComponent<EnemyLootDrop>();
                if (loot == null)
                    loot = frog.gameObject.AddComponent<EnemyLootDrop>();

                SerializedObject data = new SerializedObject(loot);
                data.FindProperty("pickupPrefab").objectReferenceValue = pickupPrefab;
                data.FindProperty("spawnHeight").floatValue = 0.4f;
                data.FindProperty("horizontalScatter").floatValue = 0.7f;

                SerializedProperty drops = data.FindProperty("drops");
                drops.arraySize = 2;
                ConfigureDrop(drops.GetArrayElementAtIndex(0), frogOrb);
                ConfigureDrop(drops.GetArrayElementAtIndex(1), brokenSealTrace);
                data.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(marshland);
                EditorSceneManager.SaveScene(marshland);
            }
            finally
            {
                if (openedForSetup && marshland.IsValid() && marshland.isLoaded)
                    EditorSceneManager.CloseScene(marshland, true);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Marshland Frog now drops Frog Orb and Broken Seal Trace.");
        }

        private static ItemDefinition GetOrCreateItem(
            string fileName,
            string displayName,
            string description,
            Sprite icon)
        {
            string path = $"{ItemFolder}/{fileName}.asset";
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }

            SerializedObject data = new SerializedObject(item);
            SerializedProperty id = data.FindProperty("itemId");
            if (string.IsNullOrWhiteSpace(id.stringValue))
                id.stringValue = Guid.NewGuid().ToString("N");
            data.FindProperty("displayName").stringValue = displayName;
            data.FindProperty("description").stringValue = description;
            data.FindProperty("icon").objectReferenceValue = icon;
            data.FindProperty("category").enumValueIndex =
                (int)ItemCategory.Miscellaneous;
            data.FindProperty("equipmentSlot").enumValueIndex =
                (int)EquipmentSlot.None;
            data.FindProperty("consumableEffect").enumValueIndex =
                (int)ConsumableEffect.None;
            data.FindProperty("maximumStack").intValue = 99;
            data.FindProperty("buyPrice").intValue = 0;
            data.FindProperty("sellPrice").intValue = 0;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static WorldItemPickup GetOrCreatePickupPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            if (existing != null)
            {
                WorldItemPickup pickup = existing.GetComponent<WorldItemPickup>();
                SerializedObject data = new SerializedObject(pickup);
                data.FindProperty("pickupRadius").floatValue = 0.45f;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pickup);
                return pickup;
            }

            GameObject root = new GameObject(
                "WorldItemPickup",
                typeof(SpriteRenderer),
                typeof(CircleCollider2D),
                typeof(WorldItemPickup));
            try
            {
                CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.4f;
                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                renderer.sortingOrder = 8;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PickupPrefabPath);
                return prefab.GetComponent<WorldItemPickup>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureDrop(
            SerializedProperty drop,
            ItemDefinition item)
        {
            drop.FindPropertyRelative("item").objectReferenceValue = item;
            drop.FindPropertyRelative("dropChance").floatValue = 1f;
            drop.FindPropertyRelative("minimumQuantity").intValue = 1;
            drop.FindPropertyRelative("maximumQuantity").intValue = 1;
        }
    }
}
