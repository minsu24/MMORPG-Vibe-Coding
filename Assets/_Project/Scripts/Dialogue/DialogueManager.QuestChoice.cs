using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.Dialogue
{
    public sealed partial class DialogueManager
    {
        private static readonly Color QuestSelectionGold = new(1f, .75f, .28f, 1f);
        private Button selectedQuestChoice;
        private GameObject acceptChoiceBorder;
        private GameObject declineChoiceBorder;
        private int questChoiceOpenedFrame = -1;
        private int questChoiceResolvedFrame = -1;

        private void BeginQuestChoiceSelection()
        {
            questChoiceOpenedFrame = Time.frameCount;
            if (acceptChoiceButton == null || declineChoiceButton == null) return;
            if (acceptChoiceBorder == null)
            {
                acceptChoiceBorder = CreateChoiceBorder(acceptChoiceButton);
                declineChoiceBorder = CreateChoiceBorder(declineChoiceButton);
                acceptChoiceButton.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnRight = declineChoiceButton,
                    selectOnDown = declineChoiceButton
                };
                declineChoiceButton.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = acceptChoiceButton,
                    selectOnUp = acceptChoiceButton
                };
            }
            SelectQuestChoice(acceptChoiceButton);
        }

        private void UpdateQuestChoice()
        {
            if (selectedQuestChoice == null) return;
            var focused = EventSystem.current?.currentSelectedGameObject;
            if (focused == acceptChoiceButton.gameObject) SetQuestChoiceHighlight(acceptChoiceButton);
            else if (focused == declineChoiceButton.gameObject) SetQuestChoiceHighlight(declineChoiceButton);

            var keyboard = Keyboard.current;
            if (keyboard == null || Time.frameCount == questChoiceOpenedFrame) return;

            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                SelectQuestChoice(acceptChoiceButton);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                SelectQuestChoice(declineChoiceButton);

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame)
                selectedQuestChoice.onClick.Invoke();
        }

        private void SelectQuestChoice(Button button)
        {
            SetQuestChoiceHighlight(button);
            EventSystem.current?.SetSelectedGameObject(button.gameObject);
        }

        private void SetQuestChoiceHighlight(Button button)
        {
            if (selectedQuestChoice == button) return;
            selectedQuestChoice = button;
            acceptChoiceBorder?.SetActive(button != null && button == acceptChoiceButton);
            declineChoiceBorder?.SetActive(button != null && button == declineChoiceButton);
        }

        private void ClearQuestChoiceSelection()
        {
            var events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null
                && (events.currentSelectedGameObject == acceptChoiceButton?.gameObject
                    || events.currentSelectedGameObject == declineChoiceButton?.gameObject))
                events.SetSelectedGameObject(null);
            SetQuestChoiceHighlight(null);
        }

        private static GameObject CreateChoiceBorder(Button button)
        {
            const float thickness = 3f;
            var border = new GameObject("Gold Selection Border", typeof(RectTransform));
            border.layer = button.gameObject.layer;
            var rect = (RectTransform)border.transform;
            rect.SetParent(button.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * -5f;
            rect.offsetMax = Vector2.one * 5f;

            AddBorderEdge(rect, "Top", Vector2.up, Vector2.one, new Vector2(0f, -thickness), Vector2.zero);
            AddBorderEdge(rect, "Bottom", Vector2.zero, Vector2.right, Vector2.zero, new Vector2(0f, thickness));
            AddBorderEdge(rect, "Left", Vector2.zero, Vector2.up, new Vector2(0f, thickness), new Vector2(thickness, -thickness));
            AddBorderEdge(rect, "Right", Vector2.right, Vector2.one, new Vector2(-thickness, thickness), new Vector2(0f, -thickness));
            border.SetActive(false);
            return border;
        }

        private static void AddBorderEdge(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var edge = new GameObject(name, typeof(RectTransform), typeof(Image));
            edge.layer = parent.gameObject.layer;
            var image = edge.GetComponent<Image>();
            image.color = QuestSelectionGold;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
