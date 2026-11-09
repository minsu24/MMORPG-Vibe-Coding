using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    [InitializeOnLoad]
    public static class DamageTextValidation
    {
        private const string State = "DamageText.Validation";
        private const string Report = "Artifacts/DamageText/Validation.txt";
        private static readonly string[] Scenes = { "SmogMountain", "SmogMountain", "SmogMountain", "GiantFarm_01", "GiantFarm_02", "GiantFarm_03_Boss", "Marshland", "MovementPrototype" };
        private static readonly string[] Types = { "MON_Ginseng", "MON_Ghost", "MON_Spider", "FarmCartAttack", "MON_Boar", "MON_Giant", "MON_Frog", "MON_Goblin" };
        private static int index, phase;
        private static double started;

        static DamageTextValidation()
        {
            EditorApplication.playModeStateChanged += Changed;
            if (EditorApplication.isPlaying && SessionState.GetBool(State, false))
            { started = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
        }
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            Directory.CreateDirectory("Artifacts/DamageText");
            SessionState.SetString(State + ".Save", SessionState.GetString("EasternFantasy.SaveTestDirectory", ""));
            SessionState.SetString("EasternFantasy.SaveTestDirectory", Path.GetFullPath("Artifacts/DamageText/TestSaves-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")));
            SessionState.SetBool(State, true); File.WriteAllText(Report, "RUNNING: normal/critical/suppressed damage numbers for all eight monster types.\n");
            EditorApplication.isPlaying = true;
        }
        private static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(State, false))
            {
                index = 0; phase = 0; started = EditorApplication.timeSinceStartup;
                EditorApplication.update -= Tick; EditorApplication.update += Tick;
            }
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(State, false)) return;
            EditorApplication.update -= Tick; SessionState.SetBool(State, false);
            SessionState.SetString("EasternFantasy.SaveTestDirectory", SessionState.GetString(State + ".Save", ""));
        }
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); File.AppendAllText(Report, "PASS: " + message + "\n"); }
        private static void Tick()
        {
            if (!Application.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (EditorApplication.timeSinceStartup-started > 120) throw new TimeoutException("Damage validation phase " + phase + " monster " + index);
                if (phase == 0)
                {
                    Application.runInBackground = true;
                    GameTimeController.SetPaused(typeof(DamageTextValidation), true);
                    SceneManager.LoadSceneAsync(Scenes[index]); phase = 1; return;
                }
                if (SceneManager.GetActiveScene().name != Scenes[index] || EasternFantasy.Saving.SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) return;
                GameTimeController.SetPaused(typeof(DamageTextValidation), true);
                EnemyController enemy;
                if (index < 3)
                {
                    var path = "Assets/_Project/Prefabs/Enemy/" + new[] { "Ginseng", "Ghost", "Spider" }[index] + ".prefab";
                    enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path)).GetComponent<EnemyController>();
                    var spawn = GameObject.Find("MonsterSpawner");
                    enemy.transform.position = spawn != null ? spawn.transform.position : Vector3.zero;
                }
                else enemy = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .First(e => e.GetType().Name == Types[index] && e.gameObject.scene == SceneManager.GetActiveScene());
                enemy.enabled = false;
                Physics2D.SyncTransforms();
                float health = enemy.HP;
                Check(health > 2, Types[index] + " initialized with enough health for nonlethal checks");
                var normal = Hit(enemy, false);
                Check(enemy.HP == health-1 && normal.GetComponent<TMP_Text>().text == "1", Types[index] + " normal hit creates the correct damage value");
                var critical = Hit(enemy, true);
                Check(critical.GetComponent<TMP_Text>().text == "CRIT! 1", Types[index] + " critical hit creates the critical value");
                var hitbox = enemy.GetComponent<Collider2D>();
                Check(hitbox == null || normal.transform.position.y >= hitbox.bounds.max.y+.24f, Types[index] + " number spawns above the head collider");
                Check(normal.GetComponent<Renderer>().sortingOrder == 1000 && critical.GetComponent<Renderer>().sortingOrder == 1000,
                    Types[index] + " normal and critical text render in front of world sprites");
                int before = Object.FindObjectsByType<DamageText>(FindObjectsSortMode.None).Length;
                enemy.TakeDamage(1, false, false);
                Check(Object.FindObjectsByType<DamageText>(FindObjectsSortMode.None).Length == before, Types[index] + " multi-hit suppression does not create duplicate numbers");
                if (index == 0) RenderGinseng(enemy, normal, critical);
                Object.Destroy(normal.gameObject); Object.Destroy(critical.gameObject);
                if (++index < Types.Length) { phase = 0; return; }
                File.AppendAllText(Report, "PASS: ALL 8 MONSTER TYPES VERIFIED. Original scenes/saves restored on exit.\n");
                EditorApplication.update -= Tick; GameTimeController.SetPaused(typeof(DamageTextValidation), false); EditorApplication.isPlaying = false;
            }
            catch (Exception exception)
            {
                File.AppendAllText(Report, "FAILED: " + exception + "\n"); Debug.LogException(exception);
                EditorApplication.update -= Tick; GameTimeController.SetPaused(typeof(DamageTextValidation), false); EditorApplication.isPlaying = false;
            }
        }
        private static DamageText Hit(EnemyController enemy, bool critical)
        {
            var before = new HashSet<DamageText>(Object.FindObjectsByType<DamageText>(FindObjectsSortMode.None));
            enemy.TakeDamage(1, critical);
            return Object.FindObjectsByType<DamageText>(FindObjectsSortMode.None).Single(t => !before.Contains(t));
        }
        private static void RenderGinseng(EnemyController enemy, DamageText normal, DamageText critical)
        {
            var source = Camera.main;
            var root = new GameObject("Damage Number Verification Camera");
            var rt = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                var camera = root.AddComponent<Camera>(); camera.CopyFrom(source); camera.enabled = false;
                var data = source.GetComponent<UniversalAdditionalCameraData>();
                if (data != null) EditorUtility.CopySerialized(data, root.AddComponent<UniversalAdditionalCameraData>());
                camera.transform.position = new Vector3(enemy.transform.position.x, enemy.DamageTextPosition.y-1, source.transform.position.z);
                camera.orthographic = true; camera.orthographicSize = 3.5f; camera.aspect = 1280f/720;
                camera.targetTexture = rt;
                critical.transform.position = normal.transform.position + Vector3.up * .6f + Vector3.right * .7f;
                foreach (var text in new[] { normal, critical }) text.GetComponent<TMP_Text>().ForceMeshUpdate();
                camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes("Artifacts/DamageText/Ginseng-after.png",image.EncodeToPNG());
                var after = image.GetPixels32();
                normal.GetComponent<Renderer>().sortingOrder = critical.GetComponent<Renderer>().sortingOrder = 0;
                camera.Render(); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes("Artifacts/DamageText/Ginseng-before.png",image.EncodeToPNG());
                var before = image.GetPixels32(); int changed = 0;
                for (int i=0;i<before.Length;i++) if (Mathf.Abs(before[i].r-after[i].r)+Mathf.Abs(before[i].g-after[i].g)+Mathf.Abs(before[i].b-after[i].b)>30) changed++;
                Check(changed>5,"Ginseng render comparison confirms text visibility changes when sorting is corrected ("+changed+" pixels)");
            }
            finally
            {
                root.GetComponent<Camera>().targetTexture = null; RenderTexture.active=previous;
                Object.Destroy(root); Object.Destroy(rt); Object.Destroy(image);
            }
        }
    }
}
