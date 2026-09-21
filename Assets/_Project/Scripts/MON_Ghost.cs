using EasternFantasy.Quest;
using UnityEngine;

public class MON_Ghost : EnemyController
{
    [SerializeField] private QuestDefinition questDefinition;
    protected override void MonsterAbility()
    {
        
    }

    protected override void OnDefeated()
    {
       QuestManager.Instance.AddProgress(questDefinition, 1);
       base.OnDefeated();
    }
    
}
