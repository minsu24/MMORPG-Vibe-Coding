using UnityEngine;

public sealed class GiantAttackVisual : MonoBehaviour
{
    private SpriteRenderer visual;
    private Sprite[] frames;
    private float age, delay;
    private bool isWarning;
    private Vector2 groundPosition;
    private float dustHeight = 4f;
    private static GiantAttackVisual Create(string name, Vector2 position, SpriteRenderer source)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
        GiantAttackVisual effect = go.AddComponent<GiantAttackVisual>();
        effect.visual = go.AddComponent<SpriteRenderer>();
        effect.visual.sortingLayerID = source.sortingLayerID;
        effect.visual.sortingOrder = source.sortingOrder + 1;
        return effect;
    }

    public static void CreateDust(Vector2 position, Sprite[] sprites, SpriteRenderer source, float delay, float height = 4f)
    {
        if (sprites == null || sprites.Length == 0) return;
        GiantAttackVisual effect = Create("Giant Smash Dust", position, source);
        effect.frames = sprites;
        effect.groundPosition = position;
        effect.delay = delay;
        effect.dustHeight = height;
        effect.visual.enabled = false;
        if (delay <= 0f) effect.ShowDustFrame(0);
    }

    public static GiantAttackVisual CreateWarning(Vector2 center, Vector2 size, float direction,
        SpriteRenderer source, float duration = 2.2f)
    {
        GiantAttackVisual effect = Create("Giant Sprint Warning", center, source);
        effect.isWarning = true;
        effect.visual.enabled = false;
        AttackTelegraph telegraph = AttackTelegraph.Create("Charge danger area", center, size,
            duration, direction, source, source);
        telegraph.transform.SetParent(effect.transform, true);
        return effect;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (isWarning) return;
        float elapsed = age - delay;
        if (elapsed < 0f) return;
        int frame = Mathf.FloorToInt(elapsed * 10f);
        if (frames == null || frame >= frames.Length) { Destroy(gameObject); return; }
        ShowDustFrame(frame);
    }

    private void ShowDustFrame(int frame)
    {
        visual.enabled = true;
        Sprite sprite = frames[frame];
        visual.sprite = sprite;
        float scale = dustHeight / frames[0].bounds.size.y;
        transform.localScale = Vector3.one * scale;
        // Keep every irregularly trimmed frame planted on the ground.
        transform.position = new Vector3(groundPosition.x, groundPosition.y - sprite.bounds.min.y * scale, 0f);
        visual.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.15f, (float)frame / frames.Length));
    }
}
