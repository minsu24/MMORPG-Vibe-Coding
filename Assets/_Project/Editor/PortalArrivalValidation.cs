using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class PortalArrivalValidation
    {
        private const string Report = "Artifacts/PortalArrival/Validation.txt";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;

        public static void Prepare()
        {
            MonkChargeAudioValidation.Prepare();
            string path = SaveGameService.PathFor(CharacterClassId.Monk);
            var data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
            data.scene = "BrokenSeal";
            data.position = new Vector3(-5f, 3f, 0f);
            data.bossDefeats = data.dungeonDefeats = Array.Empty<string>();
            data.toadDefeated = false;
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            Directory.CreateDirectory("Artifacts/PortalArrival");
            File.WriteAllText(Report, "Native portal arrival validation\n");
        }

        public static void Run()
        {
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(Validate());
        }

        private static IEnumerator Validate()
        {
            if (UnityEngine.Object.FindFirstObjectByType<PlayerEntity>() == null)
            {
                yield return SceneManager.LoadSceneAsync("MovementPrototype");
                yield return null;
            }
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            SaveGameService.BeginAdminSession();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            var movement = player.GetComponent<PlayerMovement2D>();
            player.isInvincible = true;
            player.GetComponent<PlayerInputReader>().enabled = false;
            movement.body.constraints = RigidbodyConstraints2D.FreezeAll;
            var source = Portal("In_BrokenSeal_Portal");
            var sourceData = new SerializedObject(source);
            Check(sourceData.FindProperty("sceneName").stringValue == "Marshland"
                && sourceData.FindProperty("targetPointName").stringValue == "Out_Marshland_Portal", "return portal targets Marshland exit anchor");
            typeof(PortalManager).GetField("playerInPortal", Hidden).SetValue(source, movement);
            typeof(PortalManager).GetMethod("MoveToScene", Hidden).Invoke(source, null);
            Check(PortalSceneTransition.IsTransitioning, "real portal MoveToScene accepts transfer");
            yield return WaitForTransfer();
            var destination = Portal("Out_Marshland_Portal");
            Check(SceneManager.GetActiveScene().name == "Marshland", "portal reaches Marshland");
            Check(Vector2.Distance(movement.body.position, destination.transform.position) < .02f,
                "player arrives at Out_Marshland_Portal instead of map entry");
            Check(!destination.gameObject.activeSelf,
                "inactive boss portal is a valid arrival anchor without activating it");
            Check(MapTransferData.TargetSpawnPointName == null, "arrival consumes transfer target");

            Check(PortalSceneTransition.TryLoad("BrokenSeal", "Out_BrokenSeal_Portal"), "arrival at sealed-door anchor requested");
            yield return WaitForTransfer();
            destination = Portal("Out_BrokenSeal_Portal");
            Check(Vector2.Distance(movement.body.position, destination.transform.position) < .02f,
                "destination name with trailing scene whitespace is resolved");

            var root = new GameObject("Inactive Arrival Parent");
            var anchor = new GameObject("InactiveArrivalFixture");
            anchor.transform.SetParent(root.transform);
            anchor.transform.position = destination.transform.position + Vector3.left;
            anchor.AddComponent<BoxCollider2D>().isTrigger = true;
            var lockedAnchor = anchor.AddComponent<PortalManager>();
            lockedAnchor.SetLocked(true);
            root.SetActive(false);
            try
            {
                MapTransferData.SetTarget("  InactiveArrivalFixture  ");
                typeof(PlayerLocationSetter).GetMethod("OnSceneLoaded", Hidden).Invoke(player.GetComponent<PlayerLocationSetter>(),
                    new object[] { SceneManager.GetActiveScene(), LoadSceneMode.Single });
                Check(Vector2.Distance(movement.body.position, anchor.transform.position) < .001f && !root.activeSelf && lockedAnchor.IsLocked,
                    "nested inactive and locked arrival anchor resolves without activating or unlocking it");
                var missing = typeof(PlayerLocationSetter).GetMethod("FindSpawnPoint", Static)
                    .Invoke(null, new object[] { SceneManager.GetActiveScene(), "MissingArrivalFixture" });
                Check(missing == null, "missing target never falls back to another portal");
            }
            finally { UnityEngine.Object.Destroy(root); }
            File.AppendAllText(Report, "COMPLETE\n");
        }

        private static PortalManager Portal(string name) => UnityEngine.Object.FindObjectsByType<PortalManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(p => p.gameObject.scene == SceneManager.GetActiveScene() && p.name.Trim() == name);

        private static IEnumerator WaitForTransfer()
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (PortalSceneTransition.IsTransitioning || SceneLoadingScreen.IsVisible)
            {
                if (Time.realtimeSinceStartup > deadline) throw new InvalidOperationException("Portal transfer timeout");
                yield return null;
            }
        }

        private static void Check(bool ok, string message)
        {
            File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
