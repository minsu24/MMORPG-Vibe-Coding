using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
internal sealed class GroundContactShadow : MonoBehaviour
{
    private const float ProjectionDistance = 5.5f;
    private const float GroundOffset = .035f;
    private const float NpcGroundOffset = .005f;
    private const float NpcGroundTolerance = .45f;
    private const float DepthBehindActor = .015f;
    private static readonly Color ShadowColor = new(.10f, .085f, .10f, .42f);

    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    private ContactFilter2D groundFilter;
    private SpriteRenderer source;
    private SpriteRenderer shadow;
    private Collider2D body;
    private EnemyController enemy;
    private Vector3 lastNpcPosition;
    private bool placedNpcShadow;

    internal static void AttachToNpc(GameObject npc)
    {
        if (npc == null) return;
        var renderer = npc.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer != null) Attach(npc, renderer, null, null);
    }

    internal static void AttachToEnemy(EnemyController enemy, SpriteRenderer renderer, Collider2D body)
    {
        if (renderer != null) Attach(enemy.gameObject, renderer, body, enemy);
    }

    private static void Attach(GameObject owner, SpriteRenderer renderer, Collider2D body, EnemyController enemy)
    {
        if (owner.GetComponent<GroundContactShadow>() != null || owner.transform.Find("Contact Shadow") != null
            || owner.transform.Find("Ground Shadow") != null)
            return;

        var contact = owner.AddComponent<GroundContactShadow>();
        contact.source = renderer;
        contact.body = body;
        contact.enemy = enemy;
        contact.groundFilter.SetLayerMask(~LayerMask.GetMask("Player", "Enemy", "EnemyWall", "UI"));
        contact.groundFilter.useTriggers = false;

        var shadowObject = new GameObject("Ground Contact Shadow");
        shadowObject.transform.SetParent(owner.transform, false);
        contact.shadow = shadowObject.AddComponent<SpriteRenderer>();
        contact.shadow.sprite = PlayerGroundShadow.SharedSprite;
        contact.shadow.enabled = false;
    }

    private void OnDisable()
    {
        if (shadow != null) shadow.enabled = false;
        placedNpcShadow = false;
    }

    private void LateUpdate()
    {
        if (shadow == null) return;
        if (source == null || !source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null
            || (enemy != null && enemy.HP <= 0f))
        {
            shadow.enabled = false;
            return;
        }

        shadow.sortingLayerID = source.sortingLayerID;
        // Keep the shadow above terrain drawn between the actor and the previous sorting order.
        shadow.sortingOrder = source.sortingOrder;
        if (enemy != null) ProjectEnemyShadow();
        else PlaceNpcShadow();
    }

    private void PlaceNpcShadow()
    {
        Vector3 currentPosition = source.transform.position;
        if (!placedNpcShadow || currentPosition != lastNpcPosition)
        {
            Bounds bounds = source.bounds;
            float foot = bounds.min.y;
            // Some full-rect sprites have transparent padding below the NPC's feet.
            if (TryFindNpcGround(bounds.center.x, foot, out float ground)
                || TryFindNpcGround(bounds.center.x, currentPosition.y, out ground))
                foot = ground;
            float width = Mathf.Clamp(bounds.size.x * 1.12f, .60f, 3.8f);
            PositionShadow(bounds.center.x, foot + NpcGroundOffset, width, width * .22f, .48f);
            lastNpcPosition = currentPosition;
            placedNpcShadow = true;
        }
        shadow.enabled = true;
    }

    private bool TryFindNpcGround(float x, float foot, out float ground) =>
        TryFindGround(x, foot + NpcGroundTolerance, NpcGroundTolerance * 2f, out ground);

    private void ProjectEnemyShadow()
    {
        Bounds bounds = body != null && body.enabled ? body.bounds : source.bounds;
        float foot = bounds.min.y;
        float x = bounds.center.x;
        shadow.enabled = TryFindGround(x, foot + .28f, ProjectionDistance, out float ground);
        if (!shadow.enabled) return;

        float height = Mathf.Max(0f, foot - ground);
        float visibility = 1f - Mathf.Clamp01(height / (ProjectionDistance - .28f));
        float width = Mathf.Clamp(bounds.size.x * .95f, .48f, 4.5f)
            * Mathf.Lerp(1.25f, 1f, visibility);
        PositionShadow(x, ground + GroundOffset, width, width * .26f, ShadowColor.a * visibility * visibility);
    }

    private bool TryFindGround(float x, float originY, float distance, out float ground)
    {
        ground = 0f;
        PhysicsScene2D physics = gameObject.scene.GetPhysicsScene2D();
        int count = physics.Raycast(new Vector2(x, originY), Vector2.down, distance, groundFilter, hits);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = hits[i];
            var surface = hit.collider;
            if (surface == null || hit.distance <= .001f || hit.normal.y < .5f
                || surface.gameObject.scene != gameObject.scene
                || surface.transform.IsChildOf(transform) || surface.GetComponentInParent<Entity>() != null
                || (hit.rigidbody != null && hit.rigidbody.bodyType == RigidbodyType2D.Dynamic))
                continue;

            ground = hit.point.y;
            return true;
        }
        return false;
    }

    private void PositionShadow(float x, float y, float width, float depth, float opacity)
    {
        Transform target = shadow.transform;
        Vector3 scale = transform.lossyScale;
        target.SetPositionAndRotation(new Vector3(x, y, source.transform.position.z + DepthBehindActor),
            Quaternion.identity);
        target.localScale = new Vector3(width / Mathf.Max(.001f, Mathf.Abs(scale.x)),
            depth / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1f);
        shadow.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, opacity);
    }
}
