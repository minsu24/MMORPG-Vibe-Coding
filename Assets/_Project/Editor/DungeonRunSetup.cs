using System;
using System.Linq;
using EasternFantasy.Dungeon;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class DungeonRunSetup
    {
        private static TMP_FontAsset font;
        private static readonly Color Gold = new Color(0.96f, 0.84f, 0.63f);

        [MenuItem("Eastern Fantasy/Setup Dungeon Timer and Exit")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode before setup.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
            var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/UI/죽음 UI.png").OfType<Sprite>().ToArray();
            var frame = sprites.First(s => s.name.EndsWith("_1"));
            var normal = sprites.First(s => s.name.EndsWith("_2"));
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources")) AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            var root = new GameObject("DungeonRunUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder = 900;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                var ui = root.AddComponent<DungeonRunUI>();
                root.AddComponent<DungeonRunController>().ui = ui;
                var hud = Picture("TopTimer", root.transform, normal, new Vector2(0, -70), new Vector2(300, 90));
                hud.rectTransform.anchorMin = hud.rectTransform.anchorMax = new Vector2(0.5f, 1);
                ui.timerCaption = Text("TimerCaption", hud.transform, "던전 제한시간", new Vector2(0, 22), new Vector2(250, 28), 19);
                ui.timer = Text("Timer", hud.transform, "10:00", new Vector2(0, -13), new Vector2(250, 46), 34);
                var shade = Picture("ExitPrompt", root.transform, null, Vector2.zero, Vector2.zero);
                shade.color = new Color(0, 0, 0, 0.55f);
                shade.raycastTarget = true;
                Stretch(shade.rectTransform);
                ui.promptRoot = shade.gameObject;
                var panel = Picture("DeathArtFrame", shade.transform, frame, Vector2.zero, new Vector2(1100, 540));
                panel.raycastTarget = true;
                ui.exitTimer = Text("ExitCountdown", panel.transform, "00:30", new Vector2(0, 115), new Vector2(500, 64), 48);
                Text("Question", panel.transform, "던전을 퇴장하시겠습니까?", new Vector2(0, 20), new Vector2(840, 80), 40);
                Text("Notice", panel.transform, "남은 시간이 끝나면 자동으로 퇴장합니다.", new Vector2(0, -52), new Vector2(840, 44), 24);
                ui.noButton = MakeButton("No", panel.transform, normal, "아니오", -210);
                ui.yesButton = MakeButton("Yes", panel.transform, normal, "예", 210);
                var navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = ui.yesButton, selectOnLeft = ui.yesButton };
                ui.noButton.navigation = navigation;
                navigation.selectOnLeft = navigation.selectOnRight = ui.noButton;
                ui.yesButton.navigation = navigation;
                shade.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Resources/DungeonRunUI.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            var definition = AssetDatabase.LoadAssetAtPath<DungeonDefinition>("Assets/_Project/Data/Dungeons/GiantGarden.asset");
            foreach (string name in new[] { definition.sceneName }.Concat(definition.continuationScenes))
            {
                string path = "Assets/_Project/Scenes/GiantFarm/" + name + ".unity";
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var marker = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DungeonSceneController>(true)).FirstOrDefault();
                if (marker == null)
                {
                    var go = new GameObject("Dungeon Run Settings");
                    SceneManager.MoveGameObjectToScene(go, scene);
                    marker = go.AddComponent<DungeonSceneController>();
                }
                marker.definition = definition;
                EditorUtility.SetDirty(marker);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Dungeon timer and exit UI installed for all three Giant Farm scenes.");
        }

        private static Image Picture(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Gold;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(string name, Transform parent, Sprite sprite, string label, float x)
        {
            var image = Picture(name, parent, sprite, new Vector2(x, -150), new Vector2(340, 87));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.4f, 1.25f, 1f);
            colors.selectedColor = new Color(1.15f, 0.85f, 0.5f);
            colors.pressedColor = new Color(0.85f, 0.55f, 0.3f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            Text("Label", image.transform, label, Vector2.zero, new Vector2(300, 65), 32);
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
