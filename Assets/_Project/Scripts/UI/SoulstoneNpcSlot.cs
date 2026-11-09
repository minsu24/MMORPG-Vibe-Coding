using TMPro;
using UnityEngine;

namespace EasternFantasy.UI
{
    public sealed class SoulstoneNpcSlot : MonoBehaviour
    {
        public MapNpcPortraitGraphic portrait;
        public TMP_Text caption;

        public void Bind(SoulstoneCatalog.Npc npc)
        {
            portrait.portrait = npc.portrait;
            portrait.SetVerticesDirty();
            caption.text = npc.name;
        }
    }
}
