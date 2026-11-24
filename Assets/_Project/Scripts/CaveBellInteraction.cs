using System.Collections;
using System.Collections.Generic;
using EasternFantasy.Player;
using EasternFantasy.Inventory;
using EasternFantasy.Dialogue;
using EasternFantasy.Quest;
using EasternFantasy.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// The player interaction system supplies the same T prompt and key used by NPCs.
[RequireComponent(typeof(SpriteRenderer), typeof(AudioSource))]
[DefaultExecutionOrder(10000)]
public sealed class CaveBellInteraction : MonoBehaviour
{
    private static readonly HashSet<CaveBellInteraction> bells = new();
    private static CaveBellInteraction activeBell;
    private static readonly int[] SwingFrames = { 0, 1, 2, 3, 4, 5, 6, 7, 6, 5, 0 };

    [SerializeField] private Sprite[] frames;
    [SerializeField] private AudioClip bellSound;
    [SerializeField] private QuestDefinition bellQuest;
    [SerializeField] private ItemDefinition questCompletionTitle;
    [SerializeField] private QuestDefinition requiredHunt;
    [SerializeField] private GameObject noviceMonk;
    [SerializeField] private DialogueSequence questCompletionDialogue;
    [SerializeField] private Vector2 backgroundCenter = new(9.83f, -5.93f);
    [SerializeField, Min(1f)] private float overviewSize = 9.4f;
    [SerializeField, Min(0.1f)] private float pullbackSeconds = 2f;
    [SerializeField, Min(0.1f)] private float returnSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float swingSeconds = 1.2f;
    [SerializeField] private Canvas exitHint;

    private SpriteRenderer image;
    private AudioSource sound;
    private Camera view;
    private CinemachineBrain brain;
    private PlayerInputReader playerInput;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float originalSize;
    private bool brainWasEnabled;
    private bool ownsCamera;
    private Coroutine sequence;
    private readonly Dictionary<Canvas, bool> hiddenCanvases = new();
    private bool exitRequested;
    private float originalVolume;
    public bool IsPlaying { get; private set; }
    public static bool IsViewing => activeBell != null && activeBell.IsPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        bells.Clear();
        activeBell = null;
    }

    private void Awake()
    {
        image = GetComponent<SpriteRenderer>();
        GroundContactShadow.AttachToNpc(noviceMonk);
        sound = GetComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.loop = false;
        sound.spatialBlend = 0f;
        ShowFrame(0);
        if (exitHint != null) exitHint.enabled = false;
    }

    private void OnEnable() => bells.Add(this);

    private void Update()
    {
        if (noviceMonk != null)
        {
            var manager = QuestManager.Instance;
            var progress = manager?.FindProgress(bellQuest);
            bool visible = progress != null
                && (progress.Status == QuestStatus.Completed || IsHuntComplete(manager));
            if (noviceMonk.activeSelf != visible) noviceMonk.SetActive(visible);
        }
        if (IsPlaying && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            exitRequested = true;
    }

    private void LateUpdate()
    {
        if (IsPlaying) HideGameUI();
    }

    private void HideGameUI()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas == exitHint) continue;
            if (!hiddenCanvases.ContainsKey(canvas)) hiddenCanvases.Add(canvas, canvas.enabled);
            canvas.enabled = false;
        }
    }

    public static CaveBellInteraction FindNear(Transform player, float radius)
    {
        CaveBellInteraction nearest = null;
        float distance = radius * radius;
        foreach (var bell in bells)
        {
            if (bell == null || !bell.isActiveAndEnabled || bell.IsPlaying ||
                bell.gameObject.scene != SceneManager.GetActiveScene()) continue;
            float candidate = ((Vector2)(bell.transform.position - player.position)).sqrMagnitude;
            if (candidate > distance) continue;
            nearest = bell;
            distance = candidate;
        }
        return nearest;
    }

    public bool TryInteract(PlayerInputReader player)
    {
        if (!CanStartSequence(player)) return false;

        var manager = QuestManager.Instance;
        var progress = manager?.FindProgress(bellQuest);
        if (progress == null || progress.Status == QuestStatus.Completed)
        {
            StartSequence(player);
            return true;
        }
        if (!IsHuntComplete(manager))
        {
            SystemNoticeUI.Show("외진 산길에서 동자승의 삵 사냥 부탁을 먼저 마쳐 주세요.", this);
            return true;
        }

        var dialogue = DialogueManager.Instance;
        if (dialogue == null) return false;
        if (progress.Status == QuestStatus.Active)
            manager.AddProgress(bellQuest, bellQuest.RequiredAmount - progress.CurrentAmount);

        var inventory = player.GetComponent<PlayerInventory>();
        return dialogue.StartQuestCompletion(questCompletionDialogue, bellQuest, questCompletionTitle,
            completeAtRewardStep: true,
            grantTitle: title => inventory != null && inventory.GrantCaveBellTitle(title),
            onCompleted: () =>
            {
                if (this != null && CanStartSequence(player)) StartSequence(player);
            });
    }

    private bool CanStartSequence(PlayerInputReader player) => isActiveAndEnabled && !IsViewing
        && player != null && player.CanControlGameplay && Time.timeScale > 0f
        && bellSound != null && frames != null && frames.Length >= 8 && Camera.main != null;

    private void StartSequence(PlayerInputReader player)
    {
        view = Camera.main;
        brain = view.GetComponent<CinemachineBrain>();
        originalPosition = view.transform.position;
        originalRotation = view.transform.rotation;
        originalSize = view.orthographicSize;
        brainWasEnabled = brain != null && brain.enabled;
        ownsCamera = true;
        if (brain != null) brain.enabled = false;
        playerInput = player;
        IsPlaying = true;
        activeBell = this;
        exitRequested = false;
        originalVolume = sound.volume;
        HideGameUI();
        if (exitHint != null) exitHint.enabled = true;
        playerInput.SetInputBlocked(this, true);
        GameTimeController.SetPaused(this, true);
        sequence = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        Vector3 overviewPosition = new(backgroundCenter.x, backgroundCenter.y, originalPosition.z);
        float size = Mathf.Max(overviewSize, originalSize);
        double started = Time.unscaledTimeAsDouble;
        int strikes = 0;
        while (view != null && !exitRequested)
        {
            float elapsed = (float)(Time.unscaledTimeAsDouble - started);
            if (strikes < 2 && elapsed >= strikes * swingSeconds)
            {
                sound.PlayOneShot(bellSound);
                strikes++;
            }

            // Four swings continue on the unscaled clock, then settle at rest.
            if (elapsed < swingSeconds * 4f)
            {
                float cycle = elapsed % swingSeconds / swingSeconds;
                ShowFrame(SwingFrames[Mathf.Min(SwingFrames.Length - 1, (int)(cycle * SwingFrames.Length))]);
            }
            else ShowFrame(0);

            float blend = Ease(elapsed / pullbackSeconds);
            view.transform.SetPositionAndRotation(Vector3.Lerp(originalPosition, overviewPosition, blend), originalRotation);
            view.orthographicSize = Mathf.Lerp(originalSize, size, blend);
            yield return null;
        }
        // ESC works during the pullback as well as while admiring the full view.
        ShowFrame(0);
        if (exitHint != null) exitHint.enabled = false;
        if (view != null)
        {
            Vector3 returnPosition = view.transform.position;
            float returnSize = view.orthographicSize;
            started = Time.unscaledTimeAsDouble;
            while (view != null)
            {
                float t = Ease((float)(Time.unscaledTimeAsDouble - started) / returnSeconds);
                view.transform.SetPositionAndRotation(Vector3.Lerp(returnPosition, originalPosition, t), originalRotation);
                view.orthographicSize = Mathf.Lerp(returnSize, originalSize, t);
                sound.volume = originalVolume * (1f - t);
                if (t >= 1f) break;
                yield return null;
            }
        }
        Restore();
        sequence = null;
    }

    private bool IsHuntComplete(QuestManager manager) => requiredHunt == null
        || manager.FindProgress(requiredHunt)?.Status == QuestStatus.Completed;

    private static float Ease(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

    private void ShowFrame(int index)
    {
        if (image != null && frames != null && index < frames.Length) image.sprite = frames[index];
    }

    private void Restore()
    {
        if (ownsCamera && view != null)
        {
            view.transform.SetPositionAndRotation(originalPosition, originalRotation);
            view.orthographicSize = originalSize;
            if (brain != null) brain.enabled = brainWasEnabled;
        }
        ownsCamera = false;
        foreach (var entry in hiddenCanvases)
            if (entry.Key != null) entry.Key.enabled = entry.Value;
        hiddenCanvases.Clear();
        if (exitHint != null) exitHint.enabled = false;
        if (sound != null)
        {
            sound.Stop();
            if (IsPlaying) sound.volume = originalVolume;
        }
        if (playerInput != null) playerInput.SetInputBlocked(this, false);
        playerInput = null;
        GameTimeController.SetPaused(this, false);
        IsPlaying = false;
        if (activeBell == this) activeBell = null;
        ShowFrame(0);
    }

    private void OnDisable()
    {
        bells.Remove(this);
        if (sequence != null) StopCoroutine(sequence);
        sequence = null;
        if (sound != null) sound.Stop();
        Restore();
    }
}
