using System;
using System.Linq;
using EasternFantasy.Inventory;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
namespace EasternFantasy.EditorTools
{
    public static class ItemGradeSetup
    {
        private const string Art = "Assets/_Project/Art/Item/Grades/";
        private const string Data = "Assets/_Project/Data/Items/Grades/";
        private static readonly string[] Names = { "OldFan", "OldHat", "ClothTop", "ClothBottom", "ClothGloves", "ClothShoes" };
        private static readonly string[] Grades = { "Common", "Rare", "Heroic", "Legendary", "Mythic" };
        [MenuItem("Eastern Fantasy/Inventory/Setup Item Grades")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Data.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/_Project/Data/Items", "Grades");
            Rect[] common = {
                new Rect(5, 180, 345, 355), new Rect(370, 190, 367, 380),
                new Rect(756,190,360,335), new Rect(1136,190,310,335),
                new Rect(25,625,320,325), new Rect(370,655,360,315)
            };
            Sprite[][] icons = new Sprite[5][];
            icons[0] = Slice("Common", Names, common);
            for (int g=1; g<5; g++)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + Grades[g] + ".png");
                if (tex == null) throw new InvalidOperationException("Missing atlas: " + Grades[g]);
                Rect[] rects = new Rect[6];
                // Per-column row boundaries keep the gat ribbon intact without capturing the gloves.
                float coatX = g == 2 ? 995 : g == 1 ? 1017 : 1025;
                float fanBottom = g <= 2 ? 525 : 535;
                float hatBottom = g == 2 ? 545 : g == 1 ? 555 : 565;
                rects[0] = new Rect(0,0,510,fanBottom);
                rects[1] = new Rect(510,0,coatX-510,hatBottom);
                rects[2] = new Rect(coatX,0,tex.width-coatX,530);
                rects[3] = new Rect(0,fanBottom,560,tex.height-fanBottom);
                rects[4] = new Rect(560,hatBottom,465,tex.height-hatBottom);
                rects[5] = new Rect(1025,530,tex.width-1025,tex.height-530);
                icons[g] = Slice(Grades[g], Names, rects);
            }
            Sprite[] frames = Slice("Frames", Grades, new[] {
                new Rect(30,110,438,438), new Rect(478,110,447,438), new Rect(933,110,445,438),
                new Rect(232,572,452,440), new Rect(718,572,456,440)
            });
            for (int i=0; i<6; i++)
            {
                ItemGradeAppearance appearance = LoadOrCreate<ItemGradeAppearance>(Data + Names[i] + "Appearance.asset");
                SerializedObject ap = new SerializedObject(appearance);
                AssignArray(ap.FindProperty("icons"), Enumerable.Range(0,5).Select(g => (UnityEngine.Object)icons[g][i]).ToArray());
                AssignArray(ap.FindProperty("frames"), frames.Cast<UnityEngine.Object>().ToArray());
                ap.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(appearance);
                ItemDefinition original = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/" + Names[i] + ".asset");
                ItemDefinition[] variants = new ItemDefinition[5];
                variants[0] = original;
                for (int g=1; g<5; g++)
                {
                    string path = Data + Names[i] + "_" + Grades[g] + ".asset";
                    variants[g] = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                    if (variants[g] == null)
                    {
                        variants[g] = UnityEngine.Object.Instantiate(original);
                        variants[g].name = Names[i] + "_" + Grades[g];
                        AssetDatabase.CreateAsset(variants[g], path);
                        SerializedObject copy = new SerializedObject(variants[g]);
                        copy.FindProperty("itemId").stringValue = Guid.NewGuid().ToString("N");
                        copy.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                for (int g=0; g<5; g++)
                {
                    SerializedObject item = new SerializedObject(variants[g]);
                    item.FindProperty("grade").intValue = g;
                    item.FindProperty("gradeAppearance").objectReferenceValue = appearance;
                    item.FindProperty("icon").objectReferenceValue = icons[g][i];
                    AssignArray(item.FindProperty("gradeVariants"), variants.Cast<UnityEngine.Object>().ToArray());
                    item.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(variants[g]);
                }
            }
            ItemGradeAppearance other = LoadOrCreate<ItemGradeAppearance>(Data + "DefaultAppearance.asset");
            SerializedObject otherData = new SerializedObject(other);
            AssignArray(otherData.FindProperty("frames"), frames.Cast<UnityEngine.Object>().ToArray());
            otherData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(other);
            foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition", new[] {"Assets/_Project/Data/Items"}))
            {
                ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (item.Category != ItemCategory.Miscellaneous) continue;
                SerializedObject so = new SerializedObject(item);
                so.FindProperty("grade").intValue = 0;
                so.FindProperty("gradeAppearance").objectReferenceValue = other;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
            }
            AssetDatabase.SaveAssets();
            if (System.IO.File.Exists("Assets/_Project/Art/Item/ArmorSets/Sources/Mythic.png")) ArmorSetImageSetup.Run();
            ItemIconAlignmentSetup.Run();
            Debug.Log("Item grades configured: 30 equipment definitions, 5 frames; consumables excluded.");
        }
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,path); }
            return asset;
        }
        private static void AssignArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            property.arraySize = values.Length;
            for (int i=0; i<values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        // Input rectangles use top-left coordinates. Sprite IDs are preserved on reruns.
        private static Sprite[] Slice(string sheet, string[] names, Rect[] regions)
        {
            string path = Art + sheet + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing sheet: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var factories = new SpriteDataProviderFactories(); factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] old = provider.GetSpriteRects();
            SpriteRect[] rects = new SpriteRect[names.Length];
            for (int i=0;i<names.Length;i++)
            {
                Rect r = regions[i];
                var previous = old.FirstOrDefault(x => x.name == names[i]);
                rects[i] = new SpriteRect { name=names[i], rect=new Rect(r.x,texture.height-r.y-r.height,r.width,r.height),
                    alignment=(int)SpriteAlignment.Center, pivot=new Vector2(0.5f,0.5f),
                    spriteID=previous != null ? previous.spriteID : GUID.Generate() };
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(x => new SpriteNameFileIdPair(x.name,x.spriteID)));
            provider.Apply();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Sprite[] all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            return names.Select(n => all.First(s => s.name==n)).ToArray();
        }
    }
}
