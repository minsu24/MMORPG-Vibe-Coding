using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class MapDetailDiscoverySetup
    {
        [MenuItem("Eastern Fantasy/UI/Setup Detail Map Discovery")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Run setup outside Play mode.");
            const string materialPath = "Assets/_Project/Material/MapDetailFog.mat";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Project/Effect/MapInkReveal.shader");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material,materialPath); }
            material.SetFloat("_DetailFogEnabled",1); material.SetFloat("_Reveal",1); EditorUtility.SetDirty(material);
            const string path = "Assets/_Project/Prefabs/UI/MapWindow.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var window = new SerializedObject(root.GetComponent<MapWindowUI>());
                var discovery = root.GetComponent<MapDetailDiscoveryUI>();
                if (discovery == null) discovery = root.AddComponent<MapDetailDiscoveryUI>();
                var data = new SerializedObject(discovery);
                data.FindProperty("detailImage").objectReferenceValue = window.FindProperty("detailImage").objectReferenceValue;
                data.FindProperty("fogTemplate").objectReferenceValue = material;
                data.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Detail maps now reveal scenery and names per visited scene.");
        }
    }
}
