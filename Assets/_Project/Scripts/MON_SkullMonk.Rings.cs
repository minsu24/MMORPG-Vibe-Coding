using System.Collections.Generic;
using UnityEngine;

public sealed partial class MON_SkullMonk
{
    [Header("Four staff rings")]
    [SerializeField] private Sprite[] rotatingRingFrames;
    [SerializeField] private Sprite[] flyingRingFrames;
    [SerializeField, Min(1f)] private float ringCooldown = 14f;
    [SerializeField, Min(.1f)] private float ringLaunchInterval = .55f;
    [SerializeField, Min(0f)] private float ringTrackingSeconds = .4f;
    private enum RingStage { None, Charging, Lifting, Holding, Recovering }
    private RingStage ringStage;
    private float nextRingAt, ringAge, liftStarted, holdingAge, nextLaunchAge, lastRingLaunchAge;
    private bool ringsLifted;
    private readonly SkullMonkStaffRing[] heldRings = new SkullMonkStaffRing[4];
    private readonly List<SkullMonkStaffRing> ringShots = new List<SkullMonkStaffRing>();
    public bool IsRingAttacking => ringStage != RingStage.None;
    public int RingsLaunched { get; private set; }
    // Matches the four baked rings in the final charge pose, in the source art's facing direction.
    public static Vector2 StaffRingStart(int index)
    {
        switch (index) { case 0:return new Vector2(-.30f,2.55f); case 1:return new Vector2(1.50f,2.62f);
            case 2:return new Vector2(-.05f,2.12f); default:return new Vector2(1.38f,2.12f); }
    }
    public static Vector2 StaffRingOverhead(int index) => new Vector2(-1.45f+index, index==0||index==3?3.30f:3.70f);

    private bool CanStartRingPattern()
    {
        return Time.time >= nextRingAt && rotatingRingFrames != null && rotatingRingFrames.Length == 8
            && flyingRingFrames != null && flyingRingFrames.Length == 8 && waveFloor != null
            && Mathf.Abs(rb.linearVelocity.y)<.4f
            && Mathf.Abs(capsuleCollider2D.bounds.min.y-waveFloor.bounds.max.y)<.4f;
    }
    private void StartRingPattern()
    {
        float direction=playerEntity.transform.position.x>=transform.position.x?1f:-1f;
        var scale=transform.localScale;scale.x=-Mathf.Abs(scale.x)*direction;transform.localScale=scale;
        ringStage=RingStage.Charging;ringAge=holdingAge=0;RingsLaunched=0;ringsLifted=false;
        isAttacking=true;emitted=true;endsAt=Time.time+10f;
        rb.linearVelocity=new Vector2(0,rb.linearVelocity.y);animator.SetBool("isMoving",false);
        animator.Play("SkullMonkRingCharge",0,0);
    }
    // Replace the four baked rings exactly when moving to the sheet without those rings.
    public void SkullMonkRingChargeFinished()
    {
        if(ringStage!=RingStage.Charging)return;
        ringShots.RemoveAll(r=>r==null);
        for(int i=0;i<4;i++)
        {
            var ring=SkullMonkStaffRing.Create(this,rotatingRingFrames,flyingRingFrames,i,spriteRenderer);
            heldRings[i]=ring;ringShots.Add(ring);
        }
        ringStage=RingStage.Lifting;
        UpdateStaffRings();animator.Play("SkullMonkRingLift",0,0);
    }
    public void SkullMonkLiftRings()
    {
        if(ringStage!=RingStage.Lifting||ringsLifted)return;
        ringsLifted=true;liftStarted=ringAge;
    }
    public void SkullMonkRingHoldReady()
    {
        if(ringStage!=RingStage.Lifting)return;
        if(!ringsLifted){ringsLifted=true;liftStarted=ringAge-.65f;}
        ringStage=RingStage.Holding;holdingAge=0;nextLaunchAge=.25f;
        animator.Play("SkullMonkRingHold",0,0);
    }
    private void TickRingPattern(float deltaTime)
    {
        ringAge+=Mathf.Max(0,deltaTime);
        if(ringStage!=RingStage.Holding)return;
        holdingAge+=Mathf.Max(0,deltaTime);
        if(RingsLaunched<4 && holdingAge>=nextLaunchAge)
        {
            var ring=heldRings[RingsLaunched];
            if(ring!=null)ring.Launch(playerEntity,Attack_Power,ringTrackingSeconds);
            RingsLaunched++;
            lastRingLaunchAge=holdingAge;nextLaunchAge=holdingAge+ringLaunchInterval;
        }
        if(RingsLaunched==4 && holdingAge>=lastRingLaunchAge+.3f)
        {
            ringStage=RingStage.Recovering;animator.Play("SkullMonkRingRecover",0,0);
        }
    }
    private void UpdateStaffRings()
    {
        if(!IsRingAttacking)return;
        float t=ringsLifted?Mathf.SmoothStep(0,1,(ringAge-liftStarted)/.65f):0;
        for(int i=0;i<4;i++)if(heldRings[i]!=null&&!heldRings[i].HasLaunched)
        {
            var p=Vector2.Lerp(StaffRingStart(i),StaffRingOverhead(i),t);
            p.y+=Mathf.Sin(ringAge*3f+i*1.7f)*.035f*t;
            heldRings[i].SetHover(new Vector2(-p.x,p.y),ringAge);
        }
    }
    public void SkullMonkRingRecovered(){if(ringStage==RingStage.Recovering)FinishAttack();}
    private void EndRingPattern()
    {
        if(!IsRingAttacking)return;
        ringStage=RingStage.None;nextRingAt=Time.time+ringCooldown;
        foreach(var ring in heldRings)if(ring!=null&&!ring.HasLaunched)ring.Cancel();
        System.Array.Clear(heldRings,0,heldRings.Length);
    }
    private void CancelStaffRings()
    {
        foreach(var ring in ringShots)if(ring!=null)ring.Cancel();
        ringShots.Clear();
    }
}
