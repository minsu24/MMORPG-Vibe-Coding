using EasternFantasy.Dungeon;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace EasternFantasy.UI
{
    public sealed class DungeonListEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button button;
        public Image background, thumbnail;
        public TMP_Text title, state;
        public Sprite normalSprite, highlightSprite;
        private bool selected, hovered;
        public void Bind(DungeonWindowUI window, DungeonDefinition dungeon)
        {
            title.text = dungeon.displayName;
            thumbnail.sprite = dungeon.preview;
            thumbnail.enabled = dungeon.preview != null;
            state.text = window.CanEnter(dungeon) ? "입장" : "잠김";
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => window.Select(dungeon));
            SetSelected(false);
        }
        public void SetSelected(bool value) { selected = value; Refresh(); }
        public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }
        private void OnDisable() { hovered = false; Refresh(); }
        private void Refresh() => background.sprite = selected || hovered ? highlightSprite : normalSprite;
    }
}
