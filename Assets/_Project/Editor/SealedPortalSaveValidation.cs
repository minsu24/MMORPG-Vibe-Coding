using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SealedPortalSaveValidation
    {
        private const string Folder = "Artifacts/SealedPortalSave";
        private const string DialogueId = "d189677d599e1ba4fae4d158d8d8f3af";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;

        public static void Prepare()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Prepare outside Play Mode.");
            Directory.CreateDirectory(Folder);
            MonkChargeAudioValidation.Prepare();
            string path = SaveGameService.PathFor(CharacterClassId.Monk);
            var data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
            data.scene = "BrokenSeal";
            data.position = new Vector3(-5f, 3f, 0f);
            data.level = 20;
            data.experience = 0;
            data.playedDialogues = (data.playedDialogues ?? Array.Empty<string>()).Where(id => id != DialogueId).ToArray();
            data.rewardedDialogues = (data.rewardedDialogues ?? Array.Empty<string>()).Where(id => id != DialogueId).ToArray();
            data.unlockedPortalDialogues = Array.Empty<string>();
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            File.WriteAllText(Folder + "/Validation.txt", "Native sealed portal save validation\n");
        }

        public static void RunFirstSession()
        {
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(FirstSession());
        }

        private static IEnumerator FirstSession()
        {
            if (UnityEngine.Object.FindFirstObjectByType<PlayerEntity>() == null)
            {
                yield return SceneManager.LoadSceneAsync("MovementPrototype");
                yield return null;
            }
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            var sequence = UnityEngine.Object.FindFirstObjectByType<LockedPortalSequence>();
            Check(sequence != null && sequence.GetComponent<PortalManager>().IsLocked, "new character starts with sealed door");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            player.isInvincible = true;
            player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            var dialogue = SaveAssetCatalog.Resolve<DialogueSequence>(DialogueId);
            var trigger = UnityEngine.Object.FindObjectsByType<DialogueTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(t => new SerializedObject(t).FindProperty("sequence").objectReferenceValue == dialogue);
            trigger.PlayDialogue();
            Check(DialogueManager.Instance.IsDialogueActive, "actual unlock dialogue starts");
            typeof(DialogueManager).GetMethod("CompleteDialogue", Hidden).Invoke(DialogueManager.Instance, null);
            Check(sequence.IsUnlocking, "dialogue completion starts original unlock cinematic");
            string path = SaveGameService.PathFor(CharacterClassId.Monk);
            var saved = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
            Check(saved.unlockedPortalDialogues != null && saved.unlockedPortalDialogues.Contains(DialogueId),
                "unlock saved to disk immediately, before cinematic finishes");
            float deadline = Time.realtimeSinceStartup + 10f;
            while (sequence.IsUnlocking && Time.realtimeSinceStartup < deadline) yield return null;
            Check(!sequence.IsUnlocking && !sequence.GetComponent<PortalManager>().IsLocked, "original cinematic completes with open portal");
            Check(!Seal(sequence).enabled, "unlocked seal graphic hidden");

            Restore(new SaveGameData());
            Check(sequence.GetComponent<PortalManager>().IsLocked && Seal(sequence).enabled, "fresh character reset clears another character's unlock");
            Restore(saved);
            Check(!sequence.GetComponent<PortalManager>().IsLocked && !Seal(sequence).enabled, "JSON round trip restores door and seal appearance");
            Restore(new SaveGameData { unlockedPortalDialogues = new[] { "missing-dialogue-id" } });
            Check(sequence.GetComponent<PortalManager>().IsLocked, "unknown save IDs safely ignored");
            Restore(new SaveGameData { playedDialogues = new[] { DialogueId } });
            Check(sequence.GetComponent<PortalManager>().IsLocked, "started but unrewarded conversation does not falsely unlock door");
            var legacy = new SaveGameData { playedDialogues = new[] { DialogueId }, rewardedDialogues = new[] { DialogueId } };
            Restore(legacy);
            Check(!sequence.GetComponent<PortalManager>().IsLocked && !Seal(sequence).enabled,
                "legacy save with completed GoldFrog dialogue recovers unlocked door");
            Check(Capture().Contains(DialogueId), "legacy recovery is promoted into explicit saved unlock");
            Restore(saved);

            yield return SceneManager.LoadSceneAsync("BrokenSeal");
            yield return null;
            sequence = UnityEngine.Object.FindFirstObjectByType<LockedPortalSequence>();
            Check(sequence != null && !sequence.GetComponent<PortalManager>().IsLocked && !sequence.IsUnlocking,
                "map revisit retains unlock without replaying cinematic");
            string message;
            Check(SaveGameService.SaveNow(out message), "normal save succeeds after revisit");
            SaveGameService.BeginAdminSession();
            string before = File.ReadAllText(path);
            Restore(new SaveGameData());
            typeof(LockedPortalSequence).GetMethod("OnDialogueCompleted", Hidden).Invoke(sequence, new object[] { dialogue });
            Check(!SaveGameService.SaveNow(out message) && File.ReadAllText(path) == before,
                "administrator unlock cannot write to save file");
            Restore(saved);
            File.AppendAllText(Folder + "/Validation.txt", "FIRST_SESSION_COMPLETE\n");
        }

        public static void RunRestartCheck()
        {
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(RestartCheck());
        }

        private static IEnumerator RestartCheck()
        {
            if (UnityEngine.Object.FindFirstObjectByType<PlayerEntity>() == null)
            {
                yield return SceneManager.LoadSceneAsync("MovementPrototype");
                yield return null;
            }
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            var sequence = UnityEngine.Object.FindFirstObjectByType<LockedPortalSequence>();
            Check(SceneManager.GetActiveScene().name == "BrokenSeal", "new Play session loads saved BrokenSeal map");
            Check(sequence != null && !sequence.GetComponent<PortalManager>().IsLocked && !Seal(sequence).enabled,
                "cold session restores open portal and hidden seal from disk");
            Check(!sequence.IsUnlocking && !SaveGameService.IsAdminSession, "restart clears test mode and does not replay unlock cinematic");
            File.AppendAllText(Folder + "/Validation.txt", "COMPLETE\n");
        }

        private static SpriteRenderer Seal(LockedPortalSequence sequence) =>
            (SpriteRenderer)typeof(LockedPortalSequence).GetField("sealRenderer", Hidden).GetValue(sequence);
        private static void Restore(SaveGameData data) =>
            typeof(LockedPortalSequence).GetMethod("RestoreSave", Static).Invoke(null, new object[] { data });
        private static string[] Capture() => (string[])typeof(LockedPortalSequence).GetMethod("CaptureSave", Static).Invoke(null, null);

        private static void Check(bool ok, string message)
        {
            File.AppendAllText(Folder + "/Validation.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
