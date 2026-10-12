using EasternFantasy.Inventory;
using UnityEngine;
using UnityEngine.UI;
namespace EasternFantasy.UI
{
    public static class ItemGradeVisual
    {
        public static void Apply(Image icon, ItemDefinition item)
        {
            if (icon == null) return;
            icon.useSpriteMesh = false;
            Transform parent = icon.transform.parent;
            if (parent == null) return;
            Transform existing = parent.Find(icon.name + "GradeFrame");
            Image frame = existing != null ? existing.GetComponent<Image>() : null;
            bool show = item != null && item.GradeFrame != null;
            if (frame == null && show)
            {
                var root = new GameObject(icon.name + "GradeFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                root.transform.SetParent(parent, false);
                frame = root.GetComponent<Image>();
                frame.raycastTarget = false;
                root.GetComponent<LayoutElement>().ignoreLayout = true;
            }
            if (frame != null)
            {
                frame.enabled = show;
                frame.sprite = show ? item.GradeFrame : null;
                frame.preserveAspect = true;
                frame.color = Color.white;
                RectTransform rect = frame.rectTransform;
                RectTransform source = icon.rectTransform;
                rect.anchorMin = source.anchorMin;
                rect.anchorMax = source.anchorMax;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localRotation = source.localRotation;
                rect.localScale = source.localScale;
                rect.position = source.TransformPoint(source.rect.center);
                rect.sizeDelta = source.sizeDelta + source.rect.size * 0.12f;
                if (frame.transform.GetSiblingIndex() > icon.transform.GetSiblingIndex())
                    frame.transform.SetSiblingIndex(icon.transform.GetSiblingIndex());
            }
            icon.preserveAspect = true;
        }
        // Align to the drawn frame, including preserveAspect letterboxing. The slot
        // artwork may have decorative glow/tassels outside its actual item well.
        public static void CenterInFrame(Image icon, Image frame, Vector2 contentCenter)
        {
            if (icon == null || frame == null) return;
            Rect drawn = frame.rectTransform.rect;
            Sprite sprite = frame.overrideSprite != null ? frame.overrideSprite : frame.sprite;
            if (frame.preserveAspect && sprite != null)
            {
                float ratio = sprite.rect.width / sprite.rect.height;
                if (drawn.width / drawn.height > ratio)
                {
                    float width = drawn.height * ratio;
                    drawn.x += (drawn.width - width) * .5f;
                    drawn.width = width;
                }
                else
                {
                    float height = drawn.width / ratio;
                    drawn.y += (drawn.height - height) * .5f;
                    drawn.height = height;
                }
            }
            Vector2 center = new Vector2(drawn.x + drawn.width * contentCenter.x,
                drawn.y + drawn.height * contentCenter.y);
            RectTransform rect = icon.rectTransform;
            Vector2 size = rect.rect.size;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.position = frame.rectTransform.TransformPoint(center);
            icon.useSpriteMesh = false;
            icon.preserveAspect = true;
        }

        public static void CenterInUpgradeFrame(Image icon, Image frame)
        {
            Sprite sprite = frame != null ? frame.sprite : null;
            if (sprite == null) return;
            Vector2 atlasCenter;
            switch (sprite.name)
            {
                case "TargetSlot": atlasCenter = new Vector2(722.5f, sprite.texture.height - 233f); break;
                case "MaterialFilled": atlasCenter = new Vector2(290f, sprite.texture.height - 605.5f); break;
                case "MaterialReady": atlasCenter = new Vector2(577f, sprite.texture.height - 607f); break;
                case "MaterialEmpty": atlasCenter = new Vector2(867f, sprite.texture.height - 606.5f); break;
                default: CenterInFrame(icon, frame, new Vector2(.5f, .5f)); return;
            }
            Vector2 normalized = (atlasCenter - sprite.rect.position) / sprite.rect.size;
            CenterInFrame(icon, frame, normalized);
        }
        public static void ApplyDrag(Image root, ItemDefinition item)
        {
            root.preserveAspect = true;
            root.useSpriteMesh = false;
            if (item.GradeFrame == null) return;
            root.sprite = item.GradeFrame;
            root.color = Color.white;
            var child = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            child.transform.SetParent(root.transform, false);
            Image image = child.GetComponent<Image>();
            image.sprite = item.Icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            CenterInFrame(image, root, new Vector2(.5f, .5f));
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = new Vector2(5,5);
            image.rectTransform.offsetMax = new Vector2(-5,-5);
        }
    }
}
