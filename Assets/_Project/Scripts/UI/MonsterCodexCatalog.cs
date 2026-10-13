using System;
using System.Collections.Generic;
using UnityEngine;
namespace EasternFantasy.UI
{
    [CreateAssetMenu(menuName = "Eastern Fantasy/Monster Codex")]
    public sealed class MonsterCodexCatalog : ScriptableObject
    {
        public List<CodexRegion> regions = new List<CodexRegion>();
    }
    [Serializable] public sealed class CodexRegion
    {
        public string id;
        public string name;
        public bool hiddenUntilVisited;
        public List<CodexMonster> monsters = new List<CodexMonster>();
    }
    [Serializable] public sealed class CodexMonster
    {
        public string id;
        public string name;
        public Sprite portrait;
        [TextArea(3,10)] public string description;
        [Tooltip("Legacy preset. Runtime discoveries begin empty each play session.")]
        public bool discovered;
    }
}

