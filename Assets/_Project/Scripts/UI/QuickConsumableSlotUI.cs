using EasternFantasy.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class QuickConsumableSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private TMP_Text hotkeyText;

        private PlayerInventory inventory;
        private int slotIndex;

        public void Initialize(PlayerInventory playerInventory, int index)
        {
            inventory = playerInventory;
            slotIndex = index;
            if (hotkeyText != null)
                hotkeyText.text = (index + 1).ToString();
            Refresh();
        }

        public void Refresh()
        {
            if (inventory == null || slotIndex < 0
                || slotIndex >= inventory.ConsumableSlots.Count)
                return;

            ItemDefinition item = inventory.ConsumableSlots[slotIndex];
            int quantity = item != null ? inventory.GetQuantity(item) : 0;
            if (iconImage != null)
            {
                iconImage.sprite = item != null ? item.Icon : null;
                iconImage.enabled = item != null && item.Icon != null;
                iconImage.color = quantity > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }
            if (placeholderText != null)
            {
                placeholderText.text = item != null ? item.DisplayName : string.Empty;
                placeholderText.gameObject.SetActive(item != null && item.Icon == null);
            }
            if (quantityText != null)
                quantityText.text = item != null ? quantity.ToString() : string.Empty;
        }

        public void OnDrop(PointerEventData eventData)
        {
            InventorySlotUI source = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventorySlotUI>() : null;
            if (source != null && inventory != null)
                inventory.AssignConsumableSlot(slotIndex, source.Definition);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (inventory == null)
                return;
            if (eventData.button == PointerEventData.InputButton.Right)
                inventory.ClearConsumableSlot(slotIndex);
            else if (eventData.button == PointerEventData.InputButton.Left)
                inventory.TryUseConsumableSlot(slotIndex);
        }
    }
}
