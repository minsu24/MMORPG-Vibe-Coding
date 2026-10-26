using EasternFantasy.Inventory;
using TMPro;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class ItemTooltipUI : MonoBehaviour
    {
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text effectText;

        private bool pendingPointerHide;
        private float hideAfter;
        public void HideAfterPointerExit()
        {
            pendingPointerHide = true;
            hideAfter = Time.unscaledTime + .15f;
        }
        private void Update()
        {
            if (!pendingPointerHide || Time.unscaledTime < hideAfter) return;
            Canvas canvas = tooltipRoot != null ? tooltipRoot.GetComponentInParent<Canvas>() : null;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (tooltipRoot != null && Mouse.current != null &&
                RectTransformUtility.RectangleContainsScreenPoint(tooltipRoot.transform as RectTransform,
                    Mouse.current.position.ReadValue(), camera)) return;
            Hide();
        }
        public void Hide()
        {
            pendingPointerHide = false;
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);
        }

        public void Show(ItemDefinition item, PlayerInventory inventory)
        {
            ShowContent(item, inventory, false, false);
        }

        public void ShowShop(ItemDefinition item, PlayerInventory inventory, RectTransform anchor, bool selling)
        {
            ShowContent(item, inventory, true, selling);
            if (tooltipRoot == null || anchor == null || item == null || inventory == null) return;
            var panel = tooltipRoot.transform as RectTransform;
            var canvas = panel.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var canvasRect = canvas.rootCanvas.transform as RectTransform;
            panel.pivot = new Vector2(0f, 1f);
            Vector3 topRight = anchor.TransformPoint(new Vector3(anchor.rect.xMax, anchor.rect.yMax));
            Vector3 topLeft = anchor.TransformPoint(new Vector3(anchor.rect.xMin, anchor.rect.yMax));
            panel.position = topRight + canvasRect.TransformVector(new Vector3(18f, 0f));
            var corners = new Vector3[4];
            panel.GetWorldCorners(corners);
            if (canvasRect.InverseTransformPoint(corners[2]).x > canvasRect.rect.xMax - 18f)
                panel.position = topLeft - canvasRect.TransformVector(new Vector3(panel.rect.width + 18f, 0f));
            UIReadability.ClampToCanvas(panel);
        }

        private void ShowContent(ItemDefinition item, PlayerInventory inventory, bool shop, bool selling)
        {
            if (item == null || inventory == null || tooltipRoot == null)
                return;

            titleText.text = item.GradedDisplayName;
            titleText.color = item.GradeColor;
            categoryText.text = GetCategoryName(item.Category) + (item.IsAccessory ? " · " + AccessorySlotName(item.EquipmentSlot) : "")
                + (item.CanSell ? "" : " · 판매불가");
            descriptionText.text = item.Description;
            effectText.text = BuildEffectText(item, inventory, shop);
            if (shop)
                effectText.text += selling
                    ? (item.CanSell ? $"\n판매 가격: {item.SellPrice} 엽전\n클릭하여 판매" : "\n판매할 수 없는 아이템입니다.")
                    : $"\n구매 가격: {item.BuyPrice} 엽전\n더블클릭하여 구매";
            pendingPointerHide = false;
            tooltipRoot.SetActive(true);
            UIReadability.LayoutTooltip(tooltipRoot, titleText, categoryText, descriptionText, effectText);
        }

        private static string BuildEffectText(ItemDefinition item, PlayerInventory inventory, bool shop)
        {
            if (item.IsEquippable)
            {
                string value = item.AttackBonus > 0f
                    ? $"공격력 +{item.AttackBonus:F0}"
                    : $"방어력 +{item.DefenseBonus:F0}";
                if (item.IsAccessory && !string.IsNullOrEmpty(item.SpecialAbilityDescription))
                    value = item.SpecialAbilityDescription;
                string state = inventory.IsEquipped(item) ? "장착 중 · 더블클릭하여 해제" : "더블클릭하여 장착";
                if (!inventory.CanEquip(item) && !inventory.IsEquipped(item))
                    state = "<color=#EF8888>현재 직업은 장착할 수 없습니다.</color>";
                if (shop && inventory.CanEquip(item)) state = inventory.IsEquipped(item) ? "현재 장착 중" : "현재 직업 장착 가능";
                string classes = item.AllowedClassesLabel + (item.HasClassRestriction ? " 전용" : "");
                return value + "\n장착 가능: " + classes + "\n" + state + BuildSetText(item, inventory);
            }

            if (item.Category == ItemCategory.Consumable)
            {
                string resource = item.ConsumableEffect == ConsumableEffect.RestoreHealth ? "체력" : "마나";
                return $"{resource} {item.UseAmount:F0} 회복" + (shop ? "" : "\n클릭하여 사용");
            }

            return "보유 수량 " + inventory.GetQuantity(item);
        }

        private static string BuildSetText(ItemDefinition item, PlayerInventory inventory)
        {
            EquipmentSetDefinition set = item.EquipmentSet;
            if (set == null) return "";
            int count = inventory.GetEquippedSetCount(set);
            var text = new StringBuilder("\n\n" + set.DisplayName + " · " + count + "개 장착");
            foreach (EquipmentSetBonus bonus in set.Bonuses)
            {
                if (bonus == null) continue;
                bool active = count >= bonus.RequiredPieces;
                text.Append("\n<color=").Append(active ? "#8FDF9C" : "#AAAAAA").Append(">");
                text.Append(bonus.RequiredPieces).Append("세트: ");
                bool first = true;
                foreach (EquipmentSetEffect effect in bonus.Effects)
                {
                    if (effect == null) continue;
                    if (!first) text.Append(", ");
                    text.Append(effect.Description);
                    first = false;
                }
                text.Append(active ? " (활성)" : " (미활성)").Append("</color>");
            }
            return text.ToString();
        }

        private static string AccessorySlotName(EquipmentSlot slot) => slot == EquipmentSlot.Necklace ? "목걸이"
            : slot == EquipmentSlot.Bracelet ? "팔찌" : "반지";

        private static string GetCategoryName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Equipment: return "장비";
                case ItemCategory.Accessory: return "장신구";
                case ItemCategory.Consumable: return "소비";
                default: return "기타";
            }
        }
    }
}
