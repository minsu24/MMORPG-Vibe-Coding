using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MapNpcIconsUI : MonoBehaviour
    {
        [Serializable]
        public sealed class Entry
        {
            public string sceneName, displayName;
            public MapNpcPortrait portrait;
            [NonSerialized] public MapNpcPortraitGraphic Image;
            [NonSerialized] public CanvasGroup Group;
            [NonSerialized] public RectTransform Root;
            [NonSerialized] public TMP_Text Label;
        }
        [SerializeField] private Image detailImage;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        private MapWindowUI window;
        private MapPlayerLocationUI playerLocation;
        private bool initialized;

        private void Initialize()
        {
            if (initialized || detailImage == null) return;
            initialized = true;
            window = GetComponent<MapWindowUI>();
            playerLocation = GetComponent<MapPlayerLocationUI>();
            foreach (var entry in entries)
            {
                var go = new GameObject("Map NPC " + entry.displayName, typeof(RectTransform), typeof(CanvasGroup));
                go.layer = detailImage.gameObject.layer;
                go.transform.SetParent(detailImage.transform, false);
                entry.Group = go.GetComponent<CanvasGroup>();
                entry.Group.blocksRaycasts = false;
                entry.Root = go.GetComponent<RectTransform>();
                entry.Root.anchorMin = entry.Root.anchorMax = entry.Root.pivot = new Vector2(.5f,.5f);
                var badge = Child<MapNpcBadgeGraphic>("Ink Gold Badge",go.transform);
                badge.raycastTarget=false; Fill(badge.rectTransform,Vector2.zero);
                var portrait=Child<MapNpcPortraitGraphic>("Portrait",go.transform);
                portrait.portrait=entry.portrait;portrait.raycastTarget=false;
                Fill(portrait.rectTransform,new Vector2(-14,-14));
                entry.Image=portrait;
                var plate=Child<MapNpcBadgeGraphic>("NPC Name Plate",go.transform);
                plate.namePlate=true;plate.raycastTarget=false;
                plate.rectTransform.anchorMin=plate.rectTransform.anchorMax=new Vector2(.5f,0);
                plate.rectTransform.pivot=new Vector2(.5f,1);
                plate.rectTransform.anchoredPosition=new Vector2(0,-6);
                plate.rectTransform.sizeDelta=new Vector2(96,28);
                var text=Child<TextMeshProUGUI>("NPC Name",plate.transform);
                text.font=font;text.text=entry.displayName;text.fontSize=20;
                text.color=new Color(1,.93f,.76f);text.alignment=TextAlignmentOptions.Center;
                text.textWrappingMode=TextWrappingModes.NoWrap;text.raycastTarget=false;
                text.outlineWidth=.2f;Fill(text.rectTransform,Vector2.zero);
                entry.Label=text;
                go.SetActive(false);
            }
        }
        private T Child<T>(string name,Transform parent) where T : Component
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(T));
            go.layer=detailImage.gameObject.layer;go.transform.SetParent(parent,false);
            return go.GetComponent<T>();
        }
        private static void Fill(RectTransform rect,Vector2 inset)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=inset;
        }
        private void LateUpdate()
        {
            Initialize();
            if (!initialized || window == null || playerLocation == null) return;
            Rect drawn = MapPlayerLocationUI.GetDrawingRect(detailImage);
            float scale = drawn.height / 1080;
            foreach (var entry in entries)
            {
                MapPlayerLocationUI.Location location = null;
                foreach (var candidate in playerLocation.Locations)
                    if (candidate.sceneName == entry.sceneName) { location = candidate; break; }
                bool visible = window.IsOpen && window.IsShowingDetail && location != null
                    && location.giantFarm == window.IsShowingGiantFarm && MapRegionDiscovery.IsSceneVisited(entry.sceneName);
                entry.Root.gameObject.SetActive(visible);
                if (!visible) continue;
                int index = 0, total = 0;
                foreach (var other in entries)
                    if (other.sceneName == entry.sceneName) { if (other == entry) index = total; total++; }
                float left=float.MaxValue, right=float.MinValue, bottom=0;
                foreach (var point in location.bannerOutline)
                { left=Mathf.Min(left,point.x); right=Mathf.Max(right,point.x); bottom=Mathf.Max(bottom,point.y); }
                var rect=entry.Root;
                // Reserve the first row for the player portrait, even in other visited regions.
                rect.anchoredPosition = new Vector2(drawn.xMin+(left+right)*.5f/1920*drawn.width
                    +(index-(total-1)*.5f)*102*scale, drawn.yMax-bottom/1080*drawn.height-145*scale);
                rect.sizeDelta = new Vector2(86,86);
                rect.localScale = Vector3.one*scale;
                entry.Group.alpha = playerLocation.DetailPresentationAlpha;
            }
        }
    }
}
