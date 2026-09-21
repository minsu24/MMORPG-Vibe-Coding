using System;
using System.Collections.Generic;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class InventorySystemSetup
    {
        private const string ItemFolder = "Assets/_Project/Data/Items";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string UiPrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string UiPrefabPath = UiPrefabFolder + "/InventoryWindow.prefab";
        private const string FontPath = "Assets/_Project/Fonts/GowunBatang-Bold SDF.asset";

        private static readonly Color PanelColor = new Color(0.035f, 0.055f, 0.06f, 0.98f);
        private static readonly Color SlotColor = new Color(0.08f, 0.105f, 0.11f, 1f);
        private static readonly Color GoldColor = new Color(0.86f, 0.66f, 0.31f, 1f);
        private static readonly Color IvoryColor = new Color(0.96f, 0.93f, 0.84f, 1f);
        private static readonly Color MutedColor = new Color(0.70f, 0.72f, 0.69f, 1f);

        [MenuItem("Eastern Fantasy/Setup Inventory System In Active Scene")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerEntity player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>(FindObjectsInactive.Include);
            if (player == null)
                throw new MissingReferenceException("The active scene needs a PlayerEntity.");

            EnsureFolder(ItemFolder);
            ItemDefinition oldFan = GetOrCreateItem(
                "OldFan", "헌 부채", "오래 사용해 가장자리가 닳은 부채입니다.",
                ItemCategory.Equipment, EquipmentSlot.Weapon, 3f, 0f,
                ConsumableEffect.None, 0f, 1, 120, 60);
            ItemDefinition oldHat = GetOrCreateItem(
                "OldHat", "헌 삿갓", "비바람의 흔적이 남은 낡은 삿갓입니다.",
                ItemCategory.Equipment, EquipmentSlot.Hat, 0f, 1f,
                ConsumableEffect.None, 0f, 1, 100, 50);
            ItemDefinition clothTop = GetOrCreateItem(
                "ClothTop", "천 상의", "가볍고 편하게 움직일 수 있는 천 상의입니다.",
                ItemCategory.Equipment, EquipmentSlot.Top, 0f, 1f,
                ConsumableEffect.None, 0f, 1, 80, 40);
            ItemDefinition clothBottom = GetOrCreateItem(
                "ClothBottom", "천 하의", "질긴 천으로 만든 기본 하의입니다.",
                ItemCategory.Equipment, EquipmentSlot.Bottom, 0f, 1f,
                ConsumableEffect.None, 0f, 1, 80, 40);
            ItemDefinition clothGloves = GetOrCreateItem(
                "ClothGloves", "천 장갑", "손을 보호해 주는 얇은 천 장갑입니다.",
                ItemCategory.Equipment, EquipmentSlot.Gloves, 0f, 1f,
                ConsumableEffect.None, 0f, 1, 60, 30);
            ItemDefinition clothShoes = GetOrCreateItem(
                "ClothShoes", "천 신발", "발소리를 줄여 주는 가벼운 천 신발입니다.",
                ItemCategory.Equipment, EquipmentSlot.Shoes, 0f, 1f,
                ConsumableEffect.None, 0f, 1, 60, 30);
            ItemDefinition healthPotion = GetOrCreateItem(
                "HealthPotion", "체력 물약", "상처를 회복시키는 붉은 물약입니다.",
                ItemCategory.Consumable, EquipmentSlot.None, 0f, 0f,
                ConsumableEffect.RestoreHealth, 30f, 99, 20, 10);
            ItemDefinition manaPotion = GetOrCreateItem(
                "ManaPotion", "마나 물약", "소모한 마나를 회복시키는 푸른 물약입니다.",
                ItemCategory.Consumable, EquipmentSlot.None, 0f, 0f,
                ConsumableEffect.RestoreMana, 30f, 99, 25, 12);

            ItemDefinition[] initialItems = Array.Empty<ItemDefinition>();
            int[] quantities = Array.Empty<int>();

            AssignInventoryToPlayerPrefab(initialItems, quantities);
            PlayerInventory sceneInventory = AssignInventory(player.gameObject, initialItems, quantities);

            InventoryWindowUI existing = UnityEngine.Object.FindFirstObjectByType<InventoryWindowUI>(FindObjectsInactive.Include);
            if (existing == null)
                existing = CreateWindow(sceneInventory);

            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null && eventSystem.GetComponent<PersistentEventSystem>() == null)
                eventSystem.gameObject.AddComponent<PersistentEventSystem>();

            EnsureFolder(UiPrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(existing.gameObject, UiPrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("The empty I-key inventory window and eight item definitions are ready.");
        }

        private static ItemDefinition GetOrCreateItem(
            string fileName,
            string displayName,
            string description,
            ItemCategory category,
            EquipmentSlot equipmentSlot,
            float attackBonus,
            float defenseBonus,
            ConsumableEffect consumableEffect,
            float useAmount,
            int maximumStack,
            int buyPrice,
            int sellPrice)
        {
            string path = ItemFolder + "/" + fileName + ".asset";
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }

            SerializedObject data = new SerializedObject(item);
            SerializedProperty id = data.FindProperty("itemId");
            if (string.IsNullOrWhiteSpace(id.stringValue))
                id.stringValue = Guid.NewGuid().ToString("N");
            data.FindProperty("displayName").stringValue = displayName;
            data.FindProperty("description").stringValue = description;
            data.FindProperty("category").enumValueIndex = (int)category;
            data.FindProperty("equipmentSlot").enumValueIndex = (int)equipmentSlot;
            data.FindProperty("attackBonus").floatValue = attackBonus;
            data.FindProperty("defenseBonus").floatValue = defenseBonus;
            data.FindProperty("consumableEffect").enumValueIndex = (int)consumableEffect;
            data.FindProperty("useAmount").floatValue = useAmount;
            data.FindProperty("maximumStack").intValue = maximumStack;
            data.FindProperty("buyPrice").intValue = buyPrice;
            data.FindProperty("sellPrice").intValue = sellPrice;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static void AssignInventoryToPlayerPrefab(ItemDefinition[] items, int[] quantities)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                AssignInventory(root, items, quantities);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static PlayerInventory AssignInventory(
            GameObject player,
            ItemDefinition[] items,
            int[] quantities)
        {
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null)
                inventory = player.AddComponent<PlayerInventory>();

            SerializedObject data = new SerializedObject(inventory);
            SerializedProperty list = data.FindProperty("initialItems");
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                SerializedProperty stack = list.GetArrayElementAtIndex(i);
                stack.FindPropertyRelative("item").objectReferenceValue = items[i];
                stack.FindPropertyRelative("quantity").intValue = quantities[i];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            return inventory;
        }

        private static InventoryWindowUI CreateWindow(PlayerInventory inventory)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            GameObject root = new GameObject(
                "InventoryOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 410;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            InventoryWindowUI controller = root.AddComponent<InventoryWindowUI>();
            GameObject panel = CreateImage("InventoryWindow", root.transform, PanelColor);
            SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1000f, 700f));

            TMP_Text title = CreateText("Title", panel.transform, "인벤토리", font, 38f, GoldColor);
            SetRect(title.rectTransform, new Vector2(0f, 300f), new Vector2(400f, 55f));

            TMP_Text currency = CreateText("Currency", panel.transform, "엽전  0", font, 23f, GoldColor);
            currency.alignment = TextAlignmentOptions.Right;
            SetRect(currency.rectTransform, new Vector2(315f, 300f), new Vector2(280f, 45f));

            Button equipmentTab = CreateTab(panel.transform, font, "장비", new Vector2(-310f, 230f));
            Button consumableTab = CreateTab(panel.transform, font, "소비", new Vector2(-100f, 230f));
            Button miscellaneousTab = CreateTab(panel.transform, font, "기타", new Vector2(110f, 230f));

            GameObject gridRoot = new GameObject("ItemGrid", typeof(RectTransform));
            gridRoot.layer = LayerMask.NameToLayer("UI");
            gridRoot.transform.SetParent(panel.transform, false);
            SetRect(gridRoot.GetComponent<RectTransform>(), new Vector2(-150f, -45f), new Vector2(500f, 470f));
            GridLayoutGroup grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(90f, 90f);
            grid.spacing = new Vector2(18f, 18f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;

            var slots = new List<InventorySlotUI>();
            for (int i = 0; i < 20; i++)
                slots.Add(CreateSlot(gridRoot.transform, font, i));

            ItemTooltipUI tooltip = CreateTooltip(panel.transform, font);

            TMP_Text hint = CreateText("CloseHint", panel.transform, "[ I ]  닫기", font, 20f, GoldColor);
            SetRect(hint.rectTransform, new Vector2(0f, -315f), new Vector2(300f, 35f));

            SerializedObject controllerData = new SerializedObject(controller);
            controllerData.FindProperty("windowRoot").objectReferenceValue = panel;
            controllerData.FindProperty("equipmentTab").objectReferenceValue = equipmentTab;
            controllerData.FindProperty("consumableTab").objectReferenceValue = consumableTab;
            controllerData.FindProperty("miscellaneousTab").objectReferenceValue = miscellaneousTab;
            controllerData.FindProperty("tooltip").objectReferenceValue = tooltip;
            controllerData.FindProperty("inventory").objectReferenceValue = inventory;
            controllerData.FindProperty("currencyText").objectReferenceValue = currency;
            SerializedProperty slotList = controllerData.FindProperty("slots");
            slotList.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
                slotList.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return controller;
        }

        private static Button CreateTab(Transform parent, TMP_FontAsset font, string label, Vector2 position)
        {
            GameObject root = CreateImage(label + "Tab", parent, SlotColor);
            SetRect(root.GetComponent<RectTransform>(), position, new Vector2(190f, 55f));
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.86f, 0.55f, 1f);
            colors.pressedColor = new Color(0.75f, 0.62f, 0.36f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.58f, 0.42f, 0.20f, 1f);
            button.colors = colors;
            TMP_Text text = CreateText("Label", root.transform, label, font, 24f, IvoryColor);
            SetRect(text.rectTransform, Vector2.zero, new Vector2(170f, 45f));
            return button;
        }

        private static InventorySlotUI CreateSlot(Transform parent, TMP_FontAsset font, int index)
        {
            GameObject root = CreateImage("InventorySlot_" + index, parent, SlotColor);
            root.AddComponent<CanvasGroup>();
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.86f, 0.55f, 1f);
            colors.pressedColor = new Color(0.72f, 0.60f, 0.36f, 1f);
            colors.disabledColor = Color.white;
            button.colors = colors;

            GameObject iconRoot = CreateImage("Icon", root.transform, Color.white);
            Image icon = iconRoot.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, Vector2.zero, new Vector2(74f, 74f));

            TMP_Text placeholder = CreateText("Placeholder", root.transform, "?", font, 38f, MutedColor);
            SetRect(placeholder.rectTransform, Vector2.zero, new Vector2(74f, 74f));

            TMP_Text quantity = CreateText("Quantity", root.transform, string.Empty, font, 18f, IvoryColor);
            quantity.alignment = TextAlignmentOptions.BottomRight;
            SetRect(quantity.rectTransform, new Vector2(0f, -31f), new Vector2(78f, 25f));

            TMP_Text equipped = CreateText("Equipped", root.transform, "장착", font, 14f, GoldColor);
            equipped.alignment = TextAlignmentOptions.TopLeft;
            SetRect(equipped.rectTransform, new Vector2(0f, 31f), new Vector2(78f, 24f));

            InventorySlotUI slot = root.AddComponent<InventorySlotUI>();
            SerializedObject data = new SerializedObject(slot);
            data.FindProperty("button").objectReferenceValue = button;
            data.FindProperty("iconImage").objectReferenceValue = icon;
            data.FindProperty("placeholderText").objectReferenceValue = placeholder;
            data.FindProperty("quantityText").objectReferenceValue = quantity;
            data.FindProperty("equippedText").objectReferenceValue = equipped;
            data.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        private static ItemTooltipUI CreateTooltip(Transform parent, TMP_FontAsset font)
        {
            GameObject controllerRoot = new GameObject("ItemTooltipController", typeof(RectTransform));
            controllerRoot.layer = LayerMask.NameToLayer("UI");
            controllerRoot.transform.SetParent(parent, false);
            SetRect(controllerRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            GameObject popup = CreateImage("ItemTooltip", controllerRoot.transform, SlotColor);
            SetRect(popup.GetComponent<RectTransform>(), new Vector2(315f, -25f), new Vector2(330f, 420f));
            popup.GetComponent<Image>().raycastTarget = false;

            TMP_Text title = CreateText("ItemName", popup.transform, string.Empty, font, 27f, GoldColor);
            title.alignment = TextAlignmentOptions.Left;
            SetRect(title.rectTransform, new Vector2(0f, 165f), new Vector2(280f, 45f));

            TMP_Text category = CreateText("Category", popup.transform, string.Empty, font, 18f, MutedColor);
            category.alignment = TextAlignmentOptions.Left;
            SetRect(category.rectTransform, new Vector2(0f, 125f), new Vector2(280f, 30f));

            TMP_Text description = CreateText("Description", popup.transform, string.Empty, font, 18f, IvoryColor);
            description.alignment = TextAlignmentOptions.TopLeft;
            description.textWrappingMode = TextWrappingModes.Normal;
            SetRect(description.rectTransform, new Vector2(0f, 35f), new Vector2(280f, 120f));

            TMP_Text effect = CreateText("Effect", popup.transform, string.Empty, font, 19f, GoldColor);
            effect.alignment = TextAlignmentOptions.TopLeft;
            effect.textWrappingMode = TextWrappingModes.Normal;
            SetRect(effect.rectTransform, new Vector2(0f, -115f), new Vector2(280f, 130f));

            ItemTooltipUI tooltip = controllerRoot.AddComponent<ItemTooltipUI>();
            SerializedObject data = new SerializedObject(tooltip);
            data.FindProperty("tooltipRoot").objectReferenceValue = popup;
            data.FindProperty("titleText").objectReferenceValue = title;
            data.FindProperty("categoryText").objectReferenceValue = category;
            data.FindProperty("descriptionText").objectReferenceValue = description;
            data.FindProperty("effectText").objectReferenceValue = effect;
            data.ApplyModifiedPropertiesWithoutUndo();
            popup.SetActive(false);
            return tooltip;
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
