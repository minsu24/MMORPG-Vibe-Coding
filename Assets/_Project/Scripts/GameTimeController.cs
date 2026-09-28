using System.Collections.Generic;
using UnityEngine;

// Owns short combat stops and menu/dialogue pauses so one cannot resume another.
public sealed class GameTimeController : MonoBehaviour
{
    private static GameTimeController instance;
    private readonly HashSet<object> pauseOwners = new HashSet<object>();
    private float normalTimeScale = 1f;
    private float hitStopUntil;

    private static GameTimeController Instance
    {
        get
        {
            if (instance != null)
                return instance;
            GameObject controller = new GameObject("Game Time Controller");
            instance = controller.AddComponent<GameTimeController>();
            DontDestroyOnLoad(controller);
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        normalTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
    }

    public static float RequestHitStop(float seconds)
    {
        if (seconds <= 0f || Time.timeScale <= 0f)
            return 0f;
        GameTimeController controller = Instance;
        if (controller.pauseOwners.Count > 0)
            return 0f;
        controller.hitStopUntil = Mathf.Max(controller.hitStopUntil,
            Time.unscaledTime + seconds);
        controller.ApplyTimeScale();
        return seconds;
    }

    public static void SetPaused(object owner, bool paused)
    {
        if (owner == null)
            return;
        if (!paused && instance == null)
            return;
        GameTimeController controller = Instance;
        if (paused)
            controller.pauseOwners.Add(owner);
        else
            controller.pauseOwners.Remove(owner);
        controller.ApplyTimeScale();
    }

    private void Update()
    {
        if (hitStopUntil > 0f && Time.unscaledTime >= hitStopUntil)
        {
            hitStopUntil = 0f;
            ApplyTimeScale();
        }
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = pauseOwners.Count > 0 || hitStopUntil > Time.unscaledTime
            ? 0f : normalTimeScale;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;
        instance = null;
        Time.timeScale = normalTimeScale;
    }
}
