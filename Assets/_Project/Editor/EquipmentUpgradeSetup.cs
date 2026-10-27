using System;
using System.Collections.Generic;
using System.Linq;
using EasternFantasy.Dialogue;
using EasternFantasy.EquipmentUpgrade;
using EasternFantasy.Inventory;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class EquipmentUpgradeSetup
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/EquipmentUpgradeWindow.prefab";
        public const string RecipeFolder = "Assets/_Project/Data/Items/UpgradeRecipes";
        private const string Art = "Assets/_Project/Art/UI/EquipmentUpgrade/";
        private const string ScenePath = "Assets/_Project/Scenes/TutorialMap/PrologueVilage.unity";
        private static TMP_FontAsset font;
        private static readonly Color Gold = new Color(.96f,.84f,.58f);
        private static Dictionary<string, Sprite> sprites;

        [MenuItem("Eastern Fantasy/Inventory/Setup Equipment Upgrade UI")]
        public static void Run()
        {
            ImportSprites();
            var recipes = CreateRecipes();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
            GameObject root = BuildWindow(recipes);
            try { PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            ConnectVillage();
            AssetDatabase.SaveAssets();
            Debug.Log("Equipment upgrade UI and blacksmith configured. Eight recipes are intentionally unconfigured until materials/costs are entered.");
        }

        private static void ImportSprites()
        {
            sprites = new Dictionary<string, Sprite>();
            string[] names = { "TargetSlot", "MaterialFilled", "MaterialReady", "MaterialEmpty", "MaterialSuccess", "CostBar", "ConfirmNormal", "ConfirmPressed" };
            Rect[] rects = {
                new Rect(374,0,710,463),
                new Rect(165,490,249,239),
                new Rect(427,490,289,252),
                new Rect(740,490,251,240),
                new Rect(1007,464,273,280),
                new Rect(255,746,934,128),
                new Rect(93,887,621,153),
                new Rect(720,880,651,183)
            };
            Slice(Art+"UpgradeControls.png", names, rects);
            Slice(Art+"UpgradeWindow.png", new[]{"WindowFrame"}, new[]{new Rect(0,0,1448,1086)});
        }

        private static void Slice(string path, string[] names, Rect[] topLeftRects)
        {
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
                var pixels = raw.GetPixels32();
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var old = provider.GetSpriteRects();
                var rects = new SpriteRect[names.Length];
                for(int i=0;i<names.Length;i++)
                {
                    Rect r = topLeftRects[i];
                    r.y = raw.height-r.y-r.height;
                    r = ItemIconAlignmentSetup.VisibleBounds(pixels,raw.width,raw.height,r);
                    var previous = old.FirstOrDefault(s=>s.name==names[i]);
                    rects[i] = new SpriteRect { name=names[i], rect=r, alignment=(int)SpriteAlignment.Center,
                        pivot=new Vector2(.5f,.5f), spriteID=previous!=null?previous.spriteID:GUID.Generate() };
                }
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
                provider.Apply();
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) sprites[sprite.name]=sprite;
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }
        private static EquipmentUpgradeRecipe[] CreateRecipes()
        {
            if(!AssetDatabase.IsValidFolder(RecipeFolder)) AssetDatabase.CreateFolder("Assets/_Project/Data/Items","UpgradeRecipes");
            var result = new List<EquipmentUpgradeRecipe>();
            for(int kind=0;kind<2;kind++)
                for(int grade=0;grade<4;grade++)
                {
                    string name = ((UpgradeEquipmentKind)kind)+"_"+((ItemGrade)grade)+"_To_"+((ItemGrade)(grade+1));
                    string path = RecipeFolder+"/"+name+".asset";
                    var recipe=AssetDatabase.LoadAssetAtPath<EquipmentUpgradeRecipe>(path);
                    if(recipe==null)
                    {
                        recipe=ScriptableObject.CreateInstance<EquipmentUpgradeRecipe>();
                        AssetDatabase.CreateAsset(recipe,path);
                        var data=new SerializedObject(recipe);
                        data.FindProperty("equipmentKind").enumValueIndex=kind;
                        data.FindProperty("sourceGrade").intValue=grade;
                        data.FindProperty("materials").arraySize=1;
                        data.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(recipe);
                    }
                    result.Add(recipe);
                }
            return result.ToArray();
        }
        private static GameObject BuildWindow(EquipmentUpgradeRecipe[] recipes)
        {
            var root=new GameObject("EquipmentUpgradeOverlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.SetActive(false);
            root.layer=LayerMask.NameToLayer("UI");
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting=true;canvas.sortingOrder=460;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var controller=root.AddComponent<EquipmentUpgradeWindowUI>();
            var content=Rect("Content",root.transform,Vector2.zero,new Vector2(1920,1080));
            var contentRect=content.GetComponent<RectTransform>();
            contentRect.anchorMin=Vector2.zero;contentRect.anchorMax=Vector2.one;contentRect.sizeDelta=Vector2.zero;
            var dim=Image("Dim",content.transform,null,Vector2.zero,new Vector2(1920,1080));
            dim.color=new Color(0,0,0,.65f);
            dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.sizeDelta=Vector2.zero;
            var panel=Image("UpgradeWindow",content.transform,sprites["WindowFrame"],new Vector2(195,0),new Vector2(960,720));
            panel.preserveAspect=false;
            Text("Title",panel.transform,"장비 승급",new Vector2(0,272),new Vector2(400,50),34);
            Text("Instruction",panel.transform,"장비와 재료를 넣어 다음 등급으로 승급합니다",new Vector2(0,220),new Vector2(750,28),17);
            var target=Image("TargetSlot",panel.transform,sprites["TargetSlot"],new Vector2(0,128),new Vector2(254,150));
            target.preserveAspect=true;
            var drop=target.gameObject.AddComponent<UpgradeDropSlotUI>();
            var clear=target.gameObject.AddComponent<Button>();clear.onClick.RemoveAllListeners();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(clear.onClick,drop.Clear);
            var targetIcon=Image("Icon",target.transform,null,new Vector2(0,8),new Vector2(88,88));
            targetIcon.preserveAspect=true;targetIcon.raycastTarget=false;
            ItemGradeVisual.CenterInUpgradeFrame(targetIcon,target);
            var targetText=Text("TargetName",panel.transform,"승급 대상 장비",new Vector2(0,30),new Vector2(650,28),21);
            var next=Text("NextGrade",panel.transform,"장비를 선택해 주세요",new Vector2(0,0),new Vector2(750,24),17);
            Text("MaterialsTitle",panel.transform,"필요 재료",new Vector2(0,-40),new Vector2(450,26),20);
            var frames=new List<Image>();var icons=new List<Image>();var labels=new List<TMP_Text>();
            for(int i=0;i<4;i++)
            {
                float x=(i-1.5f)*161f;
                var frame=Image("Material_"+i,panel.transform,sprites["MaterialEmpty"],new Vector2(x,-109),new Vector2(106,106));
                frame.preserveAspect=true;
                var material=frame.gameObject.AddComponent<UpgradeDropSlotUI>();
                var so=new SerializedObject(material);so.FindProperty("materialIndex").intValue=i;so.ApplyModifiedPropertiesWithoutUndo();
                var b=frame.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;
                UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick,material.Clear);
                var icon=Image("Icon",frame.transform,null,Vector2.zero,new Vector2(80,80));
                icon.preserveAspect=true;icon.raycastTarget=false;
                ItemGradeVisual.CenterInUpgradeFrame(icon,frame);
                var label=Text("MaterialLabel_"+i,panel.transform,"재료 없음",new Vector2(x,-177),new Vector2(152,38),16);
                frames.Add(frame);icons.Add(icon);labels.Add(label);
            }
            var message=Text("Message",panel.transform,"",new Vector2(0,-208),new Vector2(790,22),16);
            var bar=Image("CostBar",panel.transform,sprites["CostBar"],new Vector2(0,-243),new Vector2(680,38));
            var cost=Text("Cost",bar.transform,"필요 엽전 미설정",Vector2.zero,new Vector2(600,30),18);
            var confirm=Button("Confirm",panel.transform,"승급 확인",new Vector2(0,-287),new Vector2(384,48),sprites["ConfirmNormal"]);
            confirm.gameObject.AddComponent<UpgradeConfirmFeedback>();
            confirm.transition=Selectable.Transition.SpriteSwap;
            confirm.spriteState=new SpriteState {highlightedSprite=sprites["ConfirmPressed"],pressedSprite=sprites["ConfirmPressed"],
                selectedSprite=sprites["ConfirmPressed"],disabledSprite=sprites["CostBar"]};
            var close=Button("Close",panel.transform,"×",new Vector2(393,272),new Vector2(48,48),null);
            close.GetComponent<Image>().color=new Color(.1f,.14f,.18f,.7f);

            var picker=Image("InventoryPicker",content.transform,null,new Vector2(-474,0),new Vector2(330,720));
            picker.color=new Color(.04f,.065f,.085f,.97f);
            var title=Text("PickerTitle",picker.transform,"보유 장비",new Vector2(0,310),new Vector2(300,44),27);
            var equipmentTab=Button("EquipmentTab",picker.transform,"장비",new Vector2(-76,251),new Vector2(140,44),sprites["ConfirmNormal"]);
            var materialTab=Button("MaterialTab",picker.transform,"재료",new Vector2(76,251),new Vector2(140,44),sprites["ConfirmNormal"]);
            var grid=Rect("Items",picker.transform,new Vector2(0,35),new Vector2(308,354));
            var layout=grid.AddComponent<GridLayoutGroup>();layout.cellSize=new Vector2(68,68);layout.spacing=new Vector2(10,10);
            layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=4;layout.childAlignment=TextAnchor.UpperCenter;
            var slots=new List<UpgradeInventorySlotUI>();
            for(int i=0;i<20;i++)
            {
                var image=Image("Item_"+i,grid.transform,null,Vector2.zero,new Vector2(68,68));image.color=new Color(.12f,.16f,.19f);
                var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
                var icon=Image("Icon",image.transform,null,Vector2.zero,new Vector2(50,50));icon.preserveAspect=true;icon.raycastTarget=false;
                var label=Text("Quantity",image.transform,"",new Vector2(20,-21),new Vector2(28,20),14);
                var slot=image.gameObject.AddComponent<UpgradeInventorySlotUI>();
                var data=new SerializedObject(slot);Set(data,"icon",icon);Set(data,"label",label);data.ApplyModifiedPropertiesWithoutUndo();slots.Add(slot);
            }
            Text("PickerHint",picker.transform,"클릭 또는 끌어 놓기로 등록\n등록된 슬롯을 누르면 취소\n재료는 승급할 때 차감됩니다\n\n[ ESC ] 닫기",new Vector2(0,-248),new Vector2(305,165),17);
            var info=new SerializedObject(controller);
            Set(info,"windowRoot",content);Set(info,"targetIcon",targetIcon);Set(info,"targetText",targetText);Set(info,"nextGradeText",next);
            Set(info,"emptyMaterialFrame",sprites["MaterialEmpty"]);Set(info,"filledMaterialFrame",sprites["MaterialFilled"]);Set(info,"readyMaterialFrame",sprites["MaterialReady"]);
            Set(info,"selectedTabSprite",sprites["ConfirmPressed"]);Set(info,"unselectedTabSprite",sprites["ConfirmNormal"]);
            Set(info,"costText",cost);Set(info,"messageText",message);Set(info,"pickerTitle",title);
            Set(info,"confirmButton",confirm);Set(info,"closeButton",close);Set(info,"equipmentTab",equipmentTab);Set(info,"materialTab",materialTab);
            SetArray(info,"materialFrames",frames.Cast<UnityEngine.Object>().ToArray());SetArray(info,"materialIcons",icons.Cast<UnityEngine.Object>().ToArray());
            SetArray(info,"materialTexts",labels.Cast<UnityEngine.Object>().ToArray());SetArray(info,"inventorySlots",slots.Cast<UnityEngine.Object>().ToArray());
            SetArray(info,"recipes",recipes.Cast<UnityEngine.Object>().ToArray());info.ApplyModifiedPropertiesWithoutUndo();
            content.SetActive(false);root.SetActive(true);
            return root;
        }
        private static void ConnectVillage()
        {
            Scene scene=SceneManager.GetSceneByPath(ScenePath);
            bool opened=!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                var blacksmith=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name.Trim()=="대장장이");
                if(blacksmith==null) throw new Exception("Blacksmith missing in PrologueVilage");
                var component=blacksmith.GetComponent<EquipmentUpgradeBlacksmith>();
                if(component==null) component=blacksmith.gameObject.AddComponent<EquipmentUpgradeBlacksmith>();
                if(blacksmith.GetComponent<Collider2D>()==null)
                {
                    var collider=blacksmith.gameObject.AddComponent<CapsuleCollider2D>();
                    collider.size=new Vector2(1.4f,2.5f);collider.offset=new Vector2(0,1.2f);collider.isTrigger=true;
                }
                var greeting=CreateGreeting(blacksmith.GetComponent<SpriteRenderer>());
                var data=new SerializedObject(component);Set(data,"greetingDialogue",greeting);data.ApplyModifiedPropertiesWithoutUndo();
                if(!scene.GetRootGameObjects().Any(r=>r.GetComponent<EquipmentUpgradeWindowUI>()!=null))
                    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
        private static DialogueSequence CreateGreeting(SpriteRenderer npc)
        {
            const string folder="Assets/_Project/Data/Tutorial_DIalogue/";
            string characterPath=folder+"BlacksmithDialogueCharacter.asset";
            var character=AssetDatabase.LoadAssetAtPath<DialogueCharacter>(characterPath);
            if(character==null)
            {
                character=ScriptableObject.CreateInstance<DialogueCharacter>();AssetDatabase.CreateAsset(character,characterPath);
                var data=new SerializedObject(character);data.FindProperty("displayName").stringValue="대장장이";
                Set(data,"portrait",npc!=null?npc.sprite:null);data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(character);
            }
            string path=folder+"BlacksmithUpgradeDialogue.asset";
            var sequence=AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
            if(sequence==null)
            {
                sequence=ScriptableObject.CreateInstance<DialogueSequence>();AssetDatabase.CreateAsset(sequence,path);
                var data=new SerializedObject(sequence);Set(data,"partnerCharacter",character);
                Set(data,"playerCharacter",AssetDatabase.LoadAssetAtPath<DialogueCharacter>(folder+"DialoguePlayer.asset"));
                var lines=data.FindProperty("lines");lines.arraySize=1;
                lines.GetArrayElementAtIndex(0).FindPropertyRelative("speaker").enumValueIndex=1;
                lines.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue="장비를 한 단계 높이고 싶나? 승급할 장비와 재료, 엽전을 준비해 보게.";
                data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(sequence);
            }
            return sequence;
        }
        private static GameObject Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var root=new GameObject(name,typeof(RectTransform));root.layer=LayerMask.NameToLayer("UI");
            root.transform.SetParent(parent,false);var rect=root.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return root;
        }
        private static Image Image(string name,Transform parent,Sprite sprite,Vector2 pos,Vector2 size)
        {
            var image=Rect(name,parent,pos,size).AddComponent<Image>();image.sprite=sprite;return image;
        }
        private static TMP_Text Text(string name,Transform parent,string value,Vector2 pos,Vector2 size,float fontSize)
        {
            var text=Rect(name,parent,pos,size).AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=fontSize;
            text.color=Gold;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=fontSize;return text;
        }
        private static Button Button(string name,Transform parent,string label,Vector2 pos,Vector2 size,Sprite sprite)
        {
            var image=Image(name,parent,sprite,pos,size);var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            Text("Label",image.transform,label,Vector2.zero,size-new Vector2(25,6),21);return button;
        }
        private static void Set(SerializedObject data,string name,UnityEngine.Object value) => data.FindProperty(name).objectReferenceValue=value;
        private static void SetArray(SerializedObject data,string name,UnityEngine.Object[] values)
        {
            var array=data.FindProperty(name);array.arraySize=values.Length;
            for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
        }
    }
}