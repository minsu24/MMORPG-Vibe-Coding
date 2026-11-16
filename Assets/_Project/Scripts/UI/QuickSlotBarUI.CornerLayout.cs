using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed partial class QuickSlotBarUI
    {
        [Header("Gameplay settings")]
        [SerializeField] private SettingsMenuController settingsMenuPrefab;
        private SettingsMenuController activeHudSettings;

        public void ApplyCornerLayout()
        {
            var currencyLabel = transform.Find("Currency");
            if (currencyLabel != null) currencyLabel.gameObject.SetActive(false);
            if (currencyText != null)
            {
                // Some scene HUDs wrap the label in a separate decorative plate.
                // Hide that dedicated wrapper as well as the currency text.
                Transform currencyRoot = currencyText.transform;
                while (currencyRoot.parent != null && currencyRoot.parent != transform
                    && currencyRoot.parent.childCount == 1 && currencyRoot.parent.GetComponent<Image>() != null)
                    currencyRoot = currencyRoot.parent;
                currencyRoot.gameObject.SetActive(false);
            }
            var child = transform.Find("HUD Settings Button");
            if (child == null)
            {
                var go = new GameObject("HUD Settings Button", typeof(RectTransform), typeof(Image), typeof(Button));
                go.layer = gameObject.layer; go.transform.SetParent(transform, false); child = go.transform;
            }
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.sizeDelta = new Vector2(132f, 36f); rect.anchoredPosition = new Vector2(-12f, -12f);
            var image = child.GetComponent<Image>();
            var panel = transform.Find("QuickSlots/SkillPanel")?.GetComponent<Image>();
            image.sprite = panel != null ? panel.sprite : null;
            image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2.8f;
            image.color = image.sprite != null ? Color.white : new Color(.055f, .07f, .08f, .96f);
            image.raycastTarget = true;
            var button = child.GetComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.RemoveListener(OpenHudSettings); button.onClick.AddListener(OpenHudSettings);
            var label = child.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.layer = gameObject.layer; go.transform.SetParent(child, false); label = go.GetComponent<TextMeshProUGUI>();
            }
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            if (currencyText != null) label.font = currencyText.font;
            label.text = "설정"; label.fontSize = 20; label.color = new Color(.94f, .81f, .53f);
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            child.SetAsLastSibling();
        }

        public void OpenHudSettings()
        {
            if (GameplayHudVisibility.IsMenuScene || SceneLoadingScreen.IsVisible) return;
            if (activeHudSettings == null)
            {
                foreach (var candidate in FindObjectsByType<SettingsMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate.gameObject.scene.IsValid() && candidate.isActiveAndEnabled) { activeHudSettings = candidate; break; }
                if (activeHudSettings == null && settingsMenuPrefab != null)
                {
                    activeHudSettings = Instantiate(settingsMenuPrefab, transform, false);
                    activeHudSettings.name = "Gameplay Settings";
                    var rect = activeHudSettings.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                if (activeHudSettings == null)
                {
                    Debug.LogError("The gameplay HUD needs its Settings Menu prefab.", this); return;
                }
                var canvas = activeHudSettings.GetComponent<Canvas>();
                if (canvas == null) canvas = activeHudSettings.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true; canvas.sortingOrder = 1000;
                if (activeHudSettings.GetComponent<GraphicRaycaster>() == null) activeHudSettings.gameObject.AddComponent<GraphicRaycaster>();
            }
            activeHudSettings.Open();
        }
    }
}
