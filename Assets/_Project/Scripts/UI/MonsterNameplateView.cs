using TMPro;
using UnityEngine;

namespace EasternFantasy.UI
{
    public sealed class MonsterNameplateView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private MonsterCodexCatalog catalog;
        public void Configure(TMP_Text text, MonsterCodexCatalog data) { label = text; catalog = data; }

        public void SetMonster(EnemyController enemy)
        {
            string displayName = enemy.MonsterDisplayName;
            string id = MonsterCodexDiscovery.IdForEnemy(enemy);
            if (string.IsNullOrWhiteSpace(displayName) && catalog != null && !string.IsNullOrEmpty(id))
                foreach (var region in catalog.regions)
                    foreach (var monster in region.monsters)
                        if (monster.id == id) displayName = monster.name;
            if (string.IsNullOrWhiteSpace(displayName) && enemy is MON_Ginseng) displayName = "인삼 요괴";
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = enemy.gameObject.name.Replace("(Clone)", "").Trim();
                int suffix = displayName.LastIndexOf('_');
                if (suffix >= 0 && int.TryParse(displayName.Substring(suffix + 1), out _))
                    displayName = displayName.Substring(0, suffix);
            }
            label.text = displayName;
            ((RectTransform)transform).sizeDelta = new Vector2(Mathf.Max(64f, label.GetPreferredValues(displayName).x + 24f), 30f);
        }
    }
}
