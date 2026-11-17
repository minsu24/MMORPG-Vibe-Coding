using EasternFantasy.Player;
using UnityEngine;

// The same ring instance spins at the staff, rises overhead, then becomes a bounded-homing projectile.
public sealed class SkullMonkStaffRing : MonoBehaviour
{
    private MON_SkullMonk owner;
    private PlayerEntity target;
    private SpriteRenderer visual;
    private Sprite[] spinFrames, flightFrames;
    private float elapsed, trackingDuration, maxHealthDamageFraction, impactAge;
    private int slot;
    private bool launched, locked, impacted;
    private Vector2 travelDirection;
    private RaycastHit2D[] castHits=new RaycastHit2D[16];
    public bool HasLaunched=>launched;
    public bool TrackingLocked=>locked;
    public bool HasImpacted=>impacted;
    public Vector2 TravelDirection=>travelDirection;

    public static SkullMonkStaffRing Create(MON_SkullMonk owner,Sprite[] spin,Sprite[] flight,int slot,SpriteRenderer source)
    {
        var go=new GameObject("Skull Monk Staff Ring "+(slot+1));go.transform.SetParent(owner.transform,false);
        var ring=go.AddComponent<SkullMonkStaffRing>();ring.owner=owner;ring.spinFrames=spin;ring.flightFrames=flight;ring.slot=slot;
        ring.visual=go.AddComponent<SpriteRenderer>();ring.visual.sharedMaterial=source.sharedMaterial;
        ring.visual.sortingLayerID=source.sortingLayerID;ring.visual.sortingOrder=source.sortingOrder+2;
        ring.visual.sprite=spin[0];return ring;
    }
    public void SetHover(Vector2 localPosition,float phase)
    {
        if(launched)return;
        transform.localPosition=localPosition;
        // Forward and backward playback makes a full rotation without an abrupt width reset.
        int step=(Mathf.FloorToInt(phase*14f)+slot*3)%14;
        int frame=step<8?step:14-step;
        visual.sprite=spinFrames[frame];
    }
    public void Launch(PlayerEntity player,float maxHealthDamageFraction,float homingSeconds)
    {
        if(launched)return;
        launched=true;target=player;this.maxHealthDamageFraction=Mathf.Clamp01(maxHealthDamageFraction);trackingDuration=Mathf.Max(0,homingSeconds);
        elapsed=impactAge=0;locked=trackingDuration<=0;
        transform.SetParent(null,true);
        var scale=transform.localScale;transform.localScale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        Vector2 delta=TargetPoint()-(Vector2)transform.position;
        travelDirection=delta.sqrMagnitude>.0001f?delta.normalized:Vector2.down;
        DrawFlight();
    }
    private Vector2 TargetPoint()
    {
        var body=target!=null?target.GetComponent<Collider2D>():null;
        return body!=null?(Vector2)body.bounds.center:target!=null?(Vector2)target.transform.position:(Vector2)transform.position+travelDirection;
    }
    private void FixedUpdate(){if(launched)Advance(Time.fixedDeltaTime);}
    public void Advance(float deltaTime)
    {
        if(owner==null||!owner.isActiveAndEnabled||owner.HP<=0){Cancel();return;}
        if(!launched||deltaTime<=0)return;
        if(impacted)
        {
            impactAge+=deltaTime;
            if(impactAge>=.16f){Cancel();return;}
            visual.sprite=flightFrames[impactAge<.075f?6:7];return;
        }
        float remaining=deltaTime;
        while(remaining>.000001f&&!impacted)
        {
            float step=remaining;
            if(!locked)
            {
                if(target==null||target.IsDead)locked=true;
                else
                {
                    step=Mathf.Min(step,trackingDuration-elapsed);
                    var delta=TargetPoint()-(Vector2)transform.position;
                    if(delta.sqrMagnitude>.0001f)
                    {
                        var turned=Vector3.RotateTowards(travelDirection,delta.normalized,210f*Mathf.Deg2Rad*step,0);
                        travelDirection=((Vector2)turned).normalized;
                    }
                }
            }
            if(step<=.000001f){locked=true;continue;}
            float speed=Mathf.Lerp(9.75f,15.6f,Mathf.Clamp01((elapsed+step)/.4f));
            Move(step*speed);
            elapsed+=step;remaining-=step;
            if(elapsed>=trackingDuration-.000001f)locked=true;
            if(elapsed>=6f){Cancel();return;}
        }
        if(!impacted)DrawFlight();
    }
    private void Move(float distance)
    {
        var filter=new ContactFilter2D();filter.SetLayerMask(~LayerMask.GetMask("Enemy","Ignore Raycast"));filter.useTriggers=false;
        int count;
        do
        {
            count=gameObject.scene.GetPhysicsScene2D().CircleCast(transform.position,.22f,travelDirection,distance,filter,castHits);
            if(count<castHits.Length)break;
            System.Array.Resize(ref castHits,castHits.Length*2);
        }while(true);
        RaycastHit2D nearest=default;float nearestDistance=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            var c=castHits[i].collider;
            if(c==null||c.GetComponentInParent<EnemyController>()!=null||castHits[i].distance>=nearestDistance)continue;
            nearest=castHits[i];nearestDistance=castHits[i].distance;
        }
        if(nearest.collider!=null)
        {
            transform.position+=(Vector3)(travelDirection*nearestDistance);
            impacted=true;impactAge=0;visual.sprite=flightFrames[6];transform.rotation=Quaternion.identity;
            var player=nearest.collider.GetComponentInParent<PlayerEntity>();
            if(player!=null&&!player.IsDead)
            {
                uint previousHit=player.HitReactionVersion;
                player.TakeMaxHealthDamage(maxHealthDamageFraction);
                if(!player.IsDead&&player.HitReactionVersion!=previousHit)
                    player.ApplyKnockback(travelDirection.x>=0f?1f:-1f);
            }
            return;
        }
        transform.position+=(Vector3)(travelDirection*distance);
    }
    private void DrawFlight()
    {
        int step=Mathf.FloorToInt(elapsed*16f)%10;
        visual.sprite=flightFrames[step<6?step:10-step];
        transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(travelDirection.y,travelDirection.x)*Mathf.Rad2Deg-25f);
    }
    public void Cancel()
    {
        gameObject.SetActive(false);
        if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);
    }
}
