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
    public static class BossPortalRoundTripValidation
    {
        private const string Folder = "Artifacts/BossPortalRoundTrip";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void Prepare()
        {
            MonkChargeAudioValidation.Prepare();
            string path = SaveGameService.PathFor(CharacterClassId.Monk);
            var data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
            data.scene = "Marshland";
            data.position = new Vector3(-5f, 2f, 0f);
            data.level = 20;
            data.experience = 0;
            data.bossDefeats = data.dungeonDefeats = Array.Empty<string>();
            data.toadDefeated = false;
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            Directory.CreateDirectory(Folder);
        }

        public static void Run(bool reproduce)
        {
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(Validate(reproduce));
        }

        private static IEnumerator Validate(bool reproduce)
        {
            string report = Folder + (reproduce ? "/BeforeFix.txt" : "/AfterFix.txt");
            File.WriteAllText(report, "Native Marshland -> BrokenSeal -> Marshland boss portal round trip\n");
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
            var frog = UnityEngine.Object.FindFirstObjectByType<MON_Frog>();
            Check(frog != null && frog.HP > 0f && !Portal("Out_Marshland_Portal").gameObject.activeSelf,
                "living boss starts with hidden portal", report);
            frog.TakeDamage(frog.maxHP + 100000f);
            Check(frog.WasDefeatedThisSession && Portal("Out_Marshland_Portal").gameObject.activeSelf,
                "real boss defeat opens portal and records encounter", report);
            while (Time.timeScale <= 0f) yield return null;
            int experience = player.GetComponent<PlayerProgression>().CurrentExperience;
            yield return Transfer(Portal("Out_Marshland_Portal"), movement);
            Check(SceneManager.GetActiveScene().name == "BrokenSeal", "outgoing portal reaches BrokenSeal", report);
            yield return Transfer(Portal("In_BrokenSeal_Portal"), movement);
            var exit = Portal("Out_Marshland_Portal");
            Check(SceneManager.GetActiveScene().name == "Marshland"
                && Vector2.Distance(movement.body.position, exit.transform.position) < .02f,
                "return transfer reaches named exit location", report);
            var restoredFrog = UnityEngine.Object.FindFirstObjectByType<MON_Frog>(FindObjectsInactive.Include);
            Check(restoredFrog != null && restoredFrog.WasDefeatedThisSession && !restoredFrog.gameObject.activeSelf,
                "defeated boss remains absent on revisit", report);
            if (reproduce)
            {
                Check(!exit.gameObject.activeSelf, "REPRODUCED: restored defeated boss is inactive but its portal disappears", report);
                File.AppendAllText(report, "REPRODUCED\n");
                yield break;
            }
            Check(exit.gameObject.activeSelf && exit.GetComponent<Collider2D>().enabled,
                "restored boss portal remains visible and usable", report);
            Check(player.GetComponent<PlayerProgression>().CurrentExperience == experience,
                "world restoration does not award defeat experience again", report);
            yield return Transfer(exit, movement);
            Check(SceneManager.GetActiveScene().name == "BrokenSeal", "returned portal can be used again", report);
            SceneWorldState.Invalidate("Marshland");
            yield return Transfer(Portal("In_BrokenSeal_Portal"), movement);
            Check(Portal("Out_Marshland_Portal").gameObject.activeSelf,
                "portal also restores without temporary map cache", report);
            File.AppendAllText(report, "COMPLETE\n");
        }

        private static IEnumerator Transfer(PortalManager portal, PlayerMovement2D movement)
        {
            // Isolate boss visibility from an unrelated necklace quest prerequisite.
            var data = new SerializedObject(portal);
            data.FindProperty("requiresToadNecklace").boolValue = false;
            data.ApplyModifiedPropertiesWithoutUndo();
            typeof(PortalManager).GetField("playerInPortal", Hidden).SetValue(portal, movement);
            typeof(PortalManager).GetMethod("MoveToScene", Hidden).Invoke(portal, null);
            if (!PortalSceneTransition.IsTransitioning) throw new InvalidOperationException("Portal did not start transfer: " + portal.name);
            float deadline = Time.realtimeSinceStartup + 20f;
            while (PortalSceneTransition.IsTransitioning || SceneLoadingScreen.IsVisible)
            {
                if (Time.realtimeSinceStartup > deadline) throw new InvalidOperationException("Transfer timeout");
                yield return null;
            }
        }

        private static PortalManager Portal(string name) => UnityEngine.Object.FindObjectsByType<PortalManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(p => p.gameObject.scene == SceneManager.GetActiveScene() && p.name.Trim() == name);

        private static void Check(bool ok, string message, string report)
        {
            File.AppendAllText(report, (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
