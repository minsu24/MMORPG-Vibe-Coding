using EasternFantasy.Inventory;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class EquipmentSetSetup
    {
        public const string SetPath = "Assets/_Project/Data/Items/TrainingSet.asset";
        [MenuItem("Eastern Fantasy/Inventory/Setup Training Set")]
        public static void Run()
        {
            var set = AssetDatabase.LoadAssetAtPath<EquipmentSetDefinition>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<EquipmentSetDefinition>();
                AssetDatabase.CreateAsset(set, SetPath);
                var data = new SerializedObject(set);
                data.FindProperty("displayName").stringValue = "수련자 세트";
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(set);
            }
            string[] names = { "OldFan", "MonkGauntlets", "OldHat", "ClothTop", "ClothBottom", "ClothGloves", "ClothShoes" };
            foreach (string name in names)
            {
                var original = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/" + name + ".asset");
                if (original == null) continue;
                for (int grade = 0; grade < 5; grade++)
                {
                    ItemDefinition item = original.GetGradeVariant((ItemGrade)grade);
                    if (item == null) continue;
                    var data = new SerializedObject(item);
                    data.FindProperty("equipmentSet").objectReferenceValue = set;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(item);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Training set assigned to 30 equipment definitions. Thresholds/effects editable on TrainingSet.");
        }
    }
}