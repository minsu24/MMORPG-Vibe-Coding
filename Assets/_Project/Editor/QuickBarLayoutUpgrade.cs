using System.Collections.Generic;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class QuickBarLayoutUpgrade
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/QuickSlotBar.prefab";

        [MenuItem("Eastern Fantasy/Upgrade Skill And Consumable Bar")]
        public static void Apply()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Upgrade(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            QuickSlotBarUI sceneBar = Object.FindFirstObjectByType<QuickSlotBarUI>(FindObjectsInactive.Include);
            if (sceneBar != null)
            {
                Upgrade(sceneBar.gameObject);
                Scene scene = sceneBar.gameObject.scene;
                GameObject controls = GameObject.Find("Controls");
                TextMesh text = controls != null ? controls.GetComponent<TextMesh>() : null;
                if (text != null)
                    text.text = "ARROW KEYS: Move  |  SPACE: Jump  |  QWERASDF: Skills  |  1234: Items";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }

        public static void Upgrade(GameObject root)
        {
            QuickSlotBarUI controller = root.GetComponent<QuickSlotBarUI>();
            Transform bar = root.transform.Find("QuickSlots");
            if (controller == null || bar == null)
                throw new MissingReferenceException("Quick slot bar needs QuickSlotBarUI and QuickSlots.");

            QuickSkillSlotUI template = bar.GetComponentInChildren<QuickSkillSlotUI>(true);
            if (template == null)
                throw new MissingReferenceException("QuickSlots needs a QuickSkillSlotUI template.");

            // QuickSlots is only a positioning container. Keep its user-adjusted
            // anchors and anchored position while giving each group its own panel.
            Image oldBackground = bar.GetComponent<Image>();
            if (oldBackground != null)
            {
                oldBackground.enabled = false;
                oldBackground.raycastTarget = false;
            }
            RectTransform container = (RectTransform)bar;
            container.sizeDelta = new Vector2(container.sizeDelta.x,
                Mathf.Max(container.sizeDelta.y, 260f));
            RectTransform skillPanel = EnsurePanel(bar, "SkillPanel",
                new Vector2(-245f, 8f), new Vector2(430f, 238f),
                new Color(0.025f, 0.055f, 0.065f, 0.82f));
            RectTransform consumablePanel = EnsurePanel(bar, "ConsumablePanel",
                new Vector2(235f, 8f), new Vector2(360f, 132f),
                new Color(0.075f, 0.055f, 0.035f, 0.86f));

            var skillSlots = new List<QuickSkillSlotUI>();
            for (int i = 1; i <= 8; i++)
            {
                Transform child = skillPanel.Find("QuickSlot" + i) ?? bar.Find("QuickSlot" + i);
                if (child == null)
                {
                    GameObject copy = Object.Instantiate(template.gameObject, skillPanel);
                    copy.name = "QuickSlot" + i;
                    child = copy.transform;
                }
                if (child.parent != skillPanel)
                    child.SetParent(skillPanel, false);
                QuickSkillSlotUI slot = child.GetComponent<QuickSkillSlotUI>();
                RectTransform rect = (RectTransform)child;
                int column = (i - 1) % 4;
                int row = (i - 1) / 4;
                rect.anchoredPosition = new Vector2(-141f + column * 94f,
                    row == 0 ? 32f : -62f);
                rect.sizeDelta = new Vector2(70f, 70f);
                TMP_Text key = child.Find("Hotkey")?.GetComponent<TMP_Text>();
                if (key != null)
                    key.text = QuickSlotBarUI.SkillHotkeys[i - 1].ToString();
                skillSlots.Add(slot);
            }

            var itemSlots = new List<QuickConsumableSlotUI>();
            for (int i = 1; i <= 4; i++)
            {
                Transform child = consumablePanel.Find("ConsumableSlot" + i)
                    ?? bar.Find("ConsumableSlot" + i);
                if (child == null)
                {
                    GameObject copy = Object.Instantiate(template.gameObject, consumablePanel);
                    copy.name = "ConsumableSlot" + i;
                    Object.DestroyImmediate(copy.GetComponent<QuickSkillSlotUI>());
                    child = copy.transform;
                }
                if (child.parent != consumablePanel)
                    child.SetParent(consumablePanel, false);
                RectTransform rect = (RectTransform)child;
                rect.anchoredPosition = new Vector2(-120f + (i - 1) * 80f, -18f);
                rect.sizeDelta = new Vector2(64f, 70f);
                Image background = child.GetComponent<Image>();
                if (background != null)
                    background.color = new Color(0.08f, 0.07f, 0.04f, 0.96f);

                TMP_Text key = child.Find("Hotkey")?.GetComponent<TMP_Text>();
                TMP_Text quantity = child.Find("Cooldown")?.GetComponent<TMP_Text>();
                if (key != null)
                    key.text = i.ToString();
                if (quantity != null)
                {
                    quantity.fontSize = 20f;
                    quantity.alignment = TextAlignmentOptions.BottomLeft;
                }
                QuickConsumableSlotUI slot = child.GetComponent<QuickConsumableSlotUI>();
                if (slot == null)
                    slot = child.gameObject.AddComponent<QuickConsumableSlotUI>();
                SerializedObject slotData = new SerializedObject(slot);
                slotData.FindProperty("iconImage").objectReferenceValue = child.Find("Icon")?.GetComponent<Image>();
                slotData.FindProperty("placeholderText").objectReferenceValue = child.Find("SkillName")?.GetComponent<TMP_Text>();
                slotData.FindProperty("quantityText").objectReferenceValue = quantity;
                slotData.FindProperty("hotkeyText").objectReferenceValue = key;
                slotData.ApplyModifiedPropertiesWithoutUndo();
                itemSlots.Add(slot);
            }

            TMP_Text labelTemplate = template.transform.Find("Hotkey")?.GetComponent<TMP_Text>();
            EnsureGroupLabel(bar, skillPanel, "Skill Group Label", "스킬", 90f, labelTemplate);
            EnsureGroupLabel(bar, consumablePanel, "Consumable Group Label", "소모품", 43f, labelTemplate);

            SerializedObject data = new SerializedObject(controller);
            SerializedProperty skills = data.FindProperty("slots");
            skills.arraySize = skillSlots.Count;
            for (int i = 0; i < skillSlots.Count; i++)
                skills.GetArrayElementAtIndex(i).objectReferenceValue = skillSlots[i];
            SerializedProperty items = data.FindProperty("consumableSlots");
            items.arraySize = itemSlots.Count;
            for (int i = 0; i < itemSlots.Count; i++)
                items.GetArrayElementAtIndex(i).objectReferenceValue = itemSlots[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform EnsurePanel(Transform parent, string objectName,
            Vector2 position, Vector2 size, Color color)
        {
            Transform existing = parent.Find(objectName);
            GameObject item = existing != null ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            if (existing == null)
                item.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image background = item.GetComponent<Image>();
            background.color = color;
            background.raycastTarget = false;
            Outline outline = item.GetComponent<Outline>();
            if (outline == null)
                outline = item.AddComponent<Outline>();
            outline.effectColor = new Color(0.66f, 0.48f, 0.23f, 0.18f);
            outline.effectDistance = new Vector2(2f, -2f);
            return rect;
        }

        private static void EnsureGroupLabel(Transform oldParent, Transform parent,
            string objectName, string label, float y, TMP_Text template)
        {
            Transform existing = parent.Find(objectName) ?? oldParent.Find(objectName);
            GameObject item = existing != null ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            if (item.transform.parent != parent)
                item.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(300f, 28f);
            TMP_Text text = item.GetComponent<TMP_Text>();
            if (template != null)
                text.font = template.font;
            text.text = label;
            text.fontSize = 20f;
            text.color = new Color(1f, 0.88f, 0.6f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }
    }
}
