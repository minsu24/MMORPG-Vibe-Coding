using System.Collections.Generic;
using UnityEngine;

/// <summary>A fixed danger boundary with an independent windup gauge. Never drives damage.</summary>
public sealed class AttackTelegraph : MonoBehaviour
{
    private static Sprite pixel;
    private readonly List<SpriteRenderer> edges = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> arrows = new List<SpriteRenderer>();
    private SpriteRenderer fill, gauge;
    private Component owner;
    private Vector2 size;
    private float duration, elapsed, direction, opacity;
    private bool centered;
    private int layer, order;

    public static AttackTelegraph Create(string label, Vector2 center, Vector2 size,
        float seconds, float direction, SpriteRenderer source, Component owner,
        bool centered = false, float opacity = 0.18f)
    {
        var go = new GameObject(label);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
        go.transform.position = new Vector3(center.x, center.y, source.transform.position.z);
        var effect = go.AddComponent<AttackTelegraph>();
        effect.owner = owner;
        effect.size = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
        effect.duration = Mathf.Max(0.01f, seconds);
        effect.direction = direction < 0f ? -1f : 1f;
        effect.centered = centered;
        effect.opacity = opacity;
        effect.layer = source.sortingLayerID;
        effect.order = source.sortingOrder + 1;
        effect.Build();
        effect.Draw(0f);
        return effect;
    }

    private SpriteRenderer Bar(string label, Vector2 position, Vector2 dimensions, Color color, int depth)
    {
        if (pixel == null)
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(dimensions.x, dimensions.y, 1f);
        var sprite = go.AddComponent<SpriteRenderer>();
        sprite.sprite = pixel;
        sprite.color = color;
        sprite.sortingLayerID = layer;
        sprite.sortingOrder = order + depth;
        return sprite;
    }

    private void Build()
    {
        fill = Bar("Danger area", Vector2.zero, size, new Color(1f, 0.16f, 0.05f, opacity), 0);
        gauge = Bar("Windup fill", Vector2.zero, size, Color.white, 1);
        float stroke = Mathf.Min(0.055f, size.y * 0.12f);
        // Dark backing keeps the thin warm edge readable on both grass and dungeon floors.
        for (int axis = 0; axis < 2; axis++)
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Vector2 position = axis == 0 ? new Vector2(0, sign * size.y * 0.5f) : new Vector2(sign * size.x * 0.5f, 0);
            Vector2 dimensions = axis == 0 ? new Vector2(size.x + stroke, stroke) : new Vector2(stroke, size.y + stroke);
            Bar("Boundary shadow", position, dimensions + Vector2.one * stroke * 1.8f, new Color(0.15f, 0.035f, 0.02f, 0.85f), 2);
            edges.Add(Bar("Fixed boundary", position, dimensions, Color.white, 3));
        }
        if (centered)
        {
            // Landing marker: static center ticks, so a filling gauge never suggests a moving target.
            Bar("Impact center", Vector2.zero, new Vector2(0.055f, size.y * 0.65f), new Color(1f, 0.8f, 0.4f, 0.9f), 3);
            return;
        }
        int count = Mathf.Clamp(Mathf.FloorToInt(size.x / 1.25f), 1, 12);
        float height = Mathf.Min(size.y * 0.28f, 0.24f);
        for (int i = 0; i < count; i++)
        for (int arm = -1; arm <= 1; arm += 2)
        {
            float x = -size.x * 0.5f + size.x * (i + 0.5f) / count;
            var arrow = Bar("Flow chevron", new Vector2(x, arm * height * 0.35f),
                new Vector2(height, stroke), Color.white, 3);
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, -arm * this.direction * 40f);
            arrows.Add(arrow);
        }
    }

    private void Update()
    {
        if (owner == null || !owner.gameObject.activeInHierarchy) { Destroy(gameObject); return; }
        elapsed += Time.deltaTime;
        // The attack owns removal; reaching 100% never clears the warning ahead of the hit.
        Draw(Mathf.Clamp01(elapsed / duration));
    }

    private void Draw(float progress)
    {
        float width = Mathf.Max(0.001f, size.x * progress);
        gauge.transform.localScale = new Vector3(width, size.y, 1f);
        gauge.transform.localPosition = centered ? Vector3.zero : new Vector3(direction * (width - size.x) * 0.5f, 0, 0);
        gauge.color = new Color(1f, 0.29f, 0.06f, 0.1f + progress * 0.18f);
        float urgency = Mathf.InverseLerp(0.75f, 1f, progress);
        Color edge = Color.Lerp(new Color(1f, 0.38f, 0.12f, 0.95f), new Color(1f, 0.87f, 0.5f, 1f), urgency);
        foreach (var item in edges) item.color = edge;
        for (int i = 0; i < arrows.Count; i++)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(elapsed * 9f - direction * (i / 2) * 1.5f);
            arrows[i].color = new Color(1f, 0.66f, 0.25f, 0.3f + wave * 0.55f);
        }
    }

    public void Clear()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
