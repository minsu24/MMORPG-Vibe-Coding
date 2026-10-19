using System;
using System.Collections.Generic;
using System.Linq;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class PetCompanionSetup
    {
        public const string PrefabPath = "Assets/_Project/Resources/PetCompanionWindow.prefab";
        private const string Art = "Assets/_Project/Art/UI/PetCompanion/";
        private static Dictionary<string, Sprite> sprites;
        private static TMP_FontAsset font, titleFont;
        private static readonly Color Ink = new Color(.19f, .12f, .07f);
        private static readonly Color Cream = new Color(.98f, .89f, .72f);

        [MenuItem("Eastern Fantasy/Companions/Setup Companion Window")]
        public static void Run()
        {
            sprites = new Dictionary<string, Sprite>();
            Slice("Window", new[] { "Window" }, new[] { new Rect(0,0,1448,1086) });
            Slice("Backdrop", new[] { "Stage", "NameScroll", "PoseSelected", "PoseNormal" }, new[] {
                new Rect(243,307,448,354), new Rect(960,0,210,550),
                new Rect(968,644,214,195), new Rect(1186,645,211,195) });
            Slice("Buttons", new[] { "DarkButton", "LightButton", "RoundButton", "ReplayButton" }, new[] {
                new Rect(28,12,707,294), new Rect(754,22,678,250),
                new Rect(392,302,265,236), new Rect(33,879,1390,202) });
            Slice("List", new[] { "ManageTab", "ListSelected", "ListNormal", "ListLocked", "ScrollTrack", "ScrollHandle" }, new[] {
                new Rect(738,14,682,198), new Rect(108,523,990,188),
                new Rect(117,364,970,160), new Rect(117,873,980,169),
                new Rect(1176,306,100,730), new Rect(1307,422,86,376) });
            Slice("Info", new[] { "Description", "GoldBar", "LockedSlot", "SoulPlate" }, new[] {
                new Rect(733,62,712,171), new Rect(726,421,704,86),
                new Rect(1090,590,163,162), new Rect(735,920,710,134) });
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
            titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/Dokdo-Regular SDF.asset");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Goblin/Goblin_Idle.anim");
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b => b.propertyName == "m_Sprite");
            var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding).Select(k => (Sprite)k.value).ToArray();
            var root = Build(frames, clip.frameRate);
            try { PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Companion UI built from five supplied atlases; BrokenSeal tutorial gate and Goblin_Idle animation configured.");
        }

        private static void Slice(string atlas, string[] names, Rect[] regions)
        {
            string path = Art + atlas + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            var raw = new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                raw.LoadImage(System.IO.File.ReadAllBytes(path));
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var old = provider.GetSpriteRects();
                var rects = new SpriteRect[names.Length];
                for (int i=0; i<names.Length; i++)
                {
                    Rect rect = regions[i]; rect.y = raw.height - rect.y - rect.height;
                    rect = ItemIconAlignmentSetup.VisibleBounds(raw.GetPixels32(),raw.width,raw.height,rect);
                    var previous = old.FirstOrDefault(s => s.name == names[i]);
                    rects[i] = new SpriteRect { name=names[i], rect=rect, pivot=new Vector2(.5f,.5f),
                        alignment=(int)SpriteAlignment.Center, spriteID=previous!=null?previous.spriteID:GUID.Generate() };
                }
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name,r.spriteID)));
                provider.Apply();
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) sprites[sprite.name]=sprite;
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }

        private static GameObject Build(Sprite[] idle, float rate)
        {
            var root = new GameObject("PetCompanionWindow",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=5000;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1536,1024); scaler.matchWidthOrHeight=.5f;
            var window = Rect("WindowRoot",root.transform,new Rect(0,0,1448,1000));
            var wr = (RectTransform)window.transform; wr.anchorMin=Vector2.zero; wr.anchorMax=Vector2.one;
            wr.offsetMin=wr.offsetMax=Vector2.zero;
            var dim = new GameObject("Dim",typeof(RectTransform),typeof(Image)); dim.transform.SetParent(window.transform,false);
            dim.layer=LayerMask.NameToLayer("UI");
            var dr=(RectTransform)dim.transform; dr.anchorMin=Vector2.zero;dr.anchorMax=Vector2.one;dr.offsetMin=dr.offsetMax=Vector2.zero;
            dim.GetComponent<Image>().color=new Color(.015f,.012f,.015f,.78f);
            var board = Rect("CompanionPanel",window.transform,new Rect(0,0,1448,1000));
            ((RectTransform)board.transform).anchoredPosition=Vector2.zero;
            var controller=root.AddComponent<PetCompanionWindowUI>();
            // The central moonlit stage shows through the main window's transparent well.
            var stage=Rect("StageViewport",board.transform,new Rect(476,148,462,656));
            stage.AddComponent<RectMask2D>();
            var backdrop=Picture("MoonlitStage",stage.transform,"Stage",new Rect(0,0,462,656));
            float backgroundWidth=656f*sprites["Stage"].rect.width/sprites["Stage"].rect.height;
            PlaceLocal(backdrop.rectTransform,Vector2.zero,new Vector2(backgroundWidth,656));
            Picture("WindowFrame",board.transform,"Window",new Rect(0,0,1448,1000));
            Text("Title",board.transform,"혼령 동행",new Rect(135,53,280,65),58,Cream,true);
            Picture("ManagementTab",board.transform,"ManageTab",new Rect(81,149,356,79));
            Text("ManagementLabel",board.transform,"관리",new Rect(122,164,280,44),31,Ink);
            Text("ListTitle",board.transform,"동행 목록",new Rect(90,243,230,40),29,Ink,true,TextAlignmentOptions.Left);
            Text("FilterLabel",board.transform,"전체",new Rect(331,245,90,34),20,Ink);

            var viewport = Rect("ListViewport",board.transform,new Rect(84,298,349,602));
            viewport.AddComponent<RectMask2D>();
            var content=Rect("ListContent",viewport.transform,new Rect(0,0,349,648));
            var cr=(RectTransform)content.transform;cr.anchorMin=cr.anchorMax=new Vector2(.5f,1);cr.pivot=new Vector2(.5f,1);
            cr.anchoredPosition=Vector2.zero;cr.sizeDelta=new Vector2(349,648);
            var catalog=AssetDatabase.LoadAssetAtPath<PetCompanionCatalog>(PetCompanionPrefabSetup.CatalogPath);
            if(catalog==null) throw new InvalidOperationException("Create the companion prefab catalog first.");
            var companions=catalog.CompanionPrefabs.Where(p=>p!=null).GroupBy(p=>p.CompanionId).Select(g=>g.First()).ToArray();
            if(companions.Length==0) throw new InvalidOperationException("Register at least one companion prefab.");
            string[] names={companions[0].DisplayName,"???"};
            cr.sizeDelta=new Vector2(349,Mathf.Max(602,names.Length*92));
            for(int i=0;i<names.Length;i++)
            {
                var row=new GameObject("CompanionRow"+i,typeof(RectTransform),typeof(Image),typeof(Button));row.transform.SetParent(content.transform,false);
                var rr=(RectTransform)row.transform;rr.anchorMin=rr.anchorMax=new Vector2(.5f,1);rr.pivot=new Vector2(.5f,1);
                rr.anchoredPosition=new Vector2(0,-i*92);rr.sizeDelta=new Vector2(349,86);
                row.GetComponent<Image>().sprite=sprites[i==0?"ListSelected":"ListLocked"];
                var button=row.GetComponent<Button>();
                if(i==0) UnityEventTools.AddPersistentListener(button.onClick,controller.ReplayIdle);
                else UnityEventTools.AddPersistentListener(button.onClick,controller.ShowLockedCompanion);
                var portrait=Picture("Portrait",row.transform,i==0?"PoseNormal":"LockedSlot",new Rect(0,0,60,60));
                PlaceLocal(portrait.rectTransform,new Vector2(-133,0),new Vector2(60,60));
                if(i==0) { var icon=Picture("Goblin",row.transform,null,new Rect(0,0,54,66));icon.sprite=idle[0];icon.preserveAspect=true;PlaceLocal(icon.rectTransform,new Vector2(-133,0),new Vector2(54,66)); }
                var name=Text("Name",row.transform,names[i],new Rect(0,0,148,28),24,Ink,false,TextAlignmentOptions.Left);
                PlaceLocal(name.rectTransform,new Vector2(-7,13),new Vector2(148,28));
                var status=Text("Status",row.transform,i==0?"미소환":"미해금",new Rect(0,0,175,25),17,Ink,false,TextAlignmentOptions.Left);
                PlaceLocal(status.rectTransform,new Vector2(7,-17),new Vector2(175,25));
            }
            var scroll=viewport.AddComponent<ScrollRect>();scroll.viewport=(RectTransform)viewport.transform;scroll.content=cr;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
            var track=Picture("ScrollTrack",board.transform,"ScrollTrack",new Rect(433,298,24,602));
            track.raycastTarget=true;
            var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
            var handle=Picture("Handle",track.transform,"ScrollHandle",new Rect(0,0,21,120));
            handle.raycastTarget=true;
            var hr=handle.rectTransform;hr.anchorMin=Vector2.zero;hr.anchorMax=Vector2.one;hr.offsetMin=hr.offsetMax=Vector2.zero;
            scrollbar.handleRect=hr;scrollbar.targetGraphic=handle;scroll.verticalScrollbar=scrollbar;

            Picture("NameScroll",board.transform,"NameScroll",new Rect(508,199,71,278));
            Text("VerticalName",board.transform,"도\n깨\n비",new Rect(519,263,47,125),34,Ink,true);
            var goblin=Picture("GoblinPreview",board.transform,null,new Rect(591,377,277,350));goblin.sprite=idle[0];goblin.preserveAspect=true;
            Text("PetName",board.transform,"도깨비",new Rect(995,178,260,62),48,Ink,true,TextAlignmentOptions.Left);
            Text("Grade",board.transform,"일반",new Rect(1280,195,91,35),22,Ink);
            Text("Level",board.transform,"미소환",new Rect(994,253,160,35),27,Ink,false,TextAlignmentOptions.Left);
            Picture("AffinityBar",board.transform,"GoldBar",new Rect(989,293,385,33));
            Text("BarLabel",board.transform,"도깨비의 영혼 1개로 소환",new Rect(989,297,385,23),15,Cream);
            Picture("DescriptionPanel",board.transform,"Description",new Rect(971,341,421,137));
            Text("Description",board.transform,"장난을 좋아하지만 의리가 깊은\n작은 도깨비. 인연을 맺은 자를\n스스로의 친구로 여긴다.",new Rect(998,375,358,76),17.5f,Ink,false,TextAlignmentOptions.TopLeft);
            Text("StatsHeader",board.transform,"능력치",new Rect(995,495,181,39),31,Ink,true,TextAlignmentOptions.Left);
            Text("SkillsHeader",board.transform,"혼령 기술",new Rect(1227,495,150,39),28,Ink,true,TextAlignmentOptions.Left);
            var divider=Picture("StatsDivider",board.transform,null,new Rect(1194,542,1,180));divider.color=new Color(.35f,.22f,.12f,.3f);
            Text("StatNames",board.transform,"공격력\n\n혼령 등급",new Rect(998,547,129,165),22,Ink,false,TextAlignmentOptions.TopLeft);
            Text("StatValues",board.transform,"5\n\n일반",new Rect(1136,547,45,165),22,Ink,false,TextAlignmentOptions.TopRight);
            var skillImage = Picture("LockedSkill0",board.transform,"LockedSlot",new Rect(1262,562,66,66));
            skillImage.raycastTarget = true;
            skillImage.preserveAspect = true;
            var skillDefinition = AssetDatabase.LoadAssetAtPath<EasternFantasy.Skill.SkillDefinition>(PetCompanionPrefabSetup.SkillPath);
            if (skillDefinition != null) skillImage.sprite = skillDefinition.Icon;
            var skillSlot = skillImage.gameObject.AddComponent<PetCompanionSkillSlotUI>();
            var skillSlotData = new SerializedObject(skillSlot);
            skillSlotData.FindProperty("icon").objectReferenceValue = skillImage;
            skillSlotData.ApplyModifiedPropertiesWithoutUndo();
            Text("SkillsStatus",board.transform,"미해금",new Rect(1224,645,143,27),18,Ink);

            Picture("SoulPlate",board.transform,"SoulPlate",new Rect(985,760,392,76));
            var soul=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/SoulOfGoblin.asset");
            var soulIcon=Picture("SoulIcon",board.transform,null,new Rect(998,776,45,45));soulIcon.sprite=soul.Icon;soulIcon.preserveAspect=true;
            Text("SoulLabel",board.transform,"도깨비의 영혼 · 소환 재료",new Rect(1067,772,265,27),20,Ink,false,TextAlignmentOptions.Left);
            var soulCount=Text("SoulCount",board.transform,"0 / 1 개",new Rect(1067,800,265,25),18,Ink,false,TextAlignmentOptions.Left);
            var confirm=Button("Confirm",board.transform,"DarkButton","소환",new Rect(984,860,112,68),24,Cream);
            var equip=Button("Equip",board.transform,"DarkButton","장착",new Rect(1100,860,158,68),21,Cream);
            equip.interactable=false;
            var closeBottom=Button("CloseBottom",board.transform,"LightButton","닫기",new Rect(1262,860,116,68),23,Ink);
            UnityEventTools.AddPersistentListener(closeBottom.onClick,controller.Close);
            var close=Button("Close",board.transform,"RoundButton","×",new Rect(1354,33,76,73),47,Cream);
            var hint=Text("Hint",board.transform,"P 키로 열기 · ESC 키로 닫기",new Rect(479,950,452,26),17,Cream);

            var data=new SerializedObject(controller);
            Set(data,"tutorialDialogue",AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/_Project/Data/Tutorial_DIalogue/GoldFrogPetOpenDialogue.asset"));
            Set(data,"windowRoot",window);Set(data,"panel",board.transform);Set(data,"goblinPreview",goblin);
            Set(data,"closeButton",close);Set(data,"confirmButton",confirm);Set(data,"equipButton",equip);
            Set(data,"soulCountText",soulCount);Set(data,"hintText",hint);
            SetArray(data,"idleFrames",idle);
            Set(data,"soulFragment",AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/SoulFragment.asset"));
            Set(data,"companionCatalog",catalog);Set(data,"listContent",cr);
            Set(data,"companionRowTemplate",content.transform.Find("CompanionRow0").gameObject);
            Set(data,"placeholderRow",content.transform.Find("CompanionRow1").gameObject);
            content.transform.Find("CompanionRow1").gameObject.SetActive(catalog.ShowPlaceholder);
            Set(data,"selectedListFrame",sprites["ListSelected"]);Set(data,"normalListFrame",sprites["ListNormal"]);
            Set(data,"petNameText",board.transform.Find("PetName").GetComponent<TMP_Text>());
            Set(data,"verticalNameText",board.transform.Find("VerticalName").GetComponent<TMP_Text>());
            Set(data,"descriptionText",board.transform.Find("Description").GetComponent<TMP_Text>());
            Set(data,"soulIcon",soulIcon);
            Set(data,"skillSlot",skillSlot);
            foreach (var field in new[] { ("soulLabelText", "SoulLabel"), ("levelText", "Level"),
                ("statValuesText", "StatValues"), ("skillsStatusText", "SkillsStatus"), ("barLabelText", "BarLabel") })
                Set(data,field.Item1,board.transform.Find(field.Item2).GetComponent<TMP_Text>());
            Set(data,"companionStatusText",content.transform.Find("CompanionRow0/Status").GetComponent<TMP_Text>());
            data.FindProperty("idleFrameRate").floatValue=rate;
            data.ApplyModifiedPropertiesWithoutUndo();
            window.SetActive(false);
            return root;
        }

        private static GameObject Rect(string name, Transform parent, Rect box)
        {
            var root=new GameObject(name,typeof(RectTransform));root.layer=LayerMask.NameToLayer("UI");root.transform.SetParent(parent,false);
            PlaceLocal((RectTransform)root.transform,new Vector2(box.center.x-724,500-box.center.y),box.size);return root;
        }
        private static void PlaceLocal(RectTransform rect,Vector2 position,Vector2 size)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size; }
        private static Image Picture(string name,Transform parent,string sprite,Rect box)
        {
            var image=Rect(name,parent,box).AddComponent<Image>();image.sprite=sprite!=null?sprites[sprite]:null;
            image.raycastTarget=false;return image;
        }
        private static TMP_Text Text(string name,Transform parent,string value,Rect box,float size,Color color,bool title=false,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
        {
            var text=Rect(name,parent,box).AddComponent<TextMeshProUGUI>();text.font=title?titleFont:font;text.text=value;
            text.fontSize=size;text.color=color;text.alignment=alignment;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;return text;
        }
        private static Button Button(string name,Transform parent,string sprite,string label,Rect box,float size,Color color)
        {
            var image=Picture(name,parent,sprite,box);image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();
            var colors=button.colors;colors.highlightedColor=new Color(1.12f,1.08f,.96f);colors.pressedColor=new Color(.78f,.7f,.57f);button.colors=colors;
            var text=Text("Label",image.transform,label,new Rect(0,0,box.width*.86f,box.height*.68f),size,color);
            PlaceLocal(text.rectTransform,Vector2.zero,new Vector2(box.width*.86f,box.height*.68f));return button;
        }
        private static void Set(SerializedObject data,string name,UnityEngine.Object value) => data.FindProperty(name).objectReferenceValue=value;
        private static void SetArray(SerializedObject data,string name,UnityEngine.Object[] values)
        { var list=data.FindProperty(name);list.arraySize=values.Length;for(int i=0;i<values.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; }
    }
}
