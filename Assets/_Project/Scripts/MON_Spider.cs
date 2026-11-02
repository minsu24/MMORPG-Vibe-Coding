using EasternFantasy.Quest;
using UnityEngine;

public partial class MON_Spider : EnemyController
{
    [SerializeField] private QuestDefinition questDefinition;
    [SerializeField] private string questSceneName = "SpiderCave";
    public QuestDefinition Quest => questDefinition;

    protected override void MonsterAbility() { TryBeginSpitAttack(); }

    protected override void OnDefeated()
    {
        FinishSpitAttack(false);
        EnemyDeathAudio.PlayFor(this, "Audio/SpiderDeath");
        if (questDefinition != null && gameObject.scene.name == questSceneName)
            QuestManager.Instance?.AddProgress(questDefinition);
        base.OnDefeated();
    }

    protected override void OnDisable()
    {
        FinishSpitAttack(false);
        base.OnDisable();
    }
}
