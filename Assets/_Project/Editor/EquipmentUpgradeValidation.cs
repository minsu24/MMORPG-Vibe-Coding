using System;
using System.Reflection;
using EasternFantasy.EquipmentUpgrade;
using EasternFantasy.Inventory;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class EquipmentUpgradeValidation
    {
        private static ItemDefinition Item(string name) =>
            AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/"+name+".asset");
        private static void Check(bool pass,string message) { if(!pass) throw new Exception(message); }

        internal static EquipmentUpgradeRecipe TestRecipe()
        {
            var recipe=ScriptableObject.CreateInstance<EquipmentUpgradeRecipe>();
            var data=new SerializedObject(recipe);
            data.FindProperty("configured").boolValue=true;
            data.FindProperty("equipmentKind").enumValueIndex=0;
            data.FindProperty("sourceGrade").intValue=0;
            data.FindProperty("costYeopjeon").intValue=100;
            var materials=data.FindProperty("materials");materials.arraySize=3;
            string[] names={"BoarHorn","FrogOrb","BoarHorn"};
            int[] quantities={2,1,2};
            for(int i=0;i<3;i++)
            {
                var material=materials.GetArrayElementAtIndex(i);
                material.FindPropertyRelative("item").objectReferenceValue=Item(names[i]);
                material.FindPropertyRelative("quantity").intValue=quantities[i];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            return recipe;
        }
        [MenuItem("Eastern Fantasy/Inventory/Validate Equipment Upgrade")]
        public static void Run() => Debug.Log(Validate());

        public static string Validate()
        {
            var recipe=TestRecipe();
            var player=new GameObject("UpgradeValidation");player.SetActive(false);
            GameObject ui=null, eventRoot=null;
            try
            {
                var inv=player.AddComponent<PlayerInventory>();
                var money=player.AddComponent<PlayerCurrency>();
                var fan=Item("OldFan");var rare=fan.GetGradeVariant(ItemGrade.Rare);
                inv.AddItem(fan);inv.TryEquip(fan);inv.AddItem(Item("BoarHorn"),4);inv.AddItem(Item("FrogOrb"));
                money.Add(99);
                Check(!inv.TryUpgradeEquipment(fan,recipe,money,out _,out string error)&&error.Contains("엽전"),"Insufficient currency accepted");
                Check(money.Yeopjeon==99 && inv.GetQuantity(fan)==1 && inv.GetQuantity(Item("BoarHorn"))==4,"Failure consumed resources");
                money.Add(1);inv.RemoveItem(Item("FrogOrb"));
                Check(!inv.TryUpgradeEquipment(fan,recipe,money,out _,out error)&&error.Contains("재료"),"Insufficient material accepted");
                Check(money.Yeopjeon==100 && inv.GetQuantity(fan)==1,"Material failure consumed resources");
                inv.AddItem(Item("FrogOrb"));
                int inventoryEvents=0,currencyEvents=0;
                Action changed=()=> { inventoryEvents++;Check(money.Yeopjeon==0&&inv.GetQuantity(rare)==1&&inv.GetQuantity(Item("BoarHorn"))==0,"Observer saw partial transaction"); };
                Action<int> currencyChanged=value=>currencyEvents++;
                inv.InventoryChanged+=changed;money.CurrencyChanged+=currencyChanged;
                Check(inv.TryUpgradeEquipment(fan,recipe,money,out var result,out error),"Valid upgrade rejected");
                inv.InventoryChanged-=changed;money.CurrencyChanged-=currencyChanged;
                Check(result==rare&&inv.GetEquipped(EquipmentSlot.Weapon)==rare&&inv.GetQuantity(fan)==0,"Upgrade lost equipment or stack");
                Check(inventoryEvents==1&&currencyEvents==1&&inv.GetQuantity(Item("FrogOrb"))==0,"Incorrect cost or duplicate material consumption");
                Check(!inv.TryUpgradeEquipment(rare,recipe,money,out _,out _),"Wrong source grade accepted");
                Check(!inv.TryUpgradeEquipment(Item("ClothTop"),recipe,money,out _,out _),"Weapon recipe accepted armor");

                foreach(string guid in AssetDatabase.FindAssets("t:EquipmentUpgradeRecipe",new[]{EquipmentUpgradeSetup.RecipeFolder}))
                {
                    var template=AssetDatabase.LoadAssetAtPath<EquipmentUpgradeRecipe>(AssetDatabase.GUIDToAssetPath(guid));
                    if (!template.Configured)
                    Check(!template.TryGetRequirements(out _,out _),"Empty template allowed free upgrade");
                    for(int g=0;g<5;g++)
                    {
                        bool expected=g==(int)template.SourceGrade;
                        Check(template.Matches(fan.GetGradeVariant((ItemGrade)g))==(expected&&template.EquipmentKind==UpgradeEquipmentKind.Weapon),"Weapon grade filter");
                        foreach(string name in new[]{"OldHat","ClothTop","ClothBottom","ClothGloves","ClothShoes"})
                            Check(template.Matches(Item(name).GetGradeVariant((ItemGrade)g))==(expected&&template.EquipmentKind==UpgradeEquipmentKind.Armor),"Armor grade filter");
                    }
                }

                inv.RemoveItem(rare);inv.AddItem(fan);
                ui=PrefabUtility.LoadPrefabContents(EquipmentUpgradeSetup.PrefabPath);
                var window=ui.GetComponent<EquipmentUpgradeWindowUI>();
                typeof(EquipmentUpgradeWindowUI).GetMethod("Initialize",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);
                var uiData=new SerializedObject(window);
                var recipes=uiData.FindProperty("recipes");recipes.arraySize=1;recipes.GetArrayElementAtIndex(0).objectReferenceValue=recipe;
                uiData.ApplyModifiedPropertiesWithoutUndo();
                ((GameObject)uiData.FindProperty("windowRoot").objectReferenceValue).SetActive(true);
                window.BindPlayer(inv,money);
                inv.AddItem(Item("BoarHorn"),4);inv.AddItem(Item("FrogOrb"));
                window.DropItem(fan,-1);
                inv.AddItem(Item("HealthPotion"));
                window.DropItem(Item("HealthPotion"),0);
                var text=(TMPro.TMP_Text)uiData.FindProperty("messageText").objectReferenceValue;
                Check(text.text.Contains("맞는 재료"),"Wrong material drop was not rejected");
                window.DropItem(Item("BoarHorn"),0);window.DropItem(Item("FrogOrb"),1);
                Check(!window.CanConfirm,"Button enabled with insufficient funds");
                eventRoot=new GameObject("UpgradeEventValidation",typeof(EventSystem));
                var confirm=(Button)uiData.FindProperty("confirmButton").objectReferenceValue;
                typeof(UpgradeConfirmFeedback).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(confirm.GetComponent<UpgradeConfirmFeedback>(),null);
                // Prefab contents live in a preview scene, where ExecuteEvents skips inactive handlers.
                confirm.GetComponent<UpgradeConfirmFeedback>().OnPointerClick(new PointerEventData(eventRoot.GetComponent<EventSystem>())
                    {button=PointerEventData.InputButton.Left});
                Check(text.text.Contains("엽전이 부족"),"Disabled button did not explain insufficient currency");
                Check(inv.GetQuantity(fan)==1&&money.Yeopjeon==0,"Disabled click upgraded");
                money.Add(100);
                Check(window.CanConfirm,"Valid selection did not enable button");
                int upgrades=0;window.Upgraded+=item=>upgrades++;
                confirm.onClick.Invoke();
                Check(upgrades==1&&window.Target==rare&&money.Yeopjeon==0,"Confirmation did not upgrade once");
                Check(inv.GetQuantity(rare)==1&&inv.GetQuantity(fan)==0&&!window.CanConfirm,"UI did not refresh next grade");

                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/PrologueVilage.unity");
                try
                {
                    bool npc=false,overlay=false;
                    foreach(var root in scene.GetRootGameObjects())
                    {
                        foreach(var smith in root.GetComponentsInChildren<EquipmentUpgradeBlacksmith>(true))
                            npc |= smith.GreetingDialogue!=null&&smith.GreetingDialogue.HasLines&&smith.GetComponent<Collider2D>()!=null;
                        overlay |= root.GetComponent<EquipmentUpgradeWindowUI>()!=null;
                    }
                    Check(npc&&overlay,"Village NPC/UI connection missing");
                }
                finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
                return "PASS: insufficient funds/materials leave resources intact; duplicate materials summed; complete transaction events; stack/equipped grade preserved; eight weapon/armor recipe filters; unconfigured recipes blocked; actual prefab drop rejection, disabled-button feedback, enable/confirm/success event; village blacksmith greeting and overlay.";
            }
            finally
            {
                if(ui!=null)PrefabUtility.UnloadPrefabContents(ui);
                if(eventRoot!=null)UnityEngine.Object.DestroyImmediate(eventRoot);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(recipe);
            }
        }
    }
}