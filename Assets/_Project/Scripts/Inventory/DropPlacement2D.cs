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

        public static Collider2D FindSupport(Scene scene, Vector3 origin)
        {
            foreach (RaycastHit2D hit in Physics2D.RaycastAll((Vector2)origin + Vector2.up * 0.1f,
                Vector2.down, 200f))
            {
                Collider2D surface = hit.collider;
                if (surface == null || surface.gameObject.scene != scene || surface.isTrigger
                    || hit.normal.y <= 0.25f || surface.GetComponentInParent<Entity>() != null
                    || surface.GetComponentInParent<WorldItemPickup>() != null
                    || surface.GetComponentInParent<WorldCurrencyPickup>() != null)
                    continue;
                if (surface.attachedRigidbody != null
                    && surface.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
                    continue;
                return surface;
            }
            return FindFloor(scene, origin);
        }

        private static PhysicsMaterial2D dropMaterial;

        public static Rigidbody2D LaunchPhysicalDrop(GameObject pickup, SpriteRenderer renderer,
            Vector3 landingPoint, float duration, float height)
        {
            Rigidbody2D body = pickup.GetComponent<Rigidbody2D>();
            if (body == null) body = pickup.AddComponent<Rigidbody2D>();
            BoxCollider2D collisionBody = pickup.GetComponent<BoxCollider2D>();
            if (collisionBody == null) collisionBody = pickup.AddComponent<BoxCollider2D>();
            collisionBody.isTrigger = false;
            collisionBody.enabled = true;
            collisionBody.excludeLayers = LayerMask.GetMask("Player", "Enemy", "EnemyWall");
            if (renderer.sprite != null)
            {
                collisionBody.size = renderer.sprite.bounds.size;
                collisionBody.offset = renderer.sprite.bounds.center;
            }
            if (dropMaterial == null)
                dropMaterial = new PhysicsMaterial2D("Dropped Loot") { friction = 0.8f, bounciness = 0f };
            collisionBody.sharedMaterial = dropMaterial;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.linearDamping = 0.5f;
            body.simulated = true;
            float flightTime = Mathf.Max(0.05f, duration);
            float gravity = Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y));
            body.gravityScale = Mathf.Max(1f, 8f * Mathf.Max(0f, height) / (flightTime * flightTime * gravity));
            body.linearVelocity = new Vector2((landingPoint.x - pickup.transform.position.x) / flightTime,
                Mathf.Sqrt(2f * gravity * body.gravityScale * Mathf.Max(0f, height)));
            return body;
        }

        public static void StopPhysicalDrop(Rigidbody2D body, GameObject pickup)
        {
            if (body != null) body.simulated = false;
            BoxCollider2D collisionBody = pickup.GetComponent<BoxCollider2D>();
            if (collisionBody != null) collisionBody.enabled = false;
        }

        public static bool StopOnLanding(Rigidbody2D body, Collision2D collision)
        {
            if (body == null || !body.simulated) return false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y <= 0.5f) continue;
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return true;
            }
            return false;
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
                if (x < left || x > right)
                    continue;

                RaycastHit2D[] hits = Physics2D.RaycastAll(
                    new Vector2(x, origin.y + 0.1f), Vector2.down, Mathf.Max(1f, origin.y - bounds.min.y + 1f));
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider != floor || hit.normal.y <= 0f || IsOccupied(x, halfWidth + spacing, hit.point.y))
                        continue;

                    landing = new Vector3(x, hit.point.y + 0.02f, origin.z);
                    return true;
                }
            }

            return false;
        }

        private static bool IsOccupied(float x, float halfWidth, float floorY)
        {
            foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None))
            {
                if (pickup.Item != null && Mathf.Abs(pickup.LandingPosition.y - floorY) < pickup.VisualWidth + 0.1f &&
                    Mathf.Abs(pickup.LandingPosition.x - x) < (pickup.VisualWidth * 0.5f + halfWidth))
                    return true;
            }

            foreach (WorldCurrencyPickup pickup in Object.FindObjectsByType<WorldCurrencyPickup>(FindObjectsSortMode.None))
            {
                if (pickup.Amount > 0 && Mathf.Abs(pickup.LandingPosition.y - floorY) < pickup.VisualWidth + 0.1f && Mathf.Abs(pickup.LandingPosition.x - x) <
                    (pickup.VisualWidth * 0.5f + halfWidth))
                    return true;
            }

            return false;
        }
    }
}
