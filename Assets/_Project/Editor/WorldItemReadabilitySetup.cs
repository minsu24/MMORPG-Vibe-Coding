using EasternFantasy.Inventory;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    // Keep this setup entry point aligned with the sprite-only loot presentation with a thin contour.
    public static class WorldItemReadabilitySetup
    {
        public static void Apply()
        {
            const string materialPath = "Assets/_Project/Material/LootItem.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) {
                material = new Material(Shader.Find("Eastern Fantasy/Loot Icon"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = Shader.Find("Eastern Fantasy/Loot Icon");
            material.SetColor("_OutlineColor", new Color(.92f, .96f, 1f, .8f));
            material.SetFloat("_OutlineWidth", 1f);
            material.SetFloat("_Brightness", 1f);
            EditorUtility.SetDirty(material);
            const string path = "Assets/_Project/Prefabs/WorldItemPickup.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try {
                var decorations = root.GetComponent<WorldItemReadability>();
                if (decorations != null) Object.DestroyImmediate(decorations);
                root.GetComponent<SpriteRenderer>().sharedMaterial = material;
                var data = new SerializedObject(root.GetComponent<WorldItemPickup>());
                data.FindProperty("worldIconSize").floatValue = .95f;
                data.FindProperty("slenderWeaponScale").floatValue = 1.3f;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
