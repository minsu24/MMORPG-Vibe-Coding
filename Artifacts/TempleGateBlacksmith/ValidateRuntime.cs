using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using EasternFantasy.Player;
using EasternFantasy.Inventory;
using EasternFantasy.Quest;
using EasternFantasy.Dialogue;
using EasternFantasy.EquipmentUpgrade;
using EasternFantasy.UI;

if (!EditorApplication.isPlaying) throw new Exception("Play TempleGate before validation.");
var smith = UnityEngine.Object.FindObjectsByType<EquipmentUpgradeBlacksmith>(FindObjectsSortMode.None)
    .Single(s => s.name == "GoblinBlacksmith");
var root = smith.gameObject;
if (root.scene.path != EasternFantasy.Editor.TempleGateBlacksmithSetup.ScenePath || root.transform.parent != null)
    throw new Exception("Blacksmith must be a placed TempleGate root object.");
var sourcePortrait = smith.GreetingDialogue.PartnerCharacter.Portrait;
if (!AssetDatabase.GetAssetPath(sourcePortrait).EndsWith("도깨비 대장장이 일러스트.png"))
    throw new Exception("Wrong portrait asset");
if (!root.GetComponent<Collider2D>().isTrigger) throw new Exception("NPC must not block the walking path");
var animator = root.GetComponent<Animator>();
var ambient = root.GetComponent<BlacksmithAmbientAnimation>();
var flags = BindingFlags.Instance | BindingFlags.NonPublic;
ambient.SendMessage("OnEnable");
if (ambient.IsHammering) throw new Exception("Idle must be the default pose");
ambient.Advance(13f); animator.Update(0f);
if (!ambient.IsHammering || !animator.GetCurrentAnimatorStateInfo(0).IsName(BlacksmithAmbientAnimation.HammerState))
    throw new Exception("Intermittent hammer animation did not start");
ambient.Advance(2f); animator.Update(0f);
if (ambient.IsHammering || !animator.GetCurrentAnimatorStateInfo(0).IsName(BlacksmithAmbientAnimation.IdleState))
    throw new Exception("Hammer animation did not return to idle");

var originalDialogue = DialogueManager.Instance;
var originalWindow = EquipmentUpgradeWindowUI.Instance;
var player = new GameObject("Temporary blacksmith interaction player"); player.SetActive(false);
var settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
try
{
    var inventory = player.AddComponent<PlayerInventory>();
    var movement = player.GetComponent<PlayerMovement2D>();
    typeof(PlayerMovement2D).GetField("settings", flags).SetValue(movement, settings);
    player.AddComponent<PlayerCurrency>();
    var interaction = player.AddComponent<PlayerQuestInteraction>();
    player.transform.position = root.transform.position + new Vector3(-1.5f, 1f, 0f);
    player.SetActive(true); movement.enabled = false;
    player.GetComponent<Rigidbody2D>().gravityScale = 0;
    Physics2D.SyncTransforms();
    if (!interaction.TryInteract()) throw new Exception("Existing T interaction route did not find blacksmith");
    var manager = DialogueManager.Instance;
    if (manager == null || !manager.IsDialogueActive) throw new Exception("Greeting dialogue did not open");
    var portrait = (UnityEngine.UI.Image)typeof(DialogueManager).GetField("partnerPortrait", flags).GetValue(manager);
    if (portrait.sprite != sourcePortrait) throw new Exception("Dialogue did not display blacksmith illustration");
    manager.Advance(); manager.Advance();
    if (manager.IsDialogueActive) throw new Exception("Greeting did not complete");
    if (EquipmentUpgradeWindowUI.Instance == null || !EquipmentUpgradeWindowUI.Instance.IsOpen)
        throw new Exception("Existing equipment upgrade window did not open after dialogue");
    if (interaction.TryInteract()) throw new Exception("Repeated interaction opened a second window");
    var report = new { passed = true, sceneObject = root.name, defaultIdle = true, intermittentHammer = true,
        returnsToIdle = true, existingInteractionRoute = true, correctPortrait = true, existingUpgradeUiOpens = true,
        duplicateInteractionBlocked = true, colliderIsTrigger = true };
    System.IO.File.WriteAllText("Artifacts/TempleGateBlacksmith/runtime-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
    return report;
}
finally
{
    if (DialogueManager.Instance != null) DialogueManager.Instance.EndDialogue();
    if (EquipmentUpgradeWindowUI.Instance != null)
    {
        typeof(EquipmentUpgradeWindowUI).GetMethod("CloseInstant", flags).Invoke(EquipmentUpgradeWindowUI.Instance, null);
        if (originalWindow == null) UnityEngine.Object.DestroyImmediate(EquipmentUpgradeWindowUI.Instance.gameObject);
    }
    if (originalDialogue == null && DialogueManager.Instance != null)
        UnityEngine.Object.DestroyImmediate(DialogueManager.Instance.transform.root.gameObject);
    UnityEngine.Object.DestroyImmediate(player);
    UnityEngine.Object.DestroyImmediate(settings);
    ambient.SendMessage("OnEnable");
}
