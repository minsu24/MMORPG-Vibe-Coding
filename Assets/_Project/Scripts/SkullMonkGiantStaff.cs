using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

// One fixed courtyard telegraph. Only the initial impact can damage a player.
public sealed class SkullMonkGiantStaff : MonoBehaviour
{
    public const float FallSeconds = .38f;
    public const float DissolveDelay = .65f;
    public const float StaffFadeEndAge = DissolveDelay + MON_SkullMonk.GiantStaffRegenerateTime;
    public const float CrackStartAge = 0f;
    public const float ImpactLifetime = CrackStartAge + 2.15f;
    // Courtyard artwork is ordered 2–6; player sprites are 10/29 and the boss is 111.
    private const int FallenStaffOrder = 8;
    private const int GroundEffectOrder = FallenStaffOrder - 1;
    private const float StaffMaxHeight = 2.6f;
    private MON_SkullMonk owner;
    private Sprite[] fallFrames, impactFrames;
    private Sprite highResolutionStaff;
    private SpriteRenderer staff, shadow, crack, thrown;
    private Material shadowMaterial;
    private float age, width, warningSeconds, impactHeight, groundVisualOffset;
    private bool released, impacted;
    private Vector2 throwOrigin;
    private LayerMask playerLayers;
    private readonly Collider2D[] contacts = new Collider2D[16];
    private readonly HashSet<PlayerEntity> victims = new HashSet<PlayerEntity>();
    public bool IsFinished { get; private set; }
    public bool HasImpacted => impacted;
    public bool IsDissolving => impacted && ImpactAge >= DissolveDelay;
    public float ImpactAge => age - warningSeconds - FallSeconds;
    public float TimeUntilDrop => released ? Mathf.Max(0f, warningSeconds - age) : warningSeconds;
    public float DamageWidth => width;
    public float WarningSeconds => warningSeconds;

    public static SkullMonkGiantStaff Create(MON_SkullMonk owner, Vector2 ground, float width, float warningSeconds,
        LayerMask layers, Sprite[] fallFrames, Sprite[] impactFrames, SpriteRenderer source, Vector2 throwOrigin,
        Sprite highResolutionStaff = null, float impactHeight = 5.5f, float groundVisualOffset = .4f)
    {
        var go = new GameObject("Skull Monk Giant Staff Judgment");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, owner.gameObject.scene);
        go.transform.position = ground;
        var attack = go.AddComponent<SkullMonkGiantStaff>();
        attack.owner = owner; attack.width = width; attack.warningSeconds = warningSeconds;
        attack.playerLayers = layers; attack.fallFrames = fallFrames; attack.impactFrames = impactFrames;
        attack.throwOrigin = throwOrigin;
        attack.highResolutionStaff = highResolutionStaff;
        attack.impactHeight = impactHeight;
        attack.groundVisualOffset = groundVisualOffset;
        attack.shadow = attack.MakeVisual("Black staff landing shadow", source, 1);
        attack.shadow.sortingOrder = GroundEffectOrder;
        attack.shadowMaterial = new Material(Shader.Find("Sprites/Default"));
        attack.shadow.sharedMaterial = attack.shadowMaterial;
        attack.staff = attack.MakeVisual("Falling giant staff", source, 4);
        attack.staff.sortingOrder = FallenStaffOrder;
        attack.crack = attack.MakeVisual("Ground fracture and dust", source, 3);
        attack.crack.sortingOrder = GroundEffectOrder;
        attack.thrown = attack.MakeVisual("Staff thrown skyward", source, 2);
        attack.Draw();
        return attack;
    }

    private SpriteRenderer MakeVisual(string name, SpriteRenderer source, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false);
        var visual = go.AddComponent<SpriteRenderer>();
        visual.sharedMaterial = source.sharedMaterial;
        visual.sortingLayerID = source.sortingLayerID; visual.sortingOrder = source.sortingOrder + order;
        return visual;
    }

    public void Release() { released = true; }
    private void FixedUpdate() => Advance(Time.fixedDeltaTime);
    public void Advance(float deltaTime)
    {
        if (IsFinished) return;
        if (owner == null || !owner.isActiveAndEnabled || owner.HP <= 0f || !owner.HasLivingBossTarget)
        { Cancel(); return; }
        if (!released) return;
        float previousAge = age;
        age += Mathf.Max(0f, deltaTime);
        if (!impacted && ImpactAge >= 0f)
        {
            impacted = true;
            // Silent impact: preserve the boss's existing no-hit-sound direction.
            CombatImpactFeedback.Play(transform.position, true, null, 0f);
        }
        // Crossing the impact boundary still registers a hit on a slow frame.
        if (impacted && (ImpactAge <= .16f || previousAge < warningSeconds + FallSeconds)) DamagePlayers();
        if (ImpactAge >= ImpactLifetime) { Cancel(); return; }
        Draw();
    }

    private void Draw()
    {
        shadow.enabled = released && age < warningSeconds + FallSeconds;
        shadow.sprite = highResolutionStaff != null ? highResolutionStaff : fallFrames[3];
        shadow.transform.localPosition = new Vector3(0f, groundVisualOffset + .06f, 0f);
        shadow.transform.localScale = new Vector3(width / shadow.sprite.bounds.size.x, 3.2f / shadow.sprite.bounds.size.y, 1f);
        float warningProgress = Mathf.Clamp01(age / warningSeconds);
        shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(.45f, .95f, warningProgress));

        thrown.enabled = released && age < .32f;
        thrown.sprite = highResolutionStaff != null ? highResolutionStaff : fallFrames[0];
        thrown.transform.position = (Vector3)throwOrigin + Vector3.up * (22f * Mathf.Clamp01(age / .32f));
        thrown.transform.localScale = Vector3.one * (highResolutionStaff != null ? 3.8f / highResolutionStaff.bounds.size.x : .68f);
        thrown.transform.localRotation = Quaternion.Euler(0f, 0f, highResolutionStaff != null ? -88f : -44f);

        staff.enabled = released && age >= warningSeconds && ImpactAge < StaffFadeEndAge;
        float drop = Mathf.Clamp01((age - warningSeconds) / FallSeconds);
        int frame = Mathf.Clamp(Mathf.FloorToInt(drop * 4f), 0, 3);
        staff.sprite = highResolutionStaff != null ? highResolutionStaff : fallFrames[frame];
        float size = width / staff.sprite.bounds.size.x;
        // Extend horizontally while keeping the head above the foreground railing.
        float heightScale = Mathf.Min(size, StaffMaxHeight / staff.sprite.bounds.size.y);
        staff.transform.localScale = new Vector3(size, heightScale, 1f);
        // Rotate a full-width native-resolution drawing rather than magnifying four small atlas cells.
        staff.transform.localRotation = Quaternion.Euler(0f, 0f, highResolutionStaff != null ? Mathf.Lerp(-42f, 0f, drop) : 0f);
        // Set the artwork back onto the courtyard; keep the damage box at the playable floor.
        staff.transform.localPosition = new Vector3(0, groundVisualOffset + Mathf.Lerp(24f, .1f, drop * drop * drop), 0f);
        staff.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((ImpactAge - DissolveDelay) / MON_SkullMonk.GiantStaffRegenerateTime));

        float crackAge = ImpactAge - CrackStartAge;
        crack.enabled = impacted && crackAge >= 0f;
        // Keep a visible fracture after the first billow, then clear the arena.
        int impactFrame = Mathf.Clamp(Mathf.FloorToInt(crackAge / .18f), 0, impactFrames.Length - 1);
        crack.sprite = impactFrames[impactFrame];
        crack.transform.localPosition = new Vector3(0f, groundVisualOffset, 0f);
        crack.transform.localScale = new Vector3(width / crack.sprite.bounds.size.x, impactHeight / crack.sprite.bounds.size.y, 1f);
        crack.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((crackAge - 1.65f) / .5f));
    }

    private void DamagePlayers()
    {
        var filter = new ContactFilter2D(); filter.SetLayerMask(playerLayers); filter.useTriggers = false;
        int count = gameObject.scene.GetPhysicsScene2D().OverlapBox((Vector2)transform.position + Vector2.up * 9f,
            new Vector2(width, 18f), 0f, filter, contacts);
        for (int i = 0; i < count; i++)
        {
            var player = contacts[i].GetComponentInParent<PlayerEntity>();
            if (player == null || player.IsDead || !victims.Add(player)) continue;
            uint before = player.HitReactionVersion;
            player.TakeMaxHealthDamage(.8f);
            if (!player.IsDead && player.HitReactionVersion != before)
                player.ApplyKnockback(player.transform.position.x >= transform.position.x ? 1f : -1f);
        }
    }

    public void Cancel()
    {
        if (IsFinished) return;
        IsFinished = true; gameObject.SetActive(false); Destroy(gameObject);
    }
    private void OnDestroy() { if (shadowMaterial != null) Destroy(shadowMaterial); }
}
