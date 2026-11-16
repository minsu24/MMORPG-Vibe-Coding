using EasternFantasy.Dialogue;
using UnityEngine;

public sealed partial class MON_SkullMonk
{
    [Header("Meditation encounter intro")]
    [SerializeField] private bool requireIntroDialogue;
    [SerializeField] private DialogueSequence introDialogue;
    [SerializeField] private GameObject introDialogueUiPrefab;
    private enum IntroStage { Meditating, Talking, Standing, Ready }
    private IntroStage introStage;
    private DialogueManager introManager;
    public bool IsCombatReady => !requireIntroDialogue || introStage == IntroStage.Ready;
    public bool IsMeditating => requireIntroDialogue && introStage == IntroStage.Meditating;
    public bool IsEncounterEngaged => !requireIntroDialogue || introStage == IntroStage.Standing || introStage == IntroStage.Ready;
    public bool HasLivingBossTarget => HasLivingPlayer;
    protected override bool ControlsLocomotion => !IsCombatReady;
    protected override void LocomotionFixedUpdate()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);
    }

    private void InitializeIntro()
    {
        if (!requireIntroDialogue || !isActiveAndEnabled) return;
        introStage = IntroStage.Meditating;
        animator.SetBool("isMoving", false);
        animator.Play("SkullMonkMeditate", 0, 0f);
    }

    // The trigger retries while another dialogue/loading sequence owns player input.
    public bool TryBeginIntroDialogue()
    {
        if (!IsMeditating || introDialogue == null || HP <= 0f) return false;
        if (EasternFantasy.Saving.SaveGameService.IsRestoring ||
            EasternFantasy.UI.MapTutorialUI.IsRunning || EasternFantasy.UI.SceneLoadingScreen.IsVisible) return false;
        var manager = DialogueManager.Instance;
        if (manager == null && introDialogueUiPrefab != null)
        {
            Instantiate(introDialogueUiPrefab);
            manager = DialogueManager.Instance;
        }
        if (manager == null || manager.IsDialogueActive) return false;
        UnbindIntroDialogue();
        introManager = manager;
        introManager.DialogueCompleted += IntroDialogueCompleted;
        introManager.DialogueEnded += IntroDialogueEnded;
        introStage = IntroStage.Talking;
        if (manager.StartDialogue(introDialogue)) return true;
        introStage = IntroStage.Meditating;
        UnbindIntroDialogue();
        return false;
    }

    private void IntroDialogueEnded()
    {
        // EndDialogue also runs for cancellation. Completed below advances to standing;
        // cancellation leaves the encounter available when the player approaches again.
        if (introStage == IntroStage.Talking) introStage = IntroStage.Meditating;
    }

    private void IntroDialogueCompleted(DialogueSequence sequence)
    {
        if (sequence != introDialogue || !isActiveAndEnabled || HP <= 0f) return;
        UnbindIntroDialogue();
        introStage = IntroStage.Standing;
        animator.Play("SkullMonkRise", 0, 0f);
    }

    public void SkullMonkAwakeningFinished()
    {
        if (introStage != IntroStage.Standing || HP <= 0f) return;
        introStage = IntroStage.Ready;
        nextAttackAt = Time.time + .65f;
        nextWaveAt = Time.time + 5f;
        nextRingAt = Time.time + 9f;
        nextPrayerAt = Time.time + 13f;
        nextGiantStaffAt = Time.time + 17f;
        animator.Play("SkullMonkIdle", 0, 0f);
    }

    private void UnbindIntroDialogue()
    {
        if (introManager != null)
        {
            introManager.DialogueCompleted -= IntroDialogueCompleted;
            introManager.DialogueEnded -= IntroDialogueEnded;
        }
        introManager = null;
    }
}
