using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasternFantasy.Saving
{
    [Serializable]
    public sealed class SaveGameData
    {
        public int version = 1;
        public int characterClass;
        public string savedUtc, scene;
        public Vector3 position;
        public float health, mana, mental, energy;
        public int level = 1, experience, currency, skillPoints;
        public int advancementRank, unlockedTier;
        public float advancementAttack, advancementDefense;
        public List<SavedItem> items = new List<SavedItem>();
        public List<SavedSkill> skills = new List<SavedSkill>();
        public string[] consumableSlots, quickSlots;
        public List<SavedQuest> quests = new List<SavedQuest>();
        public string[] playedDialogues, rewardedDialogues, bossDefeats, dungeonDefeats, goblinDefeats;
        public string[] visitedRegions, visitedScenes, knownScenes, presentedScenes, codex;
        public bool inventoryTutorial, statsTutorial, mapTutorial, companionTutorial, companionGuide, firstDeathGuide;
        public string companionsJson;
        public bool shopTutorial, dungeonTutorial, upgradeTutorial;
        public bool toadDefeated, toadHasPosition;
        public Vector3 toadPosition;
        public string dungeon;
        public float dungeonSeconds;
        public bool dungeonCleared;
    }

    [Serializable] public sealed class SavedItem
    {
        public string id;
        public int quantity, slot;
        public bool equipped;
    }
    [Serializable] public sealed class SavedSkill { public string id; public int level; }
    [Serializable] public sealed class SavedQuest { public string id; public int amount, status; }
}
