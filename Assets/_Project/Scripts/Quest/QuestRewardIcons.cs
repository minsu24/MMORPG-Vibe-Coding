using UnityEngine;

namespace EasternFantasy.Quest
{
    public sealed class QuestRewardIcons : ScriptableObject
    {
        [System.Serializable]
        public sealed class ItemIcon
        {
            public EasternFantasy.Inventory.ItemDefinition item;
            public Sprite illustration;
        }

        public Sprite yeopjeon;
        public Sprite experience;
        [SerializeField] private ItemIcon[] itemIllustrations = System.Array.Empty<ItemIcon>();

        public Sprite GetItemIcon(EasternFantasy.Inventory.ItemDefinition item)
        {
            if (item == null) return null;
            foreach (var entry in itemIllustrations)
                if (entry != null && entry.item == item && entry.illustration != null)
                    return entry.illustration;
            return item.Icon;
        }
    }
}
