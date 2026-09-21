using EasternFantasy.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class ShopSlotUI : MonoBehaviour
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
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
            Clear();
        }

        public void Bind(ItemDefinition definition, int quantity, bool selling)
        {
            item = definition;
            bool hasIcon = item != null && item.Icon != null;
            iconImage.sprite = hasIcon ? item.Icon : null;
            iconImage.enabled = hasIcon;
            placeholderText.gameObject.SetActive(item != null && !hasIcon);
            nameText.text = item != null ? item.DisplayName : string.Empty;
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
            placeholderText.gameObject.SetActive(false);
            nameText.text = string.Empty;
            priceText.text = string.Empty;
            button.interactable = false;
        }

        private void Select()
        {
            if (item != null)
                owner.ProcessItem(item);
        }
    }
}
