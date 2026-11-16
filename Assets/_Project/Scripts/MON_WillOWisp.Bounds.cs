using UnityEngine;

public sealed partial class MON_WillOWisp
{
    [Header("Map boundary")]
    [SerializeField] private Collider2D cameraBounds;
    private float nextBoundsSearch;

    private void FindCameraBounds()
    {
        if (cameraBounds != null)
        {
            if (cameraBounds.gameObject.scene == gameObject.scene) return;
            cameraBounds = null;
            nextBoundsSearch = 0f;
        }
        if (Time.unscaledTime < nextBoundsSearch) return;
        nextBoundsSearch = Time.unscaledTime + 1f;
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var collider in root.GetComponentsInChildren<Collider2D>(true))
                if (collider.name == "CameraCollider")
                {
                    cameraBounds = collider;
                    return;
                }
    }

    private Vector2 ClampToCamera(Vector2 position)
    {
        FindCameraBounds();
        if (cameraBounds == null) return position;
        var map = cameraBounds.bounds;
        var flame = TerrainBounds();
        Vector2 centerOffset = (Vector2)flame.center - rb.position;
        Vector2 margin = (Vector2)flame.extents + Vector2.one * .06f;
        float minX = map.min.x + margin.x - centerOffset.x;
        float maxX = map.max.x - margin.x - centerOffset.x;
        float minY = map.min.y + margin.y - centerOffset.y;
        float maxY = map.max.y - margin.y - centerOffset.y;
        position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : map.center.x - centerOffset.x;
        position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : map.center.y - centerOffset.y;
        return position;
    }

    private Vector2 KeepInsideCamera()
    {
        Vector2 before = rb.position;
        Vector2 clamped = ClampToCamera(before);
        Vector2 correction = clamped - before;
        if (correction.sqrMagnitude > .0000001f)
        {
            rb.position = clamped;
            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
            Physics2D.SyncTransforms();
        }
        Vector2 velocity = rb.linearVelocity;
        Vector2 predicted = ClampToCamera(rb.position + velocity * Time.fixedDeltaTime);
        rb.linearVelocity = (predicted - rb.position) / Time.fixedDeltaTime;
        return correction;
    }

    // Animation frames and external impulses may change the visible bounds after FixedUpdate.
    private void LateUpdate()
    {
        if (rb != null && HP > 0f) KeepInsideCamera();
    }
}
