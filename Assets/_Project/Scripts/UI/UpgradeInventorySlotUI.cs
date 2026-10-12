using EasternFantasy.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace EasternFantasy.UI {
    public sealed class UpgradeInventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMPro.TMP_Text label;
        private EquipmentUpgradeWindowUI window;
        private GameObject drag;
        public ItemDefinition Definition { get; private set; }
        public void Initialize(EquipmentUpgradeWindowUI owner)
        {
            window = owner;
            GetComponent<Button>().onClick.AddListener(() => window.SelectFromInventory(Definition));
        }
        public void Bind(ItemDefinition item, int quantity)
        {
            Definition = item;
            icon.sprite = item != null ? item.Icon : null;
            icon.enabled = item != null;
            ItemGradeVisual.Apply(icon, item);
            label.text = item != null ? quantity.ToString() : "";
            GetComponent<Button>().interactable = item != null;
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if (Definition == null) return;
            drag = new GameObject("UpgradeDrag", typeof(RectTransform), typeof(Image));
            drag.transform.SetParent(GetComponentInParent<Canvas>().transform, false);
            var image = drag.GetComponent<Image>();
            image.sprite = Definition.Icon;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(64, 64);
            ItemGradeVisual.ApplyDrag(image, Definition);
            var canvas = drag.AddComponent<Canvas>();
            canvas.overrideSorting = true; canvas.sortingOrder = 600;
            drag.transform.position = e.position;
        }
        public void OnDrag(PointerEventData e) { if (drag != null) drag.transform.position = e.position; }
        public void OnEndDrag(PointerEventData e) { if (drag != null) Destroy(drag); }
        private void OnDisable() { if (drag != null) Destroy(drag); }
    }

}