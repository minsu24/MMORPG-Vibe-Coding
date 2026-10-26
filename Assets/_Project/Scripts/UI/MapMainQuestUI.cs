using System;
using EasternFantasy.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MapMainQuestUI : MonoBehaviour
    {
        public enum Stage { None, Available, Active, ReadyToTurnIn }
        [SerializeField] private Image detailImage;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite availableSprite, activeSprite, readySprite;
        [SerializeField] private QuestDefinition[] mainQuests=Array.Empty<QuestDefinition>();
        private MapWindowUI window;
        private MapPlayerLocationUI playerLocation;
        private Marker[] markers;
        private sealed class Marker
        {
            public RectTransform Root;
            public Image Image;
            public TMP_Text Label;
            public CanvasGroup Group;
            public string Scene;
            public Stage State;
            public bool Visible;
        }
        // Destination metadata lives on quests so map markers do not depend on loaded NPC scenes.
        public static bool TryGetMarker(QuestDefinition quest,QuestManager manager,out Stage stage,out string scene)
        {
            stage=Stage.None;scene=null;
            if(quest==null || quest.Type!=QuestType.Main || manager==null)return false;
            var progress=manager.FindProgress(quest);
            if(progress==null)
            {
                if(!manager.CanAccept(quest))return false;
                stage=Stage.Available;scene=quest.MapOfferScene;
            }
            else if(progress.Status==QuestStatus.Active){stage=Stage.Active;scene=quest.MapObjectiveScene;}
            else if(progress.Status==QuestStatus.ReadyToTurnIn){stage=Stage.ReadyToTurnIn;scene=quest.MapTurnInScene;}
            else return false;
            return !string.IsNullOrEmpty(scene);
        }
        private void Initialize()
        {
            if(markers!=null || detailImage==null)return;
            window=GetComponent<MapWindowUI>();playerLocation=GetComponent<MapPlayerLocationUI>();
            markers=new Marker[mainQuests.Length];
            for(int i=0;i<markers.Length;i++)
            {
                var go=new GameObject("Map Main Quest "+mainQuests[i].name,typeof(RectTransform),typeof(CanvasGroup));
                go.layer=detailImage.gameObject.layer;go.transform.SetParent(detailImage.transform,false);
                var marker=new Marker {Root=go.GetComponent<RectTransform>(),Group=go.GetComponent<CanvasGroup>()};
                marker.Root.anchorMin=marker.Root.anchorMax=marker.Root.pivot=new Vector2(.5f,.5f);
                marker.Root.sizeDelta=new Vector2(76,76);marker.Group.blocksRaycasts=false;
                var icon=new GameObject("Quest State Icon",typeof(RectTransform),typeof(Image));
                icon.layer=go.layer;icon.transform.SetParent(go.transform,false);
                marker.Image=icon.GetComponent<Image>();marker.Image.raycastTarget=false;marker.Image.preserveAspect=true;
                marker.Image.rectTransform.anchorMin=Vector2.zero;marker.Image.rectTransform.anchorMax=Vector2.one;
                marker.Image.rectTransform.sizeDelta=Vector2.zero;
                var plate=new GameObject("Quest State Plate",typeof(RectTransform),typeof(MapNpcBadgeGraphic));
                plate.layer=go.layer;plate.transform.SetParent(go.transform,false);
                var background=plate.GetComponent<MapNpcBadgeGraphic>();background.namePlate=true;background.raycastTarget=false;
                background.rectTransform.anchorMin=background.rectTransform.anchorMax=new Vector2(.5f,0);
                background.rectTransform.pivot=new Vector2(.5f,1);background.rectTransform.anchoredPosition=new Vector2(0,-3);
                background.rectTransform.sizeDelta=new Vector2(94,25);
                var label=new GameObject("Quest State",typeof(RectTransform),typeof(TextMeshProUGUI));
                label.layer=go.layer;label.transform.SetParent(plate.transform,false);
                marker.Label=label.GetComponent<TMP_Text>();marker.Label.font=font;marker.Label.fontSize=18;
                marker.Label.alignment=TextAlignmentOptions.Center;marker.Label.textWrappingMode=TextWrappingModes.NoWrap;
                marker.Label.raycastTarget=false;marker.Label.outlineWidth=.2f;
                marker.Label.rectTransform.anchorMin=Vector2.zero;marker.Label.rectTransform.anchorMax=Vector2.one;
                marker.Label.rectTransform.sizeDelta=Vector2.zero;
                markers[i]=marker;go.SetActive(false);
            }
        }
        private void LateUpdate()
        {
            Initialize();
            if(markers==null || window==null || playerLocation==null)return;
            var manager=QuestManager.Instance;
            for(int i=0;i<markers.Length;i++)
            {
                var marker=markers[i];
                marker.Visible=TryGetMarker(mainQuests[i],manager,out marker.State,out marker.Scene)
                    && window.IsOpen && window.IsShowingDetail && MapRegionDiscovery.IsSceneNameKnown(marker.Scene);
            }
            Rect drawn=MapPlayerLocationUI.GetDrawingRect(detailImage);float scale=drawn.height/1080;
            for(int i=0;i<markers.Length;i++)
            {
                var marker=markers[i];MapPlayerLocationUI.Location location=null;
                if(marker.Visible) foreach(var candidate in playerLocation.Locations)
                    if(candidate.sceneName==marker.Scene && candidate.giantFarm==window.IsShowingGiantFarm){location=candidate;break;}
                bool visible=location!=null && location.bannerOutline!=null && location.bannerOutline.Length>0;
                marker.Root.gameObject.SetActive(visible);if(!visible)continue;
                int total=0,index=0;
                for(int j=0;j<markers.Length;j++)if(markers[j].Visible && markers[j].Scene==marker.Scene){if(j<i)index++;total++;}
                var point=location.bannerOutline[0];
                marker.Root.anchoredPosition=new Vector2(drawn.xMin+point.x/1920*drawn.width+(index-(total-1)*.5f)*104*scale,
                    drawn.yMax-point.y/1080*drawn.height+80*scale);
                marker.Root.localScale=Vector3.one*scale;
                marker.Group.alpha=playerLocation.DetailPresentationAlpha;
                switch(marker.State)
                {
                    case Stage.Available:marker.Image.sprite=availableSprite;marker.Label.text="시작 가능";marker.Label.color=new Color(1,.84f,.63f);break;
                    case Stage.Active:marker.Image.sprite=activeSprite;marker.Label.text="진행 중";marker.Label.color=new Color(.75f,.87f,1);break;
                    case Stage.ReadyToTurnIn:marker.Image.sprite=readySprite;marker.Label.text="완료 가능";marker.Label.color=new Color(.79f,1,.64f);break;
                }
            }
        }
    }
}
