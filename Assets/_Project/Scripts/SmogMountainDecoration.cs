using UnityEngine;

/// <summary>Visual-only motion, with no colliders or influence on the player camera.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SmogMountainDecoration : MonoBehaviour
{
    public enum Motion { Grass, Tree, Leaves, Cloud }
    public Motion motion;
    public Sprite[] frames;
    public float phase;
    [Range(0f, 1f)] public float opacity = 1f;
    private SpriteRenderer visual;
    private Vector3 restPosition, restScale;
    private Quaternion restRotation;
    private bool initialized;
    private float elapsed;

    private void OnEnable() => CaptureRestPose();

    private void CaptureRestPose()
    {
        if (initialized) return;
        visual = GetComponent<SpriteRenderer>();
        restPosition = transform.localPosition;
        restScale = transform.localScale;
        restRotation = transform.localRotation;
        initialized = true;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        Sample(elapsed);
    }

    // Also used by the editor to inspect a deterministic animation pose.
    public void Sample(float time)
    {
        CaptureRestPose();
        float wind = Mathf.Sin(time * 1.3f + phase);
        float alpha = opacity;
        switch (motion)
        {
            case Motion.Grass:
                transform.localRotation = restRotation * Quaternion.Euler(0, 0, wind * 4.5f);
                transform.localScale = Vector3.Scale(restScale, new Vector3(1f + wind * 0.018f, 1f, 1f));
                break;
            case Motion.Tree:
                transform.localRotation = restRotation * Quaternion.Euler(0, 0, Mathf.Sin(time * 0.7f + phase) * 1.2f);
                break;
            case Motion.Leaves:
                float travel = Mathf.Repeat(time / 13f + phase / (Mathf.PI * 2), 1f);
                transform.localPosition = restPosition + new Vector3(
                    (travel - 0.5f) * 4.5f + wind * 0.22f, (0.5f - travel) * 5.5f, 0);
                transform.localRotation = restRotation * Quaternion.Euler(0, 0, wind * 16f);
                alpha *= Mathf.SmoothStep(0, 1, Mathf.Min(travel, 1f - travel) * 7f);
                if (frames != null && frames.Length > 0)
                    visual.sprite = frames[Mathf.FloorToInt(time * 2.5f + phase) % frames.Length];
                break;
            case Motion.Cloud:
                transform.localPosition = restPosition + new Vector3(
                    Mathf.Sin(time * 0.16f + phase) * 2.2f, Mathf.Sin(time * 0.19f + phase) * 0.12f, 0);
                alpha *= 0.88f + 0.12f * Mathf.Sin(time * 0.3f + phase);
                break;
        }
        visual.color = new Color(1f, 1f, 1f, alpha);
    }
}
