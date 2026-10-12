using UnityEngine;
namespace EasternFantasy.Inventory
{
    public enum ItemGrade { None = -1, Common = 0, Rare = 1, Heroic = 2, Legendary = 3, Mythic = 4 }
    public static class ItemGradeUtility
    {
        public static string GetName(ItemGrade grade)
        {
            switch (grade)
            {
                case ItemGrade.Common: return "일반";
                case ItemGrade.Rare: return "희귀";
                case ItemGrade.Heroic: return "영웅";
                case ItemGrade.Legendary: return "설화";
                case ItemGrade.Mythic: return "신화";
                default: return string.Empty;
            }
        }
        public static Color GetColor(ItemGrade grade)
        {
            switch (grade)
            {
                case ItemGrade.Common: return new Color32(190,190,190,255);
                case ItemGrade.Rare: return new Color32(85,165,255,255);
                case ItemGrade.Heroic: return new Color32(193,115,255,255);
                case ItemGrade.Legendary: return new Color32(255,212,75,255);
                case ItemGrade.Mythic: return new Color32(255,140,50,255);
                default: return Color.white;
            }
        }
    }
}
