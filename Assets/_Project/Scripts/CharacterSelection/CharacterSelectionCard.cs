using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.CharacterSelection
{
    [DisallowMultipleComponent]
    public sealed class CharacterSelectionCard : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private CharacterClassDefinition definition;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image cardBackground;
        [SerializeField] private TMP_Text classNameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text availabilityText;

        private static readonly Color NormalColor = new Color(0.045f, 0.055f, 0.065f, 0.94f);
        private static readonly Color HoverColor = new Color(0.16f, 0.11f, 0.055f, 0.98f);

        private CharacterSelectionController controller;
        private Vector3 normalScale;

        public CharacterClassDefinition Definition => definition;

        public void Initialize(CharacterSelectionController owner)
        {
            controller = owner;
            normalScale = transform.localScale;

            if (definition == null)
                return;

            portraitImage.sprite = definition.Portrait;
            portraitImage.enabled = definition.Portrait != null;
            classNameText.text = definition.DisplayName;
            roleText.text = definition.CombatRole;
            availabilityText.text = definition.Playable ? "선택" : "준비 중";
            availabilityText.color = definition.Playable
                ? new Color(0.95f, 0.76f, 0.32f, 1f)
                : new Color(0.68f, 0.68f, 0.68f, 1f);
            cardBackground.color = NormalColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            controller?.ShowDetails(definition);
            cardBackground.color = HoverColor;
            transform.localScale = normalScale * 1.035f;
            transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            cardBackground.color = NormalColor;
            transform.localScale = normalScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                controller?.SelectClass(definition);
        }

        private void OnDisable()
        {
            transform.localScale = normalScale == Vector3.zero ? Vector3.one : normalScale;
        }
    }
}
