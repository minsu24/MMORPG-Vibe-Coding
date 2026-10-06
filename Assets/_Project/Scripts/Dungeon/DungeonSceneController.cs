using UnityEngine;

namespace EasternFantasy.Dungeon
{
    public sealed class DungeonSceneController : MonoBehaviour
    {
        public DungeonDefinition definition;
        private void Awake() => DungeonRunController.Begin(definition);
    }
}
