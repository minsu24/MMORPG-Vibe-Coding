using EasternFantasy.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text emptyLabel;
        private PlayerInventory inventory;
        private ItemTooltipUI tooltip;
        private CanvasGroup canvasGroup;
        private GameObject dragVisual;

        public EquipmentSlot Slot => slot;

        public void Initialize(PlayerInventory playerInventory, ItemTooltipUI itemTooltip)
        {
            inventory = playerInventory;
            tooltip = itemTooltip;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Refresh();
        }

        public void SetInventory(PlayerInventory playerInventory)
        {
            inventory = playerInventory;
            Refresh();
        }

        public void Refresh()
        {
            ItemDefinition item = inventory != null ? inventory.GetEquipped(slot) : null;
            icon.sprite = item != null ? item.Icon : null;
            icon.enabled = item != null && item.Icon != null;
            ItemGradeVisual.Apply(icon, item);
            emptyLabel.gameObject.SetActive(item == null || item.Icon == null);
            emptyLabel.text = item != null ? item.DisplayName : SlotName(slot);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2)
                inventory?.TryUnequip(slot);
        }

        public void OnDrop(PointerEventData eventData)
        {
            InventorySlotUI source = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventorySlotUI>() : null;
            if (source != null && source.Definition != null
                && source.Definition.EquipmentSlot == slot)
                inventory?.TryEquip(source.Definition);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ItemDefinition item = inventory != null ? inventory.GetEquipped(slot) : null;
            if (item == null)
                return;
            canvasGroup.blocksRaycasts = false;
            tooltip?.Hide();
            Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null)
            {
                canvasGroup.blocksRaycasts = true;
                return;
            }
            dragVisual = new GameObject("EquipmentDragIcon", typeof(RectTransform), typeof(Image));
            dragVisual.transform.SetParent(canvas.transform, false);
            dragVisual.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
            Image image = dragVisual.GetComponent<Image>();
            image.sprite = item.Icon;
            image.color = item.Icon != null ? Color.white : new Color(0.8f, 0.7f, 0.4f, 0.9f);
            image.raycastTarget = false;
            ItemGradeVisual.ApplyDrag(image, item);
            Canvas dragCanvas = dragVisual.AddComponent<Canvas>();
            dragCanvas.overrideSorting = true;
            dragCanvas.sortingLayerID = canvas.sortingLayerID;
            dragCanvas.sortingOrder = canvas.sortingOrder + 1;
            dragVisual.transform.SetAsLastSibling();
            UpdateDragPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragVisual != null)
                UpdateDragPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDrag();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ItemDefinition item = inventory != null ? inventory.GetEquipped(slot) : null;
            if (item != null)
                tooltip?.Show(item, inventory);
        }

        public void OnPointerExit(PointerEventData eventData) => tooltip?.HideAfterPointerExit();

        private void UpdateDragPosition(PointerEventData eventData)
        {
            if (dragVisual == null) return;
            var canvas = dragVisual.GetComponentInParent<Canvas>().rootCanvas;
            var parent = (RectTransform)dragVisual.transform.parent;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, eventData.position, camera, out Vector3 position))
                dragVisual.transform.position = position;
        }

        private void EndDrag()
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
            if (dragVisual != null) Destroy(dragVisual);
            dragVisual = null;
        }

        private void OnDisable() => EndDrag();

        private static string SlotName(EquipmentSlot value)
        {
            switch (value)
            {
                case EquipmentSlot.Weapon: return "무기";
                case EquipmentSlot.Hat: return "모자";
                case EquipmentSlot.Top: return "상의";
                case EquipmentSlot.Bottom: return "하의";
                case EquipmentSlot.Gloves: return "장갑";
                case EquipmentSlot.Shoes: return "신발";
                case EquipmentSlot.Ring: return "반지";
                case EquipmentSlot.Necklace: return "목걸이";
                case EquipmentSlot.Bracelet: return "팔찌";
                default: return string.Empty;
            }
        }
    }
}
