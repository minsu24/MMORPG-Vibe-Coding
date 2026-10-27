using System;
using System.Collections.Generic;
using System.Linq;
using EasternFantasy.Inventory;
using EasternFantasy.EquipmentUpgrade;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.EditorTools
{
    public static class TutorialGoblinLootSetup
    {
        public const string Folder = "Assets/_Project/Data/Items/Tutorial";
        private const string Items = "Assets/_Project/Data/Items/";
        private const string ScenePath = "Assets/_Project/Scenes/TutorialMap/MovementPrototype.unity";
        public static readonly string[] EquipmentNames = {
            "OldFan", "MonkGauntlets", "SwordswomanSword"
        };

        public static ItemDefinition Item(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>(Items + name + ".asset");
        public static ItemDefinition Tutorial(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>(Folder + "/" + name + ".asset");

        [MenuItem("Eastern Fantasy/Inventory/Setup Tutorial Goblin Loot")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode before setup.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Items.TrimEnd('/'), "Tutorial");
            var club = Item("GoblinClub");
            if (club == null) {
                club = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(club, Items + "GoblinClub.asset");
            }
            var clubData = new SerializedObject(club);
            clubData.FindProperty("displayName").stringValue = "도깨비 방망이";
            clubData.FindProperty("description").stringValue = "도깨비가 사용하던 가시 방망이. 모든 직업의 일반 무기를 레어로 승급할 때 영혼 조각 1개와 함께 사용합니다.";
            clubData.FindProperty("icon").objectReferenceValue = ImportClubIcon();
            clubData.FindProperty("category").intValue = (int)ItemCategory.Miscellaneous;
            clubData.FindProperty("grade").intValue = (int)ItemGrade.Rare;
            clubData.FindProperty("equipmentSlot").intValue = (int)EquipmentSlot.None;
            clubData.FindProperty("gradeAppearance").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemGradeAppearance>(Items + "Grades/DefaultAppearance.asset");
            clubData.FindProperty("cannotSell").boolValue = true;
            clubData.FindProperty("maximumStack").intValue = 99;
            clubData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(club);

            var fragment = Item("SoulFragment");
            var fragmentData = new SerializedObject(fragment);
            fragmentData.FindProperty("cannotSell").boolValue = false;
            fragmentData.FindProperty("buyPrice").intValue = 2;
            fragmentData.FindProperty("sellPrice").intValue = 1;
            fragmentData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fragment);
            var drops = new List<ItemDefinition> { Bound(Item("SoulOfGoblin")), Bound(fragment), club };
            foreach (string name in EquipmentNames)
            {
                var original = Item(name);
                var variants = new List<ItemDefinition>();
                for (int grade = 0; grade <= (int)ItemGrade.Mythic; grade++)
                {
                    var source = original.GetGradeVariant((ItemGrade)grade);
                    if (source != null) variants.Add(Bound(source));
                }
                foreach (var variant in variants)
                {
                    var data = new SerializedObject(variant);
                    var family = data.FindProperty("gradeVariants");
                    family.arraySize = variants.Count;
                    for (int i = 0; i < variants.Count; i++) family.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
                    data.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(variant);
                }
                drops.Add(variants[0]);
            }
            var recipe = AssetDatabase.LoadAssetAtPath<EquipmentUpgradeRecipe>(Items + "UpgradeRecipes/Weapon_Common_To_Rare.asset");
            var recipeData = new SerializedObject(recipe);
            recipeData.FindProperty("configured").boolValue = true;
            recipeData.FindProperty("costYeopjeon").intValue = 0;
            var materials = recipeData.FindProperty("materials"); materials.arraySize = 2;
            var ingredients = new[] { club, fragment };
            for (int i = 0; i < ingredients.Length; i++) {
                var element = materials.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = ingredients[i];
                element.FindPropertyRelative("quantity").intValue = 1;
            }
            recipeData.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(recipe);

            var scene = SceneManager.GetActiveScene();
            bool preview = scene.path != ScenePath;
            if (preview) scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try {
                var goblin = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_Goblin>(true)).Single();
                var loot = goblin.GetComponent<EnemyLootDrop>();
                if (loot == null) loot = goblin.gameObject.AddComponent<EnemyLootDrop>();
                var data = new SerializedObject(loot);
                data.FindProperty("overridesCommonLoot").boolValue = true;
                data.FindProperty("filterWeaponsByPlayerClass").boolValue = true;
                data.FindProperty("pickupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/WorldItemPickup.prefab").GetComponent<WorldItemPickup>();
                var entries = data.FindProperty("drops"); entries.arraySize = drops.Count;
                for (int i = 0; i < drops.Count; i++) {
                    var entry = entries.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("item").objectReferenceValue = drops[i];
                    entry.FindPropertyRelative("dropChance").floatValue = 1;
                    entry.FindPropertyRelative("minimumQuantity").intValue = 1;
                    entry.FindPropertyRelative("maximumQuantity").intValue = 1;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally { if (preview) EditorSceneManager.ClosePreviewScene(scene); }
            AssetDatabase.SaveAssets();
            Debug.Log("Tutorial goblin: class-specific common weapon plus 3 guaranteed unsellable materials; all-class weapon upgrade: club + fragment, one each.");
        }

        private static ItemDefinition Bound(ItemDefinition source)
        {
            string path = Folder + "/" + source.name + ".asset";
            var result = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            string id = result != null ? result.ItemId : Guid.NewGuid().ToString("N");
            if (result == null) {
                result = UnityEngine.Object.Instantiate(source);
                AssetDatabase.CreateAsset(result, path);
            } else EditorUtility.CopySerialized(source, result);
            result.name = source.name;
            var data = new SerializedObject(result);
            data.FindProperty("itemId").stringValue = id;
            data.FindProperty("cannotSell").boolValue = true;
            data.FindProperty("materialSource").objectReferenceValue = source.Category == ItemCategory.Miscellaneous ? source : null;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(result);
            return result;
        }

        private static Sprite ImportClubIcon()
        {
            const string path = "Assets/_Project/Art/Items/GoblinClub.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try {
                image.LoadImage(System.IO.File.ReadAllBytes(path));
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                var old = provider.GetSpriteRects().FirstOrDefault();
                var rect = new SpriteRect { name = "GoblinClub", rect = ItemIconAlignmentSetup.VisibleBounds(image.GetPixels32(), image.width, image.height, new Rect(0, 0, image.width, image.height)),
                    alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f), spriteID = old != null ? old.spriteID : GUID.Generate() };
                provider.SetSpriteRects(new[] { rect });
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(rect.name, rect.spriteID) });
                provider.Apply();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
        }
    }
}
