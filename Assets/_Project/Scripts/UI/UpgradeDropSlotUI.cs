using EasternFantasy.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace EasternFantasy.UI {
    public sealed class UpgradeDropSlotUI : MonoBehaviour, IDropHandler
    {
        [SerializeField] private int materialIndex = -1;
        private EquipmentUpgradeWindowUI window;
        public void Initialize(EquipmentUpgradeWindowUI owner) => window = owner;
        public void OnDrop(PointerEventData e)
        {
            if (e.pointerDrag == null) return;
            var picker = e.pointerDrag.GetComponent<UpgradeInventorySlotUI>();
            var inventory = e.pointerDrag.GetComponent<InventorySlotUI>();
            ItemDefinition item = picker != null ? picker.Definition : inventory != null ? inventory.Definition : null;
            window?.DropItem(item, materialIndex);
        }
        public void Clear() => window?.ClearSlot(materialIndex);
    }

    // A non-interactable Button still receives pointer events on its GameObject.
    // The upgrade action is blocked while this handler explains the reason.
}