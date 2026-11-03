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
        private const string PrefabPath = "Assets/_Project/Resources/DungeonWindow.prefab";

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
            Set(panel, "NamePanel", 95, 658, 355, 355f * 277 / 805);
            Set(panel, "DungeonEmblem", 114, 675, 94, 91);
            var initialDungeon = ui.dungeons != null && ui.dungeons.Length > 0 ? ui.dungeons[0] : null;
            ui.dungeonEmblem.sprite = initialDungeon != null ? initialDungeon.emblem : null;
            ui.dungeonEmblem.gameObject.SetActive(true);
            ui.dungeonEmblem.enabled = ui.dungeonEmblem.sprite != null;
            ui.dungeonEmblem.preserveAspect = true;
            ui.entryTemplate.thumbnail.sprite = initialDungeon != null ? initialDungeon.listIcon : null;
            ui.entryTemplate.thumbnail.gameObject.SetActive(true);
            ui.entryTemplate.thumbnail.enabled = ui.entryTemplate.thumbnail.sprite != null;
            ui.entryTemplate.thumbnail.preserveAspect = true;
            Set(ui.entryTemplate.transform, "Thumbnail", 29, 20, 65, 65);
            Set(panel, "DungeonName", 222, 675, 213, 45);
            Set(panel, "DungeonCategory", 222, 724, 205, 30);
            Set(panel, "LevelPanel", 465, 650, 197, 197f * 137 / 351);
            Set(panel, "TimePanel", 669, 650, 180, 180f * 137 / 320);
            Set(panel, "MonsterPanel", 881, 650, 187, 187f * 137 / 333);
            Set(panel, "Level", 540, 658, 112, 58);
            Set(panel, "Time", 744, 658, 96, 58);
            Set(panel, "Monsters", 955, 658, 104, 58);
            ui.levelText.text = "입장 레벨\n<size=23>1</size>";
            ui.timeText.text = "제한시간\n<size=23>10:00</size>";
            ui.monsterText.text = "몬스터\n<size=23>미설정</size>";
            foreach (var text in new[] { ui.levelText, ui.timeText, ui.monsterText })
            {
                text.enableAutoSizing = false;
                text.fontSize = 17;
                text.richText = true;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Truncate;
                text.lineSpacing = 0;
                text.rectTransform.localScale = Vector3.one;
            }
            // Decorative art contains circular medallions; keep its native aspect at every size.
            foreach (string name in new[] { "NamePanel", "LevelPanel", "TimePanel", "MonsterPanel", "RewardsPanel" })
            {
                var image = panel.Find(name).GetComponent<UnityEngine.UI.Image>();
                image.type = UnityEngine.UI.Image.Type.Simple;
                image.preserveAspect = true;
                image.rectTransform.localScale = Vector3.one;
            }
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
                ui.rewardIcons[i].rectTransform.localScale = Vector3.one;
                Set(panel, "RewardCount" + i, 591 + i * 75.5f, 822, 71, 36);
                ui.rewardAmounts[i].alignment = TextAlignmentOptions.Center;
                ui.rewardAmounts[i].fontSize = 12;
                ui.rewardAmounts[i].fontSizeMax = 12;
                ui.rewardAmounts[i].fontSizeMin = 10;
                ui.rewardAmounts[i].textWrappingMode = TextWrappingModes.Normal;
            }
            // Leave a separate scrollbar lane and move the controls away from the right frame.
            Set(panel, "DungeonList", 1129, 97, 421, 630);
            Set(ui.list.transform, "Viewport", 0, 0, 421, 630);
            Set(panel, "Scrollbar", 1558, 135, 22, 567);
            Set(panel, "ScrollTop", 1558, 106, 22, 29);
            Set(panel, "ScrollBottom", 1558, 702, 22, 31);
            var rowHeight = ui.entryTemplate.GetComponent<UnityEngine.UI.LayoutElement>();
            if (rowHeight != null) rowHeight.preferredHeight = 103f * 421 / 433;
            Set(panel, "EnterDungeon", 1125, 765, 438, 438f * 444 / 2125);
            var enter = (RectTransform)ui.enterButton.transform.Find("EnterLabel");
            enter.anchorMin = Vector2.zero; enter.anchorMax = Vector2.one;
            enter.offsetMin = new Vector2(12, 10);
            enter.offsetMax = new Vector2(-12, -10);
            enter.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
            Set(panel, "Status", 1125, 731, 438, 28);
            ui.message.alignment = TextAlignmentOptions.Center;
            ui.message.fontSizeMin = 12;
            var title = ui.entryTemplate.title;
            title.alignment = TextAlignmentOptions.Center;
            Set(ui.entryTemplate.transform, "Name", 108, 21, 192, 54);
            Set(ui.entryTemplate.transform, "State", 314, 32, 55, 37);
            ui.entryTemplate.state.alignment = TextAlignmentOptions.Center;
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
