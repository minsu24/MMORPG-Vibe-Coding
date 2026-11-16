using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

// One expanding ripple is centered on the planted staff. Its two ground intersections share one hit set.
public sealed class SkullMonkGroundWave : MonoBehaviour
{
    private MON_SkullMonk owner;
    private float minX, maxX, speed, height, floorY, leftX, rightX, age, originX;
    private bool leftDone, rightDone;
    private LayerMask playerLayers;
    private readonly Collider2D[] contacts = new Collider2D[16];
    private readonly HashSet<PlayerEntity> hit = new HashSet<PlayerEntity>();
    private readonly Vector3[] arc = new Vector3[97];
    private LineRenderer[] frontLines, backLines;
    private Material material;
    public float LeftFront => leftX;
    public float RightFront => rightX;
    public float Radius => speed * age;
    public Vector2 RippleCenter => new Vector2(originX, floorY);

    public static SkullMonkGroundWave Create(MON_SkullMonk owner, Vector2 origin, float minX, float maxX,
        float speed, float height, LayerMask playerLayers, SpriteRenderer source)
    {
        var go = new GameObject("Skull Monk Ground Wave");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
        var wave = go.AddComponent<SkullMonkGroundWave>();
        wave.owner = owner; wave.minX = minX; wave.maxX = maxX; wave.speed = speed;
        wave.height = height; wave.floorY = origin.y;
        wave.leftX = wave.rightX = Mathf.Clamp(origin.x, minX, maxX);
        wave.originX = wave.leftX;
        go.transform.position = new Vector3(wave.originX, origin.y + .035f, source.transform.position.z);
        wave.playerLayers = playerLayers;
        wave.material = new Material(Shader.Find("Sprites/Default"));
        wave.backLines = wave.BuildRipple(source, false);
        wave.frontLines = wave.BuildRipple(source, true);
        wave.Draw();
        return wave;
    }
    private LineRenderer[] BuildRipple(SpriteRenderer source, bool front)
    {
        var lines = new LineRenderer[3];
        for (int i = 0; i < lines.Length; i++)
        {
            string label = i == 0 ? "Ripple glow" : i == 1 ? "Expanding circular crest" : "Trailing circular ripple";
            var go = new GameObject(label + (front ? " Front" : " Back"));
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); lines[i] = line;
            line.sharedMaterial = material; line.useWorldSpace = false; line.loop = false;
            line.positionCount = arc.Length;
            line.widthMultiplier = i == 0 ? .18f : i == 1 ? .045f : .035f;
            if (!front) line.widthMultiplier *= .8f;
            line.numCapVertices = 3; line.numCornerVertices = 3;
            line.sortingLayerID = source.sortingLayerID;
            line.sortingOrder = source.sortingOrder + (front ? 2 + i : -4 + i);
        }
        return lines;
    }
    private void FixedUpdate() => Advance(Time.fixedDeltaTime);
    public void Advance(float deltaTime)
    {
        if (owner == null || !owner.isActiveAndEnabled || owner.HP <= 0) { Cancel(); return; }
        if (deltaTime <= 0) return;
        age += deltaTime;
        if (!leftDone)
        {
            float next = Mathf.Max(minX, leftX - speed * deltaTime);
            Sweep(leftX, next); leftX = next; leftDone = leftX <= minX;
        }
        if (!rightDone)
        {
            float next = Mathf.Min(maxX, rightX + speed * deltaTime);
            Sweep(rightX, next); rightX = next; rightDone = rightX >= maxX;
        }
        Draw();
        if (leftDone && rightDone) Cancel();
    }
    private void Sweep(float from, float to)
    {
        var filter = new ContactFilter2D(); filter.SetLayerMask(playerLayers); filter.useTriggers = false;
        var center = new Vector2((from + to) * .5f, floorY + height * .5f);
        int count = gameObject.scene.GetPhysicsScene2D().OverlapBox(center,
            new Vector2(Mathf.Abs(to - from) + 1.1f, height), 0, filter, contacts);
        for (int i = 0; i < count; i++)
        {
            var player = contacts[i].GetComponentInParent<PlayerEntity>();
            if (player == null || player.IsDead || contacts[i].bounds.min.y > floorY + height || !hit.Add(player)) continue;
            uint before = player.HitReactionVersion;
            player.TakeMaxHealthDamage(.4f);
            if (!player.IsDead && player.HitReactionVersion != before)
                player.ApplyKnockback(to >= from ? 1f : -1f);
        }
    }
    private void Draw()
    {
        float appear = Mathf.Clamp01(age / .06f);
        float maxRadius = Mathf.Max(originX - minX, maxX - originX);
        float fade = 1f - Mathf.Clamp01((Radius / Mathf.Max(.1f, maxRadius) - .85f) / .15f);
        float alpha = appear * fade;
        DrawHalf(backLines, false, alpha * .55f);
        DrawHalf(frontLines, true, alpha);
    }
    // Ground-plane depth grows with radius; a shallow projection preserves the side view.
    // This is visual foreground/background distance, not the height of the damage band.
    public static float GroundDepth(float radius) => Mathf.Max(0f, radius) * .14f;
    private void DrawHalf(LineRenderer[] lines, bool front, float alpha)
    {
        lines[0].startColor = lines[0].endColor = new Color(.48f, .72f, .74f, .25f * alpha);
        lines[1].startColor = lines[1].endColor = new Color(.9f, .96f, .93f, .9f * alpha);
        lines[2].startColor = lines[2].endColor = new Color(.65f, .83f, .82f, .4f * alpha);
        for (int layer = 0; layer < lines.Length; layer++)
        {
            float radius = Mathf.Max(0f, Radius - (layer == 2 ? .45f : 0f));
            lines[layer].enabled = radius > .03f;
            // The far half is slightly foreshortened, while the near half opens toward the viewer.
            float depth = GroundDepth(radius) * (front ? 1f : .75f);
            for (int i = 0; i < arc.Length; i++)
            {
                float angle = i * Mathf.PI / (arc.Length - 1) * (front ? -1f : 1f);
                arc[i] = new Vector3(radius * Mathf.Cos(angle), depth * Mathf.Sin(angle), 0);
            }
            lines[layer].SetPositions(arc);
        }
    }
    public void Cancel()
    {
        gameObject.SetActive(false);
        if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
    }
    private void OnDestroy()
    {
        if (material == null) return;
        if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
    }
}
