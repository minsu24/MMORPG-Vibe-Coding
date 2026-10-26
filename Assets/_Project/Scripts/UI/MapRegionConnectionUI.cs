using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    // Keep the illustrated bridge ends attached while islands are edited or hovered.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class MapRegionConnectionUI : MonoBehaviour
    {
        [SerializeField] private RectTransform fromIsland, toIsland;
        [SerializeField] private Vector2 fromAnchor = new Vector2(.92f, .62f);
        [SerializeField] private Vector2 toAnchor = new Vector2(.08f, .36f);
        [SerializeField] private Vector2 spriteFrom = new Vector2(.15f, .23f);
        [SerializeField] private Vector2 spriteTo = new Vector2(.87f, .84f);

        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (fromIsland == null || toIsland == null || transform.parent == null) return;
            var rect = (RectTransform)transform;
            var parent = transform.parent as RectTransform;
            var image = GetComponent<Image>();
            if (parent == null || image.sprite == null) return;
            Vector2 a = PointInParent(fromIsland, fromAnchor, parent);
            Vector2 b = PointInParent(toIsland, toAnchor, parent);
            float aspect = image.sprite.rect.width / image.sprite.rect.height;
            Vector2 baseSize = new Vector2(aspect, 1);
            Vector2 sourceDelta = Vector2.Scale(spriteTo - spriteFrom, baseSize);
            Vector2 targetDelta = b - a;
            if (sourceDelta.sqrMagnitude < .001f || targetDelta.sqrMagnitude < .001f) return;
            float scale = targetDelta.magnitude / sourceDelta.magnitude;
            Vector2 size = baseSize * scale;
            float angle = Mathf.Atan2(targetDelta.y, targetDelta.x) * Mathf.Rad2Deg
                - Mathf.Atan2(sourceDelta.y, sourceDelta.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            Vector2 sourceStart = Vector2.Scale(spriteFrom - Vector2.one * .5f, size);
            Vector2 center = a - (Vector2)(rotation * sourceStart);
            Vector2 position = center - parent.rect.center;
            if ((rect.sizeDelta - size).sqrMagnitude > .0001f) rect.sizeDelta = size;
            if ((rect.anchoredPosition - position).sqrMagnitude > .0001f) rect.anchoredPosition = position;
            if (Quaternion.Angle(rect.localRotation, rotation) > .001f) rect.localRotation = rotation;
        }
        private static Vector2 PointInParent(RectTransform island, Vector2 anchor, RectTransform parent)
        {
            Rect r = island.rect;
            Vector2 local = new Vector2(r.xMin + r.width * anchor.x, r.yMin + r.height * anchor.y);
            // Prefab canvases may have zero world scale before a layout pass.
            // Sibling-local coordinates keep authoring and runtime placement identical.
            if (island.parent == parent)
                return (Vector2)(island.localPosition + island.localRotation *
                    Vector3.Scale(new Vector3(local.x, local.y, 0), island.localScale));
            return parent.InverseTransformPoint(island.TransformPoint(local));
        }
    }
}
