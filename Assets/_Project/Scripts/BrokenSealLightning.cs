using EasternFantasy.UI;
using UnityEngine;

/// <summary>Occasional distant lightning, owned and removed with the scene's rain.</summary>
public sealed class BrokenSealLightning : MonoBehaviour
{
    [SerializeField, Range(0f, 0.25f)] private float flashOpacity = 0.13f;
    [SerializeField] private Vector2 strikeInterval = new Vector2(9f, 20f);
    [SerializeField, Range(0f, 1f)] private float visibleBoltChance = 0.4f;
    [SerializeField] private AudioClip thunderClip;
    private const float Duration = 0.5f;
    private readonly System.Random random = new System.Random();
    private Camera view;
    private SpriteRenderer flash;
    private Sprite flashSprite;
    private Material material;
    private LineRenderer[] lines;
    private AudioSource thunder;
    private AudioClip generatedThunder;
    private float wait, age = Duration, thunderDelay = -1f;
    private bool showBolt;

    public void Initialize(Camera camera)
    {
        view = camera;
        if (flash != null || view == null) return;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        material = new Material(shader) { name = "Distant Lightning Unlit" };
        var light = new GameObject("Soft sky flash");
        light.transform.SetParent(transform, false);
        flash = light.AddComponent<SpriteRenderer>();
        flashSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        flash.sprite = flashSprite;
        flash.sharedMaterial = material;
        flash.sortingOrder = 9;
        lines = new LineRenderer[4];
        for (int i = 0; i < lines.Length; i++)
        {
            var go = new GameObject(i < 2 ? "Distant bolt " + i : "Distant fork " + i);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.sortingOrder = 1;
            line.widthMultiplier = i % 2 == 0 ? 0.14f : 0.025f;
            line.numCapVertices = 2;
            lines[i] = line;
        }
        thunder = gameObject.AddComponent<AudioSource>();
        thunder.playOnAwake = false;
        thunder.spatialBlend = 0f;
        thunder.volume = 0.2f;
        if (thunderClip == null) thunderClip = generatedThunder = CreateDistantThunder();
        wait = Between(4f, 8f);
        SampleStrike(Duration);
    }

    private void Update()
    {
        if (flash == null || view == null) return;
        if (Time.timeScale <= 0f || SceneLoadingScreen.IsVisible)
        {
            age = Duration;
            thunderDelay = -1f;
            SampleStrike(Duration);
            if (thunder.isPlaying) thunder.Stop();
            return;
        }
        float dt = Time.deltaTime;
        wait -= dt;
        if (wait <= 0f) BeginStrike(random.NextDouble() < visibleBoltChance);
        age += dt;
        SampleStrike(age);
        if (thunderDelay >= 0f)
        {
            thunderDelay -= dt;
            if (thunderDelay <= 0f)
            {
                thunderDelay = -1f;
                thunder.pitch = Between(0.9f, 1.06f);
                thunder.PlayOneShot(thunderClip);
            }
        }
    }

    private float Between(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, (float)random.NextDouble());

    private void BeginStrike(bool visibleBolt)
    {
        showBolt = visibleBolt;
        age = 0f;
        float minimum = Mathf.Max(6f, strikeInterval.x);
        wait = Between(minimum, Mathf.Max(minimum, strikeInterval.y));
        thunderDelay = Between(0.45f, 1.25f);
        float height = view.orthographicSize * 2f;
        float width = height * view.aspect;
        float x = Between(-width * 0.33f, width * 0.33f);
        var points = new Vector3[9];
        for (int i = 0; i < points.Length; i++)
            points[i] = new Vector3(x + Between(-0.35f, 0.35f), Mathf.Lerp(height * 0.56f, -height * 0.03f, i / 8f), 0);
        for (int i = 0; i < 2; i++)
        {
            lines[i].positionCount = points.Length;
            lines[i].SetPositions(points);
        }
        var fork = new Vector3[] { points[3], points[3] + new Vector3(0.45f, -0.28f),
            points[3] + new Vector3(0.3f, -0.58f), points[3] + new Vector3(0.8f, -0.95f) };
        for (int i = 2; i < 4; i++)
        {
            lines[i].positionCount = fork.Length;
            lines[i].SetPositions(fork);
        }
    }

    // Deterministic visual sampling is also used for editor preview/validation, without audio.
    public void SampleStrike(float seconds)
    {
        if (flash == null || view == null) return;
        float height = view.orthographicSize * 2f;
        flash.transform.localScale = new Vector3(height * view.aspect + 0.2f, height + 0.2f, 1f);
        float envelope = seconds < Duration ? Mathf.Exp(-Mathf.Max(0f, seconds) * 28f) : 0f;
        if (seconds >= 0.13f && seconds < Duration)
            envelope = Mathf.Max(envelope, 0.6f * Mathf.Exp(-(seconds - 0.13f) * 17f));
        flash.color = new Color(0.69f, 0.8f, 1f, envelope * flashOpacity);
        flash.enabled = envelope > 0.001f;
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].enabled = showBolt && envelope > 0.04f;
            var tint = i % 2 == 0 ? new Color(0.46f, 0.63f, 1f, envelope * 0.18f)
                : new Color(0.9f, 0.94f, 1f, envelope * 0.92f);
            lines[i].startColor = lines[i].endColor = tint;
        }
    }

    private static AudioClip CreateDistantThunder()
    {
        const int rate = 22050;
        var samples = new float[rate * 3];
        var noise = new System.Random(7613);
        float low = 0f, deep = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            low = Mathf.Lerp(low, (float)noise.NextDouble() * 2f - 1f, 0.075f);
            deep = Mathf.Lerp(deep, low, 0.018f);
            float envelope = Mathf.Min(1f, t / 0.06f) * Mathf.Exp(-t * 1.5f)
                * Mathf.Clamp01((3f - t) * 3f);
            samples[i] = Mathf.Clamp((low * 1.2f + deep * 2.6f) * envelope, -0.85f, 0.85f);
        }
        var clip = AudioClip.Create("Distant thunder rumble (procedural)", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDisable()
    {
        age = Duration;
        thunderDelay = -1f;
        SampleStrike(Duration);
        if (thunder != null) thunder.Stop();
    }

    private void OnDestroy()
    {
        Release(material);
        Release(flashSprite);
        Release(generatedThunder);
    }

    private static void Release(Object asset)
    {
        if (asset == null) return;
        if (Application.isPlaying) Destroy(asset);
        else DestroyImmediate(asset);
    }
}
