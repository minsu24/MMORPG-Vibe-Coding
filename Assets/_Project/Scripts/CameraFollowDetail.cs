using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera), typeof(CinemachinePositionComposer))]
public sealed class CameraFollowDetail : MonoBehaviour
{
    [Header("Speed Based Follow")]
    [SerializeField, Min(0f)] private float responsiveDamping = 0.15f;
    [SerializeField, Min(0f)] private float fastMovementDamping = 0.85f;
    [SerializeField, Min(0f)] private float fastMovementSpeed = 6f;
    [SerializeField, Min(0f)] private float dampingBlendSpeed = 4f;

    [Header("Vertical Follow")]
    [SerializeField, Min(0f)] private float verticalDamping = 0.3f;

    [Header("Map Boundary")]
    [Tooltip("Short easing applied as the camera settles against the map boundary.")]
    [SerializeField, Min(0f)] private float boundaryDamping = 0.2f;
    [Tooltip("Distance over which the camera eases into its final confined position.")]
    [SerializeField, Min(0f)] private float boundarySlowingDistance = 1.5f;

    private CinemachineCamera virtualCamera;
    private CinemachinePositionComposer positionComposer;
    private CinemachineConfiner2D confiner;
    private Rigidbody2D followedBody;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        positionComposer = GetComponent<CinemachinePositionComposer>();
        confiner = GetComponent<CinemachineConfiner2D>();
        ApplyBoundarySettings();
    }

    private void OnEnable()
    {
        ResolveFollowedBody();
    }

    private void Update()
    {
        if (positionComposer == null)
            return;

        if (followedBody == null)
            ResolveFollowedBody();

        float horizontalSpeed = followedBody != null
            ? Mathf.Abs(followedBody.linearVelocity.x)
            : 0f;
        float speedRatio = fastMovementSpeed > 0f
            ? Mathf.Clamp01(horizontalSpeed / fastMovementSpeed)
            : 1f;
        float targetDamping = Mathf.Lerp(responsiveDamping,
            fastMovementDamping, speedRatio * speedRatio);

        // Once the confiner has stopped the camera, remove the speed lag. This
        // makes reversing away from a map edge feel immediate instead of sticky.
        if (confiner != null && confiner.CameraWasDisplaced(virtualCamera))
            targetDamping = responsiveDamping;

        Vector3 damping = positionComposer.Damping;
        damping.x = Mathf.MoveTowards(damping.x, targetDamping,
            dampingBlendSpeed * Time.deltaTime);
        damping.y = verticalDamping;
        damping.z = 0f;
        positionComposer.Damping = damping;
    }

    private void ResolveFollowedBody()
    {
        Transform follow = virtualCamera != null ? virtualCamera.Follow : null;
        followedBody = follow != null ? follow.GetComponentInParent<Rigidbody2D>() : null;
    }

    private void ApplyBoundarySettings()
    {
        if (confiner == null)
            return;

        confiner.Damping = boundaryDamping;
        confiner.SlowingDistance = boundarySlowingDistance;
    }

    private void OnValidate()
    {
        responsiveDamping = Mathf.Max(0f, responsiveDamping);
        fastMovementDamping = Mathf.Max(responsiveDamping, fastMovementDamping);
        fastMovementSpeed = Mathf.Max(0f, fastMovementSpeed);
        dampingBlendSpeed = Mathf.Max(0f, dampingBlendSpeed);
        verticalDamping = Mathf.Max(0f, verticalDamping);
        boundaryDamping = Mathf.Max(0f, boundaryDamping);
        boundarySlowingDistance = Mathf.Max(0f, boundarySlowingDistance);

        if (Application.isPlaying)
            ApplyBoundarySettings();
    }
}
