using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Inventory
{
    internal static class DropPlacement2D
    {
        private const float LandingGap = 0.12f;
        public const float GatherDuration = .16f;

        public static void GatherAtDropOrigin(Transform pickup, Vector3 origin, Vector3 landingPoint, float elapsed)
        {
            float gather = Mathf.SmoothStep(0f, 1f, elapsed / GatherDuration);
            float side = Mathf.Clamp((landingPoint.x - origin.x) * .085f, -.16f, .16f);
            pickup.position = origin + new Vector3(side * (1f - gather), .10f * gather, 0f);
        }

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
            // Loot must land on the world, never on top of another item or coin.
            foreach (WorldItemPickup other in Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None))
                IgnorePickupCollision(collisionBody, other.gameObject);
            foreach (WorldCurrencyPickup other in Object.FindObjectsByType<WorldCurrencyPickup>(FindObjectsSortMode.None))
                IgnorePickupCollision(collisionBody, other.gameObject);
            body.bodyType = RigidbodyType2D.Dynamic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.linearDamping = 0f;
            body.simulated = true;
            float flightTime = Mathf.Max(0.05f, duration);
            float gravity = Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y));
            body.gravityScale = Mathf.Max(1f, 8f * Mathf.Max(0f, height) / (flightTime * flightTime * gravity));
            float acceleration = gravity * body.gravityScale;
            float verticalSpeed = Mathf.Sqrt(2f * acceleration * Mathf.Max(0f, height));
            float fallDistance = pickup.transform.position.y - landingPoint.y;
            // Include the fall from the enemy's height so horizontal motion reaches the reserved slot.
            float actualFlightTime = Mathf.Max(0.05f, (verticalSpeed + Mathf.Sqrt(
                Mathf.Max(0f, verticalSpeed * verticalSpeed + 2f * acceleration * fallDistance))) / acceleration);
            body.linearVelocity = new Vector2((landingPoint.x - pickup.transform.position.x) / actualFlightTime, verticalSpeed);
            return body;
        }

        private static void IgnorePickupCollision(Collider2D collisionBody, GameObject other)
        {
            if (other == collisionBody.gameObject) return;
            var otherBody = other.GetComponent<BoxCollider2D>();
            if (otherBody != null) Physics2D.IgnoreCollision(collisionBody, otherBody);
        }

        public static void StopPhysicalDrop(Rigidbody2D body, GameObject pickup)
        {
            if (body != null) body.simulated = false;
            BoxCollider2D collisionBody = pickup.GetComponent<BoxCollider2D>();
            if (collisionBody != null) collisionBody.enabled = false;
        }

        public static void SettlePhysicalDrop(Rigidbody2D body, GameObject pickup)
        {
            // Keep the trigger simulated so a floating pickup remains collectible.
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.None;
            }
            var collisionBody = pickup.GetComponent<BoxCollider2D>();
            if (collisionBody != null) collisionBody.enabled = false;
        }

        public static bool TrySettleOnLanding(Rigidbody2D body, SpriteRenderer renderer,
            Collision2D collision, float halfWidth, Vector3 reservedPosition, out Vector3 position)
        {
            position = default;
            if (body == null || !body.simulated) return false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                if (contact.normal.y <= 0.5f) continue;
                position = ResolveLanding(body, renderer, collision.collider, contact.point.y, halfWidth, reservedPosition);
                SettlePhysicalDrop(body, body.gameObject);
                body.position = position;
                body.transform.position = position;
                return true;
            }
            return false;
        }

        internal static Vector3 ResolveLanding(Rigidbody2D body, SpriteRenderer renderer, Collider2D surface,
            float floorY, float halfWidth, Vector3 reservedPosition)
        {
            Vector3 position = body.position;
            position.z = body.transform.position.z;
            Bounds bounds = renderer.bounds;
            Vector3 origin = body.transform.position;
            float centerOffset = bounds.center.x - origin.x;
            if (!IsOccupied(body.gameObject.scene, position.x + centerOffset, halfWidth + LandingGap, floorY, body.gameObject))
                return position;

            // An earlier platform contact can interrupt the toss before the reserved slot.
            // Check the surface we actually landed on before starting the idle float.
            Vector3 searchOrigin = new Vector3(position.x, Mathf.Max(position.y, floorY), position.z);
            if (!TryFindLanding(surface, searchOrigin, reservedPosition.x + centerOffset, halfWidth,
                out Vector3 landing, body.gameObject))
            {
                // Small platforms may have no room. Use the main ground below if it has a free slot.
                var ground = FindFloor(body.gameObject.scene, searchOrigin);
                if (ground == null || ground == surface || ground.bounds.max.y > floorY + .1f ||
                    !TryFindLanding(ground, searchOrigin, position.x + centerOffset, halfWidth, out landing, body.gameObject))
                    return position;
            }
            landing.x -= centerOffset;
            landing.y += origin.y - bounds.min.y;
            return landing;
        }

        public static bool TryFindLanding(Collider2D floor, Vector3 origin, float desiredX,
            float halfWidth, out Vector3 landing, GameObject ignored = null)
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
            float stride = halfWidth * 2f + LandingGap;
            int maximumSteps = Mathf.CeilToInt((right - left) / stride) * 2 + 1;
            for (int step = 0; step < maximumSteps; step++)
            {
                float offset = step == 0 ? 0f : ((step + 1) / 2) * (step % 2 == 1 ? 1f : -1f);
                float x = center + offset * stride;
                if (x < left || x > right)
                    continue;

                RaycastHit2D[] hits = Physics2D.RaycastAll(
                    new Vector2(x, origin.y + 0.1f), Vector2.down, Mathf.Max(1f, origin.y - bounds.min.y + 1f));
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider != floor || hit.normal.y <= 0f ||
                        IsOccupied(floor.gameObject.scene, x, halfWidth + LandingGap, hit.point.y, ignored))
                        continue;

                    landing = new Vector3(x, hit.point.y + 0.02f, origin.z);
                    return true;
                }
            }

            return false;
        }

        private static bool IsOccupied(Scene scene, float x, float halfWidth, float floorY, GameObject ignored)
        {
            foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None))
            {
                if (pickup.gameObject == ignored || pickup.gameObject.scene != scene || pickup.IsCollected)
                    continue;
                if (pickup.Item != null && Mathf.Abs(pickup.LandingPosition.y - floorY) < pickup.VisualWidth + 0.1f &&
                    Mathf.Abs(pickup.LandingCenterX - x) < (pickup.VisualWidth * 0.5f + halfWidth))
                    return true;
            }

            foreach (WorldCurrencyPickup pickup in Object.FindObjectsByType<WorldCurrencyPickup>(FindObjectsSortMode.None))
            {
                if (pickup.gameObject == ignored || pickup.gameObject.scene != scene || pickup.IsCollected)
                    continue;
                if (pickup.Amount > 0 && Mathf.Abs(pickup.LandingPosition.y - floorY) < pickup.VisualWidth + 0.1f && Mathf.Abs(pickup.LandingCenterX - x) <
                    (pickup.VisualWidth * 0.5f + halfWidth))
                    return true;
            }

            return false;
        }
    }
}
