using System.Linq;
using UnityEditor;
using UnityEngine;

var path = "Assets/_Project/Prefabs/UI/EquipmentUpgradeWindow.prefab";
var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
var portrait = "Assets/_Project/Art/Old Temple/NPC/도깨비 대장장이 일러스트.png";
return new {
    upgradePrefab = new { root.name, root.activeSelf, components = root.GetComponents<Component>().Select(c=>c.GetType().Name).ToArray(), windows = root.GetComponentsInChildren<EasternFantasy.UI.EquipmentUpgradeWindowUI>(true).Length },
    portraitSprites = AssetDatabase.LoadAllAssetsAtPath(portrait).OfType<Sprite>().Select(s=>new {s.name,rect=s.rect.ToString()}).ToArray(),
    sceneManagers = UnityEngine.Object.FindObjectsByType<EasternFantasy.Dialogue.DialogueManager>(FindObjectsSortMode.None).Length,
    equipmentInstances = UnityEngine.Object.FindObjectsByType<EasternFantasy.UI.EquipmentUpgradeWindowUI>(FindObjectsSortMode.None).Length
};
