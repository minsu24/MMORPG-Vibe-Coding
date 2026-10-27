using System;
using System.Linq;
using System.Reflection;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.UI;
using EasternFantasy.EquipmentUpgrade;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace EasternFantasy.EditorTools
{
    public static class TutorialGoblinLootValidation
    {
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        public static string Validate()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<EquipmentUpgradeRecipe>("Assets/_Project/Data/Items/UpgradeRecipes/Weapon_Common_To_Rare.asset");
            var fragment = TutorialGoblinLootSetup.Item("SoulFragment");
            var boundFragment = TutorialGoblinLootSetup.Tutorial("SoulFragment");
            var club = TutorialGoblinLootSetup.Item("GoblinClub");
            Check(club.Grade == ItemGrade.Rare && club.Category == ItemCategory.Miscellaneous && club.Icon != null && !club.CanSell, "Club item metadata");
            Check(fragment.CanSell && fragment.SellPrice > 0 && !boundFragment.CanSell, "Fragment sale policies");
            Check(recipe.TryGetRequirements(out var requirements, out _) && requirements.Count == 2 && requirements[club] == 1 && requirements[fragment] == 1 && recipe.CostYeopjeon == 0, "Weapon recipe ingredients");
            foreach (string weapon in TutorialGoblinLootSetup.EquipmentNames.Take(3))
            foreach (bool tutorial in new[] { false, true })
            {
                var root = new GameObject("GoblinUpgradeValidation"); root.SetActive(false);
                try {
                    var inv = root.AddComponent<PlayerInventory>(); var money = root.AddComponent<PlayerCurrency>();
                    var item = tutorial ? TutorialGoblinLootSetup.Tutorial(weapon) : TutorialGoblinLootSetup.Item(weapon);
                    Check(inv.AddItem(item) && inv.AddItem(fragment) && inv.AddItem(boundFragment), "Add fixture items");
                    Check(inv.GetQuantity(fragment) == 1 && inv.GetQuantity(boundFragment) == 1 && inv.GetMaterialQuantity(fragment) == 2, "Separate stacks, shared material count");
                    Check(!inv.TryUpgradeEquipment(item, recipe, money, out _, out _), "Missing club accepted");
                    Check(inv.GetQuantity(item) == 1 && inv.GetMaterialQuantity(fragment) == 2, "Failed upgrade consumed items");
                    inv.AddItem(club);
                    Check(inv.TryUpgradeEquipment(item, recipe, money, out var result, out string error), "Valid upgrade rejected: " + error);
                    Check(result.Grade == ItemGrade.Rare && result.CanSell == !tutorial, "Upgrade changed sale policy");
                    Check(inv.GetQuantity(fragment) == 1 && inv.GetQuantity(boundFragment) == 0 && inv.GetQuantity(club) == 0, "Bound materials not consumed first");
                    Check(inv.GetQuantity(item) == 0 && inv.GetQuantity(result) == 1 && money.Yeopjeon == 0, "Incorrect upgrade result");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var fixture = new GameObject("TutorialMaterialValidation"); fixture.SetActive(false);
            GameObject upgradeUi = null, shopUi = null;
            try {
                var inv = fixture.AddComponent<PlayerInventory>(); var money = fixture.AddComponent<PlayerCurrency>();
                var pet = new PetCompanionProgress(); var soul = TutorialGoblinLootSetup.Item("SoulOfGoblin");
                inv.AddItem(TutorialGoblinLootSetup.Tutorial("SoulOfGoblin"));
                Check(pet.TryUnlock(inv, soul) && pet.TrySummon(inv, soul), "Bound goblin soul cannot summon");
                inv.AddItem(boundFragment); Check(pet.TryGrow(inv, fragment), "Bound fragment cannot grow companion");
                inv.AddItem(fragment); inv.AddItem(boundFragment);
                Check(inv.RemoveMaterial(fragment, 2) && inv.GetMaterialQuantity(fragment) == 0, "Mixed material consumption");
                var fan = TutorialGoblinLootSetup.Tutorial("OldFan");
                inv.AddItem(fan); inv.AddItem(club); inv.AddItem(boundFragment);
                upgradeUi = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EquipmentUpgradeSetup.PrefabPath));
                var upgrade = upgradeUi.GetComponent<EquipmentUpgradeWindowUI>(); upgrade.BindPlayer(inv, money);
                upgrade.DropItem(fan, -1); upgrade.SelectFromInventory(club); upgrade.SelectFromInventory(boundFragment);
                Check(upgrade.CanConfirm, "Upgrade UI rejected bound fragment");
                shopUi = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/ShopWindow.prefab"));
                var shop = shopUi.GetComponent<ShopWindowUI>();
                var data = new SerializedObject(shop);
                var window = (GameObject)data.FindProperty("windowRoot").objectReferenceValue; window.SetActive(true);
                var group = window.GetComponent<CanvasGroup>(); if (group == null) group = window.AddComponent<CanvasGroup>(); group.interactable = true;
                Set(shop, "windowGroup", group); Set(shop, "inventory", inv); Set(shop, "currency", money); Set(shop, "selling", true);
                foreach (var item in new[] { fan, club, boundFragment }) {
                    int before = inv.GetQuantity(item); shop.ProcessItem(item);
                    Check(inv.GetQuantity(item) == before && money.Yeopjeon == 0, "Shop sold a tutorial item");
                }
                inv.AddItem(fragment); shop.ProcessItem(fragment);
                Check(inv.GetQuantity(fragment) == 0 && inv.GetQuantity(boundFragment) == 1 && money.Yeopjeon == fragment.SellPrice, "Ordinary fragment sale affected bound stack");
            }
            finally {
                if (shopUi != null) UnityEngine.Object.DestroyImmediate(shopUi);
                if (upgradeUi != null) UnityEngine.Object.DestroyImmediate(upgradeUi);
                UnityEngine.Object.DestroyImmediate(fixture);
            }
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/TutorialMap/MovementPrototype.unity");
            try {
                var goblin = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_Goblin>(true)).Single();
                var loot = goblin.GetComponent<EnemyLootDrop>(); Check(loot.OverridesCommonLoot, "Global saleable loot not suppressed");
                var entries = new SerializedObject(loot).FindProperty("drops"); Check(entries.arraySize == 6, "Drop table candidate count");
                foreach (EasternFantasy.CharacterSelection.CharacterClassId playerClass in Enum.GetValues(typeof(EasternFantasy.CharacterSelection.CharacterClassId))) {
                    int weapons = 0, materials = 0;
                    for (int i = 0; i < entries.arraySize; i++) {
                        var item = (ItemDefinition)entries.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue;
                        if (!loot.CanDropForClass(item, playerClass)) continue;
                        if (item.EquipmentSlot == EquipmentSlot.Weapon) {
                            Check(item.CanBeEquippedBy(playerClass), "Wrong class weapon selected"); weapons++;
                        } else materials++;
                    }
                    Check(weapons == 1 && materials == 3, "Expected one class weapon and three materials for " + playerClass);
                }
                for (int i = 0; i < entries.arraySize; i++) {
                    var entry = entries.GetArrayElementAtIndex(i);
                    var item = (ItemDefinition)entry.FindPropertyRelative("item").objectReferenceValue;
                    Check(item != null && !item.CanSell && entry.FindPropertyRelative("dropChance").floatValue == 1, "Drop is saleable or not guaranteed");
                    Check(item.Category != ItemCategory.Equipment || (item.EquipmentSlot == EquipmentSlot.Weapon && item.Grade == ItemGrade.Common), "Armor or non-common weapon in tutorial loot");
                    Check(entry.FindPropertyRelative("minimumQuantity").intValue == 1 && entry.FindPropertyRelative("maximumQuantity").intValue == 1, "Drop quantity");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            return "PASS: 4 guaranteed unsellable drops per class (one matching common weapon and three materials); all 3 classes upgrade ordinary/tutorial weapons; missing materials rejected; bound status preserved; bound materials consumed first; mixed stacks; companion summon/growth; upgrade UI selection; shop rejects tutorial items and sells only ordinary fragments.";
        }
    }
}
