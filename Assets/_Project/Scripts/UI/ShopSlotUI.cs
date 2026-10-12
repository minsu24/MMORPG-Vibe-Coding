using EasternFantasy.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class ShopSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;

        private ShopWindowUI owner;
        private ItemDefinition item;

        public void Initialize(ShopWindowUI shopWindow)
        {
            owner = shopWindow;
            button.onClick.RemoveAllListeners();
            Clear();
        }

        public void Bind(ItemDefinition definition, int quantity, bool selling)
        {
            item = definition;
            bool hasIcon = item != null && item.Icon != null;
            iconImage.sprite = hasIcon ? item.Icon : null;
            iconImage.enabled = hasIcon;
            ItemGradeVisual.Apply(iconImage, item);
            placeholderText.gameObject.SetActive(item != null && !hasIcon);
            nameText.text = item != null ? item.GradedDisplayName +
                (item.HasClassRestriction ? "\n" + item.AllowedClassesLabel + " 전용" : "") : string.Empty;
            nameText.enableAutoSizing = true;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            priceText.fontSize = 19f;
            priceText.enableAutoSizing = true;
            priceText.fontSizeMin = 18f;
            priceText.fontSizeMax = 19f;
            priceText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.fontSizeMin = 18f;
            nameText.fontSizeMax = 20f;
            nameText.rectTransform.anchoredPosition = new Vector2(0f, -17f);
            nameText.rectTransform.sizeDelta = new Vector2(165f, 68f);
            priceText.rectTransform.anchoredPosition = new Vector2(0f, -68f);
            priceText.rectTransform.sizeDelta = new Vector2(165f, 34f);
            int price = item == null ? 0 : (selling ? item.SellPrice : item.BuyPrice);
            priceText.text = item == null
                ? string.Empty
                : $"{price} 엽전" + (selling ? $"  ×{quantity}" : string.Empty);
            button.interactable = item != null;
        }

        public void Clear()
        {
            item = null;
            iconImage.sprite = null;
            iconImage.enabled = false;
            ItemGradeVisual.Apply(iconImage, null);
            placeholderText.gameObject.SetActive(false);
            nameText.text = string.Empty;
            priceText.text = string.Empty;
            button.interactable = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (item != null && button.interactable
                && eventData.button == PointerEventData.InputButton.Left
                && (owner.IsSelling || eventData.clickCount >= 2))
                owner.ProcessItem(item);
        }
    }
}
