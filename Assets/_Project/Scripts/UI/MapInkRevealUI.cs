using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MapInkRevealUI : MonoBehaviour
    {
        [SerializeField] private Material inkTemplate;
        [SerializeField] private Image[] inkImages;
        [SerializeField] private CanvasGroup[] nameplates;
        [SerializeField] private CanvasGroup[] markers;
        private Material[] originals, animated;
        private float[] labelAlphas, markerAlphas;
        private Vector3[] markerScales;
        private bool prepared;
        private void Prepare()
        {
            if (prepared || inkTemplate == null) return;
            prepared = true;
            originals = new Material[inkImages.Length];
            animated = new Material[inkImages.Length];
            for (int i = 0; i < inkImages.Length; i++)
            {
                Image image = inkImages[i];
                if (image == null) continue;
                originals[i] = image.material;
                var mat = new Material(inkTemplate) { name = image.name + " Ink Animation", hideFlags = HideFlags.DontSave };
                mat.SetFloat("_Seed", i * 3.73f + 1);
                if (image.sprite != null)
                {
                    Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(image.sprite);
                    mat.SetVector("_UVRect", new Vector4(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
                }
                animated[i] = mat;
            }
            labelAlphas = new float[nameplates.Length];
            for (int i = 0; i < nameplates.Length; i++) labelAlphas[i] = nameplates[i] != null ? nameplates[i].alpha : 1;
            markerAlphas = new float[markers.Length]; markerScales = new Vector3[markers.Length];
            for (int i = 0; i < markers.Length; i++)
                if (markers[i] != null) { markerAlphas[i] = markers[i].alpha; markerScales[i] = markers[i].transform.localScale; }
        }
        public void AnimateOpen(Sequence sequence, float duration)
        {
            Prepare();
            if (!prepared) return;
            Restore();
            for (int i = 0; i < inkImages.Length; i++)
            {
                Image image = inkImages[i]; Material mat = animated[i];
                if (image == null || mat == null || !image.gameObject.activeInHierarchy) continue;
                mat.SetFloat("_Reveal", 0); image.material = mat;
                bool bridge = image.GetComponent<MapRegionConnectionUI>() != null;
                float delay = duration * (bridge ? .58f : .10f + Mathf.Min(i, 1) * .05f);
                sequence.Insert(delay, DOTween.To(() => mat.GetFloat("_Reveal"), v => mat.SetFloat("_Reveal", v), 1,
                    duration * (bridge ? .24f : .73f)).SetEase(Ease.Linear));
            }
            for (int i = 0; i < nameplates.Length; i++)
            {
                var group = nameplates[i]; if (group == null) continue;
                group.alpha = 0;
                sequence.Insert(duration * .78f, group.DOFade(labelAlphas[i], duration * .12f));
            }
            for (int i = 0; i < markers.Length; i++)
            {
                var group = markers[i]; if (group == null) continue;
                group.alpha = 0; group.transform.localScale = markerScales[i] * .78f;
                sequence.Insert(duration * .86f, group.DOFade(markerAlphas[i], duration * .12f));
                sequence.Insert(duration * .86f, group.transform.DOScale(markerScales[i], duration * .14f).SetEase(Ease.OutBack));
            }
        }
        public void AnimateClose(Sequence sequence, float duration)
        {
            Prepare(); if (!prepared) return;
            for (int i = 0; i < inkImages.Length; i++)
            {
                var image = inkImages[i]; var mat = animated[i];
                if (image == null || mat == null || !image.gameObject.activeInHierarchy) continue;
                if (image.material != mat) { mat.SetFloat("_Reveal", 1); image.material = mat; }
                if (image.sprite != null)
                {
                    Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(image.sprite);
                    mat.SetVector("_UVRect", new Vector4(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
                }
                sequence.Insert(0, DOTween.To(() => mat.GetFloat("_Reveal"), v => mat.SetFloat("_Reveal", v), 0, duration).SetEase(Ease.InQuad));
            }
        }
        public void Restore()
        {
            if (!prepared) return;
            for (int i = 0; i < inkImages.Length; i++) if (inkImages[i] != null) inkImages[i].material = originals[i];
            for (int i = 0; i < nameplates.Length; i++) if (nameplates[i] != null) nameplates[i].alpha = labelAlphas[i];
            for (int i = 0; i < markers.Length; i++)
                if (markers[i] != null) { markers[i].alpha = markerAlphas[i]; markers[i].transform.localScale = markerScales[i]; }
        }
        private void OnDisable() => Restore();
        private void OnDestroy()
        {
            Restore();
            if (animated == null) return;
            foreach (var material in animated)
                if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
