using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using EasternFantasy.Player;
using EasternFantasy.Inventory;
using EasternFantasy.Quest;
using EasternFantasy.Dialogue;
using EasternFantasy.Shop;
using EasternFantasy.UI;

if (!EditorApplication.isPlaying) throw new Exception("Play TempleGate before validation.");
var smith = UnityEngine.Object.FindObjectsByType<Shopkeeper>(FindObjectsSortMode.None)
    .Single(s => s.name == "MonkeyMerchant");
var root = smith.gameObject;
if (smith.Stock == null || smith.Stock.Length == 0 || smith.Stock.Any(item => item == null))
    throw new Exception("Merchant stock is missing");
if (root.scene.path != EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath || root.transform.parent != null)
    throw new Exception("Blacksmith must be a placed TempleGate root object.");
var greeting = (DialogueSequence)new SerializedObject(smith).FindProperty("greetingDialogue").objectReferenceValue; var sourcePortrait = greeting.PartnerCharacter.Portrait;
if (!AssetDatabase.GetAssetPath(sourcePortrait).EndsWith("원숭이 상인 일러스트.png"))
    throw new Exception("Wrong portrait asset");
if (!root.GetComponent<Collider2D>().isTrigger) throw new Exception("NPC must not block the walking path");
var animator = root.GetComponent<Animator>();
var ambient = root.GetComponent<MonkeyMerchantAmbientAnimation>();
var flags = BindingFlags.Instance | BindingFlags.NonPublic;
ambient.SendMessage("OnEnable");
if (ambient.IsSmoking) throw new Exception("Idle must be the default pose");
ambient.Advance(15f); animator.Update(0f);
if (!ambient.IsSmoking || !animator.GetCurrentAnimatorStateInfo(0).IsName(MonkeyMerchantAmbientAnimation.PipeState))
    throw new Exception("Intermittent hammer animation did not start");
ambient.Advance(3f); animator.Update(0f);
if (ambient.IsSmoking || !animator.GetCurrentAnimatorStateInfo(0).IsName(MonkeyMerchantAmbientAnimation.IdleState))
    throw new Exception("Hammer animation did not return to idle");

var originalDialogue = DialogueManager.Instance;
var originalWindow = ShopWindowUI.Instance;
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
    if (ShopWindowUI.Instance == null || !ShopWindowUI.Instance.IsOpen)
        throw new Exception("Existing equipment upgrade window did not open after dialogue");
    if (interaction.TryInteract()) throw new Exception("Repeated interaction opened a second window");
    var report = new { passed = true, sceneObject = root.name, defaultIdle = true, intermittentPipe = true,
        returnsToIdle = true, existingInteractionRoute = true, correctPortrait = true, existingShopUiOpens = true,
        duplicateInteractionBlocked = true, colliderIsTrigger = true };
    System.IO.File.WriteAllText("Artifacts/TempleGateMerchant/runtime-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
    return report;
}
finally
{
    if (DialogueManager.Instance != null) DialogueManager.Instance.EndDialogue();
    if (ShopWindowUI.Instance != null)
    {
        typeof(ShopWindowUI).GetMethod("CloseInstant", flags).Invoke(ShopWindowUI.Instance, null);
        if (originalWindow == null) UnityEngine.Object.DestroyImmediate(ShopWindowUI.Instance.gameObject);
    }
    if (originalDialogue == null && DialogueManager.Instance != null)
        UnityEngine.Object.DestroyImmediate(DialogueManager.Instance.transform.root.gameObject);
    UnityEngine.Object.DestroyImmediate(player);
    UnityEngine.Object.DestroyImmediate(settings);
    ambient.SendMessage("OnEnable");
}
