using UnityEngine;
using EasternFantasy.Dialogue;

public class MON_Goblin : EnemyController
{
    [SerializeField] private DialogueSequence defeatedDialogue;

    protected override void MonsterAbility()
    {
        
    }

    protected override void OnDefeated()
    {
        if (defeatedDialogue != null && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(defeatedDialogue);

        base.OnDefeated();
    }
}
