using EasternFantasy.Inventory;
using TMPro;
using UnityEngine;

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

        public void Hide()
        {
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);
        }

        public void Show(ItemDefinition item, PlayerInventory inventory)
        {
            if (item == null || inventory == null || tooltipRoot == null)
                return;

            titleText.text = item.DisplayName;
            categoryText.text = GetCategoryName(item.Category);
            descriptionText.text = item.Description;
            effectText.text = BuildEffectText(item, inventory);
            tooltipRoot.SetActive(true);
        }

        private static string BuildEffectText(ItemDefinition item, PlayerInventory inventory)
        {
            if (item.Category == ItemCategory.Equipment)
            {
                string value = item.AttackBonus > 0f
                    ? $"공격력 +{item.AttackBonus:F0}"
                    : $"방어력 +{item.DefenseBonus:F0}";
                string state = inventory.IsEquipped(item) ? "장착 중 · 클릭하여 해제" : "클릭하여 장착";
                return value + "\n" + state;
            }

            if (item.Category == ItemCategory.Consumable)
            {
                string resource = item.ConsumableEffect == ConsumableEffect.RestoreHealth ? "체력" : "마나";
                return $"{resource} {item.UseAmount:F0} 회복\n클릭하여 사용";
            }

            return "보유 수량 " + inventory.GetQuantity(item);
        }

        private static string GetCategoryName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Equipment: return "장비";
                case ItemCategory.Consumable: return "소비";
                default: return "기타";
            }
        }
    }
}
