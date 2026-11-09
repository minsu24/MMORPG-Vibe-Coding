using EasternFantasy.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class TutorialMovementHintUI : MonoBehaviour
    {
        [SerializeField] private DialogueSequence introduction;
        [SerializeField] private DialogueSequence attackIntroduction;
        [SerializeField] private DialogueSequence portalIntroduction;
        [SerializeField] private Sprite leftArrow;
        [SerializeField] private Sprite rightArrow;
        [SerializeField] private Sprite spaceKey;
        [SerializeField] private Sprite ctrlKey;
        [SerializeField] private Sprite upArrow;
        [SerializeField] private TMP_FontAsset font;

        private DialogueManager manager;
        private CanvasGroup group;
        private RectTransform row;
        private bool showing;
        private enum HintKind { Movement, Attack, Portal }
        private HintKind currentHint;
        private float elapsed;
        private static readonly Vector2 RestingPosition = new Vector2(0f, -120f);

        private void OnEnable() => BindDialogue();
        private void Start() => BindDialogue();

        private void BindDialogue()
        {
            UnbindDialogue();
            manager = DialogueManager.Instance;
            if (manager == null) return;
            manager.DialogueCompleted += DialogueCompleted;
            manager.DialogueStarted += DialogueStarted;
        }

        private void UnbindDialogue()
        {
            if (manager == null) return;
            manager.DialogueCompleted -= DialogueCompleted;
            manager.DialogueStarted -= DialogueStarted;
            manager = null;
        }

        private void DialogueCompleted(DialogueSequence sequence)
        {
            if (sequence == null) return;
            bool attackHint = sequence == attackIntroduction;
            bool portalHint = sequence == portalIntroduction;
            if (sequence != introduction && !attackHint && !portalHint) return;
            currentHint = portalHint ? HintKind.Portal : attackHint ? HintKind.Attack : HintKind.Movement;
            if (row != null)
            {
                row.gameObject.SetActive(false);
                Destroy(row.parent.gameObject);
            }
            BuildHint();
            elapsed = 0f;
            showing = true;
            row.gameObject.SetActive(true);
            group.alpha = 0f;
        }

        private void Update()
        {
            if (manager != DialogueManager.Instance) BindDialogue();
            if (!showing || row == null) return;
            // Menus and loading do not consume the presentation animation.
            bool visible = Time.timeScale > 0f && !SceneLoadingScreen.IsVisible
                && !PortalSceneTransition.IsTransitioning
                && (manager == null || !manager.IsDialogueActive);
            if (!visible) { group.alpha = 0f; return; }
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / .35f));
            row.anchoredPosition = RestingPosition + Vector2.up * (Mathf.Sin(elapsed * Mathf.PI * .8f) * 6f);
        }

        private void BuildHint()
        {
            var root = new GameObject("Tutorial Movement Hint Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;

            var card = new GameObject("Floating Movement Hint", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            row = card.GetComponent<RectTransform>();
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(.5f, 1f);
            row.anchoredPosition = RestingPosition;
            group = card.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            var background = card.GetComponent<Image>();
            background.color = new Color(.025f, .035f, .045f, .65f);
            background.raycastTarget = false;
            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 28, 6, 6);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            if (currentHint == HintKind.Portal)
            {
                float portalWidth = AddInstruction("Portal", "키를 이용해 포탈을 타고 다른 맵으로 이동할 수 있습니다.", 640f, upArrow);
                row.sizeDelta = new Vector2(52f + portalWidth, 116f);
            }
            else if (currentHint == HintKind.Attack)
            {
                float attackWidth = AddInstruction("Basic Attack", "로 일반 공격을 할 수 있습니다.", 0f, ctrlKey);
                row.sizeDelta = new Vector2(52f + attackWidth, 92f);
            }
            else
            {
                float movementWidth = AddInstruction("Movement", "로 움직일 수 있습니다.", 0f, leftArrow, rightArrow);
                float jumpWidth = AddInstruction("Jump", "로 점프할 수 있습니다.", 0f, spaceKey);
                row.sizeDelta = new Vector2(52f + Mathf.Max(movementWidth, jumpWidth), 172f);
            }
        }

        private float AddInstruction(string instructionName, string message, float maximumTextWidth, params Sprite[] keys)
        {
            var instruction = new GameObject(instructionName + " Instruction Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            instruction.transform.SetParent(row, false);
            var instructionLayout = instruction.GetComponent<HorizontalLayoutGroup>();
            instructionLayout.spacing = 8f;
            instructionLayout.childAlignment = TextAnchor.MiddleLeft;
            instructionLayout.childControlWidth = instructionLayout.childControlHeight = true;
            instructionLayout.childForceExpandWidth = instructionLayout.childForceExpandHeight = false;
            var keyGroup = new GameObject(instructionName + " Keys", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            keyGroup.transform.SetParent(instruction.transform, false);
            var keyLayout = keyGroup.GetComponent<HorizontalLayoutGroup>();
            keyLayout.spacing = 8f;
            keyLayout.childAlignment = TextAnchor.MiddleCenter;
            keyLayout.childControlWidth = keyLayout.childControlHeight = true;
            keyLayout.childForceExpandWidth = keyLayout.childForceExpandHeight = false;
            var keySize = keyGroup.GetComponent<LayoutElement>();
            keySize.minWidth = keySize.preferredWidth = 168f;
            keySize.preferredHeight = 80f;
            foreach (var sprite in keys) AddKey(keyGroup.transform, sprite);

            var labelObject = new GameObject(instructionName + " Instruction", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(instruction.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = message;
            label.fontSize = 32f;
            label.color = new Color(1f, .95f, .83f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            bool wrap = maximumTextWidth > 0f;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            float textWidth = label.GetPreferredValues(label.text).x + 8f;
            if (wrap) textWidth = Mathf.Min(textWidth, maximumTextWidth);
            var labelLayout = labelObject.GetComponent<LayoutElement>();
            labelLayout.preferredWidth = textWidth;
            labelLayout.preferredHeight = wrap ? 104f : 56f;
            var instructionSize = instruction.GetComponent<LayoutElement>();
            instructionSize.preferredWidth = 168f + 8f + textWidth;
            instructionSize.preferredHeight = wrap ? 104f : 80f;
            return instructionSize.preferredWidth;
        }

        private void AddKey(Transform parent, Sprite sprite)
        {
            var key = new GameObject(sprite != null ? sprite.name : "Input Key", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            key.transform.SetParent(parent, false);
            var image = key.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var layout = key.GetComponent<LayoutElement>();
            layout.preferredWidth = layout.preferredHeight = 80f;
        }

        private void DialogueStarted()
        {
            // Keep the portal reminder pending through other dialogue until scene exit.
            if (currentHint != HintKind.Portal) Hide();
        }

        private void Hide()
        {
            showing = false;
            if (row != null) row.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            UnbindDialogue();
            Hide();
        }
    }
}
