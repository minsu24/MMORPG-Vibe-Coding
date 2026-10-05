using EasternFantasy.Player;
using EasternFantasy.UI;
using UnityEngine;
namespace EasternFantasy.Dungeon
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class DungeonPortal : MonoBehaviour
    {
        [SerializeField] private DungeonWindowUI window;
        public void Configure(DungeonWindowUI target) => window = target;
        public bool Interact(PlayerMovement2D player)
        {
            if (window == null || player == null) return false;
            window.Open(this, player);
            return window.IsOpen;
        }
    }
}
