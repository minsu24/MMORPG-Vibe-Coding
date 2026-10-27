using System.Collections.Generic;
using EasternFantasy.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

// Session-only map memory. Scene geometry is reloaded; enemies are restored without replaying rewards.
public sealed partial class SceneWorldState : MonoBehaviour
{
    public const float RetentionSeconds = 300f;

    public sealed class EnemyState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Health;
        public bool Active;
        public string Name;

        public static EnemyState Capture(EnemyController enemy) => new EnemyState
        {
            Position = enemy != null ? enemy.transform.position : Vector3.zero,
            Name = enemy != null ? enemy.name : null,
            Rotation = enemy != null ? enemy.transform.rotation : Quaternion.identity,
            Active = enemy != null && enemy.gameObject.activeInHierarchy && enemy.HP > 0f,
            Health = enemy != null && enemy.gameObject.activeInHierarchy ? enemy.HP : 0f
        };

        public void Apply(EnemyController enemy)
        {
            if (!Active) { enemy.gameObject.SetActive(false); return; }
            enemy.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(Name)) enemy.name = Name;
            enemy.transform.SetPositionAndRotation(Position, Rotation);
            var body = enemy.GetComponent<Rigidbody2D>();
            if (body != null) { body.position = Position; body.linearVelocity = Vector2.zero; }
            enemy.HP = Health;
        }
    }

    public sealed class SpawnerState
    {
        public float RemainingDelay;
        public readonly List<EnemyState> Monsters = new List<EnemyState>();
    }

    private sealed class MapState
    {
        public double ExpiresAt;
        public readonly Dictionary<string, EnemyState> Enemies = new Dictionary<string, EnemyState>();
        public readonly Dictionary<string, SpawnerState> Spawners = new Dictionary<string, SpawnerState>();
        public readonly List<GameObject> Drops = new List<GameObject>();
    }

    private static readonly Dictionary<string, MapState> maps = new Dictionary<string, MapState>();
    private static readonly Dictionary<string, EnemyController> sceneEnemies = new Dictionary<string, EnemyController>();
    private static SceneWorldState instance;
    private static int trackedSceneHandle;
    private float nextCleanup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        maps.Clear();
        sceneEnemies.Clear();
        instance = null;
        trackedSceneHandle = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private static string MapKey(Scene scene) => SceneStateIdentity.Key(scene);

    internal static string HierarchyKey(Transform target)
    {
        string key = string.Empty;
        for (var node = target; node != null; node = node.parent)
            key = node.name + "[" + node.GetSiblingIndex() + "]/" + key;
        return key;
    }

    private static IEnumerable<T> Components<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<T>(true))
                yield return component;
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        CleanupExpired();
        maps.TryGetValue(MapKey(scene), out var saved);
        sceneEnemies.Clear();
        trackedSceneHandle = scene.handle;
        foreach (var enemy in Components<EnemyController>(scene))
        {
            string key = HierarchyKey(enemy.transform);
            if (saved != null && saved.Enemies.TryGetValue(key, out var state)) state.Apply(enemy);
            sceneEnemies[key] = enemy;
        }
        foreach (var spawner in Components<MonsterSpawner>(scene))
            spawner.StateKey = HierarchyKey(spawner.transform);
        if (saved == null) return;
        RestoreDrops(scene, saved);
    }

    private static void RestoreDrops(Scene scene, MapState saved)
    {
        foreach (var drop in saved.Drops)
        {
            if (drop == null) continue; // The original loot lifetime continues while away.
            drop.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(drop, scene);
            drop.SetActive(true);
        }
        saved.Drops.Clear();
        saved.ExpiresAt = 0;
    }

    public static void CancelCapture()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (maps.TryGetValue(MapKey(scene), out var saved)) RestoreDrops(scene, saved);
    }

    public static void CaptureActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return;
        CleanupExpired();
        if (instance == null)
        {
            instance = new GameObject("Temporary Map State").AddComponent<SceneWorldState>();
            DontDestroyOnLoad(instance.gameObject);
        }
        var saved = new MapState { ExpiresAt = Time.realtimeSinceStartupAsDouble + RetentionSeconds };
        if (trackedSceneHandle == scene.handle)
            foreach (var pair in sceneEnemies) saved.Enemies[pair.Key] = EnemyState.Capture(pair.Value);
        foreach (var spawner in Components<MonsterSpawner>(scene))
            saved.Spawners[spawner.StateKey ?? HierarchyKey(spawner.transform)] = spawner.CaptureState();

        // Preserve the actual pickup, including its remaining lifetime, appearance and landing motion.
        var drops = new List<GameObject>();
        foreach (var item in Components<WorldItemPickup>(scene))
            if (item.Item != null && !item.IsCollected) drops.Add(item.gameObject);
        foreach (var coin in Components<WorldCurrencyPickup>(scene))
            if (coin.Amount > 0 && !coin.IsCollected) drops.Add(coin.gameObject);
        foreach (var drop in drops)
        {
            drop.SetActive(false);
            drop.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(drop, instance.gameObject.scene);
            drop.transform.SetParent(instance.transform, true);
            saved.Drops.Add(drop);
        }
        if (maps.TryGetValue(MapKey(scene), out var previous)) DestroyDrops(previous);
        maps[MapKey(scene)] = saved;
    }

    internal static bool TryRestoreSpawner(MonsterSpawner spawner, out SpawnerState state)
    {
        state = null;
        return maps.TryGetValue(MapKey(spawner.gameObject.scene), out var saved)
            && saved.Spawners.TryGetValue(spawner.StateKey ?? HierarchyKey(spawner.transform), out state);
    }

    public static void Invalidate(string sceneName)
    {
        var keys = new List<string>();
        foreach (var pair in maps)
            if (pair.Key == sceneName || pair.Key.EndsWith("/" + sceneName + ".unity")) keys.Add(pair.Key);
        foreach (var key in keys) { DestroyDrops(maps[key]); maps.Remove(key); }
    }

    private static void DestroyDrops(MapState state)
    {
        foreach (var drop in state.Drops) if (drop != null) Destroy(drop);
    }

    private static void CleanupExpired()
    {
        var expired = new List<string>();
        foreach (var pair in maps)
            if (pair.Value.ExpiresAt > 0 && pair.Value.ExpiresAt <= Time.realtimeSinceStartupAsDouble)
                expired.Add(pair.Key);
        foreach (var key in expired) { DestroyDrops(maps[key]); maps.Remove(key); }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCleanup) return;
        nextCleanup = Time.unscaledTime + 1f;
        CleanupExpired();
    }
}
