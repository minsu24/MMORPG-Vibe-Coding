using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class PetCompanionValidation
    {
        private const string Report = "Artifacts/PetCompanion/Validation.txt";

        [MenuItem("Eastern Fantasy/Companions/Validate Companion Changes")]
        public static void Run()
        {
            Directory.CreateDirectory("Artifacts/PetCompanion");
            ValidateProgress();
            ValidateCompanionPrefab();
            var root = PrefabUtility.LoadPrefabContents(PetCompanionSetup.PrefabPath);
            try
            {
                var board = root.transform.Find("WindowRoot/CompanionPanel");
                Check(board != null, "Missing panel");
                Check(board.Find("Replay") == null, "Replay remains");
                for (int i = 0; i < 4; i++) Check(board.Find("PoseFrame" + i) == null, "Pose remains");
                Check(board.Find("LockedSkill0") != null && board.Find("LockedSkill1") == null
                    && board.Find("LockedSkill2") == null, "Incorrect skill slot count");
                Check(board.Find("StatNames").GetComponent<TMP_Text>().text == "공격력\n\n혼령 등급", "Health remains or stat line breaks lost");
                Check(board.Find("StatValues").GetComponent<TMP_Text>().text == "5\n\n일반", "Incorrect stats");
                var data = new SerializedObject(root.GetComponent<PetCompanionWindowUI>());
                foreach (string field in new[] { "soulFragment", "companionCatalog", "listContent", "companionRowTemplate", "placeholderRow", "soulIcon", "soulLabelText", "levelText",
                    "statValuesText", "companionStatusText", "skillsStatusText", "barLabelText", "confirmButton", "equipButton" })
                    Check(data.FindProperty(field).objectReferenceValue != null, "Unbound field: " + field);
                var list = board.Find("ListViewport/ListContent");
                Check(list.childCount == 2 && list.GetChild(0).Find("Name").GetComponent<TMP_Text>().text == "도깨비"
                    && list.GetChild(1).Find("Name").GetComponent<TMP_Text>().text == "???", "Incorrect companion rows");
                Capture(root, "Artifacts/PetCompanion/UpdatedCompanionUI.png");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            const string result = "PASS: missing materials do not consume items; fragments cannot summon; exactly one soul consumed; duplicate summon rejected; growth costs 1/2/3; level and attack increase; insufficient fragments leave state unchanged; independent character state; UI has no replay/poses/health and exactly one skill slot; all bindings valid.\nUI render: UpdatedCompanionUI.png\nWorld follow/combat requires Play Mode verification.";
            File.WriteAllText(Report, result);
            Debug.Log(result);
        }

        private static void ValidateCompanionPrefab()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PetCompanionCatalog>(PetCompanionPrefabSetup.CatalogPath);
            Check(catalog != null && catalog.CompanionPrefabs.Length == 1 && catalog.CompanionPrefabs[0] != null,
                "Expected the Goblin companion catalog entry");
            var prefab = catalog.CompanionPrefabs[0];
            Check(prefab.CompanionId == "goblin" && prefab.SummonItem != null, "Missing companion metadata");
            Check(prefab.GetComponentsInChildren<Collider2D>().Length == 0
                && prefab.GetComponentsInChildren<Entity>().Length == 0, "Companion has health/collision");
            Check(Mathf.Abs(prefab.transform.localScale.x - 1f / 1.5f) < .0001f, "Incorrect pet scale");
            var instance = PrefabUtility.LoadPrefabContents(PetCompanionPrefabSetup.PrefabPath);
            try
            {
                var visual = instance.transform.Find("Visual");
                var renderer = visual.GetComponent<SpriteRenderer>();
                float referenceHeight = 0f;
                foreach (string name in new[] { "Companion_Idle", "Companion_Walk", "Companion_Attack" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PetCompanionPrefabSetup.AnimationFolder + name + ".anim");
                    Check(clip != null, "Missing companion animation");
                    var keys = AnimationUtility.GetObjectReferenceCurve(clip,
                        EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
                    float scale = -1f;
                    var sources = new HashSet<string>();
                    foreach (var key in keys)
                    {
                        clip.SampleAnimation(visual.gameObject, key.time);
                        Check(renderer.sprite != null, "Invalid sprite reference");
                        if (scale < 0f) scale = visual.localScale.y;
                        Check(Mathf.Abs(visual.localScale.y - scale) < .0001f, "Body scale changes during animation");
                        if (key.time == 0f)
                        {
                            float height = PetCompanionPrefabSetup.VisibleBounds(renderer.sprite).height
                                / renderer.sprite.pixelsPerUnit * visual.lossyScale.y;
                            if (referenceHeight == 0f) referenceHeight = height;
                            Check(Mathf.Abs(height - referenceHeight) < .001f, "Neutral body sizes differ");
                        }
                        sources.Add(AssetDatabase.GetAssetPath(renderer.sprite));
                    }
                    if (name == "Companion_Attack") Check(sources.Count == 2 && keys.Length == 8, "Attack frame sources incorrect");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(instance); }
        }

        private static void ValidateProgress()
        {
            var player = new GameObject("CompanionValidationPlayer");
            player.SetActive(false);
            try
            {
                var inventory = player.AddComponent<PlayerInventory>();
                var soul = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/SoulOfGoblin.asset");
                var fragment = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/SoulFragment.asset");
                var progress = new PetCompanionProgress();
                Check(!progress.TryEquip() && !progress.IsEquipped, "Equipped before summon");
                Check(!progress.TrySummon(inventory, soul) && !progress.IsSummoned, "Summoned without soul");
                Check(inventory.AddItem(fragment, 6), "Cannot add fragments");
                Check(!progress.TryGrow(inventory, fragment) && inventory.GetQuantity(fragment) == 6, "Grew before summon");
                Check(inventory.AddItem(soul, 2), "Cannot add souls");
                Check(progress.TrySummon(inventory, soul) && progress.Level == 1 && progress.Attack == 5
                    && inventory.GetQuantity(soul) == 1, "Summon cost/stats incorrect");
                Check(!progress.TrySummon(inventory, soul) && inventory.GetQuantity(soul) == 1, "Repeated summon consumed soul");
                Check(progress.TryEquip() && progress.IsEquipped, "Cannot equip summoned companion");
                progress.Unequip();
                Check(progress.IsSummoned && !progress.IsEquipped && progress.Level == 1 && inventory.GetQuantity(soul) == 1,
                    "Unequip lost ownership or consumed resources");
                int remaining = 6;
                for (int cost = 1; cost <= 3; cost++)
                {
                    Check(progress.GrowthCost == cost, "Growth cost did not increase");
                    remaining -= cost;
                    Check(progress.TryGrow(inventory, fragment) && progress.Level == cost + 1 && progress.Attack == cost + 5
                        && inventory.GetQuantity(fragment) == remaining && inventory.GetQuantity(soul) == 1, "Growth consumed wrong amount/material");
                }
                Check(!progress.TryGrow(inventory, fragment) && progress.Level == 4 && progress.Attack == 8, "Failed growth mutated state");
                Check(!progress.IsEquipped, "Growing an unequipped companion re-equipped it");
                Check(progress.TryEquip() && progress.IsEquipped && progress.Level == 4 && progress.Attack == 8
                    && inventory.GetQuantity(soul) == 1, "Re-equip lost growth or consumed soul");
                progress.Unequip();
                progress.Unequip();
                Check(!progress.IsEquipped && progress.Level == 4, "Repeated unequip mutated progress");
                Check(!new PetCompanionProgress().IsSummoned, "Character state shared");
                Check(!typeof(Entity).IsAssignableFrom(typeof(PetCompanionActor)), "Companion has health/damage entity");
            }
            finally { UnityEngine.Object.DestroyImmediate(player); }
        }

        private static void Capture(GameObject root, string path)
        {
            var cameraObject = new GameObject("CompanionPreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, root.scene);
            var target = new RenderTexture(1536, 1024, 24);
            var pixels = new Texture2D(1536, 1024, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = new Vector3(0f, 0f, -100f);
                camera.orthographic = true;
                camera.orthographicSize = 512f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.07f, .04f, .09f);
                camera.targetTexture = target;
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
                root.transform.Find("WindowRoot").gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1536, 1024), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
