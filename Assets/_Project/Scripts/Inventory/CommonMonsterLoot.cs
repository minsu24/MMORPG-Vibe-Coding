using UnityEngine;

namespace EasternFantasy.Inventory
{
    [CreateAssetMenu(fileName = "CommonMonsterLoot", menuName = "Eastern Fantasy/Inventory/Common Monster Loot")]
    public sealed class CommonMonsterLoot : ScriptableObject
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private WorldItemPickup pickupPrefab;
        [SerializeField, Range(0f, 1f)] private float dropChance = .5f;
        public ItemDefinition Item => item;
        public WorldItemPickup PickupPrefab => pickupPrefab;
        public float DropChance => dropChance;

        private static CommonMonsterLoot cached;
        public static void DropFor(EnemyController enemy)
        {
            if (cached == null) cached = Resources.Load<CommonMonsterLoot>("CommonMonsterLoot");
            if (cached != null) cached.Spawn(enemy, Random.value);
        }

        public bool ShouldDrop(float roll) => dropChance > 0f &&
            (dropChance >= 1f || roll < dropChance);

        private void Spawn(EnemyController enemy, float roll)
        {
            if (enemy == null || item == null || pickupPrefab == null || !ShouldDrop(roll)) return;
            Vector3 origin = enemy.transform.position;
            var floor = DropPlacement2D.FindSupport(enemy.gameObject.scene, origin);
            float desiredX = origin.x + Random.Range(-.65f, .65f);
            if (!DropPlacement2D.TryFindLanding(floor, origin, desiredX,
                pickupPrefab.VisualWidth * .5f, out Vector3 landing))
                landing = origin;
            var pickup = Instantiate(pickupPrefab, origin + Vector3.up * .35f, Quaternion.identity);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pickup.gameObject, enemy.gameObject.scene);
            pickup.Initialize(item, 1, landing);
        }
    }
}