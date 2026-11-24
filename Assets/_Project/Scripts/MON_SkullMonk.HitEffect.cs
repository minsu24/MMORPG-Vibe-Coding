using UnityEngine;

public sealed partial class MON_SkullMonk
{
    [SerializeField] private Material hitEffectMaterial;

    private const float HitEffectDuration = .24f;
    private const float HitFlashDuration = .13f;
    private static readonly int BurstMode = Shader.PropertyToID("_BurstMode");
    private SpriteRenderer hitFlash;
    private SpriteRenderer hitBurst;
    private Sprite hitBurstSprite;
    private float hitEffectStart;

    protected override void OnDamageReceived(float damage)
    {
        base.OnDamageReceived(damage);
        if (hitEffectMaterial == null || spriteRenderer == null || !isActiveAndEnabled) return;
        if (hitBurst == null) CreateHitEffect();

        Bounds body = capsuleCollider2D.bounds;
        Vector3 contact = body.center;
        if (playerEntity != null)
            contact.x += Mathf.Sign(playerEntity.transform.position.x - contact.x) * body.extents.x * .7f;
        contact.y += body.extents.y * .14f;
        contact.z = spriteRenderer.transform.position.z;
        hitBurst.transform.position = contact;
        hitEffectStart = Time.unscaledTime;
        hitBurst.enabled = true;
        UpdateHitEffect();
    }

    private void CreateHitEffect()
    {
        hitFlash = CreateHitRenderer("Hit Silhouette", 1);
        hitBurst = CreateHitRenderer("Hit Sparks", 2);
        var texture = Texture2D.whiteTexture;
        hitBurstSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(.5f, .5f), texture.width, 0, SpriteMeshType.FullRect);
        hitBurst.sprite = hitBurstSprite;
        hitBurst.transform.localScale = Vector3.one * 2.8f;

        var properties = new MaterialPropertyBlock();
        properties.SetFloat(BurstMode, 1f);
        hitBurst.SetPropertyBlock(properties);
    }

    private SpriteRenderer CreateHitRenderer(string objectName, int orderOffset)
    {
        var child = new GameObject(objectName);
        child.layer = spriteRenderer.gameObject.layer;
        child.transform.SetParent(spriteRenderer.transform, false);
        var visual = child.AddComponent<SpriteRenderer>();
        visual.sharedMaterial = hitEffectMaterial;
        visual.sortingLayerID = spriteRenderer.sortingLayerID;
        visual.sortingOrder = spriteRenderer.sortingOrder + orderOffset;
        visual.enabled = false;
        return visual;
    }

    private void UpdateHitEffect()
    {
        if (hitBurst == null || !hitBurst.enabled) return;
        float age = Time.unscaledTime - hitEffectStart;
        if (age >= HitEffectDuration)
        {
            HideHitEffect();
            return;
        }

        hitBurst.color = new Color(1f, 1f, 1f, 1f - age / HitEffectDuration);
        hitFlash.enabled = age < HitFlashDuration && spriteRenderer.enabled;
        if (!hitFlash.enabled) return;
        hitFlash.sprite = spriteRenderer.sprite;
        hitFlash.flipX = spriteRenderer.flipX;
        hitFlash.flipY = spriteRenderer.flipY;
        float fade = 1f - Mathf.InverseLerp(.035f, HitFlashDuration, age);
        hitFlash.color = new Color(1f, 1f, 1f, .68f * fade * spriteRenderer.color.a);
    }

    private void HideHitEffect()
    {
        if (hitBurst != null) hitBurst.enabled = false;
        if (hitFlash != null) hitFlash.enabled = false;
    }

    private void OnDestroy()
    {
        if (hitBurstSprite != null) Destroy(hitBurstSprite);
    }
}
