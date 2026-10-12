using System;
using System.IO;
using System.Linq;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class UIReadabilitySetup
    {
        [MenuItem("Eastern Fantasy/UI/Improve Readability")]
        public static void Run() => Debug.Log(Apply());
        public static string Apply()
        {
            var originalScene=SceneManager.GetActiveScene();
            int prefabs=0, scenes=0, labels=0;
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/Prefabs"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!path.Contains("/UI/") && asset.GetComponentInChildren<Canvas>(true)==null) continue;
                if(asset.GetComponentInChildren<TextMeshProUGUI>(true)==null)continue;
                var root=PrefabUtility.LoadPrefabContents(path);
                try {labels+=Improve(root,false);PrefabUtility.SaveAsPrefabAsset(root,path);prefabs++;}
                finally {PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(string guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/_Project/Scenes"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    int changed=0;
                    foreach(var root in scene.GetRootGameObjects())changed+=Improve(root,true);
                    if(changed>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);scenes++;labels+=changed;}
                }
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            if(originalScene.isLoaded)SceneManager.SetActiveScene(originalScene);
            AssetDatabase.SaveAssets();
            return "Readability updated: "+prefabs+" UI prefabs, "+scenes+" scenes, "+labels+" labels.";
        }
        private static int Improve(GameObject root,bool scene)
        {
            int changed=0;
            foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if(scene && PrefabUtility.IsPartOfPrefabInstance(text))continue;
                Style(text);changed++;
            }
            if(changed==0)return 0;
            foreach(var grid in root.GetComponentsInChildren<GridLayoutGroup>(true))
                if(grid.name=="ShopGrid")
                {
                    grid.cellSize=new Vector2(180,186);grid.spacing=new Vector2(16,16);
                    Set(grid.GetComponent<RectTransform>(),new Vector2(0,-74),new Vector2(800,590));
                }
            foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if(scene && PrefabUtility.IsPartOfPrefabInstance(text))continue;
                string parent=text.transform.parent.name;
                if(parent=="ShopWindow" && text.name=="Message")Set(text.rectTransform,new Vector2(0,-401),new Vector2(900,34));
                if(parent=="ShopWindow" && text.name=="CloseHint")Set(text.rectTransform,new Vector2(0,-440),new Vector2(400,34));
                if(parent.StartsWith("ShopSlot"))
                {
                    if(text.name=="ItemName")Set(text.rectTransform,new Vector2(0,-17),new Vector2(165,68));
                    if(text.name=="Price")Set(text.rectTransform,new Vector2(0,-68),new Vector2(165,34));
                }
                if(parent=="ShopWindow" && text.name=="Title")Set(text.rectTransform,new Vector2(0,370),new Vector2(300,70));
                if(parent=="ShopWindow" && text.name=="Currency")Set(text.rectTransform,new Vector2(300,370),new Vector2(300,50));
                if(parent=="InventoryWindow" && text.name=="Currency")Set(text.rectTransform,new Vector2(0,-355),new Vector2(400,46));
                if(parent=="SkillWindow" && text.name=="Title"){Set(text.rectTransform,new Vector2(0,178),new Vector2(350,52));text.fontSize=text.fontSizeMax=32;text.color=new Color(.35f,.18f,.08f);}
                if(parent=="SkillWindow" && text.name=="BaseHeader")Set(text.rectTransform,new Vector2(-240,125),new Vector2(220,40));
                if(parent=="SkillWindow" && text.name=="FirstHeader")Set(text.rectTransform,new Vector2(240,125),new Vector2(220,40));
                if(parent=="SkillWindow" && (text.name=="BaseHeader" || text.name=="FirstHeader"))text.fontSize=text.fontSizeMax=24;
                if(parent=="SkillWindow" && text.name=="CloseHint"){Set(text.rectTransform,new Vector2(0,-210),new Vector2(350,42));text.color=new Color(1f,.84f,.5f);}
                if(text.name=="QuestList" && text.GetComponentInParent<ReadableScrollText>()==null)
                {
                    Set(text.rectTransform,new Vector2(0,-32),new Vector2(420,336));
                    AddScroll(text);
                }
                if((text.name=="DialougueText" || text.name=="DialogueText") && text.GetComponentInParent<ReadableScrollText>()==null)
                {
                    text.fontSize=30;text.fontSizeMin=28;text.fontSizeMax=30;text.enableAutoSizing=false;
                    Set(text.rectTransform,new Vector2(0,-5),new Vector2(900,165));
                    AddScroll(text);
                }
                if(parent=="DialoguePanel" && (text.name=="PlayerName" || text.name=="PartnerName"))
                {
                    text.fontSize=30;text.fontSizeMax=30;
                    Set(text.rectTransform,new Vector2(text.name=="PlayerName"?300:-300,125),new Vector2(340,46));
                }
                if(parent=="StatsWindow" && text.name!="Stats" && text.name.Contains("Stats"))
                {
                    bool left=text.rectTransform.anchoredPosition.x<0;
                    Set(text.rectTransform,new Vector2(left?-120:120,text.rectTransform.anchoredPosition.y),
                        new Vector2(190,36));
                    text.fontSize=22;text.fontSizeMax=22;text.fontSizeMin=20;
                }
                if(parent=="StatsWindow" && (text.name=="ClassAndLevel" || text.name=="CloseHint")){var header=text.transform.parent.Find("Header").GetComponent<TMP_Text>();text.font=header.font;if(text.name=="ClassAndLevel"){text.fontSize=text.fontSizeMax=26;text.color=new Color(.25f,.12f,.06f);}}
                if(parent=="StatsWindow" && text.name=="CloseHint"){text.text="[ C ] 닫기";text.fontSize=text.fontSizeMax=22;}
                if(parent=="SkillWindow" && text.name=="SkillPoints")
                    {Set(text.rectTransform,new Vector2(360,175),new Vector2(330,58));text.color=new Color(.35f,.18f,.08f);}
                if(text.name=="Legend"){text.fontSize=text.fontSizeMax=18;text.fontSizeMin=18;text.text="나: 하늘색  ·  P: 포탈  ·  S: 상점";text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,32);}
                if(text.name=="QuestTrackerHeader"){text.text="현재 퀘스트";Set(text.rectTransform,new Vector2(0,185),new Vector2(420,48));}
                if(text.name.StartsWith("QuestTrackerHeader ("))text.gameObject.SetActive(false);
                if(text.name.StartsWith("MaterialLabel_"))Set(text.rectTransform,new Vector2(text.rectTransform.anchoredPosition.x,-198),new Vector2(154,68));
                if(parent=="UpgradeWindow" && text.name=="Message")Set(text.rectTransform,new Vector2(0,-238),new Vector2(790,32));
                if(text.name=="QuestChoiceTitle"){text.fontSize=text.fontSizeMax=28;Set(text.rectTransform,new Vector2(0,12),new Vector2(600,52));}
                if(text.name=="QuestChoicePrompt"){text.fontSize=text.fontSizeMax=25;text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,42);}
                if(text.name=="Title" && parent=="CharacterSelectionUI"){text.fontSize=text.fontSizeMax=60;text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,84);}
                if(parent=="GameStartButton")text.color=new Color(.25f,.12f,.04f);
                if(text.name=="DialougueText" || text.name=="DialogueText")text.color=new Color(.25f,.12f,.06f);
                if(text.transform.parent.name=="MinimapPanel")text.color=new Color(.98f,.93f,.78f);
                EditorUtility.SetDirty(text);
            }
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if(scene && PrefabUtility.IsPartOfPrefabInstance(rect))continue;
                if(rect.name=="ShopWindow" && rect.parent!=null && rect.parent.GetComponent<Canvas>()!=null)rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,980);
                if(rect.parent!=null && rect.parent.name.StartsWith("ShopSlot") && (rect.name=="Icon" || rect.name=="Placeholder"))rect.anchoredPosition=new Vector2(0,50);
                if(rect.parent!=null && rect.parent.parent!=null && rect.parent.parent.name=="ShopWindow" && rect.parent.GetComponent<Button>()!=null && rect.name=="Label"){var button=rect.parent as RectTransform;button.anchoredPosition=new Vector2(button.anchoredPosition.x,315);}
                if(rect.name=="SkillSlot"){float y=rect.anchoredPosition.y;rect.sizeDelta=new Vector2(80,80);rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,y>0?65:y>-80?-25:-115);}
                if(rect.name=="CostBar")rect.anchoredPosition=new Vector2(0,-278);
                if(rect.name=="Confirm" && rect.parent.name=="UpgradeWindow")rect.anchoredPosition=new Vector2(0,-324);
                if(rect.name=="SkillTooltip")rect.sizeDelta=new Vector2(420,500);
                if(rect.name=="ItemTooltip")rect.sizeDelta=new Vector2(420,620);
                if(rect.name=="DialoguePanel")rect.anchoredPosition=new Vector2(0,190);
                if(rect.name=="Divider" && rect.parent.name=="QuestTrackerPanel")Set(rect,new Vector2(0,150),new Vector2(420,2));
                if(rect.name=="QuestListViewport")Set(rect,new Vector2(0,-32),new Vector2(420,336));
                if(rect.name=="QuestTrackerPanel")
                {
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,480);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,440);
                    rect.anchorMin=rect.anchorMax=new Vector2(1,.70f);rect.pivot=Vector2.one;
                    rect.anchoredPosition=new Vector2(-28,0);
                }
            }
            return changed;
        }
        private static void Style(TMP_Text text)
        {
            float size=Mathf.Max(20,text.fontSize);
            if(text.name=="Quantity" || text.name=="Equipped" || text.name=="SkillName" || text.name.StartsWith("RewardCount"))size=18;
            if(text.name=="SettingsTitle")size=56;
            if(text.name=="TooltipDescription" || text.name=="Description" || text.name=="Effect" || text.name=="QuestList")size=Mathf.Max(22,size);
            text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=Mathf.Min(size,20);text.fontSizeMax=size;
            if(size==18)text.fontSizeMin=18;
            text.textWrappingMode=TextWrappingModes.Normal;
            text.overflowMode=TextOverflowModes.Ellipsis;
            if(text.name=="Hotkey")text.fontStyle|=FontStyles.Bold;
            var rect=text.rectTransform;
            if(rect.rect.height>0 && rect.rect.height<size*1.7f)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,size*1.7f);
            text.raycastTarget=false;
            if(text.GetComponentInParent<ReadableScrollText>()!=null){text.enableAutoSizing=false;text.overflowMode=TextOverflowModes.Overflow;}
        }
        private static void Set(RectTransform rect,Vector2 position,Vector2 size)
        {
            rect.anchoredPosition=position;rect.sizeDelta=size;
        }
        private static void AddScroll(TMP_Text text)
        {
            if(text.GetComponentInParent<ReadableScrollText>()!=null)return;
            var old=text.rectTransform;var parent=old.parent;
            var view=new GameObject(text.name+"Viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));
            view.layer=text.gameObject.layer;
            view.transform.SetParent(parent,false);
            var rect=view.GetComponent<RectTransform>();rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;
            rect.pivot=old.pivot;rect.anchoredPosition=old.anchoredPosition;rect.sizeDelta=old.sizeDelta;
            view.GetComponent<Image>().color=new Color(0,0,0,.01f);
            var holder=new GameObject("Content",typeof(RectTransform));
            holder.layer=text.gameObject.layer;
            holder.transform.SetParent(view.transform,false);
            var content=holder.GetComponent<RectTransform>();content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;
            content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,rect.rect.height);
            text.transform.SetParent(content,false);old.anchorMin=new Vector2(0,1);old.anchorMax=Vector2.one;
            old.pivot=new Vector2(.5f,1);old.anchoredPosition=Vector2.zero;old.sizeDelta=new Vector2(-16,rect.rect.height);
            text.enableAutoSizing=false;text.overflowMode=TextOverflowModes.Overflow;
            var scroll=view.GetComponent<ScrollRect>();scroll.viewport=rect;scroll.content=content;
            scroll.horizontal=false;scroll.scrollSensitivity=28;scroll.movementType=ScrollRect.MovementType.Clamped;
            var layout=view.AddComponent<ReadableScrollText>();var data=new SerializedObject(layout);
            data.FindProperty("text").objectReferenceValue=text;data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}