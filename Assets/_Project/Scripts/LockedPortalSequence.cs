using System.Collections;
using System.Collections.Generic;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(PortalManager))]
public sealed class LockedPortalSequence : MonoBehaviour
{
    [SerializeField] private DialogueSequence unlockDialogue;
    [SerializeField] private Animator sealAnimator;
    [SerializeField] private SpriteRenderer sealRenderer;
    [SerializeField] private AnimationClip unlockAnimation;
    [SerializeField] private SpriteRenderer cameraBounds;
    [SerializeField, Min(0.1f)] private float zoomInDuration = 0.9f;
    [SerializeField, Min(0.1f)] private float zoomOutDuration = 0.75f;
    [SerializeField, Min(0.5f)] private float zoomSize = 2.2f;
    [SerializeField, Min(0f)] private float anticipation = 0.25f;

    private PortalManager portal;
    private DialogueManager dialogueManager;
    private Coroutine sequenceRoutine;
    private Camera cinematicCamera;
    private CinemachineBrain brain;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private float savedSize;
    private bool brainWasEnabled, cameraCaptured;
    private readonly List<PlayerInputReader> inputs = new List<PlayerInputReader>();
    private readonly List<bool> inputStates = new List<bool>();
    public bool IsUnlocking { get; private set; }

    public void Configure(DialogueSequence dialogue, Animator animator, SpriteRenderer renderer, AnimationClip clip, SpriteRenderer background)
    { unlockDialogue = dialogue; sealAnimator = animator; sealRenderer = renderer; unlockAnimation = clip; cameraBounds = background; }

    private void Awake()
    {
        portal = GetComponent<PortalManager>();
        portal.SetLocked(true);
        if (sealRenderer != null) sealRenderer.enabled = true;
    }
    private void OnEnable() => BindDialogue();
    private void Start() => BindDialogue();
    private void Update()
    {
        if (dialogueManager != DialogueManager.Instance) BindDialogue();
    }
    private void BindDialogue()
    {
        if (dialogueManager != null) dialogueManager.DialogueCompleted -= OnDialogueCompleted;
        dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null) dialogueManager.DialogueCompleted += OnDialogueCompleted;
    }
    private void OnDialogueCompleted(DialogueSequence completed)
    {
        if (portal == null) portal = GetComponent<PortalManager>();
        if (!isActiveAndEnabled || completed != unlockDialogue || !portal.IsLocked || IsUnlocking) return;
        sequenceRoutine = StartCoroutine(UnlockSequence());
    }
    private IEnumerator UnlockSequence()
    {
        IsUnlocking = true;
        GameTimeController.SetPaused(this, true);
        inputs.Clear();
        inputStates.Clear();
        foreach (var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))
        {
            inputs.Add(input);
            inputStates.Add(input.enabled);
            input.enabled = false;
        }
        cinematicCamera = Camera.main;
        if (cinematicCamera != null)
        {
            cameraCaptured = true;
            savedPosition = cinematicCamera.transform.position;
            savedRotation = cinematicCamera.transform.rotation;
            savedSize = cinematicCamera.orthographicSize;
            brain = cinematicCamera.GetComponent<CinemachineBrain>();
            brainWasEnabled = brain != null && brain.enabled;
            if (brain != null) brain.enabled = false;
            Vector3 focus = sealRenderer != null ? sealRenderer.bounds.center : transform.position;
            focus.z = savedPosition.z;
            float targetSize = Mathf.Min(savedSize, zoomSize);
            if (cameraBounds != null)
            {
                Bounds bounds = cameraBounds.bounds;
                float halfWidth = targetSize * cinematicCamera.aspect;
                focus.x = bounds.size.x > halfWidth * 2f
                    ? Mathf.Clamp(focus.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth) : bounds.center.x;
                focus.y = bounds.size.y > targetSize * 2f
                    ? Mathf.Clamp(focus.y, bounds.min.y + targetSize, bounds.max.y - targetSize) : bounds.center.y;
            }
            yield return MoveCamera(savedPosition, focus, savedSize, targetSize, zoomInDuration);
        }
        yield return new WaitForSecondsRealtime(anticipation);
        if (sealAnimator != null)
        {
            sealAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            sealAnimator.Play("Unlock", 0, 0f);
            sealAnimator.Update(0f);
        }
        yield return new WaitForSecondsRealtime(unlockAnimation != null ? unlockAnimation.length + 0.05f : 1.65f);
        if (sealRenderer != null) sealRenderer.enabled = false;
        yield return new WaitForSecondsRealtime(0.25f);
        if (cinematicCamera != null && cameraCaptured)
            yield return MoveCamera(cinematicCamera.transform.position, savedPosition,
                cinematicCamera.orthographicSize, savedSize, zoomOutDuration);
        portal.SetLocked(false);
        IsUnlocking = false;
        RestoreControl();
        sequenceRoutine = null;
    }
    private IEnumerator MoveCamera(Vector3 from, Vector3 to, float fromSize, float toSize, float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds && cinematicCamera != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
            cinematicCamera.transform.position = Vector3.Lerp(from, to, t);
            cinematicCamera.orthographicSize = Mathf.Lerp(fromSize, toSize, t);
            yield return null;
        }
    }
    private void RestoreControl()
    {
        if (cameraCaptured)
        {
            if (cinematicCamera != null)
            {
                cinematicCamera.transform.SetPositionAndRotation(savedPosition, savedRotation);
                cinematicCamera.orthographicSize = savedSize;
            }
            if (brain != null) brain.enabled = brainWasEnabled;
            cameraCaptured = false;
        }
        for (int i = 0; i < inputs.Count; i++)
            if (inputs[i] != null) inputs[i].enabled = inputStates[i];
        inputs.Clear();
        inputStates.Clear();
        GameTimeController.SetPaused(this, false);
    }
    private void OnDisable()
    {
        if (dialogueManager != null) dialogueManager.DialogueCompleted -= OnDialogueCompleted;
        dialogueManager = null;
        if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);
        sequenceRoutine = null;
        if (IsUnlocking && portal != null && portal.IsLocked)
        {
            if (sealRenderer != null) sealRenderer.enabled = true;
            if (sealAnimator != null && sealAnimator.isActiveAndEnabled) sealAnimator.Play("Locked", 0, 0f);
        }
        IsUnlocking = false;
        RestoreControl();
    }
}

