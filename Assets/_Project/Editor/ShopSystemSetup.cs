using System;
using System.Collections.Generic;
using EasternFantasy.Inventory;
using EasternFantasy.Quest;
using EasternFantasy.Shop;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace EasternFantasy.Editor
{
    public static class ShopSystemSetup
    {
        private const string ItemFolder = "Assets/_Project/Data/Items";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string UiPrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string UiPrefabPath = UiPrefabFolder + "/ShopWindow.prefab";
        private const string FontPath = "Assets/_Project/Fonts/GowunBatang-Bold SDF.asset";

        private static readonly Color PanelColor = new Color(0.035f, 0.05f, 0.055f, 0.985f);
        private static readonly Color CardColor = new Color(0.08f, 0.105f, 0.11f, 1f);
        private static readonly Color GoldColor = new Color(0.88f, 0.68f, 0.32f, 1f);
        private static readonly Color IvoryColor = new Color(0.96f, 0.93f, 0.84f, 1f);
        private static readonly Color MutedColor = new Color(0.70f, 0.72f, 0.69f, 1f);

        [MenuItem("Eastern Fantasy/Setup Shop System In Prologue Village")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject shopOwner = GameObject.Find("상점주인");
            if (shopOwner == null)
                throw new MissingReferenceException("Open PrologueVilage and make sure an object named 상점주인 exists.");

            ItemDefinition[] stock = LoadStock();
            ConfigurePlayerPrefab();
            ConfigureQuestRewards();
            ConfigureShopkeeper(shopOwner, stock);
            EnsureEventSystem();

            ShopWindowUI window = UnityEngine.Object.FindFirstObjectByType<ShopWindowUI>(FindObjectsInactive.Include);
            if (window == null)
                window = CreateWindow();

            EnsureFolder(UiPrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(window.gameObject, UiPrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Yeopjeon rewards, shopkeeper interaction, and buying/selling UI are ready.");
        }

        private static ItemDefinition[] LoadStock()
        {
            string[] names =
            {
                "OldFan", "OldHat", "ClothTop", "ClothBottom",
                "ClothGloves", "ClothShoes", "HealthPotion", "ManaPotion"
            };
            var stock = new List<ItemDefinition>();
            foreach (string name in names)
            {
                ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    ItemFolder + "/" + name + ".asset");
                if (item == null)
                    throw new MissingReferenceException("Missing item asset: " + name);
                stock.Add(item);
            }
            return stock.ToArray();
        }

        private static void ConfigurePlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<PlayerCurrency>() == null)
                    root.AddComponent<PlayerCurrency>();

                PlayerInventory inventory = root.GetComponent<PlayerInventory>();
                if (inventory == null)
                    inventory = root.AddComponent<PlayerInventory>();
                SerializedObject data = new SerializedObject(inventory);
                data.FindProperty("initialItems").arraySize = 0;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureQuestRewards()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:QuestDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                QuestDefinition quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(path);
                SerializedObject data = new SerializedObject(quest);
                SerializedProperty reward = data.FindProperty("rewardYeopjeon");
                if (reward.intValue <= 0)
                    reward.intValue = 20;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(quest);
            }
        }

        private static void ConfigureShopkeeper(GameObject shopOwner, ItemDefinition[] stock)
        {
            Collider2D collider = shopOwner.GetComponent<Collider2D>();
            if (collider == null)
            {
                CircleCollider2D circle = shopOwner.AddComponent<CircleCollider2D>();
                circle.radius = 1.4f;
                circle.isTrigger = true;
            }

            Shopkeeper shopkeeper = shopOwner.GetComponent<Shopkeeper>();
            if (shopkeeper == null)
                shopkeeper = shopOwner.AddComponent<Shopkeeper>();

            SerializedObject data = new SerializedObject(shopkeeper);
            SerializedProperty list = data.FindProperty("stock");
            list.arraySize = stock.Length;
            for (int i = 0; i < stock.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = stock[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
                return;

            GameObject root = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule),
                typeof(PersistentEventSystem));
            root.layer = LayerMask.NameToLayer("UI");
        }

        private static ShopWindowUI CreateWindow()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            GameObject root = new GameObject(
                "ShopOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 430;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            ShopWindowUI controller = root.AddComponent<ShopWindowUI>();
            GameObject panel = CreateImage("ShopWindow", root.transform, PanelColor);
            SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(940f, 700f));

            TMP_Text title = CreateText("Title", panel.transform, "상점", font, 40f, GoldColor);
            SetRect(title.rectTransform, new Vector2(0f, 300f), new Vector2(300f, 55f));

            TMP_Text currency = CreateText("Currency", panel.transform, "보유 엽전  0", font, 23f, GoldColor);
            currency.alignment = TextAlignmentOptions.Right;
            SetRect(currency.rectTransform, new Vector2(300f, 300f), new Vector2(280f, 45f));

            Button buyTab = CreateTab(panel.transform, font, "구매", new Vector2(-115f, 235f));
            Button sellTab = CreateTab(panel.transform, font, "판매", new Vector2(115f, 235f));

            GameObject gridRoot = new GameObject("ShopGrid", typeof(RectTransform));
            gridRoot.layer = LayerMask.NameToLayer("UI");
            gridRoot.transform.SetParent(panel.transform, false);
            SetRect(gridRoot.GetComponent<RectTransform>(), new Vector2(0f, -30f), new Vector2(800f, 450f));
            GridLayoutGroup grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180f, 130f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;

            var slots = new List<ShopSlotUI>();
            for (int i = 0; i < 12; i++)
                slots.Add(CreateSlot(gridRoot.transform, font, i));

            TMP_Text message = CreateText("Message", panel.transform, string.Empty, font, 20f, IvoryColor);
            SetRect(message.rectTransform, new Vector2(0f, -272f), new Vector2(700f, 38f));
            TMP_Text hint = CreateText("CloseHint", panel.transform, "[ ESC ]  닫기", font, 18f, GoldColor);
            SetRect(hint.rectTransform, new Vector2(0f, -320f), new Vector2(260f, 30f));

            SerializedObject data = new SerializedObject(controller);
            data.FindProperty("windowRoot").objectReferenceValue = panel;
            data.FindProperty("buyTab").objectReferenceValue = buyTab;
            data.FindProperty("sellTab").objectReferenceValue = sellTab;
            data.FindProperty("currencyText").objectReferenceValue = currency;
            data.FindProperty("messageText").objectReferenceValue = message;
            SerializedProperty slotList = data.FindProperty("slots");
            slotList.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
                slotList.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            return controller;
        }

        private static Button CreateTab(Transform parent, TMP_FontAsset font, string label, Vector2 position)
        {
            GameObject root = CreateImage(label + "Tab", parent, CardColor);
            SetRect(root.GetComponent<RectTransform>(), position, new Vector2(210f, 52f));
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            TMP_Text text = CreateText("Label", root.transform, label, font, 23f, IvoryColor);
            SetRect(text.rectTransform, Vector2.zero, new Vector2(190f, 42f));
            return button;
        }

        private static ShopSlotUI CreateSlot(Transform parent, TMP_FontAsset font, int index)
        {
            GameObject root = CreateImage("ShopSlot_" + index, parent, CardColor);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();

            GameObject iconRoot = CreateImage("Icon", root.transform, Color.white);
            Image icon = iconRoot.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(0f, 25f), new Vector2(58f, 58f));

            TMP_Text placeholder = CreateText("Placeholder", root.transform, "?", font, 32f, MutedColor);
            SetRect(placeholder.rectTransform, new Vector2(0f, 25f), new Vector2(58f, 58f));
            TMP_Text itemName = CreateText("ItemName", root.transform, string.Empty, font, 17f, IvoryColor);
            SetRect(itemName.rectTransform, new Vector2(0f, -18f), new Vector2(165f, 28f));
            TMP_Text price = CreateText("Price", root.transform, string.Empty, font, 15f, GoldColor);
            SetRect(price.rectTransform, new Vector2(0f, -46f), new Vector2(165f, 24f));

            ShopSlotUI slot = root.AddComponent<ShopSlotUI>();
            SerializedObject data = new SerializedObject(slot);
            data.FindProperty("button").objectReferenceValue = button;
            data.FindProperty("iconImage").objectReferenceValue = icon;
            data.FindProperty("placeholderText").objectReferenceValue = placeholder;
            data.FindProperty("nameText").objectReferenceValue = itemName;
            data.FindProperty("priceText").objectReferenceValue = price;
            data.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, TMP_FontAsset font, float size, Color color)
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
