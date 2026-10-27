using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.Quest
{
    // Build inside the existing dialogue text area, so every scene shares the same reward presentation.
    public sealed class QuestRewardRowUI : MonoBehaviour
    {
        private readonly List<GameObject> entries = new List<GameObject>();
        private RectTransform row;
        private TMP_Text heading;

        public static QuestRewardRowUI Create(TMP_Text source)
        {
            var root = new GameObject("Quest Reward Icons", typeof(RectTransform), typeof(QuestRewardRowUI));
            root.transform.SetParent(source.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return root.GetComponent<QuestRewardRowUI>();
        }

        public void Show(QuestDefinition quest, TMP_Text source)
        {
            gameObject.SetActive(true);
            foreach (var entry in entries) Destroy(entry);
            entries.Clear();
            float fontSize = source.fontSize;
            float iconSize = fontSize * 1.3f;
            if (heading == null)
            {
                heading = Label(transform, "Reward Heading", source);
                heading.rectTransform.sizeDelta = new Vector2(600, fontSize * 1.2f);
                row = new GameObject("Reward Row", typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(transform, false);
                row.anchorMin = row.anchorMax = row.pivot = new Vector2(.5f, .5f);
            }
            heading.text = "보상 획득";
            heading.fontSize = fontSize * .8f;
            heading.rectTransform.anchoredPosition = new Vector2(0, fontSize * 1.05f);
            row.anchoredPosition = new Vector2(0, -fontSize * .55f);
            var icons = Resources.Load<QuestRewardIcons>("UI/QuestRewards/Icons");
            float width = 0;
            if (quest.RewardYeopjeon > 0)
                Add("Yeopjeon", icons != null ? icons.yeopjeon : null, quest.RewardYeopjeon, source, iconSize, ref width);
            if (quest.RewardEXP > 0)
                Add("Experience", icons != null ? icons.experience : null, quest.RewardEXP, source, iconSize, ref width);
            foreach (var reward in quest.RewardItems)
                if (reward != null && reward.Item != null && reward.Quantity > 0)
                    Add(reward.Item.name, icons != null ? icons.GetItemIcon(reward.Item) : reward.Item.Icon,
                        reward.Quantity, source, iconSize, ref width);
            float gap = fontSize * .9f;
            width = Mathf.Max(0, width - gap);
            row.sizeDelta = new Vector2(width, iconSize);
            row.localScale = Vector3.one * Mathf.Min(1f, Mathf.Max(1f, source.rectTransform.rect.width - 32f) / Mathf.Max(1f, width));
            foreach (var entry in entries)
                ((RectTransform)entry.transform).anchoredPosition -= Vector2.right * width * .5f;
            if (entries.Count == 0) heading.text = "퀘스트를 완료했습니다.";
        }

        private void Add(string name, Sprite sprite, int amount, TMP_Text source, float iconSize, ref float x)
        {
            var entry = new GameObject(name + " Reward", typeof(RectTransform));
            entry.transform.SetParent(row, false);
            entries.Add(entry);
            var rect = (RectTransform)entry.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = new Vector2(x, 0);
            var image = new GameObject(name + " Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(entry.transform, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(0, .5f);
            image.rectTransform.sizeDelta = Vector2.one * iconSize;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var count = Label(entry.transform, "Quantity", source);
            count.text = $"× {amount:N0}";
            count.alignment = TextAlignmentOptions.MidlineLeft;
            float textWidth = count.GetPreferredValues(count.text).x + 4;
            count.rectTransform.anchorMin = count.rectTransform.anchorMax = count.rectTransform.pivot = new Vector2(0, .5f);
            count.rectTransform.anchoredPosition = new Vector2(iconSize + source.fontSize * .25f, 0);
            count.rectTransform.sizeDelta = new Vector2(textWidth, iconSize);
            float width = iconSize + source.fontSize * .25f + textWidth;
            rect.sizeDelta = new Vector2(width, iconSize);
            x += width + source.fontSize * .9f;
        }

        private static TMP_Text Label(Transform parent, string name, TMP_Text source)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(.5f, .5f);
            label.font = source.font;
            label.fontSharedMaterial = source.fontSharedMaterial;
            label.fontSize = source.fontSize;
            label.color = source.color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }
    }
}
