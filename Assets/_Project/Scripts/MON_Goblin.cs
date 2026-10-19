using System.Collections.Generic;
using UnityEngine;
using EasternFantasy.Dialogue;

public class MON_Goblin : EnemyController
{
    [SerializeField] private DialogueSequence defeatedDialogue;
    [Tooltip("Stable ID for this goblin encounter within its scene. Use a different ID for each encounter.")]
    [SerializeField] private string encounterId = "tutorial-goblin";

    private static readonly HashSet<string> defeatedEncounters = new HashSet<string>();
    private string EncounterKey => gameObject.scene.path + ":" + encounterId;
    public bool WasDefeatedThisSession => defeatedEncounters.Contains(EncounterKey);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => defeatedEncounters.Clear();

    private void Start()
    {
        if (!WasDefeatedThisSession) return;
        // Keep scene references intact for the portal, without rewards or defeat dialogue.
        HP = 0f;
        gameObject.SetActive(false);
    }

    protected override void MonsterAbility()
    {
        
    }

    protected override void OnDefeated()
    {
        defeatedEncounters.Add(EncounterKey);
        base.OnDefeated();
        if (defeatedDialogue != null && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(defeatedDialogue);
        
    }
}
