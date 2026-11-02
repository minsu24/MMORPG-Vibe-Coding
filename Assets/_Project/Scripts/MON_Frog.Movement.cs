using UnityEngine;

public sealed partial class MON_Frog
{
    [Header("Movement between patterns")]
    [SerializeField, Min(.05f)] private float idleStepDistance = .4f;
    [SerializeField, Range(.1f, 1f)] private float idleStepSpeedFactor = .45f;
    [SerializeField, Min(.1f)] private float idleStepPause = .65f;
    private float idleStepWait = .65f;
    private float idleStepDirection, idleStepStartX, idleStepElapsed;
    private readonly Collider2D[] idleStepGround = new Collider2D[8];

    protected override bool ControlsAttackMovement => true;

    protected override void AttackFixedUpdate()
    {
        if (isAttacking || !CanDealContactDamage || !HasLivingPlayer || !CanDetectLivingPlayer())
        {
            ResetIdleStep();
            return;
        }
        if (idleStepDirection == 0f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            idleStepWait -= Time.fixedDeltaTime;
            if (idleStepWait > 0f) return;
            float delta = player.transform.position.x - rb.position.x;
            float toward = delta >= 0 ? 1f : -1f;
            // Approach from far away, keep some space when close, otherwise sidestep.
            idleStepDirection = Mathf.Abs(delta) > jumpRange ? toward
                : Mathf.Abs(delta) < attackRange ? -toward : (Random.value < .5f ? -1f : 1f);
            idleStepStartX = rb.position.x;
            idleStepElapsed = 0f;
        }
        idleStepElapsed += Time.fixedDeltaTime;
        float maximumStepTime = idleStepDistance / Mathf.Max(.1f, Speed * idleStepSpeedFactor) + .1f;
        if (Mathf.Abs(rb.position.x - idleStepStartX) >= idleStepDistance
            || idleStepElapsed >= maximumStepTime || !HasStepSupport())
        {
            ResetIdleStep();
            return;
        }
        rb.linearVelocity = new Vector2(idleStepDirection * Speed * idleStepSpeedFactor, rb.linearVelocity.y);
        var scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * idleStepDirection;
        transform.localScale = scale;
    }

    private bool HasStepSupport()
    {
        Bounds bounds = capsuleCollider2D.bounds;
        var floor = EasternFantasy.Inventory.DropPlacement2D.FindSupport(gameObject.scene, rb.position);
        if (floor == null) return false;
        float nextCenter = bounds.center.x + idleStepDirection * Speed * idleStepSpeedFactor * Time.fixedDeltaTime;
        if (nextCenter - bounds.extents.x < floor.bounds.min.x + .1f
            || nextCenter + bounds.extents.x > floor.bounds.max.x - .1f) return false;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(~(_playerLayer.value | LayerMask.GetMask("Enemy")));
        filter.useTriggers = false;
        var point = new Vector2(bounds.center.x + idleStepDirection * (bounds.extents.x + .1f), bounds.min.y - .04f);
        int count = Physics2D.OverlapBox(point, new Vector2(.16f, .16f), 0f, filter, idleStepGround);
        for (int i = 0; i < count; i++)
            if (idleStepGround[i] != capsuleCollider2D && idleStepGround[i].gameObject.scene == gameObject.scene
                && idleStepGround[i].GetComponentInParent<Entity>() == null) return true;
        return false;
    }

    private void ResetIdleStep()
    {
        idleStepDirection = 0f;
        idleStepWait = idleStepPause;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
}
