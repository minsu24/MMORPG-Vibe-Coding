using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Inventory
{
    internal static class DropPlacement2D
    {
        public static Collider2D FindFloor(Scene scene, Vector3 origin)
        {
            Collider2D closest = null;
            float closestDistance = float.PositiveInfinity;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>())
                {
                    string objectName = collider.gameObject.name;
                    bool isGround = objectName == "Floor" || objectName == "Ground";
                    if (!collider.enabled || collider.isTrigger || !isGround)
                        continue;

                    Bounds bounds = collider.bounds;
                    float distance = Mathf.Max(bounds.min.x - origin.x, 0f, origin.x - bounds.max.x);
                    if (distance < closestDistance)
                    {
                        closest = collider;
                        closestDistance = distance;
                    }
                }
            }

            return closest;
        }

        public static bool TryFindLanding(Collider2D floor, Vector3 origin, float desiredX,
            float halfWidth, out Vector3 landing)
        {
            landing = default;
            if (floor == null || !floor.enabled)
                return false;

            Bounds bounds = floor.bounds;
            float left = bounds.min.x + halfWidth;
            float right = bounds.max.x - halfWidth;
            if (left > right)
                return false;

            float center = Mathf.Clamp(desiredX, left, right);
            const float spacing = 0.12f;
            for (int step = 0; step < 24; step++)
            {
                float offset = step == 0 ? 0f : ((step + 1) / 2) * (step % 2 == 1 ? 1f : -1f);
                float x = center + offset * (halfWidth * 2f + spacing);
                if (x < left || x > right || IsOccupied(x, halfWidth + spacing))
                    continue;

                RaycastHit2D[] hits = Physics2D.RaycastAll(
                    new Vector2(x, bounds.max.y + 1f), Vector2.down, bounds.size.y + 2f);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider != floor || hit.normal.y <= 0f)
                        continue;

                    landing = new Vector3(x, hit.point.y + 0.02f, origin.z);
                    return true;
                }
            }

            return false;
        }

        private static bool IsOccupied(float x, float halfWidth)
        {
            foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None))
            {
                if (pickup.Item != null &&
                    Mathf.Abs(pickup.LandingPosition.x - x) < (pickup.VisualWidth * 0.5f + halfWidth))
                    return true;
            }

            foreach (WorldCurrencyPickup pickup in Object.FindObjectsByType<WorldCurrencyPickup>(FindObjectsSortMode.None))
            {
                if (pickup.Amount > 0 && Mathf.Abs(pickup.LandingPosition.x - x) <
                    (pickup.VisualWidth * 0.5f + halfWidth))
                    return true;
            }

            return false;
        }
    }
}
