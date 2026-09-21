using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterSpawner : MonoBehaviour
{
    [Header("Spawn Target")]
    [SerializeField] private EnemyController monsterPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform spawnedMonsterRoot;

    [Header("Spawn Rules")]
    [SerializeField, Min(1)] private int maximumAlive = 6;
    [SerializeField, Min(0f)] private float initialSpawnDelay;
    [SerializeField, Min(0.1f)] private float refillInterval = 10f;

    private readonly List<EnemyController> aliveMonsters = new List<EnemyController>();

    public int CurrentAliveCount
    {
        get
        {
            RemoveDestroyedMonsters();
            return aliveMonsters.Count;
        }
    }

    private IEnumerator Start()
    {
        if (initialSpawnDelay > 0f)
            yield return new WaitForSeconds(initialSpawnDelay);

        RefillMissingMonsters();

        while (true)
        {
            yield return new WaitForSeconds(refillInterval);
            RefillMissingMonsters();
        }
    }

    [ContextMenu("Refill Missing Monsters")]
    public void RefillMissingMonsters()
    {
        RemoveDestroyedMonsters();

        if (monsterPrefab == null)
        {
            Debug.LogWarning($"[{nameof(MonsterSpawner)}] Monster Prefab is not assigned.", this);
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[{nameof(MonsterSpawner)}] At least one Spawn Point is required.", this);
            return;
        }

        int missingCount = Mathf.Max(0, maximumAlive - aliveMonsters.Count);
        int firstPointIndex = Random.Range(0, spawnPoints.Length);

        for (int i = 0; i < missingCount; i++)
        {
            Transform spawnPoint = spawnPoints[(firstPointIndex + i) % spawnPoints.Length];
            if (spawnPoint == null)
                continue;

            EnemyController monster = Instantiate(
                monsterPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnedMonsterRoot);

            monster.name = $"{monsterPrefab.name}_{aliveMonsters.Count + 1}";
            aliveMonsters.Add(monster);
        }
    }

    private void RemoveDestroyedMonsters()
    {
        aliveMonsters.RemoveAll(monster => monster == null);
    }

    private void OnValidate()
    {
        maximumAlive = Mathf.Max(1, maximumAlive);
        initialSpawnDelay = Mathf.Max(0f, initialSpawnDelay);
        refillInterval = Mathf.Max(0.1f, refillInterval);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null)
            return;

        Gizmos.color = new Color(0.9f, 0.25f, 0.65f, 0.9f);
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            Gizmos.DrawWireSphere(spawnPoint.position, 0.35f);
        }
    }
}
