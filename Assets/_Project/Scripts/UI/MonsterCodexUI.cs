using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EasternFantasy.UI
{
    public sealed class MonsterCodexUI : MonoBehaviour
    {
        [SerializeField] private MonsterCodexCatalog catalog;
        [SerializeField] private Sprite descriptionFrame, nameFrame, scrollTrack, scrollHandle;
        [SerializeField] private TMP_FontAsset font;
        private readonly HashSet<int> expanded = new HashSet<int>();
        private readonly List<GameObject> rows = new List<GameObject>();
        private RectTransform listContent;
        private ScrollRect listScroll;
        private Image portrait;
        private TMP_Text monsterName, description;
        private int selectedRegion, selectedMonster;
        private bool built;
        public MonsterCodexCatalog Catalog => catalog;
        public string DisplayedName => monsterName ? monsterName.text : "";
        public string DisplayedDescription => description ? description.text : "";
        public bool PortraitVisible => portrait && portrait.enabled;
        public int VisibleRowCount => rows.Count;
        public void Configure(MonsterCodexCatalog data, TMP_FontAsset typeface, Sprite frame, Sprite nameplate, Sprite track, Sprite handle)
        { catalog=data; font=typeface; descriptionFrame=frame; nameFrame=nameplate; scrollTrack=track; scrollHandle=handle; }
        private void Awake() => Build();
        private void Build()
        {
            if(built || !catalog) return;
            built=true;
            var group=gameObject.AddComponent<CanvasGroup>();
            group.interactable=false; group.blocksRaycasts=false;
            var animator=GetComponentInParent<ScrollCodexAnimator>();
            if(animator) animator.Opened.AddListener(() => { group.interactable=true; group.blocksRaycasts=true; });
            Text("Title",transform,"몬스터 도감",new Vector2(0,240),new Vector2(1100,42),32,TextAlignmentOptions.Center);
            var divider=Picture("Divider",transform,null,new Vector2(95,-10),new Vector2(2,455));
            divider.color=new Color(.39f,.28f,.16f,.28f);
            // Portrait keeps its original aspect ratio and fills the available height.
            portrait=Picture("MonsterPortrait",transform,null,new Vector2(-265,65),new Vector2(660,285));
            portrait.preserveAspect=true;
            var plate=Picture("MonsterNameplate",transform,nameFrame,new Vector2(-265,-99),new Vector2(630,66));
            monsterName=Text("MonsterName",plate.transform,"",Vector2.zero,new Vector2(470,40),25,TextAlignmentOptions.Center);
            var frame=Picture("DescriptionFrame",transform,descriptionFrame,new Vector2(-265,-195),new Vector2(650,132));
            description=Text("Description",frame.transform,"",Vector2.zero,new Vector2(535,76),20,TextAlignmentOptions.TopLeft);
            description.textWrappingMode=TextWrappingModes.Normal;
            frame.type=Image.Type.Sliced; frame.pixelsPerUnitMultiplier=4f;
            plate.type=Image.Type.Sliced; plate.pixelsPerUnitMultiplier=5f;
            Text("ListTitle",transform,"지역 · 몬스터 목록",new Vector2(354,192),new Vector2(450,40),25,TextAlignmentOptions.Center);
            var list=Rect("RegionScroll",transform,new Vector2(335,-48),new Vector2(438,425));
            listScroll=list.gameObject.AddComponent<ScrollRect>(); listScroll.horizontal=false;
            listScroll.movementType=ScrollRect.MovementType.Clamped; listScroll.scrollSensitivity=32;
            var view=Rect("Viewport",list,Vector2.zero,new Vector2(438,425));
            view.gameObject.AddComponent<RectMask2D>();
            var hit=view.gameObject.AddComponent<Image>(); hit.color=new Color(1,1,1,.001f);
            listContent=Rect("Rows",view,Vector2.zero,new Vector2(438,425));
            listContent.anchorMin=new Vector2(0,1); listContent.anchorMax=new Vector2(1,1); listContent.pivot=new Vector2(.5f,1);
            listContent.sizeDelta=new Vector2(0,425); listContent.anchoredPosition=Vector2.zero;
            listScroll.viewport=view; listScroll.content=listContent;
            var track=Picture("ScrollTrack",transform,scrollTrack,new Vector2(589,-48),new Vector2(49,438));
            var bar=track.gameObject.AddComponent<Scrollbar>(); bar.direction=Scrollbar.Direction.BottomToTop;
            var slide=Rect("SlidingArea",track.transform,Vector2.zero,new Vector2(31,337));
            var handle=Picture("Handle",slide,scrollHandle,Vector2.zero,new Vector2(36,85));
            handle.raycastTarget=true; bar.handleRect=handle.rectTransform; bar.targetGraphic=handle;
            bar.size=.3f; listScroll.verticalScrollbar=bar;
            expanded.Add(0); RebuildRows();
            if(catalog.regions.Count>0 && catalog.regions[0].monsters.Count>0) SelectMonster(0,0);
        }
        private void RefreshDiscovery()
        {
            if(built && catalog.regions.Count>selectedRegion && catalog.regions[selectedRegion].monsters.Count>selectedMonster)
                SelectMonster(selectedRegion,selectedMonster);
        }
        private void OnDisable() => MonsterCodexDiscovery.Changed -= RefreshDiscovery;
        private void OnEnable()
        {
            MonsterCodexDiscovery.Changed -= RefreshDiscovery;
            MonsterCodexDiscovery.Changed += RefreshDiscovery;
            RefreshDiscovery();
            if(built) {
                var group=GetComponent<CanvasGroup>(); group.interactable=false; group.blocksRaycasts=false;
            }
        }
        public void ToggleRegion(int region)
        {
            Build(); if(!listContent || region<0 || region>=catalog.regions.Count) return;
            if(!MonsterCodexDiscovery.IsRegionDiscovered(catalog.regions[region])) return;
            if(!expanded.Add(region)) expanded.Remove(region);
            RebuildRows();
        }
        public void SelectMonster(int region,int monster)
        {
            Build(); if(!monsterName || region<0 || region>=catalog.regions.Count || monster<0 || monster>=catalog.regions[region].monsters.Count) return;
            if(!MonsterCodexDiscovery.IsRegionDiscovered(catalog.regions[region])) return;
            selectedRegion=region; selectedMonster=monster;
            var entry=catalog.regions[region].monsters[monster];
            bool discovered=MonsterCodexDiscovery.IsMonsterDiscovered(entry);
            monsterName.text=discovered ? entry.name : "미확인 요괴";
            portrait.sprite=discovered ? entry.portrait : null;
            portrait.enabled=discovered && entry.portrait;
            description.text=discovered ? entry.description : "아직 발견하지 못한 요괴입니다.\n이름과 모습, 기록이 밝혀지지 않았습니다.";
            RebuildRows();
        }
        private void RebuildRows()
        {
            foreach(var row in rows) { row.SetActive(false); Destroy(row); }
            rows.Clear();
            float y=0;
            for(int r=0;r<catalog.regions.Count;r++) {
                int region=r;
                var area=catalog.regions[r];
                bool known=MonsterCodexDiscovery.IsRegionDiscovered(area);
                Row(known ? (expanded.Contains(r) ? "▼  " : "▶  ")+area.name : "미발견 지역",ref y,false,false,known ? (UnityEngine.Events.UnityAction)(()=>ToggleRegion(region)) : null);
                if(!known || !expanded.Contains(r)) continue;
                for(int m=0;m<area.monsters.Count;m++) {
                    int monster=m; var entry=area.monsters[m];
                    Row(MonsterCodexDiscovery.IsMonsterDiscovered(entry) ? entry.name : "미확인 요괴",ref y,true,r==selectedRegion&&m==selectedMonster,()=>SelectMonster(region,monster));
                }
            }
            listContent.sizeDelta=new Vector2(0,Mathf.Max(425,y));
            listContent.anchoredPosition=new Vector2(0,Mathf.Clamp(listContent.anchoredPosition.y,0,Mathf.Max(0,y-425)));
        }
        private void Row(string label,ref float y,bool child,bool selected,UnityEngine.Events.UnityAction click)
        {
            float height=child ? 46 : 54;
            var row=Rect(child ? "MonsterRow" : "RegionRow",listContent,new Vector2(0,-y-height/2),new Vector2(430,height-4));
            row.anchorMin=row.anchorMax=new Vector2(.5f,1);
            var image=row.gameObject.AddComponent<Image>();
            image.color=selected ? new Color(.38f,.22f,.09f,.21f) : child ? new Color(.7f,.57f,.35f,.055f) : new Color(.18f,.24f,.27f,.12f);
            var button=row.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            button.interactable=click!=null;
            if(click!=null) button.onClick.AddListener(click);
            Text("Label",row,label,new Vector2(child?13:0,0),new Vector2(child?365:400,height-8),child?22:24,TextAlignmentOptions.MidlineLeft);
            rows.Add(row.gameObject); y+=height;
        }
        private RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.layer=gameObject.layer; go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform; rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);
            rt.anchoredPosition=position; rt.sizeDelta=size; return rt;
        }
        private Image Picture(string name,Transform parent,Sprite sprite,Vector2 pos,Vector2 size)
        { var rt=Rect(name,parent,pos,size); var image=rt.gameObject.AddComponent<Image>(); image.sprite=sprite; image.raycastTarget=false; return image; }
        private TMP_Text Text(string name,Transform parent,string value,Vector2 pos,Vector2 size,float fontSize,TextAlignmentOptions alignment)
        {
            var rt=Rect(name,parent,pos,size); var text=rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font; text.text=value; text.fontSize=fontSize; text.alignment=alignment; text.color=new Color(.23f,.15f,.095f);
            text.raycastTarget=false; return text;
        }
    }
}

