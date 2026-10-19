using EasternFantasy.Quest;
using UnityEngine;

public class MON_Spider : EnemyController
{
    [SerializeField] private QuestDefinition questDefinition;
    [SerializeField] private string questSceneName = "SpiderCave";
    public QuestDefinition Quest => questDefinition;

    protected override void MonsterAbility() { }

    protected override void OnDefeated()
    {
        if (questDefinition != null && gameObject.scene.name == questSceneName)
            QuestManager.Instance?.AddProgress(questDefinition);
        base.OnDefeated();
    }
}