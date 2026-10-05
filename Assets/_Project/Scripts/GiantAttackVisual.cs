using UnityEngine;

public sealed class GiantAttackVisual : MonoBehaviour
{
    private SpriteRenderer visual;
    private Sprite[] frames;
    private float age, delay;
    private bool isWarning;
    private Vector2 groundPosition;
    private float dustHeight = 4f;
    private static Sprite whiteSprite;

    private static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
                whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            return whiteSprite;
        }
    }

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

    public static GiantAttackVisual CreateWarning(Vector2 center, Vector2 size, float direction, SpriteRenderer source)
    {
        GiantAttackVisual effect = Create("Giant Sprint Warning", center, source);
        effect.isWarning = true;
        effect.visual.sprite = WhiteSprite;
        effect.visual.color = new Color(1f, 0.05f, 0.02f, 0.22f);
        effect.transform.localScale = new Vector3(size.x, size.y, 1f);
        // Repeated chevrons make the committed direction readable across the lane.
        for (float x = -size.x * 0.5f + 1f; x < size.x * 0.5f; x += 3f)
        {
            for (int arm = -1; arm <= 1; arm += 2)
            {
                GameObject line = new GameObject("Direction Chevron");
                line.transform.SetParent(effect.transform, false);
                line.transform.localPosition = new Vector3(x / size.x, arm * 0.35f / size.y, 0f);
                // Cancel the lane scale before rotating the arrow bars.
                line.transform.localScale = new Vector3(1f / size.x, 1f / size.y, 1f);
                GameObject bar = new GameObject("Arrow Bar");
                bar.transform.SetParent(line.transform, false);
                bar.transform.localRotation = Quaternion.Euler(0f, 0f, -arm * direction * 40f);
                bar.transform.localScale = new Vector3(1.1f, 0.12f, 1f);
                SpriteRenderer renderer = bar.AddComponent<SpriteRenderer>();
                renderer.sprite = WhiteSprite;
                renderer.color = new Color(1f, 0.15f, 0.07f, 0.7f);
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder + 2;
            }
        }
        return effect;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (isWarning)
        {
            visual.color = new Color(1f, 0.05f, 0.02f, 0.2f + 0.1f * Mathf.Sin(age * 12f));
            return;
        }
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
