using System;
using System.Linq;
using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Inventory;
using EasternFantasy.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.EditorTools
{
    public static class MonkGauntletsSetup
    {
        public const string CommonPath = "Assets/_Project/Data/Items/MonkGauntlets.asset";
        private const string Data = "Assets/_Project/Data/Items/Grades/";
        private const string Art = "Assets/_Project/Art/Item/MonkGauntlets/";
        private static readonly string[] Grades = { "Common", "Rare", "Heroic", "Legendary", "Mythic" };
        [MenuItem("Eastern Fantasy/Inventory/Setup Monk Gauntlets")]
        public static void Run()
        {
            var fan = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/OldFan.asset");
            var appearance = AssetDatabase.LoadAssetAtPath<ItemGradeAppearance>(Data+"MonkGauntletsAppearance.asset");
            if (appearance == null)
            {
                appearance = ScriptableObject.CreateInstance<ItemGradeAppearance>();
                AssetDatabase.CreateAsset(appearance,Data+"MonkGauntletsAppearance.asset");
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
                string path=g==0?CommonPath:Data+"MonkGauntlets_"+Grades[g]+".asset";
                var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if(item==null)
                {
                    item=UnityEngine.Object.Instantiate(fan.GetGradeVariant((ItemGrade)g));
                    item.name="MonkGauntlets"+(g==0?"":"_"+Grades[g]);
                    AssetDatabase.CreateAsset(item,path);
                    var itemData=new SerializedObject(item);
                    itemData.FindProperty("itemId").stringValue=Guid.NewGuid().ToString("N");
                    itemData.FindProperty("displayName").stringValue="권갑";
                    itemData.FindProperty("description").stringValue="수도승의 권법을 위한 무기. 양손을 감싸 타격을 강화합니다.";
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
                SetClass(so,CharacterClassId.Monk);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
                var fanVariant=fan.GetGradeVariant((ItemGrade)g);
                var fanData=new SerializedObject(fanVariant);
                SetClass(fanData,CharacterClassId.Dosa);fanData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(fanVariant);
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
            ConnectShop(variants[0],fan);
            Debug.Log("Five monk gauntlet grades ready; weapon recipes, class restrictions, shop and training set connected.");
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
                var old=provider.GetSpriteRects().FirstOrDefault(s=>s.name=="MonkGauntlets");
                var sprite=new SpriteRect {name="MonkGauntlets",rect=bounds,pivot=new Vector2(.5f,.5f),
                    alignment=(int)SpriteAlignment.Center,spriteID=old!=null?old.spriteID:GUID.Generate()};
                provider.SetSpriteRects(new[]{sprite});
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(sprite.name,sprite.spriteID)});
                provider.Apply();
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
        private static void ConnectShop(ItemDefinition gauntlets,ItemDefinition fan)
        {
            const string path="Assets/_Project/Scenes/PrologueVilage.unity";
            var scene=SceneManager.GetSceneByPath(path);
            bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            bool changed=false;
            try
            {
                foreach(var npc in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Shopkeeper>(true)))
                {
                    var stock=new List<ItemDefinition>(npc.Stock??Array.Empty<ItemDefinition>());
                    int index=stock.IndexOf(fan);
                    if(index<0)continue;
                    stock.RemoveAll(i=>i==gauntlets);index=stock.IndexOf(fan);
                    stock.Insert(index+1,gauntlets);
                    var data=new SerializedObject(npc);var array=data.FindProperty("stock");array.arraySize=stock.Count;
                    for(int i=0;i<stock.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=stock[i];
                    data.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
                    changed=true;
                }
                if(!changed)throw new Exception("Prologue village shop with OldFan was not found.");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}