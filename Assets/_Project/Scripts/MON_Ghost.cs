using EasternFantasy.Quest;
using UnityEngine;

public partial class MON_Ghost : EnemyController
{
    [SerializeField] private QuestDefinition questDefinition;
    public QuestDefinition Quest => questDefinition;
    protected override void MonsterAbility()
    {
        TryBeginScareAttack();
    }

    protected override void OnDefeated()
    {
       FinishScareAttack(false);
       EnemyDeathAudio.PlayFor(this, "Audio/GhostDeathCut", .1f);
       QuestManager.Instance.AddProgress(questDefinition, 1);
       base.OnDefeated();
    }

    protected override void OnDisable()
    {
        FinishScareAttack(false);
        base.OnDisable();
    }
    
}
