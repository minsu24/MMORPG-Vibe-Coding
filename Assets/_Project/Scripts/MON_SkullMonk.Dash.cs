using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

public sealed partial class MON_SkullMonk
{
    [Header("Distant melee approach")]
    [SerializeField, Min(1f)] private float dashMaxRange = 14f;
    [SerializeField, Min(.5f)] private float dashStopDistance = 2.8f;
    private const float DashWindup = .10f;
    private const float DashTravelTime = .26f;
    private const float DashStrikeTime = .48f;
    private readonly RaycastHit2D[] dashHits = new RaycastHit2D[16];
    private float dashAge, dashDirection, dashDestinationX, dashSpeed;
    private bool dashArrived;
    private bool lastApproachFront;
    private int repeatedApproaches;
    private readonly List<Collider2D> dashIgnoredPlayerColliders = new List<Collider2D>();
    public bool IsDashing { get; private set; }
    public bool IsFrontApproach { get; private set; }
    protected override bool ControlsAttackMovement => IsDashing || IsPrayerAttacking;

    private bool CanStartDash(Vector2 delta)
    {
        return Mathf.Abs(delta.x) <= dashMaxRange && waveFloor != null
            && animator.HasState(0, Animator.StringToHash("SkullMonkDash"))
            && Mathf.Abs(rb.linearVelocity.y) < .4f
            && Mathf.Abs(capsuleCollider2D.bounds.min.y - waveFloor.bounds.max.y) < .4f;
    }

    private void StartMeleeApproach()
    {
        var animation = playerEntity.GetComponentInChildren<PlayerIdleAnimation>();
        float facing = animation != null && Mathf.Abs(animation.FacingDirectionX) > .1f
            ? Mathf.Sign(animation.FacingDirectionX) : 1f;
        bool front = repeatedApproaches >= 2 ? !lastApproachFront : Random.value < .5f;
        float stop = Mathf.Min(dashStopDistance, attackRange - .25f);
        float side = facing * (front ? 1f : -1f);
        float destination = playerEntity.transform.position.x + side * stop;
        if (!IsDashPathClear(destination))
        {
            front = !front; side = -side;
            destination = playerEntity.transform.position.x + side * stop;
            if (!IsDashPathClear(destination))
            {
                nextAttackAt = Time.time + .3f;
                return;
            }
        }
        IsFrontApproach = front;
        repeatedApproaches = repeatedApproaches > 0 && lastApproachFront == front ? repeatedApproaches + 1 : 1;
        lastApproachFront = front;
        float currentGap = transform.position.x - playerEntity.transform.position.x;
        if (currentGap * side > 0 && Mathf.Abs(currentGap) <= attackRange)
        {
            StartMeleeAttack();
            return;
        }
        StartDash(destination);
    }

    private bool IsDashPathClear(float destination)
    {
        float distance = Mathf.Abs(destination - rb.position.x);
        if (distance <= .025f) return true;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        int count = capsuleCollider2D.Cast(Vector2.right * Mathf.Sign(destination - rb.position.x), filter, dashHits, distance + .04f);
        for (int i = 0; i < count; i++)
        {
            var hit = dashHits[i];
            if (hit.collider != null && hit.collider.attachedRigidbody != rb
                && hit.collider.GetComponentInParent<Entity>() == null && Mathf.Abs(hit.normal.x) >= .5f
                && hit.distance < distance + .04f) return false;
        }
        return true;
    }

    private void StartDash(float destination)
    {
        // Snapshot the chosen side: the monk can cross the player but never homes forever.
        dashDestinationX = destination;
        dashDirection = Mathf.Sign(destination - rb.position.x);
        dashSpeed = Mathf.Abs(dashDestinationX - rb.position.x) / DashTravelTime;
        dashAge = 0f; dashArrived = false; IsDashing = true;
        isAttacking = true; emitted = true; stepPlanted = false;
        attackDirection = dashDirection;
        var scale = transform.localScale; scale.x = -Mathf.Abs(scale.x) * dashDirection;
        transform.localScale = scale;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);
        animator.Play("SkullMonkDash", 0, 0f);
        endsAt = Time.time + 1.2f;
        foreach (var collider in playerEntity.GetComponentsInChildren<Collider2D>())
        {
            if (collider.isTrigger || Physics2D.GetIgnoreCollision(capsuleCollider2D, collider)) continue;
            Physics2D.IgnoreCollision(capsuleCollider2D, collider, true);
            dashIgnoredPlayerColliders.Add(collider);
        }
    }

    private void TickDash(float deltaTime)
    {
        dashAge += deltaTime;
        if (dashArrived && dashAge >= DashStrikeTime)
        {
            EndDash();
            StartMeleeAttack();
        }
    }

    protected override void AttackFixedUpdate()
    {
        if (IsPrayerAttacking) { PrayerFixedUpdate(); return; }
        if (!IsDashing) return;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (dashArrived || dashAge < DashWindup) return;
        float remaining = (dashDestinationX - rb.position.x) * dashDirection;
        if (remaining <= .025f)
        {
            dashArrived = true;
            return;
        }
        float step = Mathf.Min(dashSpeed * Time.fixedDeltaTime, remaining);
        var filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        int count = capsuleCollider2D.Cast(Vector2.right * dashDirection, filter, dashHits, step + .04f);
        bool blocked = false;
        for (int i = 0; i < count; i++)
        {
            var hit = dashHits[i];
            if (hit.collider == null || hit.collider.attachedRigidbody == rb
                || hit.collider.GetComponentInParent<Entity>() != null
                || Mathf.Abs(hit.normal.x) < .5f) continue;
            float allowed = Mathf.Max(0, hit.distance - .04f);
            if (allowed < step) { step = allowed; blocked = true; }
        }
        if (blocked)
        {
            // A wall cancels an out-of-range strike instead of attacking through it.
            if (Mathf.Abs(playerEntity.transform.position.x - rb.position.x) > attackRange)
            {
                FinishAttack();
                return;
            }
            dashArrived = true;
        }
        rb.linearVelocity = new Vector2(dashDirection * step / Time.fixedDeltaTime, rb.linearVelocity.y);
    }

    private void EndDash()
    {
        foreach (var collider in dashIgnoredPlayerColliders)
            if (capsuleCollider2D != null && collider != null)
                Physics2D.IgnoreCollision(capsuleCollider2D, collider, false);
        dashIgnoredPlayerColliders.Clear();
        if (!IsDashing) return;
        IsDashing = false; dashArrived = false;
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
}
