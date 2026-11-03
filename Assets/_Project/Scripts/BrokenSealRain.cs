using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Camera-sized weather owned by BrokenSeal, independent of gameplay and saved state.</summary>
public sealed class BrokenSealRain : MonoBehaviour
{
    [SerializeField, Range(0f, 2f)] private float intensity = 1f;
    private Camera view;
    private ParticleSystem distantRain, nearRain, mist;
    private Material material;
    private Mesh streakMesh, mistMesh;
    private Vector2 lastSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetHook() => SceneManager.sceneLoaded -= SceneLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
        SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "BrokenSeal") return;
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<BrokenSealRain>(true) != null) return;
        var go = new GameObject("BrokenSeal Rain Atmosphere");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<BrokenSealRain>();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (material == null) Initialize();
        else
            foreach (var system in GetComponentsInChildren<ParticleSystem>()) system.Play();
    }

    private void Initialize()
    {
        if (material != null) return;
        view = Camera.main;
        if (view == null) return;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        material = new Material(shader) { name = "BrokenSeal Weather Unlit" };
        streakMesh = CreateStreak();
        mistMesh = CreateMist();
        distantRain = CreateLayer("Distant rain", streakMesh, 1, 130f, 1.1f,
            new Color(0.58f, 0.72f, 0.83f, 0.25f), new Vector2(-2.2f, -13f), 0.55f, 0.9f);
        nearRain = CreateLayer("Near rain", streakMesh, 8, 65f, 0.8f,
            new Color(0.76f, 0.86f, 0.94f, 0.36f), new Vector2(-3.4f, -20f), 0.95f, 1.5f);
        mist = CreateLayer("Low drifting mist", mistMesh, 2, 1.4f, 7f,
            new Color(0.45f, 0.6f, 0.68f, 0.075f), new Vector2(0.3f, 0.025f), 3.5f, 6f);
        FollowCamera();
        var lightning = GetComponent<BrokenSealLightning>();
        if (lightning == null) lightning = gameObject.AddComponent<BrokenSealLightning>();
        lightning.Initialize(view);
        foreach (var system in new[] { distantRain, nearRain, mist })
        {
            system.Simulate(system == mist ? 7f : 1.1f, true, true, false);
            system.Play();
        }
    }

    private ParticleSystem CreateLayer(string label, Mesh mesh, int order, float rate,
        float lifetime, Color tint, Vector2 velocity, float minSize, float maxSize)
    {
        var go = new GameObject(label);
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = tint;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = order == 2 ? 24 : 320;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = system.emission;
        emission.rateOverTime = rate * intensity;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        var movement = system.velocityOverLifetime;
        movement.enabled = true;
        movement.space = ParticleSystemSimulationSpace.World;
        movement.x = velocity.x;
        movement.y = velocity.y;
        movement.z = 0f;
        var fade = system.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        return system;
    }

    private void LateUpdate()
    {
        if (material == null) Initialize();
        if (view == null) view = Camera.main;
        if (view != null && material != null) FollowCamera();
    }

    private void FollowCamera()
    {
        float height = view.orthographicSize * 2f;
        Vector2 size = new Vector2(height * view.aspect, height);
        Vector3 position = view.transform.position;
        // World-space particles keep their trajectory while the emitter tracks camera movement.
        transform.position = new Vector3(position.x, position.y, 0f);
        if (size == lastSize) return;
        lastSize = size;
        foreach (var system in new[] { distantRain, nearRain, mist })
        {
            var shape = system.shape;
            shape.scale = system == mist ? new Vector3(size.x + 8f, height * 0.3f, 0.01f)
                : new Vector3(size.x + 8f, height + 10f, 0.01f);
            shape.position = system == mist ? new Vector3(0f, -height * 0.3f, 0f) : new Vector3(0f, 3f, 0f);
        }
    }

    private static Mesh CreateStreak()
    {
        var mesh = new Mesh { name = "Fine slanted rain streak" };
        mesh.vertices = new[] { new Vector3(-0.055f, -0.28f), new Vector3(-0.037f, -0.28f),
            new Vector3(0.055f, 0.28f), new Vector3(0.037f, 0.28f) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.colors32 = new Color32[] { new Color32(255,255,255,70), new Color32(255,255,255,70),
            new Color32(255,255,255,255), new Color32(255,255,255,255) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateMist()
    {
        const int sides = 24;
        var vertices = new Vector3[sides + 1];
        var colors = new Color32[sides + 1];
        var triangles = new int[sides * 3];
        colors[0] = Color.white;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.23f);
            colors[i + 1] = new Color32(255, 255, 255, 0);
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % sides + 1;
        }
        var mesh = new Mesh { name = "Feathered ground mist", vertices = vertices, colors32 = colors, triangles = triangles };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDisable()
    {
        foreach (var system in GetComponentsInChildren<ParticleSystem>())
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDestroy()
    {
        Release(material);
        Release(streakMesh);
        Release(mistMesh);
    }

    private static void Release(Object asset)
    {
        if (asset == null) return;
        if (Application.isPlaying) Destroy(asset);
        else DestroyImmediate(asset);
    }
}
