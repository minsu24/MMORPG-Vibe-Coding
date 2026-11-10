using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dungeon;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class DungeonRewardsValidation
    {
        private const string Report = "Artifacts/DungeonRewards/Validation.txt";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<string> errors = new List<string>();
        private static GameObject holder;
        private static Scene boss, village, previous;
        private static int checks;
        private static bool background;
        private static object Get(object target, string field) => target.GetType().GetField(field, Hidden).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Hidden).SetValue(target, value);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
        private static void SaveFirst(SaveGameData data) => typeof(FirstDropProgress).GetMethod("CaptureSave", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { data });
        private static void LoadFirst(SaveGameData data) => typeof(FirstDropProgress).GetMethod("RestoreSave", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { data });
        private static void SaveRewards(SaveGameData data) => typeof(DungeonRewardProgress).GetMethod("CaptureSave", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { data });
        private static void LoadRewards(SaveGameData data) => typeof(DungeonRewardProgress).GetMethod("RestoreSave", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { data });
        private static void Check(bool value, string message)
        {
            File.AppendAllText(Report, (value ? "PASS " : "FAIL ") + message + "\n");
            if (!value) throw new InvalidOperationException(message);
            checks++;
        }
        private static void Log(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(text);
        }
        public static string Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
            Directory.CreateDirectory("Artifacts/DungeonRewards"); File.WriteAllText(Report, "Native Unity first drops, clear rewards and return window validation\n");
            checks = 0; errors.Clear(); background = Application.runInBackground; Application.runInBackground = true;
            SaveGameService.BeginAdminSession();
            SaveGameService.Instance.StartCoroutine(Guard());
            return "Started native dungeon reward validation.";
        }
        private static IEnumerator Guard()
        {
            var iterator = Validate(); Application.logMessageReceived += Log;
            try
            {
                for (;;)
                {
                    bool next;
                    try { next = iterator.MoveNext(); }
                    catch (Exception exception) { File.AppendAllText(Report, "ERROR " + exception + "\n"); yield break; }
                    if (!next) break;
                    yield return iterator.Current;
                }
                Check(errors.Count == 0, "No Unity runtime errors: " + string.Join("; ", errors));
                File.AppendAllText(Report, "COMPLETE " + checks + " checks passed\n");
            }
            finally
            {
                Application.logMessageReceived -= Log;
                if (DungeonClearWindowUI.Instance != null) DungeonClearWindowUI.Instance.Dismiss();
                if (DungeonRunController.Instance != null) UnityEngine.Object.DestroyImmediate(DungeonRunController.Instance.gameObject);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
                if (boss.IsValid()) SceneManager.UnloadSceneAsync(boss);
                if (village.IsValid()) SceneManager.UnloadSceneAsync(village);
                Time.timeScale = 1f; Application.runInBackground = background;
            }
        }
        private static IEnumerator Validate()
        {
            previous = SceneManager.GetActiveScene();
            boss = SceneManager.CreateScene("GiantFarm_03_Boss");
            village = SceneManager.CreateScene("PrologueVilage");
            holder = new GameObject("Dungeon reward fixtures"); holder.SetActive(false);
            SceneManager.MoveGameObjectToScene(holder, boss);
            var player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab"), holder.transform);
            UnityEngine.Object.DestroyImmediate(player.GetComponent<PlayerClassRuntime>());
            UnityEngine.Object.DestroyImmediate(player.GetComponent<PlayerAdminController>());
            UnityEngine.Object.DestroyImmediate(player.GetComponent<PlayerLocationSetter>());
            foreach (var script in player.GetComponents<MonoBehaviour>())
                if (!(script is PlayerEntity || script is PlayerMovement2D || script is PlayerIdleAnimation || script is PlayerInventory || script is PlayerProgression || script is EasternFantasy.Skill.PlayerSkillSystem || script is PlayerProjectileShooter)) script.enabled = false;
            holder.SetActive(true);
            player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            Set(player.GetComponent<PlayerIdleAnimation>(), "inputReader", null);
            var inventory = player.GetComponent<PlayerInventory>();
            var data = new SaveGameData { characterClass = (int)CharacterSelectionState.SelectedClass };
            LoadFirst(data); LoadRewards(data);
            var floor = new GameObject("Floor", typeof(BoxCollider2D)); SceneManager.MoveGameObjectToScene(floor, boss);
            floor.transform.position = new Vector3(0, -2f, 0); floor.GetComponent<BoxCollider2D>().size = new Vector2(100, 1);
            Physics2D.SyncTransforms();
            yield return null;
            foreach (string name in new[] { "BoarHorn", "GiantHeart" }) ValidateFirstDrop(name, inventory);
            var fabric = AssetDatabase.LoadAssetAtPath<ItemDefinition>(DungeonClearRewardsSetup.FabricPath);
            var dungeon = AssetDatabase.LoadAssetAtPath<DungeonDefinition>(DungeonClearRewardsSetup.DungeonPath);
            Check(dungeon.clearRewards.Count(reward => reward.item == fabric && reward.quantity == 1) == 1, "Fabric Piece configured as one item per clear");
            Check(dungeon.obtainableRewards.Any(reward => reward.item == fabric), "Dungeon guide includes Fabric Piece");
            Check(fabric.Icon != null && fabric.Icon.rect.width > 1000 && fabric.Icon.rect.height > 1000, "Fabric icon uses the full cloth illustration");
            Check(dungeon.returnSceneName == "PrologueVilage", "Clear return destination is village");
            DungeonRunController.Begin(dungeon);
            var controller = DungeonRunController.Instance;
            Check(DungeonRewardProgress.Pending.Count == 0, "Entering dungeon does not grant clear reward");
            var enemy = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Ginseng.prefab"));
            SceneManager.MoveGameObjectToScene(enemy, boss);
            Call(controller, "OnGiantDefeated", enemy.GetComponent<EnemyController>());
            Check(controller.IsCleared && DungeonRewardProgress.Pending.Count == 1, "Actual clear callback queues one reward batch");
            Check(inventory.GetQuantity(fabric) == 0, "Clear reward waits for village arrival");
            Call(controller, "OnGiantDefeated", enemy.GetComponent<EnemyController>());
            Check(DungeonRewardProgress.Pending.Count == 1, "Duplicate boss event cannot queue extra rewards");
            UnityEngine.Object.DestroyImmediate(enemy);
            var saved = new SaveGameData { characterClass = (int)CharacterSelectionState.SelectedClass };
            Call(controller, "CaptureSave", saved); SaveRewards(saved);
            var roundTrip = JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(saved));
            LoadRewards(roundTrip); Call(controller, "RestoreSave", roundTrip);
            Check(DungeonRewardProgress.Pending.Count == 1, "Cleared run and pending reward survive save reload without duplication");
            string paidRun = roundTrip.dungeonRunId;
            // Use native scene events with isolated named scenes, without touching a character save or real map objects.
            SceneManager.SetActiveScene(village);
            Call(controller, "OnSceneLoaded", village, LoadSceneMode.Single);
            yield return null; yield return null;
            var ui = DungeonClearWindowUI.Instance;
            Check(DungeonClearWindowUI.IsOpen, "Clear window opens after village scene arrival");
            Check(inventory.GetQuantity(fabric) == 1 && DungeonRewardProgress.Pending.Count == 0, "Arrival pays Fabric Piece exactly once");
            Check(player.GetComponent<PlayerInputReader>().IsInputBlocked && Time.timeScale == 0f, "Clear window blocks gameplay and pauses game");
            Check(ui.title.text.Contains("거인의 농장") && ui.rewardContent.GetComponentsInChildren<TMPro.TMP_Text>().Any(text => text.text.Contains("천 조각")), "Clear window shows dungeon title and actual reward");
            Check(ui.transform.Find("Window Root/Panel").GetComponent<UnityEngine.UI.Image>().sprite != null
                && ui.transform.Find("Window Root/Panel/Clear Heading").GetComponent<UnityEngine.UI.Image>().sprite != null
                && ui.transform.Find("Window Root/Panel/Rewards").GetComponent<UnityEngine.UI.Image>().sprite != null,
                "Frame, calligraphy banner and reward area use supplied artwork");
            Check(EventSystem.current.currentSelectedGameObject != ui.confirmButton.gameObject, "Confirm button is not automatically selected on opening");
            var pointer = new PointerEventData(EventSystem.current);
            ui.confirmButton.OnPointerExit(pointer);
            Check(ui.confirmButton.image.overrideSprite == ui.confirmButton.image.sprite, "Confirm starts in normal state without pointer hover");
            ValidateButtonAlignment(ui.closeButton, "Close");
            ValidateButtonAlignment(ui.confirmButton, "Confirm");
            ui.closeButton.OnPointerEnter(pointer);
            Check(ui.closeButton.image.overrideSprite == ui.closeButton.spriteState.highlightedSprite, "Close button changes to supplied hover sprite");
            ui.closeButton.OnPointerExit(pointer);
            Check(ui.closeButton.image.overrideSprite == ui.closeButton.image.sprite, "Close button restores supplied normal sprite");
            ui.confirmButton.OnPointerEnter(pointer);
            Check(ui.confirmButton.image.overrideSprite == ui.confirmButton.spriteState.highlightedSprite, "Confirm button changes to supplied hover sprite");
            ScreenCapture.CaptureScreenshot("Artifacts/DungeonRewards/ClearWindowHover.png");
            yield return null; yield return null;
            ui.confirmButton.OnPointerExit(pointer);
            Check(ui.confirmButton.image.overrideSprite == ui.confirmButton.image.sprite, "Confirm button restores supplied normal sprite");
            ScreenCapture.CaptureScreenshot("Artifacts/DungeonRewards/ClearWindow.png");
            yield return null; yield return null;
            ui.confirmButton.onClick.Invoke();
            Check(!DungeonClearWindowUI.IsOpen && !player.GetComponent<PlayerInputReader>().IsInputBlocked, "Confirm closes window and releases input");
            ui.confirmButton.onClick.Invoke(); DungeonRewardProgress.Queue(paidRun, dungeon);
            Check(inventory.GetQuantity(fabric) == 1 && DungeonRewardProgress.Pending.Count == 0, "Repeated confirm and repeated run ID cannot duplicate payment");
            DungeonRunController.Begin(dungeon);
            Call(DungeonRunController.Instance, "OnSceneLoaded", village, LoadSceneMode.Single);
            Check(DungeonRewardProgress.Pending.Count == 0, "Leaving an uncleared dungeon grants no reward");
            yield return null;
            inventory.AddItem(fabric, fabric.MaximumStack - inventory.GetQuantity(fabric));
            DungeonRewardProgress.Queue("validation-full-inventory", dungeon);
            var pending = DungeonRewardProgress.Pending.First();
            ui.Show(pending, inventory);
            Check(DungeonRewardProgress.Pending.Count == 1 && inventory.GetQuantity(fabric) == fabric.MaximumStack, "Full stack preserves pending reward without partial payout");
            ui.closeButton.onClick.Invoke();
            Check(!DungeonClearWindowUI.IsOpen && DungeonRewardProgress.Pending.Count == 1, "Close X preserves unreceived reward");
            saved = new SaveGameData { characterClass = (int)CharacterSelectionState.SelectedClass };
            SaveRewards(saved);
            LoadRewards(JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(saved)));
            inventory.RemoveItem(fabric, fabric.MaximumStack);
            yield return null; yield return null;
            Check(DungeonClearWindowUI.IsOpen && inventory.GetQuantity(fabric) == 1 && DungeonRewardProgress.Pending.Count == 0, "Freeing inventory space retries and pays saved pending reward");
            ui.closeButton.onClick.Invoke();
            Check(!DungeonClearWindowUI.IsOpen && Time.timeScale == 1f, "Close X releases pause after reward receipt");
            SaveRewards(saved); LoadRewards(saved);
            DungeonRewardProgress.Queue("validation-full-inventory", dungeon);
            Check(DungeonRewardProgress.Pending.Count == 0, "Claimed run receipt persists after save reload");
        }
        private static void ValidateButtonAlignment(UnityEngine.UI.Button button, string name)
        {
            var normal = button.image.sprite;
            var hover = button.spriteState.highlightedSprite;
            Check(normal != hover && normal.rect.size == hover.rect.size, name + " uses distinct states on identical sprite canvases");
            Check(button.spriteState.selectedSprite == normal && button.navigation.mode == UnityEngine.UI.Navigation.Mode.None,
                name + " does not leave its hover sprite active through automatic selection");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                ImageConversion.LoadImage(texture, File.ReadAllBytes(AssetDatabase.GetAssetPath(normal)));
                var pixels = texture.GetPixels32();
                Vector2 normalCenter = ArtworkCenter(normal, pixels, texture.width);
                Vector2 hoverCenter = ArtworkCenter(hover, pixels, texture.width);
                var rect = button.image.rectTransform.rect;
                Vector2 renderedSize = rect.size;
                float aspect = normal.rect.width / normal.rect.height;
                if (renderedSize.x / renderedSize.y > aspect) renderedSize.x = renderedSize.y * aspect;
                else renderedSize.y = renderedSize.x / aspect;
                Vector2 movement = Vector2.Scale(hoverCenter - normalCenter, renderedSize);
                Check(Mathf.Abs(movement.x) < .25f && Mathf.Abs(movement.y) < .25f,
                    name + " opaque artwork stays centered on hover; movement=" + movement.ToString("F3"));
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        private static Vector2 ArtworkCenter(Sprite sprite, Color32[] pixels, int width)
        {
            int left = int.MaxValue, right = -1, bottom = int.MaxValue, top = -1;
            var rect = sprite.rect;
            for (int y = Mathf.CeilToInt(rect.yMin); y < Mathf.FloorToInt(rect.yMax); y++)
                for (int x = Mathf.CeilToInt(rect.xMin); x < Mathf.FloorToInt(rect.xMax); x++)
                {
                    var pixel = pixels[y * width + x];
                    int low = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
                    int high = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
                    if (pixel.a < 245 || (low > 180 && high - low < 30)) continue;
                    left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                }
            if (right < left || top < bottom) throw new InvalidOperationException("No opaque button artwork found.");
            return new Vector2(((left + right + 1f) * .5f - rect.x) / rect.width,
                ((bottom + top + 1f) * .5f - rect.y) / rect.height);
        }
        private static void ValidateFirstDrop(string name, PlayerInventory inventory)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/" + name + ".asset");
            Check(item.FirstAcquisitionOnly, name + " is first-acquisition-only");
            Check(FirstDropProgress.CanDrop(item), name + " eligible before first acquisition");
            var first = Spawn(item);
            Check(first != null && first.Quantity == 1, name + " actually spawns once");
            Check(!FirstDropProgress.CanDrop(item) && Spawn(item) == first, name + " blocks another live uncollected drop");
            UnityEngine.Object.DestroyImmediate(first.gameObject);
            Check(FirstDropProgress.CanDrop(item), name + " can be recovered if never collected");
            var recovered = Spawn(item);
            Call(recovered, "Collect", inventory.GetComponent<Collider2D>());
            Check(recovered.IsCollected && inventory.GetQuantity(item) == 1 && FirstDropProgress.IsAcquired(item), name + " pickup records acquisition");
            UnityEngine.Object.DestroyImmediate(recovered.gameObject);
            inventory.RemoveItem(item);
            Check(!FirstDropProgress.CanDrop(item) && Spawn(item) == null, name + " does not return after item removal or sale");
            var saved = new SaveGameData { characterClass = (int)CharacterSelectionState.SelectedClass };
            SaveFirst(saved);
            LoadFirst(JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(saved)));
            Check(!FirstDropProgress.CanDrop(item), name + " first-drop history survives save reload");
            var other = new SaveGameData { characterClass = ((int)CharacterSelectionState.SelectedClass + 1) % 3 };
            SaveFirst(other);
            Check(other.firstDropItems.Length == 0, name + " does not consume another class character's first drop");
        }
        private static WorldItemPickup Spawn(ItemDefinition item)
        {
            Time.timeScale = 1f;
            var enemyObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Ginseng.prefab"));
            SceneManager.MoveGameObjectToScene(enemyObject, boss); enemyObject.GetComponent<EnemyController>().enabled = false;
            var entry = new EnemyDropEntry(); Set(entry, "item", item); Set(entry, "dropChance", 1f);
            var loot = enemyObject.GetComponent<EnemyLootDrop>(); Set(loot, "drops", new[] { entry });
            Physics2D.SyncTransforms(); Call(loot, "HandleDefeated", enemyObject.GetComponent<EnemyController>());
            UnityEngine.Object.DestroyImmediate(enemyObject);
            return boss.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WorldItemPickup>()).FirstOrDefault(pickup => pickup.Item == item && !pickup.IsCollected);
        }
    }
}
