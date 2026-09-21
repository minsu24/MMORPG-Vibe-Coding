using System;
using System.Collections;
using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueManager : MonoBehaviour
    {
        [Header("Dialogue UI - connect objects from the bottom dialogue panel")]
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private GameObject portraitRoot;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private GameObject continueIndicator;

        [Header("Left: Player")]
        [SerializeField] private Image playerPortrait;
        [SerializeField] private TMP_Text playerNameText;

        [Header("Right: Conversation Partner")]
        [SerializeField] private Image partnerPortrait;
        [SerializeField] private TMP_Text partnerNameText;

        [Header("Typing")]
        [SerializeField, Min(1f)] private float charactersPerSecond = 35f;
        [SerializeField, Range(0f, 1f)] private float inactivePortraitAlpha = 0.45f;

        [Header("Quest offer choice shown after the final line")]
        [SerializeField] private GameObject questChoiceRoot;
        [SerializeField] private TMP_Text questChoiceTitleText;

        [Header("Player control disabled during dialogue")]
        [SerializeField] private PlayerInputReader playerInput;

        private DialogueSequence currentSequence;
        private Coroutine typingRoutine;
        private int lineIndex;
        private bool isTyping;
        private bool playerInputWasEnabled;
        private float previousTimeScale = 1f;
        private Action<bool> pendingQuestDecision;
        private string pendingQuestTitle;
        private bool isWaitingForQuestDecision;

        public static DialogueManager Instance { get; private set; }
        public bool IsDialogueActive { get; private set; }
        public event Action DialogueStarted;
        public event Action DialogueEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Only one DialogueManager may exist in a scene.", this);
                enabled = false;
                return;
            }

            Instance = this;
            if (dialogueRoot != null)
                dialogueRoot.SetActive(false);
            if (portraitRoot != null)
                portraitRoot.SetActive(false);
            if (questChoiceRoot != null)
                questChoiceRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsDialogueActive)
                return;

            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
                Advance();
        }

        public bool StartDialogue(DialogueSequence sequence)
        {
            return BeginDialogue(sequence);
        }

        public bool StartQuestOffer(
            DialogueSequence sequence,
            string questTitle,
            Action<bool> decision)
        {
            if (decision == null || questChoiceRoot == null)
            {
                Debug.LogError("Quest dialogue needs a choice UI and decision callback.", this);
                return false;
            }

            pendingQuestDecision = decision;
            pendingQuestTitle = questTitle;
            if (BeginDialogue(sequence))
                return true;

            pendingQuestDecision = null;
            pendingQuestTitle = null;
            return false;
        }

        private bool BeginDialogue(DialogueSequence sequence)
        {
            if (IsDialogueActive)
                return false;

            if (sequence == null || !sequence.HasLines)
            {
                Debug.LogWarning("Dialogue cannot start without a sequence containing lines.", this);
                return false;
            }

            if (!HasRequiredUi())
                return false;

            currentSequence = sequence;
            lineIndex = 0;
            IsDialogueActive = true;

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            DisablePlayerInput();

            ApplyCharacter(playerPortrait, playerNameText, sequence.PlayerCharacter);
            ApplyCharacter(partnerPortrait, partnerNameText, sequence.PartnerCharacter);
            if (portraitRoot != null)
                portraitRoot.SetActive(true);
            dialogueRoot.SetActive(true);

            DialogueStarted?.Invoke();
            ShowCurrentLine();
            return true;
        }

        public void Advance()
        {
            if (!IsDialogueActive || isWaitingForQuestDecision)
                return;

            if (isTyping)
            {
                CompleteCurrentLineImmediately();
                return;
            }

            lineIndex++;
            if (lineIndex >= currentSequence.Lines.Length)
            {
                if (pendingQuestDecision != null)
                    ShowQuestDecision();
                else
                    EndDialogue();
                return;
            }

            ShowCurrentLine();
        }

        public void EndDialogue()
        {
            if (!IsDialogueActive)
                return;

            StopTyping();
            IsDialogueActive = false;
            currentSequence = null;
            isWaitingForQuestDecision = false;
            pendingQuestDecision = null;
            pendingQuestTitle = null;

            if (questChoiceRoot != null)
                questChoiceRoot.SetActive(false);

            if (dialogueRoot != null)
                dialogueRoot.SetActive(false);
            if (portraitRoot != null)
                portraitRoot.SetActive(false);

            RestorePlayerInput();
            Time.timeScale = previousTimeScale;
            DialogueEnded?.Invoke();
        }

        public void AcceptQuestChoice() => ResolveQuestDecision(true);
        public void DeclineQuestChoice() => ResolveQuestDecision(false);

        private void ShowQuestDecision()
        {
            isWaitingForQuestDecision = true;
            if (continueIndicator != null)
                continueIndicator.SetActive(false);
            if (questChoiceTitleText != null)
                questChoiceTitleText.text = pendingQuestTitle ?? string.Empty;
            questChoiceRoot.SetActive(true);
        }

        private void ResolveQuestDecision(bool accepted)
        {
            if (!isWaitingForQuestDecision || pendingQuestDecision == null)
                return;

            Action<bool> decision = pendingQuestDecision;
            pendingQuestDecision = null;
            pendingQuestTitle = null;
            EndDialogue();
            decision.Invoke(accepted);
        }

        private void ShowCurrentLine()
        {
            DialogueLine line = currentSequence.Lines[lineIndex];
            SetActiveSpeaker(line.Speaker);
            StopTyping();
            typingRoutine = StartCoroutine(TypeLine(line.Text ?? string.Empty));
        }

        private IEnumerator TypeLine(string text)
        {
            isTyping = true;
            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            dialogueText.text = text;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            int totalCharacters = dialogueText.textInfo.characterCount;
            float visibleCharacters = 0f;
            while (dialogueText.maxVisibleCharacters < totalCharacters)
            {
                visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
                dialogueText.maxVisibleCharacters = Mathf.Min(
                    totalCharacters,
                    Mathf.FloorToInt(visibleCharacters));
                yield return null;
            }

            isTyping = false;
            typingRoutine = null;
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        private void CompleteCurrentLineImmediately()
        {
            StopTyping();
            dialogueText.maxVisibleCharacters = int.MaxValue;
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        private void StopTyping()
        {
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);

            typingRoutine = null;
            isTyping = false;
        }

        private void SetActiveSpeaker(DialogueSpeakerSide speaker)
        {
            SetPortraitAlpha(playerPortrait,
                speaker == DialogueSpeakerSide.Player ? 1f : inactivePortraitAlpha);
            SetPortraitAlpha(partnerPortrait,
                speaker == DialogueSpeakerSide.Partner ? 1f : inactivePortraitAlpha);
        }

        private static void ApplyCharacter(
            Image portraitImage,
            TMP_Text nameText,
            DialogueCharacter character)
        {
            if (nameText != null)
                nameText.text = character != null ? character.DisplayName : string.Empty;

            if (portraitImage == null)
                return;

            portraitImage.sprite = character != null ? character.Portrait : null;
            portraitImage.enabled = portraitImage.sprite != null;
        }

        private static void SetPortraitAlpha(Image portrait, float alpha)
        {
            if (portrait == null)
                return;

            Color color = portrait.color;
            color.a = alpha;
            portrait.color = color;
        }

        private bool HasRequiredUi()
        {
            if (dialogueRoot != null && dialogueText != null)
                return true;

            Debug.LogError(
                "DialogueManager needs Dialogue Root and Dialogue Text UI references.",
                this);
            return false;
        }

        private void DisablePlayerInput()
        {
            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInputReader>();

            if (playerInput == null)
                return;

            playerInputWasEnabled = playerInput.enabled;
            playerInput.enabled = false;
        }

        private void RestorePlayerInput()
        {
            if (playerInput != null)
                playerInput.enabled = playerInputWasEnabled;
        }

        private void OnDisable()
        {
            if (IsDialogueActive)
                EndDialogue();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
