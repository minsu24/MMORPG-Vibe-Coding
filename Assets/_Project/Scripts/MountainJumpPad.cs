using EasternFantasy.Player;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MountainJumpPad : MonoBehaviour
{
    [SerializeField] private Transform landingTarget;
    [SerializeField, Min(.5f)] private float apexClearance = 2.2f;
    [SerializeField] private LineRenderer windRing;
    [SerializeField] private MeshRenderer lotusGlow;
    private BoxCollider2D surface;
    private float nextLaunchAt;
    private float effectAge = 1f;
    private Vector3 ringOrigin;
    private float glowTime;
    private MaterialPropertyBlock glowProperties;
    private static readonly int GlowTime = Shader.PropertyToID("_MotionTime");
    private static readonly int LaunchPulse = Shader.PropertyToID("_LaunchPulse");
    public Transform LandingTarget => landingTarget;

    private void Awake()
    {
        surface = GetComponent<BoxCollider2D>();
        if (windRing == null) return;
        ringOrigin = windRing.transform.localPosition;
        windRing.enabled = false;
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryLaunch(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryLaunch(collision);

    private void TryLaunch(Collision2D collision)
    {
        if (landingTarget == null || Time.timeScale <= 0f || Time.time < nextLaunchAt) return;
        var movement = collision.collider.GetComponentInParent<PlayerMovement2D>();
        if (movement == null || movement.body == null || movement.body.linearVelocity.y > .1f) return;
        if (collision.collider.bounds.min.y < surface.bounds.max.y - .18f) return;
        bool onTop = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (Mathf.Abs(contact.normal.y) > .65f && contact.point.y >= surface.bounds.max.y - .06f)
                onTop = true;
        }
        if (!onTop || !movement.LaunchFromPad(landingTarget.position, apexClearance)) return;
        nextLaunchAt = Time.time + .3f;
        effectAge = 0f;
        if (windRing != null) windRing.enabled = true;
    }

    private void Update()
    {
        glowTime += Time.deltaTime;
        float boost = windRing != null && windRing.enabled ? Mathf.Clamp01(1f - effectAge / .55f) : 0f;
        SampleGlow(glowTime, boost);
        if (windRing == null || !windRing.enabled) return;
        effectAge += Time.deltaTime;
        float progress = Mathf.Clamp01(effectAge / .55f);
        windRing.transform.localPosition = ringOrigin + Vector3.up * progress * 1.6f;
        windRing.transform.localScale = Vector3.one * (1f + progress * .7f);
        var color = new Color(.55f, .95f, .9f, (1f - progress) * .8f);
        windRing.startColor = windRing.endColor = color;
        if (progress >= 1f) windRing.enabled = false;
    }

    public void SampleGlow(float time, float boost = 0f)
    {
        if (lotusGlow == null) return;
        if (glowProperties == null) glowProperties = new MaterialPropertyBlock();
        lotusGlow.GetPropertyBlock(glowProperties);
        glowProperties.SetFloat(GlowTime, time);
        glowProperties.SetFloat(LaunchPulse, Mathf.Clamp01(boost));
        lotusGlow.SetPropertyBlock(glowProperties);
    }

    private void OnDrawGizmosSelected()
    {
        if (landingTarget == null) return;
        var collider = GetComponent<BoxCollider2D>();
        Vector3 start = collider.bounds.center;
        start.y = collider.bounds.max.y;
        Vector3 end = landingTarget.position;
        float apex = Mathf.Max(start.y, end.y) + apexClearance;
        float rise = Mathf.Sqrt(2f * (apex - start.y));
        float duration = rise + Mathf.Sqrt(2f * (apex - end.y));
        Vector3 previous = start;
        Gizmos.color = new Color(.4f, 1f, .85f);
        for (int i = 1; i <= 32; i++)
        {
            float time = duration * i / 32f;
            Vector3 point = Vector3.Lerp(start, end, (float)i / 32f);
            point.y = start.y + rise * time - .5f * time * time;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
        Gizmos.DrawWireSphere(end, .2f);
    }
}
