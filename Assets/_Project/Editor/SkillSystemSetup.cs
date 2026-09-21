using System.Collections.Generic;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class SkillSystemSetup
    {
        private const string SkillDataFolder = "Assets/_Project/Data/Skills/Dosa";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string UiPrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string UiPrefabPath = UiPrefabFolder + "/SkillWindow.prefab";
        private const string QuickSlotPrefabPath = UiPrefabFolder + "/QuickSlotBar.prefab";
        private const string BackgroundPath = "Assets/_Project/Art/스킬창 UI.png";
        private const string FontPath = "Assets/_Project/Fonts/GowunBatang-Bold SDF.asset";

        private static readonly Color SlotColor = new Color(0.025f, 0.055f, 0.065f, 0.94f);
        private static readonly Color GoldColor = new Color(0.48f, 0.31f, 0.14f, 1f);
        private static readonly Color LightGoldColor = new Color(0.95f, 0.76f, 0.40f, 1f);
        private static readonly Color IvoryColor = new Color(0.96f, 0.93f, 0.84f, 1f);
        private static readonly Color DarkColor = new Color(0.10f, 0.13f, 0.13f, 1f);

        [MenuItem("Eastern Fantasy/Setup Skill System In Active Scene")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || player.GetComponent<PlayerProgression>() == null)
                throw new MissingReferenceException("The active scene needs the Player with PlayerProgression.");

            EnsureFolder(SkillDataFolder);
            SkillDefinition health = GetOrCreateSkill(
                "BaseHealth", "체력 증가",
                "최대 체력을 증가시키는 기본 패시브 스킬입니다.",
                "스킬 레벨당 최대 HP가 20 증가합니다.",
                SkillTier.Base, 3,
                SkillActivationType.Passive, 0f,
                SkillEffectType.MaximumHealth, 20f, null);
            SkillDefinition mana = GetOrCreateSkill(
                "BaseMana", "마나 증가",
                "최대 마나를 증가시키는 기본 패시브 스킬입니다.",
                "스킬 레벨당 최대 MP가 30 증가합니다.",
                SkillTier.Base, 3,
                SkillActivationType.Passive, 0f,
                SkillEffectType.MaximumMana, 30f, null);
            SkillDefinition speed = GetOrCreateSkill(
                "BaseMoveSpeed", "이동속도 증가",
                "이동속도를 증가시키는 기본 패시브 스킬입니다.",
                "스킬 레벨당 이동속도가 0.5 증가합니다.",
                SkillTier.Base, 3,
                SkillActivationType.Passive, 0f,
                SkillEffectType.MoveSpeed, 0.5f, null);
            SkillDefinition tripleTalisman = GetOrCreateSkill(
                "FirstTripleTalisman", "3단 부적 날리기",
                "전방으로 부적 세 장을 날리는 공격 스킬입니다.",
                "피해량, 투사체 간격과 재사용 대기시간은 여기에 작성하세요.",
                SkillTier.FirstAdvancement, 1,
                SkillActivationType.Active, 4f,
                SkillEffectType.None, 0f, mana);
            SkillDefinition teleport = GetOrCreateSkill(
                "FirstTeleport", "텔레포트",
                "바라보는 방향으로 빠르게 이동하는 스킬입니다.",
                "이동 거리, 무적 여부와 재사용 대기시간은 여기에 작성하세요.",
                SkillTier.FirstAdvancement, 1,
                SkillActivationType.Active, 6f,
                SkillEffectType.None, 0f, speed);
            SkillDefinition talismanShield = GetOrCreateSkill(
                "FirstTalismanShield", "부적 실드",
                "부적으로 보호막을 생성하는 방어 스킬입니다.",
                "보호막 수치, 지속시간과 재사용 대기시간은 여기에 작성하세요.",
                SkillTier.FirstAdvancement, 1,
                SkillActivationType.Active, 10f,
                SkillEffectType.None, 0f, health);

            SkillDefinition[] skills =
            {
                health, mana, speed,
                talismanShield, tripleTalisman, teleport
            };

            AssignSkillsToPlayerPrefab(skills, tripleTalisman, teleport, talismanShield);
            PlayerSkillSystem sceneSystem = AssignSkills(player, skills);
            PlayerActiveSkillCaster sceneCaster = AssignCaster(
                player, tripleTalisman, teleport, talismanShield);

            SkillWindowUI window = Object.FindFirstObjectByType<SkillWindowUI>();
            if (window == null)
                window = CreateWindow(sceneSystem);

            QuickSlotBarUI quickSlotBar = Object.FindFirstObjectByType<QuickSlotBarUI>();
            if (quickSlotBar == null)
                quickSlotBar = CreateQuickSlotBar(sceneSystem, sceneCaster);

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null && eventSystem.GetComponent<PersistentEventSystem>() == null)
                eventSystem.gameObject.AddComponent<PersistentEventSystem>();

            EnsureFolder(UiPrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(window.gameObject, UiPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(quickSlotBar.gameObject, QuickSlotPrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Skill points, six Dosa skills, prerequisites, and the K-key skill window are ready.");
        }

        private static SkillDefinition GetOrCreateSkill(
            string fileName,
            string displayName,
            string shortDescription,
            string detailedDescription,
            SkillTier tier,
            int maximumLevel,
            SkillActivationType activationType,
            float cooldownSeconds,
            SkillEffectType effectType,
            float effectPerLevel,
            SkillDefinition prerequisite)
        {
            string path = SkillDataFolder + "/" + fileName + ".asset";
            SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(skill, path);
            }

            SerializedObject data = new SerializedObject(skill);
            data.FindProperty("displayName").stringValue = displayName;
            data.FindProperty("shortDescription").stringValue = shortDescription;
            data.FindProperty("detailedDescription").stringValue = detailedDescription;
            data.FindProperty("tier").enumValueIndex = (int)tier;
            data.FindProperty("maximumLevel").intValue = maximumLevel;
            data.FindProperty("activationType").enumValueIndex = (int)activationType;
            data.FindProperty("cooldownSeconds").floatValue = cooldownSeconds;
            data.FindProperty("effectType").enumValueIndex = (int)effectType;
            data.FindProperty("effectPerLevel").floatValue = effectPerLevel;

            SerializedProperty requirements = data.FindProperty("requirements");
            requirements.arraySize = prerequisite != null ? 1 : 0;
            if (prerequisite != null)
            {
                SerializedProperty requirement = requirements.GetArrayElementAtIndex(0);
                requirement.FindPropertyRelative("skill").objectReferenceValue = prerequisite;
                requirement.FindPropertyRelative("requiredLevel").intValue = 1;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void AssignSkillsToPlayerPrefab(
            SkillDefinition[] skills,
            SkillDefinition tripleTalisman,
            SkillDefinition teleport,
            SkillDefinition talismanShield)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                AssignSkills(root, skills);
                AssignCaster(root, tripleTalisman, teleport, talismanShield);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static PlayerSkillSystem AssignSkills(GameObject player, SkillDefinition[] skills)
        {
            PlayerSkillSystem system = player.GetComponent<PlayerSkillSystem>();
            if (system == null)
                system = player.AddComponent<PlayerSkillSystem>();

            SerializedObject data = new SerializedObject(system);
            SerializedProperty list = data.FindProperty("availableSkills");
            list.arraySize = skills.Length;
            for (int i = 0; i < skills.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            return system;
        }

        private static PlayerActiveSkillCaster AssignCaster(
            GameObject player,
            SkillDefinition tripleTalisman,
            SkillDefinition teleport,
            SkillDefinition talismanShield)
        {
            PlayerActiveSkillCaster caster = player.GetComponent<PlayerActiveSkillCaster>();
            if (caster == null)
                caster = player.AddComponent<PlayerActiveSkillCaster>();

            SerializedObject data = new SerializedObject(caster);
            data.FindProperty("tripleTalismanSkill").objectReferenceValue = tripleTalisman;
            data.FindProperty("teleportSkill").objectReferenceValue = teleport;
            data.FindProperty("talismanShieldSkill").objectReferenceValue = talismanShield;
            data.ApplyModifiedPropertiesWithoutUndo();
            return caster;
        }

        private static SkillWindowUI CreateWindow(PlayerSkillSystem system)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            GameObject root = new GameObject(
                "SkillWindowOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 420;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            SkillWindowUI controller = root.AddComponent<SkillWindowUI>();
            GameObject panel = CreateImage("SkillWindow", root.transform, Color.white);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = LoadFirstSprite(BackgroundPath);
            panelImage.preserveAspect = true;
            panelImage.raycastTarget = true;
            SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1500f, 750f));

            TMP_Text title = CreateText("Title", panel.transform, "스킬", font, 36f, DarkColor);
            SetRect(title.rectTransform, new Vector2(0f, 250f), new Vector2(350f, 55f));

            TMP_Text points = CreateText("SkillPoints", panel.transform, "보유 스킬 포인트  0", font, 25f, DarkColor);
            points.alignment = TextAlignmentOptions.Right;
            SetRect(points.rectTransform, new Vector2(410f, 245f), new Vector2(420f, 45f));

            TMP_Text baseHeader = CreateText("BaseHeader", panel.transform, "0차", font, 30f, GoldColor);
            SetRect(baseHeader.rectTransform, new Vector2(-240f, 175f), new Vector2(220f, 45f));

            TMP_Text firstHeader = CreateText("FirstHeader", panel.transform, "1차", font, 30f, GoldColor);
            SetRect(firstHeader.rectTransform, new Vector2(240f, 175f), new Vector2(220f, 45f));

            float[] rowPositions = { 75f, -20f, -115f };
            foreach (float y in rowPositions)
            {
                GameObject line = CreateImage("SkillConnection", panel.transform, GoldColor);
                line.GetComponent<Image>().raycastTarget = false;
                SetRect(line.GetComponent<RectTransform>(), new Vector2(0f, y), new Vector2(370f, 5f));
            }

            var slots = new List<SkillSlotUI>();
            foreach (float y in rowPositions)
                slots.Add(CreateSlot(panel.transform, font, new Vector2(-240f, y)));
            foreach (float y in rowPositions)
                slots.Add(CreateSlot(panel.transform, font, new Vector2(240f, y)));

            SkillTooltipUI tooltip = CreateTooltip(panel.transform, font);

            TMP_Text hint = CreateText("CloseHint", panel.transform, "[ K ]  닫기", font, 22f, GoldColor);
            SetRect(hint.rectTransform, new Vector2(0f, -310f), new Vector2(350f, 40f));

            SerializedObject controllerData = new SerializedObject(controller);
            controllerData.FindProperty("windowRoot").objectReferenceValue = panel;
            controllerData.FindProperty("skillPointText").objectReferenceValue = points;
            controllerData.FindProperty("skillSystem").objectReferenceValue = system;
            controllerData.FindProperty("tooltip").objectReferenceValue = tooltip;
            SerializedProperty slotList = controllerData.FindProperty("skillSlots");
            slotList.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
                slotList.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return controller;
        }

        private static SkillSlotUI CreateSlot(Transform parent, TMP_FontAsset font, Vector2 position)
        {
            GameObject root = CreateImage("SkillSlot", parent, SlotColor);
            root.AddComponent<CanvasGroup>();
            SetRect(root.GetComponent<RectTransform>(), position, new Vector2(88f, 88f));
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.58f, 1f);
            colors.pressedColor = new Color(0.78f, 0.65f, 0.38f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = Color.white;
            button.colors = colors;

            GameObject iconRoot = CreateImage("Icon", root.transform, new Color(0.05f, 0.08f, 0.08f, 1f));
            Image icon = iconRoot.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, Vector2.zero, new Vector2(72f, 72f));

            TMP_Text placeholder = CreateText("Placeholder", root.transform, "?", font, 44f, LightGoldColor);
            SetRect(placeholder.rectTransform, Vector2.zero, new Vector2(72f, 72f));

            SkillSlotUI slot = root.AddComponent<SkillSlotUI>();
            SerializedObject data = new SerializedObject(slot);
            data.FindProperty("learnButton").objectReferenceValue = button;
            data.FindProperty("iconImage").objectReferenceValue = icon;
            data.FindProperty("placeholderText").objectReferenceValue = placeholder;
            data.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        private static QuickSlotBarUI CreateQuickSlotBar(
            PlayerSkillSystem system,
            PlayerActiveSkillCaster caster)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            GameObject root = new GameObject(
                "QuickSlotOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            // Stay above the skill window so every slot can receive drop events.
            canvas.sortingOrder = 450;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            QuickSlotBarUI controller = root.AddComponent<QuickSlotBarUI>();
            GameObject bar = CreateImage("QuickSlots", root.transform, new Color(0.02f, 0.04f, 0.05f, 0.88f));
            SetAnchoredRect(
                bar.GetComponent<RectTransform>(),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-34f, 34f),
                new Vector2(300f, 104f));

            var slots = new List<QuickSkillSlotUI>();
            for (int i = 0; i < 3; i++)
            {
                GameObject slotRoot = CreateImage(
                    "QuickSlot" + (i + 1), bar.transform, SlotColor);
                SetRect(slotRoot.GetComponent<RectTransform>(), new Vector2(-92f + i * 92f, 0f), new Vector2(82f, 82f));

                GameObject iconRoot = CreateImage("Icon", slotRoot.transform, Color.white);
                Image icon = iconRoot.GetComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetRect(icon.rectTransform, Vector2.zero, new Vector2(68f, 68f));

                TMP_Text placeholder = CreateText("SkillName", slotRoot.transform, string.Empty, font, 13f, LightGoldColor);
                placeholder.enableAutoSizing = true;
                placeholder.fontSizeMin = 9f;
                placeholder.fontSizeMax = 13f;
                placeholder.textWrappingMode = TextWrappingModes.Normal;
                SetRect(placeholder.rectTransform, Vector2.zero, new Vector2(68f, 58f));

                TMP_Text cooldown = CreateText("Cooldown", slotRoot.transform, string.Empty, font, 34f, Color.white);
                cooldown.fontStyle = FontStyles.Bold;
                SetRect(cooldown.rectTransform, Vector2.zero, new Vector2(76f, 76f));

                TMP_Text hotkey = CreateText("Hotkey", slotRoot.transform, (i + 1).ToString(), font, 18f, LightGoldColor);
                hotkey.alignment = TextAlignmentOptions.BottomRight;
                SetRect(hotkey.rectTransform, Vector2.zero, new Vector2(70f, 70f));

                QuickSkillSlotUI slot = slotRoot.AddComponent<QuickSkillSlotUI>();
                SerializedObject slotData = new SerializedObject(slot);
                slotData.FindProperty("iconImage").objectReferenceValue = icon;
                slotData.FindProperty("placeholderText").objectReferenceValue = placeholder;
                slotData.FindProperty("cooldownText").objectReferenceValue = cooldown;
                slotData.FindProperty("hotkeyText").objectReferenceValue = hotkey;
                slotData.ApplyModifiedPropertiesWithoutUndo();
                slots.Add(slot);
            }

            TMP_Text currency = CreateText("Currency", root.transform, "엽전  0", font, 28f, LightGoldColor);
            currency.alignment = TextAlignmentOptions.Right;
            SetAnchoredRect(
                currency.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-34f, -30f),
                new Vector2(300f, 54f));

            SerializedObject controllerData = new SerializedObject(controller);
            SerializedProperty slotList = controllerData.FindProperty("slots");
            slotList.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
                slotList.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            controllerData.FindProperty("currencyText").objectReferenceValue = currency;
            controllerData.FindProperty("skillSystem").objectReferenceValue = system;
            controllerData.FindProperty("caster").objectReferenceValue = caster;
            controllerData.FindProperty("currency").objectReferenceValue = system.GetComponent<PlayerCurrency>();
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static SkillTooltipUI CreateTooltip(Transform parent, TMP_FontAsset font)
        {
            GameObject controllerRoot = new GameObject("SkillTooltipController", typeof(RectTransform));
            controllerRoot.layer = LayerMask.NameToLayer("UI");
            controllerRoot.transform.SetParent(parent, false);
            SetRect(controllerRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            GameObject popup = CreateImage("SkillTooltip", controllerRoot.transform, SlotColor);
            SetRect(popup.GetComponent<RectTransform>(), Vector2.zero, new Vector2(410f, 230f));
            popup.GetComponent<Image>().raycastTarget = false;

            TMP_Text title = CreateText("TooltipTitle", popup.transform, string.Empty, font, 24f, LightGoldColor);
            title.alignment = TextAlignmentOptions.Left;
            SetRect(title.rectTransform, new Vector2(0f, 78f), new Vector2(360f, 46f));

            TMP_Text description = CreateText("TooltipDescription", popup.transform, string.Empty, font, 17f, IvoryColor);
            description.alignment = TextAlignmentOptions.TopLeft;
            description.textWrappingMode = TextWrappingModes.Normal;
            SetRect(description.rectTransform, new Vector2(0f, -28f), new Vector2(360f, 150f));

            SkillTooltipUI tooltip = controllerRoot.AddComponent<SkillTooltipUI>();
            SerializedObject data = new SerializedObject(tooltip);
            data.FindProperty("tooltipRoot").objectReferenceValue = popup;
            data.FindProperty("titleText").objectReferenceValue = title;
            data.FindProperty("descriptionText").objectReferenceValue = description;
            data.ApplyModifiedPropertiesWithoutUndo();
            popup.SetActive(false);
            return tooltip;
        }

        private static Sprite LoadFirstSprite(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite)
                    return sprite;

            throw new MissingReferenceException("No Sprite was found at " + path);
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            TMP_FontAsset font,
            float size,
            Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(parent, false);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(parent, false);
            Image image = root.AddComponent<Image>();
            image.color = color;
            return root;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetAnchoredRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
