using System;
using System.Linq;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Inventory;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class SwordswomanSwordSetup
    {
        public const string CommonPath = "Assets/_Project/Data/Items/SwordswomanSword.asset";
        private const string Data = "Assets/_Project/Data/Items/Grades/";
        private const string Art = "Assets/_Project/Art/Item/SwordswomanSword/";
        private static readonly string[] Grades = { "Common", "Rare", "Heroic", "Legendary", "Mythic" };
        [MenuItem("Eastern Fantasy/Inventory/Setup Swordswoman Sword")]
        public static void Run()
        {
            var fan = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/OldFan.asset");
            var appearance = AssetDatabase.LoadAssetAtPath<ItemGradeAppearance>(Data+"SwordswomanSwordAppearance.asset");
            if (appearance == null)
            {
                appearance = ScriptableObject.CreateInstance<ItemGradeAppearance>();
                AssetDatabase.CreateAsset(appearance,Data+"SwordswomanSwordAppearance.asset");
            }
            var appearanceData = new SerializedObject(appearance);
            appearanceData.FindProperty("icons").arraySize=5;
            appearanceData.FindProperty("frames").arraySize=5;
            var variants = new ItemDefinition[5];
            for(int g=0;g<5;g++)
            {
                var sprite=ImportIcon(Art+Grades[g]+".png");
                appearanceData.FindProperty("icons").GetArrayElementAtIndex(g).objectReferenceValue=sprite;
                appearanceData.FindProperty("frames").GetArrayElementAtIndex(g).objectReferenceValue=fan.GetGradeVariant((ItemGrade)g).GradeFrame;
                string path=g==0?CommonPath:Data+"SwordswomanSword_"+Grades[g]+".asset";
                var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if(item==null)
                {
                    item=UnityEngine.Object.Instantiate(fan.GetGradeVariant((ItemGrade)g));
                    item.name="SwordswomanSword"+(g==0?"":"_"+Grades[g]);
                    AssetDatabase.CreateAsset(item,path);
                    var itemData=new SerializedObject(item);
                    itemData.FindProperty("itemId").stringValue=Guid.NewGuid().ToString("N");
                    itemData.FindProperty("displayName").stringValue="검";
                    itemData.FindProperty("description").stringValue="여검사 전용 검. 수련자 세트의 이동속도 증가 효과를 받습니다.";
                    itemData.ApplyModifiedPropertiesWithoutUndo();
                }
                variants[g]=item;
                var so=new SerializedObject(item);
                so.FindProperty("category").enumValueIndex=(int)ItemCategory.Equipment;
                so.FindProperty("equipmentSlot").enumValueIndex=(int)EquipmentSlot.Weapon;
                so.FindProperty("grade").intValue=g;
                so.FindProperty("icon").objectReferenceValue=sprite;
                so.FindProperty("gradeAppearance").objectReferenceValue=appearance;
                so.FindProperty("equipmentSet").objectReferenceValue=fan.EquipmentSet;
                SetClass(so,CharacterClassId.Swordswoman);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
            }
            appearanceData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(appearance);
            foreach(var item in variants)
            {
                var data=new SerializedObject(item);
                var links=data.FindProperty("gradeVariants");links.arraySize=5;
                for(int g=0;g<5;g++)links.GetArrayElementAtIndex(g).objectReferenceValue=variants[g];
                data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(item);
            }
            AssetDatabase.SaveAssets();

            Debug.Log("Five swordswoman sword grades ready; weapon recipes, class restrictions and training set connected.");
        }
        private static void SetClass(SerializedObject data, CharacterClassId characterClass)
        {
            var classes=data.FindProperty("allowedClasses");classes.arraySize=1;
            classes.GetArrayElementAtIndex(0).intValue=(int)characterClass;
        }
        private static Sprite ImportIcon(string path)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                texture.LoadImage(System.IO.File.ReadAllBytes(path));
                Rect bounds=ItemIconAlignmentSetup.VisibleBounds(texture.GetPixels32(),texture.width,texture.height,new Rect(0,0,texture.width,texture.height));
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var old=provider.GetSpriteRects().FirstOrDefault(s=>s.name=="SwordswomanSword");
                var sprite=new SpriteRect {name="SwordswomanSword",rect=bounds,pivot=new Vector2(.5f,.5f),
                    alignment=(int)SpriteAlignment.Center,spriteID=old!=null?old.spriteID:GUID.Generate()};
                provider.SetSpriteRects(new[]{sprite});
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(sprite.name,sprite.spriteID)});
                provider.Apply();
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
