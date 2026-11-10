using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class MonsterNameplateSetup
    {
        public const string Prefab = "Assets/_Project/Resources/UI/MonsterNameplate.prefab";
        [MenuItem("Eastern Fantasy/UI/Build Monster Nameplate")]
        public static void Build()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            if(!AssetDatabase.IsValidFolder("Assets/_Project/Resources/UI"))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "UI");
            var root = new GameObject("MonsterNameplate",typeof(RectTransform),typeof(Image),typeof(MonsterNameplateView));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
                rect.pivot = new Vector2(.5f,1f);
                rect.sizeDelta = new Vector2(120,30);
                var background = root.GetComponent<Image>();
                background.color = new Color(0,0,0,.65f);
                background.raycastTarget = false;
                var child = new GameObject("Name",typeof(RectTransform),typeof(TextMeshProUGUI));
                child.transform.SetParent(root.transform,false);
                var textRect = (RectTransform)child.transform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(12,2); textRect.offsetMax = new Vector2(-12,-2);
                var text = child.GetComponent<TextMeshProUGUI>();
                text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
                text.fontSize = 18;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.richText = false;
                text.raycastTarget = false;
                root.GetComponent<MonsterNameplateView>().Configure(text,
                    AssetDatabase.LoadAssetAtPath<MonsterCodexCatalog>("Assets/_Project/Data/MonsterCodex.asset"));
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
                AssetDatabase.SaveAssets();
            }
            finally {Object.DestroyImmediate(root);}
        }
    }
}
