using System;
using EasternFantasy.Inventory;
using UnityEngine;

namespace EasternFantasy.Dungeon
{
    [CreateAssetMenu(menuName = "Eastern Fantasy/Dungeon/Definition")]
    public sealed class DungeonDefinition : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public Sprite preview;
        public string sceneName;
        public string spawnPointName = "DungeonSpawn";
        public bool available = true;
        [Min(1)] public int minimumLevel = 1;
        [Tooltip("0 means not configured.")] [Min(0)] public int timeLimitSeconds;
        [Tooltip("0 means not configured.")] [Min(0)] public int monsterCount;
        public DungeonReward[] clearRewards = Array.Empty<DungeonReward>();
    }

    [Serializable]
    public sealed class DungeonReward
    {
        public ItemDefinition item;
        [Min(1)] public int quantity = 1;
    }
}
