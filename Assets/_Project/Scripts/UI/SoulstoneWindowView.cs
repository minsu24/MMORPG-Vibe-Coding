using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    /// <summary>Editable Canvas prefab. Gameplay binds data and events without rebuilding its layout.</summary>
    [ExecuteAlways]
    public sealed class SoulstoneWindowView : MonoBehaviour
    {
        [Header("Window")]
        public GameObject modal;
        public RectTransform panel;
        public Vector2 screenMargin = new Vector2(32, 24);
        public Button close;

        [Header("Selected destination")]
        public Image preview;
        public AspectRatioFitter previewAspect;
        public TMP_Text mapName;
        public Button travel;
        public MenuButtonHoverEffect travelHover;
        public TMP_Text travelStatus;

        [Header("Destination list - duplicate the first row for additional destinations")]
        public RectTransform destinationContent;
        public List<Button> destinationRows = new List<Button>();

        [Header("NPC row - template icons remain editable in Prefab Mode")]
        public RectTransform npcContent;
        public TMP_Text noNpcs;
        public List<SoulstoneNpcSlot> npcSlots = new List<SoulstoneNpcSlot>();

        public bool LayoutDrivenByTutorial { get; set; }
        private void LateUpdate() => FitToCanvas();

        public void FitToCanvas()
        {
            if (panel == null || LayoutDrivenByTutorial) return;
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;
            var available = ((RectTransform)canvas.transform).rect.size - screenMargin * 2;
            var size = panel.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            float scale = Mathf.Min(1f, available.x / size.x, available.y / size.y);
            panel.localScale = Vector3.one * Mathf.Max(.1f, scale);
        }

        public void SetNpcs(SoulstoneCatalog.Npc[] npcs)
        {
            noNpcs.gameObject.SetActive(npcs.Length == 0);
            npcContent.gameObject.SetActive(npcs.Length > 0);
            while (npcSlots.Count < npcs.Length)
            {
                var slot = Instantiate(npcSlots[0], npcContent);
                slot.name = "NPC " + (npcSlots.Count + 1);
                npcSlots.Add(slot);
            }
            for (int i = 0; i < npcSlots.Count; i++)
            {
                npcSlots[i].gameObject.SetActive(i < npcs.Length);
                if (i < npcs.Length) npcSlots[i].Bind(npcs[i]);
            }
        }
    }
}
