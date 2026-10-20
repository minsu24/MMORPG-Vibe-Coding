using EasternFantasy.Dungeon;
using EasternFantasy.Inventory;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    // Shared by the original builder and the repair command so rebuilding preserves the layout.
    public static class DungeonWindowLayoutSetup
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/DungeonWindow.prefab";

        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var dungeon = AssetDatabase.LoadAssetAtPath<DungeonDefinition>("Assets/_Project/Data/Dungeons/GiantGarden.asset");
            var money = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/WorldCurrencyPickup.prefab");
            dungeon.obtainableRewards = new[]
            {
                new DungeonRewardPreview { displayName = "엽전", icon = money.GetComponentInChildren<SpriteRenderer>().sprite },
                Preview("BoarHorn"), Preview("GiantHeart")
            };
            EditorUtility.SetDirty(dungeon);
            AssetDatabase.SaveAssetIfDirty(dungeon);
        }

        private static DungeonRewardPreview Preview(string name) => new DungeonRewardPreview
        { item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/" + name + ".asset") };

        public static void Configure(GameObject root)
        {
            var ui = root.GetComponent<DungeonWindowUI>();
            var panel = ui.windowRoot.transform.Find("Window");
            Set(panel, "LeftParchment", 87, 83, 1000, 565);
            Set(panel, "InformationBackground", 87, 648, 1000, 213);
            Set(panel, "PreviewViewport", 96, 89, 982, 550);
            var viewport = panel.Find("PreviewViewport");
            ui.preview.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            ui.preview.rectTransform.pivot = new Vector2(.5f, 1f);
            Set(viewport, "PreviewCaptionBackground", 0, 492, 982, 58);
            Set(viewport, "Description", 22, 498, 935, 46);
            Set(panel, "NamePanel", 95, 665, 355, 355f * 277 / 805);
            Set(panel, "DungeonEmblem", 125, 694, 68, 68);
            ui.dungeonEmblem.gameObject.SetActive(false);
            ui.entryTemplate.thumbnail.gameObject.SetActive(false);
            Set(panel, "DungeonName", 222, 683, 213, 45);
            Set(panel, "DungeonCategory", 222, 738, 205, 30);
            Set(panel, "LevelPanel", 465, 650, 197, 197f * 137 / 351);
            Set(panel, "TimePanel", 669, 650, 180, 180f * 137 / 320);
            Set(panel, "MonsterPanel", 881, 650, 187, 187f * 137 / 333);
            Set(panel, "Level", 540, 668, 116, 35);
            Set(panel, "Time", 744, 668, 100, 35);
            Set(panel, "Monsters", 955, 668, 108, 35);
            ui.levelText.text = "입장 레벨  1";
            ui.timeText.text = "제한시간  10:00";
            ui.monsterText.text = "몬스터  미설정";
            ui.levelText.fontSizeMin = ui.timeText.fontSizeMin = ui.monsterText.fontSizeMin = 12;
            ui.levelText.textWrappingMode = ui.timeText.textWrappingMode = ui.monsterText.textWrappingMode = TextWrappingModes.NoWrap;
            Set(panel, "RewardSummary", 473, 724, 596, 28);
            ui.rewardText.text = "클리어 보상";
            ui.rewardText.fontSize = ui.rewardText.fontSizeMax = 17;
            ui.rewardText.fontSizeMin = 14;
            ui.rewardText.overflowMode = TextOverflowModes.Overflow;
            Set(panel, "RewardsPanel", 465, 752, 605, 605f * 185 / 1010);
            for (int i = 0; i < ui.rewardIcons.Length; i++)
            {
                // Slot centers are derived from the six square slots in the source sprite.
                Set(panel, "RewardIcon" + i, 602 + i * 75.5f, 786, 48, 48);
                ui.rewardIcons[i].preserveAspect = true;
                Set(panel, "RewardCount" + i, 591 + i * 75.5f, 822, 71, 36);
                ui.rewardAmounts[i].alignment = TextAlignmentOptions.Center;
                ui.rewardAmounts[i].fontSize = 12;
                ui.rewardAmounts[i].fontSizeMax = 12;
                ui.rewardAmounts[i].fontSizeMin = 10;
                ui.rewardAmounts[i].textWrappingMode = TextWrappingModes.Normal;
            }
            // Both controls share the center of the frame's right opening (1123..1593).
            Set(panel, "DungeonList", 1141.5f, 97, 433, 630);
            Set(ui.list.transform, "Viewport", 0, 0, 433, 630);
            Set(panel, "EnterDungeon", 1129, 765, 458, 458f * 444 / 2125);
            var enter = (RectTransform)ui.enterButton.transform.Find("EnterLabel");
            enter.anchorMin = Vector2.zero; enter.anchorMax = Vector2.one;
            enter.offsetMin = enter.offsetMax = Vector2.zero;
            enter.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
            Set(panel, "Status", 1129, 731, 458, 28);
            ui.message.alignment = TextAlignmentOptions.Center;
            ui.message.fontSizeMin = 12;
            var title = ui.entryTemplate.title;
            title.alignment = TextAlignmentOptions.Center;
            Set(ui.entryTemplate.transform, "Name", 111, 22, 198, 56);
        }

        private static void Set(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = (RectTransform)parent.Find(name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
