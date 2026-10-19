using System;
using System.Collections;
using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
using EasternFantasy.Quest;
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

        private TMP_Text advanceHint;
        private float advanceHintStartedAt;
        private DialogueSequence currentSequence;
        private string completionRewardText;
        private Coroutine typingRoutine;
        private int lineIndex;
        private bool isTyping;
        private bool playerInputWasEnabled;
        private Action<bool> pendingQuestDecision;
        private string pendingQuestTitle;
        private bool isWaitingForQuestDecision;

        public static DialogueManager Instance { get; private set; }
        public bool IsDialogueActive { get; private set; }
        public event Action DialogueStarted;
        public event Action DialogueEnded;
        public event Action<DialogueSequence> DialogueCompleted;

        private static readonly HashSet<CharacterClassId> advanceHintShownClasses = new HashSet<CharacterClassId>();
        private static readonly HashSet<DialogueSequence> rewardedSequences = new HashSet<DialogueSequence>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDialogueRewards()
        {
            rewardedSequences.Clear();
            advanceHintShownClasses.Clear();
        }

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

            if (advanceHint != null && advanceHint.gameObject.activeSelf)
            {
                float pulse = .5f - .5f * Mathf.Cos((Time.unscaledTime - advanceHintStartedAt) * Mathf.PI * 2f / 1.4f);
                advanceHint.rectTransform.localScale = Vector3.one * (1f + .1f * pulse);
            }

            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
                Advance();
        }

        public bool StartDialogue(DialogueSequence sequence)
        {
            return BeginDialogue(sequence);
        }

        public bool StartQuestCompletion(DialogueSequence sequence, QuestDefinition quest)
        {
            if (quest == null) return false;
            string rewardText = "보상 획득";
            if (quest.RewardYeopjeon > 0)
                rewardText += $"\n엽전 {quest.RewardYeopjeon:N0} 획득";
            if (quest.RewardEXP > 0)
                rewardText += $"\n경험치 {quest.RewardEXP:N0} 획득";
            if (quest.RewardYeopjeon <= 0 && quest.RewardEXP <= 0)
                rewardText = "퀘스트를 완료했습니다.";
            // Append a runtime notice without modifying the shared dialogue asset or its identity.
            return BeginDialogue(sequence, rewardText);
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

        private bool BeginDialogue(DialogueSequence sequence, string finalMessage = null)
        {
            if (IsDialogueActive)
                return false;

            if ((sequence == null || !sequence.HasLines) && string.IsNullOrEmpty(finalMessage))
            {
                Debug.LogWarning("Dialogue cannot start without a sequence containing lines.", this);
                return false;
            }

            if (!HasRequiredUi())
                return false;

            currentSequence = sequence;
            completionRewardText = finalMessage;
            lineIndex = 0;
            IsDialogueActive = true;

            GameTimeController.SetPaused(this, true);
            DisablePlayerInput();

            DialogueCharacter selectedPlayer =
                PlayerClassRuntime.ActiveDefinition?.DialogueCharacter
                ?? sequence?.PlayerCharacter;
            ApplyCharacter(playerPortrait, playerNameText, selectedPlayer);
            ApplyCharacter(partnerPortrait, partnerNameText, sequence?.PartnerCharacter);
            if (portraitRoot != null)
                portraitRoot.SetActive(true);
            dialogueRoot.SetActive(true);
            ShowAdvanceHint();

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
            if (advanceHint != null) advanceHint.gameObject.SetActive(false);
            int lineCount = (currentSequence?.Lines.Length ?? 0)
                + (string.IsNullOrEmpty(completionRewardText) ? 0 : 1);
            if (lineIndex >= lineCount)
            {
                if (pendingQuestDecision != null)
                    ShowQuestDecision();
                else
                    CompleteDialogue();
                return;
            }

            ShowCurrentLine();
        }

        private void CompleteDialogue()
        {
            DialogueSequence completed = currentSequence;
            EndDialogue();
            GrantCompletionExperience(completed);
            if (completed != null) DialogueCompleted?.Invoke(completed);
        }

        private void GrantCompletionExperience(DialogueSequence completed)
        {
            if (completed == null || completed.CompletionExperience <= 0 || rewardedSequences.Contains(completed))
                return;

            PlayerProgression progression = FindFirstObjectByType<PlayerProgression>();
            if (progression == null || !progression.isActiveAndEnabled)
                return;

            // Record before payment: a level-up can immediately start an advancement dialogue.
            rewardedSequences.Add(completed);
            progression.AddExperience(completed.CompletionExperience);
        }

        public void EndDialogue()
        {
            if (!IsDialogueActive)
                return;

            StopTyping();
            if (advanceHint != null) advanceHint.gameObject.SetActive(false);
            IsDialogueActive = false;
            currentSequence = null;
            completionRewardText = null;
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
            GameTimeController.SetPaused(this, false);
            DialogueEnded?.Invoke();
        }

        public void AcceptQuestChoice() => ResolveQuestDecision(true);
        public void DeclineQuestChoice() => ResolveQuestDecision(false);

        private void ShowQuestDecision()
        {
            isWaitingForQuestDecision = true;
            if (advanceHint != null) advanceHint.gameObject.SetActive(false);
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
            if (!string.IsNullOrEmpty(completionRewardText)
                && lineIndex >= (currentSequence?.Lines.Length ?? 0))
            {
                // A system notice, rather than an NPC speaking the reward numbers.
                StopTyping();
                if (portraitRoot != null) portraitRoot.SetActive(false);
                if (playerPortrait != null) playerPortrait.enabled = false;
                if (partnerPortrait != null) partnerPortrait.enabled = false;
                if (playerNameText != null) playerNameText.text = "퀘스트 완료";
                if (partnerNameText != null) partnerNameText.text = string.Empty;
                typingRoutine = StartCoroutine(TypeLine(completionRewardText));
                return;
            }
            DialogueLine line = currentSequence.Lines[lineIndex];
            SetActiveSpeaker(line.Speaker);
            StopTyping();

            CharacterClassDefinition activeClass =
                PlayerClassRuntime.ActiveDefinition;
            string text = activeClass != null
                ? line.GetText(activeClass.ClassId)
                : line.Text;
            typingRoutine = StartCoroutine(TypeLine(text ?? string.Empty));
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

        private void ShowAdvanceHint()
        {
            if (!advanceHintShownClasses.Add(CharacterSelectionState.SelectedClass)) return;
            if (advanceHint == null)
            {
                var hintObject = new GameObject("DialogueAdvanceHint", typeof(RectTransform), typeof(TextMeshProUGUI));
                hintObject.layer = dialogueRoot.layer;
                hintObject.transform.SetParent(dialogueRoot.transform, false);
                advanceHint = hintObject.GetComponent<TextMeshProUGUI>();
                advanceHint.font = dialogueText.font;
                advanceHint.fontSize = 34f;
                advanceHint.color = new Color(1f, .96f, .84f);
                advanceHint.fontStyle = FontStyles.Bold;
                advanceHint.outlineColor = new Color(.025f, .015f, .01f, 1f);
                advanceHint.outlineWidth = .3f;
                var hintMaterial = advanceHint.fontMaterial;
                hintMaterial.EnableKeyword("UNDERLAY_ON");
                hintMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, .85f));
                hintMaterial.SetFloat("_UnderlayOffsetX", .7f);
                hintMaterial.SetFloat("_UnderlayOffsetY", -.7f);
                hintMaterial.SetFloat("_UnderlaySoftness", .15f);
                advanceHint.alignment = TextAlignmentOptions.Center;
                advanceHint.raycastTarget = false;
                var rect = advanceHint.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
                rect.pivot = new Vector2(.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, -10f);
                rect.sizeDelta = new Vector2(1000f, 64f);
            }
            advanceHint.text = "Space(스페이스 바)로 다음 대사";
            advanceHintStartedAt = Time.unscaledTime;
            advanceHint.rectTransform.localScale = Vector3.one;
            advanceHint.gameObject.SetActive(true);
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
