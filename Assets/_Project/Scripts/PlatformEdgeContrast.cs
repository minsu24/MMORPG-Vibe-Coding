using UnityEngine;

// Kept for compatibility with existing scene components; terrain edges are no longer drawn.
public sealed class PlatformEdgeContrast : MonoBehaviour
{
    private void Awake()
    {
        foreach (string childName in new[] { "Edge Highlight", "Edge Shade" })
        {
            Transform edge = transform.Find(childName);
            if (edge != null) edge.gameObject.SetActive(false);
        }
        enabled = false;
    }
}
