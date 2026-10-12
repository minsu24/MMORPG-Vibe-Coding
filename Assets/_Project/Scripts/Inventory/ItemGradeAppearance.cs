using UnityEngine;
namespace EasternFantasy.Inventory
{
    [CreateAssetMenu(fileName = "ItemGradeAppearance", menuName = "Eastern Fantasy/Inventory/Grade Appearance")]
    public sealed class ItemGradeAppearance : ScriptableObject
    {
        [SerializeField] private Sprite[] icons = new Sprite[5];
        [SerializeField] private Sprite[] frames = new Sprite[5];
        public Sprite GetIcon(ItemGrade grade) => Get(icons, grade);
        public Sprite GetFrame(ItemGrade grade) => Get(frames, grade);
        private static Sprite Get(Sprite[] sprites, ItemGrade grade)
        {
            int index = (int)grade;
            return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
        }
    }
}
