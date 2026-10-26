using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    // Share the HUD skin across persistent instances, including scenes with layout overrides.
    [DisallowMultipleComponent]
    public sealed class HudThemeUI : MonoBehaviour
    {
        [SerializeField] private Sprite panelFrame, slotFrame, headingFrame;
        private static readonly Color Gold = new Color(.94f, .81f, .53f);
        private static readonly Color Ink = new Color(.055f, .075f, .083f);
        private void Start() => Apply();
        public void Configure(Sprite panel, Sprite slot, Sprite heading)
        { panelFrame = panel; slotFrame = slot; headingFrame = heading; }

        public void Apply()
        {
            if (panelFrame == null || slotFrame == null) return;
            if (GetComponent<MinimapUI>() != null) ApplyMinimap();
            else ApplyQuickSlots();
        }
        private void ApplyQuickSlots()
        {
            var bar = transform.Find("QuickSlots");
            if (bar == null) return;
            StylePanel(bar.Find("SkillPanel"), "Skill Group Label", "스킬");
            StylePanel(bar.Find("ConsumablePanel"), "Consumable Group Label", "소모품");
            foreach (var slot in GetComponentsInChildren<QuickSkillSlotUI>(true)) StyleSlot(slot.transform, true);
            foreach (var slot in GetComponentsInChildren<QuickConsumableSlotUI>(true)) StyleSlot(slot.transform, false);
        }
        private void StylePanel(Transform panel, string labelName, string caption)
        {
            if (panel == null) return;
            Frame(panel.GetComponent<Image>());
            var label = panel.Find(labelName)?.GetComponent<TMP_Text>();
            panel.Find("HUD Heading")?.gameObject.SetActive(false);
            if (label != null)
            {
                SingleLine(label);
                Place(label.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -7), new Vector2(140, 23));
                label.text = caption; label.fontSize = 17; label.color = Gold;
                label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            }
        }
        private void StyleSlot(Transform slot, bool skill)
        {
            var rect = (RectTransform)slot;
            rect.sizeDelta = Vector2.one * (skill ? 76 : 70);
            var image = slot.GetComponent<Image>();
            image.sprite = slotFrame; image.type = Image.Type.Simple; image.color = Color.white;
            image.raycastTarget = true;
            foreach (var outline in slot.GetComponents<Outline>()) outline.enabled = false;
            var icon = slot.Find("Icon") as RectTransform;
            if (icon != null)
            {
                icon.anchorMin = Vector2.zero; icon.anchorMax = Vector2.one;
                icon.offsetMin = new Vector2(9, 10); icon.offsetMax = new Vector2(-9, -8);
                icon.GetComponent<Image>().preserveAspect = true;
                icon.GetComponent<Image>().raycastTarget = false;
            }
            var key = slot.Find("Hotkey")?.GetComponent<TMP_Text>();
            if (key != null)
            {
                SingleLine(key);
                Place(key.rectTransform, Vector2.zero, Vector2.zero, new Vector2(7, 4), new Vector2(23, 20));
                key.fontSize = skill ? 18 : 16; key.color = Gold;
                key.alignment = TextAlignmentOptions.BottomLeft; key.raycastTarget = false;
                var plate = Picture("HUD Key Backing", slot, null, new Vector2(25, 20));
                Place(plate.rectTransform, Vector2.zero, Vector2.zero, new Vector2(4, 3), new Vector2(25, 20));
                plate.color = new Color(.025f, .032f, .038f, .82f);
                plate.transform.SetSiblingIndex(key.transform.GetSiblingIndex());
                key.transform.SetAsLastSibling();
            }
            var count = slot.Find("Cooldown")?.GetComponent<TMP_Text>();
            if (count != null)
            {
                SingleLine(count);
                if (!skill) Place(count.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-6, 4), new Vector2(32, 20));
                else Place(count.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 45));
                count.fontSize = skill ? 24 : 16;
                count.color = new Color(1, .94f, .77f);
                count.alignment = skill ? TextAlignmentOptions.Center : TextAlignmentOptions.BottomRight;
                count.raycastTarget = false; count.transform.SetAsLastSibling();
            }
        }
        private void ApplyMinimap()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;
            Frame(panel.GetComponent<Image>());
            var group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = .96f;
            var background = panel.Find("Panel Background")?.GetComponent<Image>();
            if (background != null) { background.color = Color.clear; background.raycastTarget = false; }
            var view = panel.Find("Map Viewport")?.GetComponent<Image>();
            if (view != null)
            {
                view.color = Ink;
                view.rectTransform.offsetMin = new Vector2(18, 35);
                view.rectTransform.offsetMax = new Vector2(-18, -43);
            }
            var title = panel.Find("Region Name")?.GetComponent<TMP_Text>();
            panel.Find("HUD Heading")?.gameObject.SetActive(false);
            if (title != null)
            {
                SingleLine(title);
                title.fontSize = 18; title.color = Gold;
                Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -10), new Vector2(220, 25));
                title.transform.SetAsLastSibling();
            }
            var legend = panel.Find("Legend")?.GetComponent<TMP_Text>();
            if (legend != null)
            {
                SingleLine(legend);
                legend.text = "<color=#83DFCB>나</color>   <color=#BAA0ED>포탈</color>   <color=#E8C579>상점</color>   <color=#E5A475>대장간</color>";
                legend.fontSize = 11; legend.color = Gold;
                Place(legend.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 9), new Vector2(310, 18));
            }
            var badge = Picture("HUD Map Key", panel, slotFrame, new Vector2(24, 24));
            Place(badge.rectTransform, Vector2.one, Vector2.one, new Vector2(-15, -12), new Vector2(24, 24));
            var key = badge.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (key == null)
            {
                var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(badge.transform, false); go.layer = gameObject.layer;
                key = go.GetComponent<TMP_Text>();
                key.rectTransform.anchorMin = Vector2.zero; key.rectTransform.anchorMax = Vector2.one;
                key.rectTransform.offsetMin = key.rectTransform.offsetMax = Vector2.zero;
            }
            SingleLine(key);
            key.font = title != null ? title.font : TMP_Settings.defaultFontAsset;
            key.text = "M"; key.fontSize = 13; key.color = Gold; key.alignment = TextAlignmentOptions.Center; key.raycastTarget = false;
        }
        private void Frame(Image image)
        {
            if (image == null) return;
            image.sprite = panelFrame; image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2.8f; image.color = Color.white; image.raycastTarget = false;
            foreach (var outline in image.GetComponents<Outline>()) outline.enabled = false;
        }
        private Image Picture(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false); go.layer = gameObject.layer; child = go.transform;
            }
            var image = child.GetComponent<Image>(); image.sprite = sprite; image.color = Color.white;
            image.raycastTarget = false; image.rectTransform.sizeDelta = size;
            return image;
        }
        private static void SingleLine(TMP_Text text)
        {
            text.enableAutoSizing = false; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow; text.margin = Vector4.zero;
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
    }
}
