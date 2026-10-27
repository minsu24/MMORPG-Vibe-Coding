using System;
using System.Collections.Generic;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.EquipmentUpgrade;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Shop;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class SwordswomanSwordValidation
    {
        private static ItemDefinition Item(string name) =>
            AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/" + name + ".asset");
        private static void Check(bool pass, string message) { if (!pass) throw new Exception(message); }
        [MenuItem("Eastern Fantasy/Inventory/Validate Swordswoman Sword")]
        public static void Run() => Debug.Log(Validate());
        public static string Validate()
        {
            var common = Item("SwordswomanSword");
            var fan = Item("OldFan");
            var ids = new HashSet<string>();
            for (int g = 0; g < 5; g++)
            {
                var item = common.GetGradeVariant((ItemGrade)g);
                Check(item != null && item.Grade == (ItemGrade)g, "Missing grade");
                Check(item.Icon != null && item.GradeFrame == fan.GetGradeVariant((ItemGrade)g).GradeFrame, "Missing artwork/frame");
                Check(item.EquipmentSlot == EquipmentSlot.Weapon && item.EquipmentSet == fan.EquipmentSet, "Weapon/set mismatch");
                Check(item.CanBeEquippedBy(CharacterClassId.Swordswoman) && !item.CanBeEquippedBy(CharacterClassId.Dosa)
                    && !item.CanBeEquippedBy(CharacterClassId.Monk), "Sword class permissions");
                Check(ids.Add(item.ItemId), "Duplicate sword ID");
                Check(item.ItemId != fan.GetGradeVariant((ItemGrade)g).ItemId, "Cloned fan ID");
                for (int target = 0; target < 5; target++)
                    Check(item.GetGradeVariant((ItemGrade)target) == common.GetGradeVariant((ItemGrade)target), "Broken grade chain");
            }
            var root = new GameObject("SwordValidation"); root.SetActive(false);
            var recipe = EquipmentUpgradeValidation.TestRecipe();
            try
            {
                var runtime = root.AddComponent<PlayerClassRuntime>();
                var runtimeData = new SerializedObject(runtime);
                runtimeData.FindProperty("representedClass").intValue = (int)CharacterClassId.Swordswoman;
                runtimeData.ApplyModifiedPropertiesWithoutUndo();
                var inventory = root.AddComponent<PlayerInventory>();
                var currency = root.AddComponent<PlayerCurrency>();
                var player = root.GetComponent<PlayerEntity>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(PlayerEntity).GetMethod("Awake", flags).Invoke(player, null);
                typeof(PlayerEntity).GetMethod("Start", flags).Invoke(player, null);
                float baseSpeed = player.Speed;
                Check(inventory.AddItem(fan) && !inventory.TryEquip(fan), "Swordswoman equipped fan");
                Check(inventory.AddItem(common) && inventory.TryEquip(common), "Swordswoman could not equip swords");
                foreach (string name in new[] { "OldHat", "ClothTop", "ClothBottom", "ClothGloves", "ClothShoes" })
                {
                    var item = Item(name);
                    Check(inventory.AddItem(item) && inventory.TryEquip(item), "Swordswoman armor failed");
                }
                Check(inventory.GetEquippedSetCount(common.EquipmentSet) == 6, "Swordswoman set incomplete");
                Check(Mathf.Abs(player.Speed - baseSpeed * 1.1f) < .001f, "Swordswoman speed bonus");
                inventory.AddItem(Item("BoarHorn"), 16); inventory.AddItem(Item("FrogOrb"), 4); currency.Add(400);
                var current = common;
                for (int g = 0; g < 4; g++)
                {
                    var data = new SerializedObject(recipe);
                    data.FindProperty("sourceGrade").intValue = g;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    Check(inventory.TryUpgradeEquipment(current, recipe, currency, out var upgraded, out string error), "Upgrade failed: " + error);
                    Check(upgraded == common.GetGradeVariant((ItemGrade)(g + 1)) && inventory.GetEquipped(EquipmentSlot.Weapon) == upgraded, "Equipped upgrade lost");
                    Check(inventory.GetQuantity(current) == 0 && inventory.GetQuantity(upgraded) == 1, "Upgrade duplicated weapon");
                    Check(inventory.GetEquippedSetCount(common.EquipmentSet) == 6 && Mathf.Abs(player.Speed - baseSpeed * 1.1f) < .001f, "Upgrade lost speed set");
                    current = upgraded;
                }
                Check(currency.Yeopjeon == 0 && inventory.GetQuantity(Item("BoarHorn")) == 0 && inventory.GetQuantity(Item("FrogOrb")) == 0, "Upgrade cost incorrect");
                inventory.TryUnequip(EquipmentSlot.Weapon);
                foreach (var characterClass in new[] { CharacterClassId.Dosa, CharacterClassId.Monk })
                {
                    runtimeData.Update();
                    runtimeData.FindProperty("representedClass").intValue = (int)characterClass;
                    runtimeData.ApplyModifiedPropertiesWithoutUndo();
                    Check(!inventory.TryEquip(current) && !inventory.TryUseOrEquip(current), "Other class equipped swords");
                }

                foreach (string guid in AssetDatabase.FindAssets("t:EquipmentUpgradeRecipe", new[] { EquipmentUpgradeSetup.RecipeFolder }))
                {
                    var template = AssetDatabase.LoadAssetAtPath<EquipmentUpgradeRecipe>(AssetDatabase.GUIDToAssetPath(guid));
                    for (int g = 0; g < 5; g++)
                        Check(template.Matches(common.GetGradeVariant((ItemGrade)g)) ==
                            (template.EquipmentKind == UpgradeEquipmentKind.Weapon && (int)template.SourceGrade == g), "Existing recipe filter mismatch");
                }
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/TutorialMap/PrologueVilage.unity");
                try
                {
                    bool found = false;
                    foreach (var sceneRoot in scene.GetRootGameObjects())
                        foreach (var shop in sceneRoot.GetComponentsInChildren<Shopkeeper>(true))
                            for (int i = 0; i < shop.Stock.Length; i++)
                                if (shop.Stock[i] == common) found = true;
                    Check(found, "Village shop missing");
                }
                finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
                return "PASS: 5 sword grades, unique IDs, bidirectional grade links, Swordswoman-only equipment, all 4 equipped upgrades and exact costs, 6-piece speed preserved, shared weapon recipe filters, village shop stock.";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(recipe);
            }
        }
    }
}
