using System.Linq;
using EasternFantasy.Inventory;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class SoulFragmentSetup
    {
        public const string ItemPath = "Assets/_Project/Data/Items/SoulFragment.asset";
        public const string LootPath = "Assets/_Project/Resources/CommonMonsterLoot.asset";

        [MenuItem("Eastern Fantasy/Inventory/Setup Soul Fragment")]
        public static void Run()
        {
            const string artPath = "Assets/_Project/Art/Item/SoulFragment.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(artPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var raw = new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                raw.LoadImage(System.IO.File.ReadAllBytes(artPath));
                Rect bounds = ItemIconAlignmentSetup.VisibleBounds(raw.GetPixels32(),raw.width,raw.height,new Rect(0,0,raw.width,raw.height));
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var previous = provider.GetSpriteRects().FirstOrDefault(s=>s.name=="SoulFragment");
                var sprite = new SpriteRect { name="SoulFragment",rect=bounds,pivot=new Vector2(.5f,.5f),
                    alignment=(int)SpriteAlignment.Center,spriteID=previous!=null?previous.spriteID:GUID.Generate() };
                provider.SetSpriteRects(new[]{sprite});
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(sprite.name,sprite.spriteID)});
                provider.Apply();
                AssetDatabase.ImportAsset(artPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
            var icon = AssetDatabase.LoadAllAssetsAtPath(artPath).OfType<Sprite>().First();
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemPath);
            if(item==null)
            {
                item=ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item,ItemPath);
                var data=new SerializedObject(item);
                data.FindProperty("displayName").stringValue="영혼 조각";
                data.FindProperty("description").stringValue="몬스터의 영혼이 응축된 조각. 장비 승급과 동행요괴 성장에 사용하는 재료입니다.";
                data.FindProperty("category").enumValueIndex=(int)ItemCategory.Miscellaneous;
                data.FindProperty("equipmentSlot").enumValueIndex=(int)EquipmentSlot.None;
                data.FindProperty("grade").intValue=(int)ItemGrade.Common;
                data.FindProperty("maximumStack").intValue=99;
                data.FindProperty("buyPrice").intValue=0;
                data.FindProperty("sellPrice").intValue=0;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var appearance=new SerializedObject(item);
            appearance.FindProperty("icon").objectReferenceValue=icon;
            appearance.FindProperty("gradeAppearance").objectReferenceValue=
                AssetDatabase.LoadAssetAtPath<ItemGradeAppearance>("Assets/_Project/Data/Items/Grades/DefaultAppearance.asset");
            appearance.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(item);
            if(!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                AssetDatabase.CreateFolder("Assets/_Project","Resources");
            var loot=AssetDatabase.LoadAssetAtPath<CommonMonsterLoot>(LootPath);
            if(loot==null){loot=ScriptableObject.CreateInstance<CommonMonsterLoot>();AssetDatabase.CreateAsset(loot,LootPath);}
            var settings=new SerializedObject(loot);
            settings.FindProperty("item").objectReferenceValue=item;
            settings.FindProperty("pickupPrefab").objectReferenceValue=
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/WorldItemPickup.prefab").GetComponent<WorldItemPickup>();
            settings.FindProperty("dropChance").floatValue=.5f;
            settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(loot);
            AssetDatabase.SaveAssets();
            Debug.Log("Soul Fragment miscellaneous item ready. Every enemy rolls a 50% chance for one fragment.");
        }
    }
}