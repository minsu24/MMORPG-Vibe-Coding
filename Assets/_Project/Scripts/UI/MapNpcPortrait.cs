using UnityEngine;

namespace EasternFantasy.UI
{
    // Atlas selection geometry keeps the supplied bitmap intact.
    public sealed class MapNpcPortrait : ScriptableObject
    {
        public Texture2D atlas;
        public Rect region;
        public Vector4[] strips;
    }
}
