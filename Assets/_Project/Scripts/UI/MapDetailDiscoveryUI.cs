using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MapDetailDiscoveryUI : MonoBehaviour
    {
        [SerializeField] private Image detailImage;
        [SerializeField] private Material fogTemplate;
        [SerializeField] private TMP_FontAsset font;
        private MapWindowUI window;
        private MapPlayerLocationUI playerLocation;
        private Material fogMaterial, originalMaterial, lastMaterial;
        private readonly List<Banner> banners = new List<Banner>();
        private readonly Vector4[] regions = new Vector4[6], names = new Vector4[6], numbers = new Vector4[6], bodies = new Vector4[6];
        private int count;
        private bool initialized;
        private readonly List<Banner> visible = new List<Banner>();
        private readonly List<string> pendingDiscoveries = new List<string>();
        private bool presenting;
        private float presentationTime, inkReveal = 1;
        private const float OpenSeconds = .5f, DiscoverySeconds = 1.15f;
        private Rect previousRect;
        private sealed class Banner
        {
            public MapPlayerLocationUI.Location Location;
            public TMP_Text Label;
            public MapDetailBannerGraphic Body;
            public Rect NameRect, NumberRect, BodyRect;
            public float Center;
            public bool Visited, NewlyDiscovered;
        }
        private void Awake() => Initialize();
        private void OnEnable() { MapRegionDiscovery.Changed += Refresh; }
        private void OnDisable() { MapRegionDiscovery.Changed -= Refresh; StopPresentation(); }
        public void Initialize()
        {
            if (initialized || detailImage == null || fogTemplate == null) return;
            window = GetComponent<MapWindowUI>(); playerLocation = GetComponent<MapPlayerLocationUI>();
            if (window == null || playerLocation == null) return;
            initialized = true;
            originalMaterial = detailImage.material;
            fogMaterial = new Material(fogTemplate) { name = "Detail Map Discovery", hideFlags = HideFlags.DontSave };
            detailImage.material = fogMaterial;
            foreach (var location in playerLocation.Locations)
            {
                var points = location.bannerOutline;
                if (points == null || points.Length < 6) continue;
                float center = points[0].x;
                float left = Mathf.Min(points[2].x, points[5].x) + 3;
                float right = Mathf.Max(points[2].x, points[5].x) - 3;
                var nameRect = Rect.MinMaxRect(left, points[2].y + 18, right, points[4].y - 15);
                float minX = center, maxX = center;
                foreach (var point in points) { minX = Mathf.Min(minX, point.x); maxX = Mathf.Max(maxX, point.x); }
                var numberRect = Rect.MinMaxRect(minX - 3, points[0].y - 3, maxX + 3, points[2].y + 20);
                var bodyRect = Rect.MinMaxRect(left-3,points[2].y+16,right+3,points[4].y);
                var bodyObject = new GameObject("Discovery Banner " + location.sceneName, typeof(RectTransform), typeof(MapDetailBannerGraphic));
                bodyObject.layer = detailImage.gameObject.layer; bodyObject.transform.SetParent(detailImage.transform,false);
                var body = bodyObject.GetComponent<MapDetailBannerGraphic>(); body.raycastTarget = false;
                body.rectTransform.anchorMin = body.rectTransform.anchorMax = body.rectTransform.pivot = new Vector2(.5f,.5f);
                bodyObject.SetActive(false);
                var go = new GameObject("Discovery Name " + location.sceneName, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.layer = detailImage.gameObject.layer; go.transform.SetParent(detailImage.transform, false);
                var label = go.GetComponent<TMP_Text>(); label.font = font; label.color = new Color(.95f,.88f,.70f);
                label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                label.enableAutoSizing = true; label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(.5f,.5f);
                go.SetActive(false);
                banners.Add(new Banner { Location = location, Label = label, Body = body, NameRect = nameRect, NumberRect = numberRect, BodyRect = bodyRect, Center = center });
            }
        }
        public void Refresh()
        {
            Initialize();
            if (!initialized || !window.IsShowingDetail) return;
            StopPresentation();
            bool farm = window.IsShowingGiantFarm;
            visible.Clear();
            visible.AddRange(banners.FindAll(b => b.Location.giantFarm == farm));
            pendingDiscoveries.Clear();
            visible.Sort((a,b) => a.Center.CompareTo(b.Center));
            count = Mathf.Min(6, visible.Count);
            foreach (var banner in banners) { banner.Label.gameObject.SetActive(false); banner.Body.gameObject.SetActive(false); }
            for (int i = 0; i < count; i++)
            {
                var banner = visible[i];
                bool visited = MapRegionDiscovery.IsSceneVisited(banner.Location.sceneName);
                banner.Visited = visited;
                banner.NewlyDiscovered = visited && !MapRegionDiscovery.HasPresentedScene(banner.Location.sceneName);
                if (banner.NewlyDiscovered) pendingDiscoveries.Add(banner.Location.sceneName);
                banner.Body.canvasRenderer.SetAlpha(1);
                banner.Label.alpha = 1;
                float left = i == 0 ? 0 : (visible[i-1].Center + banner.Center)*.5f;
                float right = i == count-1 ? 1920 : (visible[i+1].Center + banner.Center)*.5f;
                regions[i] = new Vector4(left/1920, right/1920, visited ? 1 : 0, visited ? 1 : 0);
                names[i] = Normalize(banner.NameRect); numbers[i] = Normalize(banner.NumberRect); bodies[i] = Normalize(banner.BodyRect);
                if (!visited || banner.NewlyDiscovered)
                {
                    bool known = MapRegionDiscovery.IsSceneNameKnown(banner.Location.sceneName);
                    banner.Label.text = known ? Vertical(MapLocationNames.GetDisplayName(banner.Location.sceneName)) : "?\n?\n?";
                    banner.Body.gameObject.SetActive(true);
                    banner.Label.gameObject.SetActive(true);
                }
            }
            Apply(fogMaterial);
            ApplyCurrentMaterial();
            previousRect = default;
            UpdateLayout();
            presenting = Application.isPlaying;
            presentationTime = 0;
            UpdatePresentation(0);
        }
        // Completion is recorded only after the full reveal; interruptions can be replayed.
        public void StopPresentation()
        {
            presenting = false; inkReveal = 1;
            for (int i = 0; i < visible.Count; i++)
            {
                var banner = visible[i];
                regions[i].w = banner.Visited ? 1 : 0;
                banner.Body.canvasRenderer.SetAlpha(1); banner.Label.alpha = 1;
                banner.Body.gameObject.SetActive(!banner.Visited);
                banner.Label.gameObject.SetActive(!banner.Visited);
            }
            if (playerLocation != null) playerLocation.SetDetailPresentationAlpha(1);
            if (fogMaterial != null) { fogMaterial.SetFloat("_Reveal", 1); Apply(fogMaterial); }
        }
        private void UpdatePresentation(float delta)
        {
            if (!presenting) return;
            presentationTime += delta;
            inkReveal = Mathf.Clamp01(presentationTime / OpenSeconds);
            bool discovery = pendingDiscoveries.Count > 0;
            float progress = Mathf.Clamp01((presentationTime - .25f) / .7f);
            float labels = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.72f, .95f, presentationTime));
            float openLabels = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.30f, .5f, presentationTime));
            for (int i = 0; i < visible.Count; i++)
            {
                var banner = visible[i];
                if (banner.NewlyDiscovered)
                {
                    regions[i].w = progress;
                    // Source names are baked in; the matching overlay bridges the late name reveal.
                    banner.Label.alpha = labels * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.95f, 1.05f, presentationTime)));
                    banner.Body.canvasRenderer.SetAlpha(1 - labels);
                    banner.Body.gameObject.SetActive(progress < 1);
                    banner.Label.gameObject.SetActive(presentationTime < 1.05f);
                }
                else if (!banner.Visited)
                {
                    banner.Label.alpha = openLabels;
                    banner.Body.canvasRenderer.SetAlpha(openLabels);
                }
            }
            playerLocation.SetDetailPresentationAlpha(Mathf.SmoothStep(0, 1,
                Mathf.InverseLerp(discovery ? .95f : .4f, discovery ? DiscoverySeconds : .6f, presentationTime)));
            Apply(fogMaterial); ApplyCurrentMaterial();
            if (presentationTime < (discovery ? DiscoverySeconds : .6f)) return;
            foreach (string scene in pendingDiscoveries) MapRegionDiscovery.MarkScenePresented(scene);
            StopPresentation();
        }
        private static string Vertical(string name)
        {
            var glyphs = new List<string>();
            foreach (char c in name) if (!char.IsWhiteSpace(c)) glyphs.Add(c.ToString());
            return string.Join("\n", glyphs);
        }
        private static Vector4 Normalize(Rect r) => new Vector4(r.xMin/1920, 1-r.yMax/1080, r.xMax/1920, 1-r.yMin/1080);
        private void Apply(Material material)
        {
            if (material == null) return;
            if (material == fogMaterial || presenting) material.SetFloat("_Reveal", inkReveal);
            material.SetFloat("_DetailFogEnabled", 1); material.SetFloat("_DetailRegionCount", count);
            material.SetFloat("_DetailFarm", window.IsShowingGiantFarm ? 1 : 0);
            material.SetVectorArray("_DetailRegions", regions); material.SetVectorArray("_DetailNames", names); material.SetVectorArray("_DetailNumbers", numbers); material.SetVectorArray("_DetailBodies", bodies);
            if (detailImage.sprite != null)
            {
                var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(detailImage.sprite);
                material.SetVector("_UVRect", new Vector4(uv.x,uv.y,uv.z-uv.x,uv.w-uv.y));
            }
        }
        private void ApplyCurrentMaterial()
        {
            var material = detailImage.material;
            if (material == null || !material.HasProperty("_DetailFogEnabled"))
                detailImage.material = material = fogMaterial;
            Apply(material); lastMaterial = material;
        }
        private void LateUpdate()
        {
            if (!initialized || !window.IsOpen || !window.IsShowingDetail) return;
            UpdatePresentation(Time.unscaledDeltaTime);
            // Ink opening/closing animations swap materials; keep discovery on that material too.
            if (detailImage.material != lastMaterial) ApplyCurrentMaterial();
            UpdateLayout();
        }
        private void UpdateLayout()
        {
            Rect drawn = MapPlayerLocationUI.GetDrawingRect(detailImage);
            if (drawn == previousRect) return;
            previousRect = drawn;
            foreach (var banner in banners)
            {
                Rect bodySource = banner.BodyRect;
                var body = banner.Body.rectTransform;
                body.anchoredPosition = new Vector2(drawn.xMin + bodySource.center.x/1920*drawn.width, drawn.yMax-bodySource.center.y/1080*drawn.height);
                body.sizeDelta = new Vector2(bodySource.width/1920*drawn.width,bodySource.height/1080*drawn.height);
                Rect source = banner.NameRect;
                var rect = banner.Label.rectTransform;
                rect.anchoredPosition = new Vector2(drawn.xMin + source.center.x/1920*drawn.width, drawn.yMax-source.center.y/1080*drawn.height);
                rect.sizeDelta = new Vector2(source.width/1920*drawn.width,source.height/1080*drawn.height);
                float scale = drawn.height/1080;
                banner.Label.fontSize = banner.Label.fontSizeMax = 24*scale;
                banner.Label.fontSizeMin = 13*scale;
            }
        }
        private void OnDestroy()
        {
            if (detailImage != null && detailImage.material == fogMaterial) detailImage.material = originalMaterial;
            if (fogMaterial != null) Destroy(fogMaterial);
        }
    }
}
