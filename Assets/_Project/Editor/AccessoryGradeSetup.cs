using System;
using System.Linq;
using EasternFantasy.Inventory;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class AccessoryGradeSetup
    {
        private const string FrameSheet = "Assets/_Project/Art/Item/장비 등급 프레임.png";
        private const string PotionSheet = "Assets/_Project/Art/Item/튜토리얼 장비 찐.png";
        private const string AppearancePath = "Assets/_Project/Resources/UI/ItemGradeFrames.asset";

        [MenuItem("Eastern Fantasy/Inventory/Apply Accessory and Potion Grade Frames")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Apply item grade data outside Play mode.");
            // Correct the source sheet's slice bounds without replacing its existing sprite identities.
            var importer = (TextureImporter)AssetImporter.GetAtPath(FrameSheet);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects = provider.GetSpriteRects();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(FrameSheet);
            Rect[] regions = {
                new Rect(30,110,438,438), new Rect(478,110,447,438), new Rect(933,110,445,438),
                new Rect(232,572,452,440), new Rect(718,572,456,440)
            };
            for (int i = 0; i < regions.Length; i++)
            {
                var sprite = rects.Single(s => s.name.EndsWith("_" + i));
                var r = regions[i];
                sprite.rect = new Rect(r.x, texture.height - r.y - r.height, r.width, r.height);
                sprite.alignment = (int)SpriteAlignment.Center;
                sprite.pivot = Vector2.one * .5f;
            }
            provider.SetSpriteRects(rects);
            provider.Apply();
            AssetDatabase.ImportAsset(FrameSheet, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var frames = AssetDatabase.LoadAllAssetsAtPath(FrameSheet).OfType<Sprite>().ToArray();
            var appearance = AssetDatabase.LoadAssetAtPath<ItemGradeAppearance>(AppearancePath);
            if (appearance == null)
            {
                appearance = ScriptableObject.CreateInstance<ItemGradeAppearance>();
                AssetDatabase.CreateAsset(appearance, AppearancePath);
            }
            var data = new SerializedObject(appearance);
            var targets = data.FindProperty("frames");
            targets.arraySize = 5;
            for (int i = 0; i < 5; i++)
                targets.GetArrayElementAtIndex(i).objectReferenceValue = frames.Single(s => s.name.EndsWith("_" + i));
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(appearance);

            var potions = AssetDatabase.LoadAllAssetsAtPath(PotionSheet).OfType<Sprite>().ToArray();
            Configure("ToadBellNecklace", ItemGrade.Rare, appearance, null);
            Configure("HealthPotion", ItemGrade.Common, appearance, potions.Single(s => s.name.EndsWith("_6")));
            Configure("ManaPotion", ItemGrade.Common, appearance, potions.Single(s => s.name.EndsWith("_7")));
            AssetDatabase.SaveAssets();
        }

        private static void Configure(string name, ItemGrade grade, ItemGradeAppearance appearance, Sprite icon)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/" + name + ".asset");
            var data = new SerializedObject(item);
            data.FindProperty("grade").intValue = (int)grade;
            data.FindProperty("gradeAppearance").objectReferenceValue = appearance;
            if (icon != null) data.FindProperty("icon").objectReferenceValue = icon;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }
    }
}
