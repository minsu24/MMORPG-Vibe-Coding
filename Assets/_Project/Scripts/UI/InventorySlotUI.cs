using EasternFantasy.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class InventorySlotUI : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private TMP_Text equippedText;

        private PlayerInventory inventory;
        private ItemTooltipUI tooltip;
        private ItemDefinition item;
        private int slotIndex;
        private CanvasGroup canvasGroup;
        private GameObject dragVisual;

        public void Initialize(PlayerInventory playerInventory, ItemTooltipUI itemTooltip)
        {
            inventory = playerInventory;
            tooltip = itemTooltip;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            button.onClick.RemoveListener(UseOrEquip);
            button.onClick.AddListener(UseOrEquip);
            Clear();
        }

        public void Bind(ItemDefinition definition, int quantity, int index)
        {
            item = definition;
            slotIndex = index;
            bool hasItem = item != null;
            bool hasIcon = hasItem && item.Icon != null;
            iconImage.sprite = hasIcon ? item.Icon : null;
            iconImage.enabled = hasIcon;
            placeholderText.gameObject.SetActive(hasItem && !hasIcon);
            quantityText.text = hasItem && (item.Category != ItemCategory.Equipment || quantity > 1)
                ? quantity.ToString()
                : string.Empty;
            equippedText.gameObject.SetActive(hasItem && inventory.IsEquipped(item));
            button.interactable = hasItem;
        }

        public void Clear(int index = 0)
        {
            item = null;
            slotIndex = index;
            iconImage.sprite = null;
            iconImage.enabled = false;
            placeholderText.gameObject.SetActive(false);
            quantityText.text = string.Empty;
            equippedText.gameObject.SetActive(false);
            button.interactable = false;
        }

        private void UseOrEquip()
        {
            if (item != null && inventory.TryUseOrEquip(item))
                tooltip?.Show(item, inventory);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (item != null)
                tooltip?.Show(item, inventory);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip?.Hide();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (item == null)
                return;

            canvasGroup.blocksRaycasts = false;
            tooltip?.Hide();
            Canvas canvas = GetComponentInParent<Canvas>();
            dragVisual = new GameObject("InventoryDragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            dragVisual.transform.SetParent(canvas.transform, false);
            RectTransform rect = dragVisual.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64f, 64f);
            Image image = dragVisual.GetComponent<Image>();
            image.sprite = item.Icon;
            image.color = item.Icon != null ? Color.white : new Color(0.8f, 0.7f, 0.4f, 0.9f);
            image.raycastTarget = false;
            dragVisual.transform.position = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragVisual != null)
                dragVisual.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            if (dragVisual != null)
                Destroy(dragVisual);
        }

        public void OnDrop(PointerEventData eventData)
        {
            InventorySlotUI source = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventorySlotUI>()
                : null;
            if (source == null || source.item == null || source.item.Category != GetCurrentCategory())
                return;

            inventory.MoveItem(source.item, slotIndex);
        }

        private ItemCategory GetCurrentCategory()
        {
            return item != null ? item.Category : GetComponentInParent<InventoryWindowUI>().SelectedCategory;
        }
    }
}
