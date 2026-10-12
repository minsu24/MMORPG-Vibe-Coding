using EasternFantasy.EquipmentUpgrade;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    [CustomEditor(typeof(EquipmentUpgradeRecipe))]
    public sealed class EquipmentUpgradeRecipeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("무기/방어구와 현재 등급을 선택하고 Materials에 재료·수량, Cost Yeopjeon에 비용을 넣으세요. 설정 완료 후 Configured를 켜면 즉시 적용됩니다. 최대 4종의 재료를 사용합니다.",MessageType.Info);
            DrawDefaultInspector();
            var recipe=(EquipmentUpgradeRecipe)target;
            EditorGUILayout.LabelField("승급 단계",recipe.SourceGrade+" → "+recipe.TargetGrade);
            if(!recipe.TryGetRequirements(out _,out string error))
                EditorGUILayout.HelpBox(error,MessageType.Warning);
            else
                EditorGUILayout.HelpBox("승급 조건이 적용됩니다.",MessageType.Info);
        }
    }
}