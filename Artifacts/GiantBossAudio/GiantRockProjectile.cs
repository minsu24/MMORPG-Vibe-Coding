using UnityEngine;
using EasternFantasy.Player;

public sealed class GiantRockProjectile : MonoBehaviour
{
    private Vector2 velocity;
    private float damage, age;
    private Sprite[] frames;
    private SpriteRenderer visual;
    private LayerMask playerMask;
    private GameObject owner;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private const float VisualWidth = 5.5f;
    private const float radius = 1.21f;

    public static GiantRockProjectile Create(Vector2 position, Vector2 velocity, float damage,
        Sprite[] frames, SpriteRenderer source, LayerMask playerMask, GameObject owner)
    {
        GameObject go = new GameObject("Giant Rock Projectile");
        go.transform.position = position;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, owner.scene);
        GiantRockProjectile rock = go.AddComponent<GiantRockProjectile>();
        rock.velocity = velocity;
        rock.damage = damage;
        rock.frames = frames;
        rock.playerMask = playerMask;
        rock.owner = owner;
        rock.visual = go.AddComponent<SpriteRenderer>();
        rock.visual.sortingLayerID = source.sortingLayerID;
        rock.visual.sortingOrder = source.sortingOrder + 2;
        if (frames != null && frames.Length > 0)
        {
            rock.visual.sprite = frames[0];
            go.transform.localScale = Vector3.one * (VisualWidth / frames[0].bounds.size.x);
        }
        // The supplied sheet points left; rotate it onto the actual flight direction.
        go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 180f);
        return rock;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (owner == null || age >= 5f) { Destroy(gameObject); return; }
        if (frames != null && frames.Length > 0)
            visual.sprite = frames[Mathf.FloorToInt(age * 12f) % frames.Length];
    }

    private void FixedUpdate()
    {
        Vector2 origin = transform.position;
        Vector2 step = velocity * Time.fixedDeltaTime;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.DefaultRaycastLayers | playerMask.value);
        filter.useTriggers = true;
        int count = Physics2D.CircleCast(origin, radius, step.normalized, filter, hits, step.magnitude);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = hits[i].collider;
            if (collider == null || collider.gameObject == owner || collider.GetComponentInParent<EnemyController>() != null) continue;
            PlayerEntity player = collider.GetComponentInParent<PlayerEntity>();
            if (player != null)
            {
                float before = player.HP;
                player.TakeDamage(damage);
                if (!player.IsDead && player.HP < before) player.ApplyKnockback(Mathf.Sign(velocity.x));
                Destroy(gameObject);
                return;
            }
            if (collider.isTrigger || collider.attachedRigidbody != null) continue;
            Destroy(gameObject);
            return;
        }
        transform.position = origin + step;
    }
}
