using EasternFantasy.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class ShopSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;

        private ShopWindowUI owner;
        private ItemDefinition item;
        private bool pointerInside;

        public void Initialize(ShopWindowUI shopWindow)
        {
            owner = shopWindow;
            button.onClick.RemoveAllListeners();
            ConfigureLayout();
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
            nameText.text = item != null ? item.GradedDisplayName : string.Empty;
            int price = item == null ? 0 : (selling ? item.SellPrice : item.BuyPrice);
            priceText.text = item == null
                ? string.Empty
                : $"{price} 엽전" + (selling ? $"  ×{quantity}" : string.Empty);
            if (selling && item != null && !item.CanSell) priceText.text = "판매불가";
            button.interactable = item != null && (!selling || item.CanSell);
            if (pointerInside) owner?.ShowItemTooltip(this, item);
        }

        public void Clear()
        {
            owner?.HideItemTooltip(this);
            item = null;
            iconImage.sprite = null;
            iconImage.enabled = false;
            ItemGradeVisual.Apply(iconImage, null);
            placeholderText.gameObject.SetActive(false);
            nameText.text = string.Empty;
            priceText.text = string.Empty;
            button.interactable = false;
        }

        private void ConfigureLayout()
        {
            iconImage.rectTransform.anchoredPosition = new Vector2(0f, 35f);
            iconImage.rectTransform.sizeDelta = new Vector2(100f, 100f);
            iconImage.rectTransform.localScale = Vector3.one;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            placeholderText.rectTransform.anchoredPosition = iconImage.rectTransform.anchoredPosition;
            placeholderText.rectTransform.sizeDelta = iconImage.rectTransform.sizeDelta;
            nameText.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            nameText.rectTransform.sizeDelta = new Vector2(156f, 32f);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 18f;
            nameText.fontSizeMax = 20f;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.raycastTarget = false;
            priceText.rectTransform.anchoredPosition = new Vector2(0f, -112f);
            priceText.rectTransform.sizeDelta = new Vector2(156f, 28f);
            priceText.enableAutoSizing = true;
            priceText.fontSizeMin = 16f;
            priceText.fontSizeMax = 18f;
            priceText.alignment = TextAlignmentOptions.Center;
            priceText.overflowMode = TextOverflowModes.Ellipsis;
            priceText.raycastTarget = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            if (item != null) owner?.ShowItemTooltip(this, item);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            owner?.HideItemTooltip(this, true);
        }

        private void OnDisable()
        {
            pointerInside = false;
            owner?.HideItemTooltip(this);
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
