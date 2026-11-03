using UnityEngine;

/// <summary>Visual drops follow the existing recording's sample clock, including random seeks.</summary>
public sealed class SpiderCaveDrips : MonoBehaviour
{
    public IntermittentAmbientAudio ambience;
    public float[] impactTimes;
    public float[] seepPositions = { -10f, -5f, 0f, 4f, 10f, 16f, 19f };
    private const int PoolSize = 6;
    private const float LeadTime = 1.1f;
    private sealed class Drop
    {
        public LineRenderer body, ripple;
        public LineRenderer[] spray;
        public Vector3 start, landing;
        public float impact, fallTime, splashAge;
        public bool active, splashing;
    }
    private Drop[] drops;
    private Material material;
    private Camera view;
    private bool wasPlaying;
    private float previousTime = -1f;
    private int nextImpact, pointSequence;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[48];

    private void Start() => Initialize();

    public void Initialize()
    {
        if (drops != null) return;
        view = Camera.main;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        material = new Material(shader) { name = "Cave water unlit" };
        drops = new Drop[PoolSize];
        for (int i = 0; i < drops.Length; i++)
        {
            var d = new Drop { body = Line("Falling water " + i, 0.065f),
                ripple = Line("Landing ripple " + i, 0.013f), spray = new LineRenderer[5] };
            d.body.widthCurve = new AnimationCurve(new Keyframe(0, 0.02f), new Keyframe(0.65f, 1f), new Keyframe(1, 0.1f));
            d.ripple.loop = true;
            d.ripple.positionCount = 28;
            for (int j = 0; j < d.spray.Length; j++) d.spray[j] = Line("Water spray " + i + "-" + j, 0.026f);
            drops[i] = d;
        }
    }

    private LineRenderer Line(string label, float width)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.positionCount = 2;
        line.numCapVertices = 3;
        line.sortingOrder = 6;
        line.enabled = false;
        return line;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (drops == null) Initialize();
        if (drops == null) return;
        if (view == null) view = Camera.main;
        bool playing = ambience != null && ambience.IsPlayingBurst && ambience.Source != null && ambience.Source.clip != null;
        if (playing)
        {
            var source = ambience.Source;
            float time = source.timeSamples / (float)source.clip.frequency;
            TickRecording(time, !wasPlaying || time < previousTime - 0.05f, Time.deltaTime);
            previousTime = time;
        }
        else
        {
            foreach (var d in drops) if (d.active && !d.splashing) Hide(d);
            TickSplashes(Time.deltaTime);
        }
        wasPlaying = playing;
    }

    public void TickRecording(float time, bool newExcerpt, float deltaTime)
    {
        if (drops == null) Initialize();
        if (drops == null || view == null || impactTimes == null) return;
        if (newExcerpt)
        {
            foreach (var d in drops) Hide(d);
            nextImpact = 0;
            // The old sound fades in: skip a transient already sounding at the seek position.
            while (nextImpact < impactTimes.Length && impactTimes[nextImpact] < time + 0.08f) nextImpact++;
        }
        while (nextImpact < impactTimes.Length && impactTimes[nextImpact] <= time + LeadTime)
        {
            if (impactTimes[nextImpact] >= time) ScheduleDrop(impactTimes[nextImpact]);
            nextImpact++;
        }
        foreach (var d in drops)
        {
            if (!d.active || d.splashing) continue;
            float remaining = d.impact - time;
            if (remaining <= 0f)
            {
                d.body.enabled = false;
                d.splashing = true;
                d.splashAge = Mathf.Min(0.45f, -remaining);
            }
            else if (remaining <= d.fallTime)
            {
                float progress = 1f - remaining / d.fallTime;
                Vector3 position = Vector3.Lerp(d.start, d.landing, progress * progress);
                d.body.enabled = true;
                d.body.SetPosition(0, position + Vector3.up * 0.19f);
                d.body.SetPosition(1, position);
                Tint(d.body, 0.85f);
            }
        }
        TickSplashes(deltaTime);
    }

    private void ScheduleDrop(float impact)
    {
        var drop = System.Array.Find(drops, d => !d.active);
        if (drop == null || seepPositions == null || seepPositions.Length == 0) return;
        float halfWidth = view.orthographicSize * view.aspect;
        float ceiling = view.transform.position.y + view.orthographicSize + 0.25f;
        for (int attempt = 0; attempt < seepPositions.Length; attempt++)
        {
            float x = seepPositions[pointSequence++ % seepPositions.Length];
            if (Mathf.Abs(x - view.transform.position.x) > halfWidth - 0.45f) continue;
            if (!TryFindLanding(x, ceiling, out var point)) continue;
            drop.start = new Vector3(x, ceiling, 0);
            drop.landing = point + Vector3.up * 0.015f;
            drop.fallTime = Mathf.Min(LeadTime, Mathf.Sqrt(2f * (ceiling - point.y) / 30f));
            drop.impact = impact;
            drop.active = true;
            drop.splashing = false;
            return;
        }
    }

    public bool TryFindLanding(float x, float ceiling, out Vector3 point)
    {
        point = default;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.DefaultRaycastLayers);
        filter.useTriggers = false;
        int count = gameObject.scene.GetPhysicsScene2D().Raycast(new Vector2(x, ceiling), Vector2.down, 35f, filter, hits);
        float closest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit.normal.y < 0.65f || hit.collider.GetComponentInParent<Entity>() != null
                || (hit.rigidbody != null && hit.rigidbody.bodyType == RigidbodyType2D.Dynamic)) continue;
            if (hit.distance >= closest) continue;
            closest = hit.distance;
            point = hit.point;
        }
        return !float.IsPositiveInfinity(closest);
    }

    private void TickSplashes(float deltaTime)
    {
        foreach (var d in drops)
        {
            if (!d.active || !d.splashing) continue;
            d.splashAge += deltaTime;
            float t = d.splashAge;
            if (t >= 0.45f) { Hide(d); continue; }
            d.ripple.enabled = true;
            float radius = Mathf.Lerp(0.05f, 0.38f, t / 0.45f);
            for (int i = 0; i < 28; i++)
            {
                float angle = i * Mathf.PI * 2f / 28;
                d.ripple.SetPosition(i, d.landing + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.22f, 0));
            }
            Tint(d.ripple, (1f - t / 0.45f) * 0.65f);
            for (int i = 0; i < d.spray.Length; i++)
            {
                var spray = d.spray[i];
                spray.enabled = t < 0.32f;
                float vx = (i - 2f) * 0.65f;
                float vy = 2.2f - Mathf.Abs(i - 2) * 0.35f;
                var position = d.landing + new Vector3(vx * t, Mathf.Max(0, vy * t - 5f * t * t), 0);
                spray.SetPosition(0, position);
                spray.SetPosition(1, position + new Vector3(vx * 0.025f, 0.035f, 0));
                Tint(spray, (1f - t / 0.45f) * 0.9f);
            }
        }
    }

    private static void Tint(LineRenderer line, float alpha) => line.startColor = line.endColor = new Color(0.69f, 0.86f, 0.97f, alpha);

    private static void Hide(Drop d)
    {
        d.active = d.splashing = false;
        d.body.enabled = d.ripple.enabled = false;
        foreach (var line in d.spray) line.enabled = false;
    }

    private void OnDisable()
    {
        if (drops != null) foreach (var d in drops) Hide(d);
        wasPlaying = false;
        previousTime = -1f;
    }

    private void OnDestroy()
    {
        if (material == null) return;
        if (Application.isPlaying) Destroy(material);
        else DestroyImmediate(material);
    }
}
