using UnityEngine;
using UnityEngine.SceneManagement;

// Adds a restrained light/dark edge to walkable floor and platform colliders.
public sealed class PlatformEdgeContrast : MonoBehaviour
{
    private static Material lineMaterial;
    private Collider2D platformCollider;
    private LineRenderer highlight;
    private LineRenderer shade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene();
    }

    private static void ApplyToScene()
    {
        Collider2D[] colliders = FindObjectsByType<Collider2D>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Collider2D collider in colliders)
        {
            string objectName = collider.name;
            bool walkableName = objectName == "Floor" || objectName == "Ground"
                || objectName.StartsWith("Test Terrain")
                || objectName.Contains("발판");
            if (walkableName && !collider.isTrigger
                && collider is BoxCollider2D
                && collider.GetComponent<PlatformEdgeContrast>() == null)
                collider.gameObject.AddComponent<PlatformEdgeContrast>();
        }
    }

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                return;
            lineMaterial = new Material(shader);
        }

        shade = CreateLine("Edge Shade", new Color(0.02f, 0.04f, 0.04f, 0.38f), 0.085f);
        highlight = CreateLine("Edge Highlight", new Color(1f, 0.83f, 0.52f, 0.48f), 0.035f);
    }

    private LineRenderer CreateLine(string objectName, Color color, float width)
    {
        GameObject child = new GameObject(objectName, typeof(LineRenderer));
        child.transform.SetParent(transform, false);
        LineRenderer line = child.GetComponent<LineRenderer>();
        line.sharedMaterial = lineMaterial;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = width;
        line.startColor = line.endColor = color;
        SpriteRenderer surface = GetComponent<SpriteRenderer>();
        line.sortingLayerID = surface != null ? surface.sortingLayerID : 0;
        line.sortingOrder = surface != null ? surface.sortingOrder + 1 : 5;
        return line;
    }

    private void LateUpdate()
    {
        if (platformCollider == null || highlight == null || shade == null)
            return;
        Bounds bounds = platformCollider.bounds;
        if (bounds.size.x <= 0f || bounds.size.y <= 0f)
            return;
        float left = bounds.min.x;
        float right = bounds.max.x;
        float top = bounds.max.y;
        shade.SetPosition(0, new Vector3(left, top - 0.07f, 0f));
        shade.SetPosition(1, new Vector3(right, top - 0.07f, 0f));
        highlight.SetPosition(0, new Vector3(left, top + 0.015f, 0f));
        highlight.SetPosition(1, new Vector3(right, top + 0.015f, 0f));
    }
}
