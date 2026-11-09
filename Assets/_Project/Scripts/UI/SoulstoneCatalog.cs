using System;
using UnityEngine;

namespace EasternFantasy.UI
{
    [CreateAssetMenu(menuName = "Eastern Fantasy/Soulstone Destinations")]
    public sealed class SoulstoneCatalog : ScriptableObject
    {
        [Serializable] public sealed class Npc
        {
            public string name;
            public MapNpcPortrait portrait;
        }
        [Serializable] public sealed class Destination
        {
            public string sceneName, displayName, stoneName;
            public Sprite preview;
            public Npc[] npcs = Array.Empty<Npc>();
        }
        public AnimationClip villageActivation, villageLoop, templeActivation, templeLoop;
        public float villagePromptTop, templePromptTop;
        public GameObject interactionPromptPrefab;
        public SoulstoneWindowView windowPrefab;
        public Sprite frame, listSlot;
        public Sprite mapFrame, mapNamePanel, listPanel, npcPanel, travelNormal, travelHover;
        public MapNpcPortrait mapFrameOutline;
        public TMPro.TMP_FontAsset font;
        public Destination[] destinations = Array.Empty<Destination>();
    }
}
