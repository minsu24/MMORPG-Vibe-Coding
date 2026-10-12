using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public static class UIReadability
    {
        public static void ClampToCanvas(RectTransform panel, float padding = 18f)
        {
            Canvas canvas = panel != null ? panel.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return;
            RectTransform bounds = canvas.rootCanvas.transform as RectTransform;
            if (bounds == null || bounds.rect.width < 1f) return;
            var corners = new Vector3[4]; panel.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                Vector2 point = bounds.InverseTransformPoint(corner);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            Rect safe = bounds.rect;
            Vector2 shift = Vector2.zero;
            if (max.x > safe.xMax - padding) shift.x = safe.xMax - padding - max.x;
            if (min.x + shift.x < safe.xMin + padding) shift.x += safe.xMin + padding - (min.x + shift.x);
            if (max.y > safe.yMax - padding) shift.y = safe.yMax - padding - max.y;
            if (min.y + shift.y < safe.yMin + padding) shift.y += safe.yMin + padding - (min.y + shift.y);
            panel.position += bounds.TransformVector(shift);
        }

        public static void LayoutTooltip(GameObject root, TMP_Text title, params TMP_Text[] body)
        {
            RectTransform panel = root.transform as RectTransform;
            if (panel == null) return;
            Image background = root.GetComponent<Image>();
            if (background != null) { Color color = background.color; color.a = .98f; background.color = color; }
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 420f);
            float maximum = 700f;
            Canvas canvas = root.GetComponentInParent<Canvas>();
            var canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if (canvasRect != null && canvasRect.rect.height > 100f) maximum = Mathf.Min(maximum, canvasRect.rect.height - 36f);
            Transform existing = panel.Find("ReadableBody");
            ScrollRect scroll;
            RectTransform content, viewport;
            if (existing == null)
            {
                var view = new GameObject("ReadableBody", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
                view.layer = root.layer;
                view.transform.SetParent(panel, false);
                viewport = view.GetComponent<RectTransform>();
                view.GetComponent<Image>().color = new Color(0,0,0,.015f);
                scroll = view.GetComponent<ScrollRect>();
                var container = new GameObject("Content", typeof(RectTransform));
                container.layer = root.layer;
                container.transform.SetParent(view.transform, false);
                content = container.GetComponent<RectTransform>();
                content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
                content.pivot = new Vector2(.5f,1);
                scroll.viewport = viewport; scroll.content = content;
                scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 28f;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.inertia = true;
            }
            else
            {
                scroll = existing.GetComponent<ScrollRect>();
                viewport = scroll.viewport; content = scroll.content;
            }
            float height = 0f;
            foreach (var text in body)
            {
                if (text == null) continue;
                text.transform.SetParent(content, false);
                var rect = text.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f,1);
                rect.pivot = new Vector2(.5f,1);
                rect.sizeDelta = new Vector2(372f,100f);
                text.fontSize = Mathf.Max(text.fontSize, 22f); text.enableAutoSizing = false;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Overflow;
                text.raycastTarget = false;
                float preferred = text.GetPreferredValues(text.text, 372f, Mathf.Infinity).y + 8f;
                rect.sizeDelta = new Vector2(372f,preferred);
                rect.anchoredPosition = new Vector2(0,-height);
                height += preferred + 12f;
            }
            float panelHeight = Mathf.Min(maximum, Mathf.Max(220f,height + 128f));
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,panelHeight);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(.5f,1);
            title.rectTransform.pivot = new Vector2(.5f,1);
            title.rectTransform.anchoredPosition = new Vector2(0,-18);
            title.rectTransform.sizeDelta = new Vector2(372,58);
            title.fontSize = 27f; title.enableAutoSizing = true; title.fontSizeMin = 22f; title.fontSizeMax = 27f;
            title.overflowMode = TextOverflowModes.Ellipsis;
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20,30); viewport.offsetMax = new Vector2(-20,-86);
            content.sizeDelta = new Vector2(0,height);
            content.anchoredPosition = Vector2.zero;
            Transform hint = panel.Find("ScrollHint");
            if (hint == null)
            {
                var label = new GameObject("ScrollHint",typeof(RectTransform),typeof(TextMeshProUGUI));
                label.layer = root.layer;
                label.transform.SetParent(panel,false); hint = label.transform;
                var text = label.GetComponent<TMP_Text>(); text.font = title.font; text.fontSize = 18;
                text.text = "마우스 휠 또는 드래그로 설명 보기";
                text.color = new Color(.9f,.85f,.7f); text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f,0);
                rect.pivot = new Vector2(.5f,0); rect.sizeDelta = new Vector2(380,24); rect.anchoredPosition = new Vector2(0,5);
            }
            hint.gameObject.SetActive(height > panelHeight - 116f);
            Canvas.ForceUpdateCanvases();
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            ClampToCanvas(panel);
            Canvas.ForceUpdateCanvases();
            viewport.GetComponent<RectMask2D>().PerformClipping();
        }
    }
}