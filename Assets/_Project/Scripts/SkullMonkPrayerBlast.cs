using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

public sealed class SkullMonkPrayerBlast : MonoBehaviour
{
    private MON_SkullMonk owner;
    private SpriteRenderer visual;
    private Sprite[] explosion, dissolve;
    private LineRenderer warningRing;
    private Material ringMaterial;
    private float age, warningSeconds, width, height, damage, groundY;
    private LayerMask playerLayers;
    private readonly Collider2D[] contacts = new Collider2D[12];
    private readonly HashSet<PlayerEntity> victims = new HashSet<PlayerEntity>();
    public bool IsFinished { get; private set; }
    public bool IsWarning => age < warningSeconds;
    public float WarningWidth => width;
    public float DamageWidth => width * .82f;
    public float ExplosionHeight => height;

    public static SkullMonkPrayerBlast Create(MON_SkullMonk owner, Vector2 position, float width, float height,
        float warningSeconds, float damage, LayerMask layers, Sprite[] explosion, Sprite[] dissolve, SpriteRenderer source)
    {
        var go = new GameObject("Skull Monk Prayer Eruption");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, owner.gameObject.scene);
        go.transform.position = position;
        var blast = go.AddComponent<SkullMonkPrayerBlast>();
        blast.owner = owner; blast.width = width; blast.height = height; blast.warningSeconds = warningSeconds;
        blast.damage = damage; blast.groundY = position.y; blast.playerLayers = layers;
        blast.explosion = explosion; blast.dissolve = dissolve;
        var artwork = new GameObject("Eruption artwork"); artwork.transform.SetParent(go.transform, false);
        blast.visual = artwork.AddComponent<SpriteRenderer>();
        blast.visual.sharedMaterial = source.sharedMaterial;
        blast.visual.sortingLayerID = source.sortingLayerID; blast.visual.sortingOrder = source.sortingOrder + 3;
        var ring = new GameObject("Ground warning footprint"); ring.transform.SetParent(go.transform, false);
        blast.warningRing = ring.AddComponent<LineRenderer>();
        blast.ringMaterial = new Material(Shader.Find("Sprites/Default"));
        blast.warningRing.sharedMaterial = blast.ringMaterial;
        blast.warningRing.useWorldSpace = false; blast.warningRing.loop = true; blast.warningRing.positionCount = 64;
        blast.warningRing.widthMultiplier = .035f;
        blast.warningRing.sortingLayerID = source.sortingLayerID; blast.warningRing.sortingOrder = source.sortingOrder + 2;
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2 / 64;
            blast.warningRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * width * .5f, .035f + Mathf.Sin(angle) * .11f, 0));
        }
        blast.Draw(); return blast;
    }

    private void FixedUpdate() => Advance(Time.fixedDeltaTime);
    public void Advance(float deltaTime)
    {
        if (IsFinished) return;
        if (owner == null || !owner.isActiveAndEnabled || owner.HP <= 0) { Cancel(); return; }
        age += Mathf.Max(0, deltaTime);
        float attackAge = age - warningSeconds;
        if (attackAge >= .12f && attackAge <= .70f) DamagePlayers(attackAge);
        if (attackAge >= 1.22f) { Cancel(); return; }
        Draw();
    }

    private void Draw()
    {
        float attackAge = age - warningSeconds;
        bool fading = attackAge >= .72f;
        visual.sprite = attackAge < 0 ? explosion[0] : fading
            ? dissolve[Mathf.Clamp(Mathf.FloorToInt((attackAge - .72f) / .1f), 0, dissolve.Length - 1)]
            : explosion[Mathf.Clamp(1 + Mathf.FloorToInt(attackAge / .06f), 1, explosion.Length - 1)];
        float scale = width / 2.5f;
        // Keep the warning low on the floor; only the eruption and its dissolve
        // stretch into sky-high columns. Preserve the same ground pivot.
        float verticalScale = attackAge < 0 ? 1f : height / (fading ? 4.16f : 3.86f);
        visual.transform.localScale = new Vector3(scale * (fading ? .75f : 1f), verticalScale, 1);
        float pulse = .75f + .25f * Mathf.Sin(age * 12);
        visual.color = attackAge < 0 ? new Color(1f, .7f, .7f, pulse) : Color.white;
        warningRing.enabled = attackAge < 0;
        warningRing.startColor = warningRing.endColor = new Color(1f, .18f, .10f, pulse);
    }

    private void DamagePlayers(float attackAge)
    {
        float damageHeight = Mathf.Lerp(.7f, height * .92f, Mathf.Clamp01((attackAge - .06f) / .3f));
        var filter = new ContactFilter2D(); filter.SetLayerMask(playerLayers); filter.useTriggers = false;
        int count = gameObject.scene.GetPhysicsScene2D().OverlapBox(new Vector2(transform.position.x, groundY + damageHeight * .5f),
            new Vector2(DamageWidth, damageHeight), 0, filter, contacts);
        for (int i = 0; i < count; i++)
        {
            var player = contacts[i].GetComponentInParent<PlayerEntity>();
            if (player == null || player.IsDead || !victims.Add(player)) continue;
            uint before = player.HitReactionVersion; player.TakeDamage(damage);
            if (player.HitReactionVersion == before) continue;
            if (!player.IsDead) player.ApplyKnockback(player.transform.position.x >= transform.position.x ? 1 : -1);
        }
    }

    public void Cancel()
    {
        if (IsFinished) return;
        IsFinished = true; gameObject.SetActive(false); Destroy(gameObject);
    }
    private void OnDestroy() { if (ringMaterial != null) Destroy(ringMaterial); }
}
