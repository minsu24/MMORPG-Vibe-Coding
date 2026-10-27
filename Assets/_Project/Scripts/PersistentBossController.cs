using System.Collections.Generic;
using UnityEngine;

// Runtime defeat sets are captured/restored by the per-character save system.
public abstract partial class PersistentBossController : EnemyController
{
    [Tooltip("Optional stable encounter ID, unique within the scene. Otherwise the initial hierarchy is used.")]
    [SerializeField] private string encounterId;
    private static readonly HashSet<string> sessionDefeats = new HashSet<string>();
    private static readonly HashSet<string> dungeonDefeats = new HashSet<string>();
    private string encounterKey;
    protected virtual bool ResetsOnDungeonEntry => false;
    private HashSet<string> Defeats => ResetsOnDungeonEntry ? dungeonDefeats : sessionDefeats;
    public bool WasDefeatedThisSession => encounterKey != null && Defeats.Contains(encounterKey);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        sessionDefeats.Clear();
        dungeonDefeats.Clear();
    }

    public static void BeginDungeonRun() => dungeonDefeats.Clear();

    protected override void Awake()
    {
        // Cache before patrol, reparenting or death can change the encounter's identity.
        string identity = encounterId;
        if (string.IsNullOrWhiteSpace(identity))
        {
            identity = string.Empty;
            for (Transform node = transform; node != null; node = node.parent)
                identity = node.name + "[" + node.GetSiblingIndex() + "]/" + identity;
        }
        encounterKey = SceneStateIdentity.Key(gameObject.scene) + ":" + GetType().FullName + ":" + identity;
        base.Awake();
    }

    protected virtual void Start()
    {
        // DungeonSceneController starts/continues the run in Awake, before this check.
        if (!WasDefeatedThisSession) return;
        HP = 0f;
        RestoreDefeatedWorldState();
        // Keep portal references intact; do not replay rewards, dialogue or defeat events.
        gameObject.SetActive(false);
    }

    protected virtual void RestoreDefeatedWorldState() { }

    protected override void OnDefeated()
    {
        Defeats.Add(encounterKey);
        base.OnDefeated();
    }
}
