using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class EquipmentSetValidation
    {
        [MenuItem("Eastern Fantasy/Inventory/Validate Item Alignment and Sets")]
        public static void Run() => Debug.Log(Validate());

        public static string Validate()
        {
            int count = ValidateAlignment();
            var root = new GameObject("SetValidation");
            root.SetActive(false);
            EquipmentSetDefinition temporarySet = null;
            ItemDefinition temporaryItem = null;
            try
            {
                var inventory = root.AddComponent<PlayerInventory>();
                var player = root.GetComponent<PlayerEntity>();
                // Initialize lifecycle on an inactive, temporary object; never touch the scene player.
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(PlayerEntity).GetMethod("Awake", flags).Invoke(player, null);
                typeof(PlayerEntity).GetMethod("Start", flags).Invoke(player, null);
                float normalSpeed = player.Speed;
                var set = AssetDatabase.LoadAssetAtPath<EquipmentSetDefinition>(EquipmentSetSetup.SetPath);
                string[] names = { "OldFan", "OldHat", "ClothTop", "ClothBottom", "ClothGloves", "ClothShoes" };
                var equipment = new List<ItemDefinition>();
                for (int i = 0; i < names.Length; i++)
                {
                    var original = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/" + names[i] + ".asset");
                    var item = original.GetGradeVariant((ItemGrade)(i % 5));
                    Check(item.EquipmentSet == set, "Grade variant lost set membership");
                    equipment.Add(item);
                    Check(inventory.AddItem(item) && inventory.TryEquip(item), "Cannot equip set item");
                    Check(inventory.GetEquippedSetCount(set) == i + 1, "Incorrect equipped count");
                    if (i < 5) Near(player.Speed, normalSpeed, "Partial set grants speed");
                }
                Near(player.Speed, normalSpeed * 1.1f, "Full set does not grant +10% speed");
                Check(inventory.TryEquip(equipment[0]), "Repeated equip failed");
                Near(player.Speed, normalSpeed * 1.1f, "Repeated equip duplicated bonus");
                Check(inventory.TryChangeGrade(equipment[0], ItemGrade.Mythic), "Equipped grade change failed");
                Near(player.Speed, normalSpeed * 1.1f, "Grade change broke set");
                Check(inventory.TryUnequip(EquipmentSlot.Shoes), "Unequip failed");
                Near(player.Speed, normalSpeed, "Unequip did not remove bonus");
                Check(inventory.TryEquip(equipment[5]), "Re-equip failed");
                Near(player.Speed, normalSpeed * 1.1f, "Re-equip did not restore bonus");
                Check(inventory.RemoveItem(equipment[5]), "Removal failed");
                Near(player.Speed, normalSpeed, "Removing equipped item retained bonus");

                // A separate set verifies cumulative thresholds and every supported stat.
                temporarySet = ScriptableObject.CreateInstance<EquipmentSetDefinition>();
                var data = new SerializedObject(temporarySet);
                var bonuses = data.FindProperty("bonuses");
                bonuses.arraySize = 2;
                for (int tier = 0; tier < 2; tier++)
                {
                    var bonus = bonuses.GetArrayElementAtIndex(tier);
                    bonus.FindPropertyRelative("requiredPieces").intValue = 1;
                    var effects = bonus.FindPropertyRelative("effects");
                    effects.arraySize = 5;
                    for (int stat = 0; stat < 5; stat++)
                    {
                        var effect = effects.GetArrayElementAtIndex(stat);
                        effect.FindPropertyRelative("stat").enumValueIndex = stat;
                        effect.FindPropertyRelative("modifier").enumValueIndex = tier;
                        effect.FindPropertyRelative("value").floatValue = tier == 0 ? 2f : 20f;
                    }
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                temporaryItem = UnityEngine.Object.Instantiate(equipment[0]);
                var itemData = new SerializedObject(temporaryItem);
                itemData.FindProperty("equipmentSet").objectReferenceValue = temporarySet;
                itemData.ApplyModifiedPropertiesWithoutUndo();
                float attackBase = player.Attack_Power;
                Check(inventory.AddItem(temporaryItem) && inventory.TryEquip(temporaryItem), "Cannot equip test set");
                Near(player.Speed, (normalSpeed + 2f) * 1.2f, "Stacked flat and percent speed");
                Near(player.Attack_Power, (attackBase + 2f) * 1.2f, "Attack effect");
                Near(player.maxHP, 122.4f, "Maximum health effect");
                Near(player.maxMP, 122.4f, "Maximum mana effect");
                Near(player.Defense, inventory.ApplySetBonuses(EquipmentSetStat.Defense,
                    5f + inventory.EquippedDefenseBonus), "Defense effect");
                Check(inventory.TryUnequip(EquipmentSlot.Weapon), "Cannot remove test set");
                Near(player.Speed, normalSpeed, "Temporary set retained bonus");
                Near(player.maxHP, 100f, "Maximum health retained bonus");
                Check(player.HP <= player.maxHP && player.MP <= player.maxMP, "Resources exceed new capacity");

                ValidateFrameCenter(equipment[1]);
                return "PASS: " + count + " unique sprites centered; mixed-grade 6-piece speed bonus; no double count; grade changes, unequip, removal; cumulative flat/percent bonuses; attack/defense/health/mana; frame alignment.";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                if (temporaryItem != null) UnityEngine.Object.DestroyImmediate(temporaryItem);
                if (temporarySet != null) UnityEngine.Object.DestroyImmediate(temporarySet);
            }
        }

        private static int ValidateAlignment()
        {
            var sprites = new HashSet<Sprite>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition"))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Check(item.Icon != null, "Missing icon: " + item.name);
                sprites.Add(item.Icon);
                if (item.GradeFrame != null) sprites.Add(item.GradeFrame);
            }
            var textures = new Dictionary<string, Texture2D>();
            try
            {
                foreach (Sprite sprite in sprites)
                {
                    string path = AssetDatabase.GetAssetPath(sprite);
                    if (!textures.TryGetValue(path, out var texture))
                    {
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        texture.LoadImage(File.ReadAllBytes(path));
                        textures.Add(path, texture);
                    }
                    Rect visible = ItemIconAlignmentSetup.VisibleBounds(texture.GetPixels32(), texture.width, texture.height, sprite.rect);
                    Check((visible.center - sprite.rect.center).magnitude <= (ArmorSetImageSetup.IsSquareIcon(path) ? .71f : .001f), "Visible bounds off-center: " + sprite.name);
                    Near((sprite.pivot - sprite.rect.size * .5f).magnitude, 0f, "Off-center pivot: " + sprite.name);
                }
            }
            finally { foreach (var texture in textures.Values) UnityEngine.Object.DestroyImmediate(texture); }
            return sprites.Count;
        }

        private static void ValidateFrameCenter(ItemDefinition item)
        {
            var parent = new GameObject("FrameValidation", typeof(RectTransform));
            try
            {
                var child = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                child.transform.SetParent(parent.transform, false);
                var icon = child.GetComponent<Image>();
                icon.sprite = item.Icon;
                icon.rectTransform.sizeDelta = new Vector2(60, 80);
                icon.rectTransform.pivot = new Vector2(.2f, .8f);
                ItemGradeVisual.Apply(icon, item);
                ItemGradeVisual.Apply(icon, item);
                var frame = parent.transform.Find("IconGradeFrame").GetComponent<Image>();
                Vector3 iconCenter = icon.rectTransform.TransformPoint(icon.rectTransform.rect.center);
                Vector3 frameCenter = frame.rectTransform.TransformPoint(frame.rectTransform.rect.center);
                Near(Vector3.Distance(iconCenter, frameCenter), 0f, "Frame not centered around icon");
                Check(frame.transform.GetSiblingIndex() < icon.transform.GetSiblingIndex(), "Frame covers icon");
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        private static void Near(float actual, float expected, string message)
        {
            if (Mathf.Abs(actual - expected) > .001f) throw new Exception(message + ": " + actual + " vs " + expected);
        }
    }
}