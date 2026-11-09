using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GiantRockBreakEffect : MonoBehaviour
{
    private const float FramesPerSecond = 12f;
    private Sprite[] frames;
    private SpriteRenderer visual;
    private float elapsed;

    public static GiantRockBreakEffect Play(Vector3 position, Sprite[] frames, SpriteRenderer source, float width, bool flipX)
    {
        if (frames == null || frames.Length == 0 || frames[0] == null) return null;
        var go = new GameObject("Giant Rock Break");
        SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * (width / frames[0].bounds.size.x);
        var effect = go.AddComponent<GiantRockBreakEffect>();
        effect.frames = frames;
        effect.visual = go.AddComponent<SpriteRenderer>();
        effect.visual.sharedMaterial = source.sharedMaterial;
        effect.visual.sortingLayerID = source.sortingLayerID;
        effect.visual.sortingOrder = source.sortingOrder + 1;
        effect.visual.flipX = flipX;
        effect.visual.sprite = frames[0];
        return effect;
    }

    private void Update() => Advance(Time.deltaTime);

    private void Advance(float deltaTime)
    {
        elapsed += Mathf.Max(0f, deltaTime);
        float animationEnd = frames.Length / FramesPerSecond;
        if (elapsed >= animationEnd + .15f)
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
            return;
        }
        visual.sprite = frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed * FramesPerSecond))];
        visual.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((elapsed - animationEnd) / .15f));
    }
}
