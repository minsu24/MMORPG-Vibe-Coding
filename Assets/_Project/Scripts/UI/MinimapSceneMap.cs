using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MinimapSceneMap : MonoBehaviour
    {
        [SerializeField] private Shader inkShader;
        private RenderTexture mapTexture;
        private Material inkMaterial;
        private RawImage artwork;

        public bool Rebuild(RectTransform content, Rect bounds)
        {
            var source = Camera.main;
            if (source == null || inkShader == null) return false;
            ReleaseTexture();
            var cameraObject = new GameObject("Minimap Capture Camera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var capture = cameraObject.GetComponent<Camera>();
            capture.CopyFrom(source); capture.enabled = false;
            capture.orthographic = true; capture.orthographicSize = bounds.height * .5f;
            capture.aspect = bounds.width / bounds.height;
            capture.rect = new Rect(0, 0, 1, 1);
            capture.cullingMask = source.cullingMask & ~(1 << LayerMask.NameToLayer("UI"));
            capture.clearFlags = CameraClearFlags.SolidColor;
            capture.backgroundColor = new Color(.17f, .22f, .23f);
            capture.transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.center.y, source.transform.position.z), Quaternion.identity);
            capture.allowHDR = false; capture.allowMSAA = false;
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false; data.renderShadows = false;
            data.volumeLayerMask = 0;
            int height = Mathf.Clamp(Mathf.RoundToInt(1024f / capture.aspect), 128, 768);
            var raw = RenderTexture.GetTemporary(1024, height, 24, RenderTextureFormat.ARGB32);
            var hidden = new List<Renderer>();
            try
            {
                // Capture scenery once, without leaving a frozen copy of a moving actor on the map.
                foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (!renderer.forceRenderingOff && (renderer.GetComponentInParent<PlayerEntity>() != null
                        || renderer.GetComponentInParent<EnemyController>() != null))
                    { renderer.forceRenderingOff = true; hidden.Add(renderer); }
                capture.targetTexture = raw;
                if (GraphicsSettings.currentRenderPipeline != null)
                    RenderPipeline.SubmitRenderRequest(capture, new UniversalRenderPipeline.SingleCameraRequest { destination = raw });
                else capture.Render();
                if (inkMaterial == null) inkMaterial = new Material(inkShader) { hideFlags = HideFlags.HideAndDontSave };
                mapTexture = new RenderTexture(1024, height, 0, RenderTextureFormat.ARGB32)
                    { name = "Minimap Ink Scenery", filterMode = FilterMode.Bilinear };
                mapTexture.Create();
                Graphics.Blit(raw, mapTexture, inkMaterial);
                if (artwork == null)
                {
                    var go = new GameObject("Ink Scenery", typeof(RectTransform), typeof(RawImage));
                    go.layer = gameObject.layer; go.transform.SetParent(content, false);
                    artwork = go.GetComponent<RawImage>(); artwork.raycastTarget = false;
                    artwork.rectTransform.anchorMin = Vector2.zero; artwork.rectTransform.anchorMax = Vector2.one;
                    artwork.rectTransform.offsetMin = artwork.rectTransform.offsetMax = Vector2.zero;
                }
                artwork.texture = mapTexture; artwork.transform.SetAsFirstSibling();
                return true;
            }
            finally
            {
                foreach (var renderer in hidden) if (renderer != null) renderer.forceRenderingOff = false;
                capture.targetTexture = null;
                RenderTexture.ReleaseTemporary(raw);
                Destroy(cameraObject);
            }
        }
        private void ReleaseTexture()
        {
            if (artwork != null) artwork.texture = null;
            if (mapTexture != null) { mapTexture.Release(); Destroy(mapTexture); mapTexture = null; }
        }
        private void OnDestroy()
        {
            ReleaseTexture();
            if (inkMaterial != null) Destroy(inkMaterial);
        }
    }
}
