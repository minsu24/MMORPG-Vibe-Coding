using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class MapIslandDiscoveryUI : MonoBehaviour
    {
        [SerializeField] private string regionId;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private string discoveredName;
        [SerializeField, Range(0, 1)] private float blackCoverOpacity = .9f;
        private Image island;
        private Button button;
        private MenuButtonHoverEffect hover;
        private Color normalColor;
        private bool normalInteractable, normalHover;
        private Selectable.Transition normalTransition;
        private bool prepared;
        public bool IsDiscovered => MapRegionDiscovery.IsVisited(regionId);

        private void Awake() => Prepare();
        private void Prepare()
        {
            if (prepared) return;
            prepared = true;
            island = GetComponent<Image>();
            button = GetComponent<Button>();
            hover = GetComponent<MenuButtonHoverEffect>();
            normalColor = island.color;
            if (button != null) { normalInteractable = button.interactable; normalTransition = button.transition; }
            if (hover != null) normalHover = hover.enabled;
        }
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            MapRegionDiscovery.Changed += Refresh;
            Refresh();
        }
        private void OnDisable() => MapRegionDiscovery.Changed -= Refresh;
        public void Refresh()
        {
            Prepare();
            bool visited = MapRegionDiscovery.IsVisited(regionId);
            // Multiplying RGB by (1-opacity) is a sprite-shaped black cover,
            // preserving transparent edges and the existing ink reveal material.
            if (hover != null) hover.enabled = visited && normalHover;
            if (button != null)
            {
                button.transition = visited ? normalTransition : Selectable.Transition.None;
                button.interactable = visited && normalInteractable;
            }
            island.CrossFadeColor(Color.white, 0, true, true);
            float light = visited ? 1 : 1 - blackCoverOpacity;
            island.color = new Color(normalColor.r * light, normalColor.g * light, normalColor.b * light, normalColor.a);
            if (nameLabel != null) nameLabel.text = visited ? discoveredName : "???";
        }
    }
}
