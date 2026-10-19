using EasternFantasy.Skill;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class PetCompanionSkillSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private PetCompanionWindowUI window;
        private PlayerSkillSystem system;
        [SerializeField] private Image icon;
        private GameObject dragVisual;
        private bool unlocked;
        public SkillDefinition Definition { get; private set; }
        public bool IsDragging => dragVisual != null;
        public void Configure(PetCompanionWindowUI owner, PlayerSkillSystem playerSkills, SkillDefinition skill, bool acquired)
        {
            GetComponent<Image>().raycastTarget = true;
            window = owner; system = playerSkills; Definition = skill; unlocked = acquired;
            // Bind the actual slot graphic instead of adding a runtime overlay over its lock image.
            if (icon == null) icon = GetComponent<Image>();
            icon.preserveAspect = true;
            icon.sprite = skill != null ? skill.Icon : null;
            icon.enabled = skill != null && skill.Icon != null;
            icon.color = acquired ? Color.white : new Color(.3f, .3f, .3f, .8f);
        }
        public void OnPointerEnter(PointerEventData data) => window?.ShowSkillDescription(Definition, unlocked);
        public void OnPointerExit(PointerEventData data) => window?.RestoreWindowHint();
        public void OnBeginDrag(PointerEventData data)
        {
            if (!unlocked || system == null || !system.CanPlaceInQuickSlot(Definition)) return;
            Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
            dragVisual = new GameObject("CompanionSkillDragIcon", typeof(RectTransform), typeof(Image));
            dragVisual.transform.SetParent(canvas.transform, false);
            ((RectTransform)dragVisual.transform).sizeDelta = new Vector2(64f, 64f);
            var image = dragVisual.GetComponent<Image>();
            image.sprite = Definition.Icon; image.preserveAspect = true; image.raycastTarget = false;
            var overlay = dragVisual.AddComponent<Canvas>(); overlay.overrideSorting = true; overlay.sortingOrder = 7000;
            window.SetSkillDragActive(true); OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (dragVisual == null) return;
            var canvas = dragVisual.GetComponentInParent<Canvas>().rootCanvas;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)canvas.transform,
                data.position, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var position))
                dragVisual.transform.position = position;
        }
        public void OnEndDrag(PointerEventData data) => EndDrag();
        private void OnDisable() => EndDrag();
        private void EndDrag()
        {
            if (dragVisual != null) Destroy(dragVisual);
            dragVisual = null; window?.SetSkillDragActive(false);
        }
    }
}
