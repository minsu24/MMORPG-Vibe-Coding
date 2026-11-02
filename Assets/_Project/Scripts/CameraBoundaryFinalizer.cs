using Unity.Cinemachine;
using UnityEngine;

// The normal confiner runs at Body, before impact shake. Enforce the same polygon
// again after Noise so the final rendered viewport cannot escape at map edges.
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera), typeof(CinemachineConfiner2D))]
public sealed class CameraBoundaryFinalizer : CinemachineExtension
{
    private CinemachineConfiner2D confiner;
    public bool WasConstrained { get; private set; }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize) return;
        if (confiner == null) confiner = GetComponent<CinemachineConfiner2D>();
        WasConstrained = false;
        if (confiner == null || !confiner.isActiveAndEnabled || confiner.BoundingShape2D == null) return;
        WasConstrained = confiner.CameraWasDisplaced(vcam);
        // Reuse Cinemachine's baked polygon/lens solution, with no boundary easing.
        confiner.InvokePostPipelineStageCallback(vcam, CinemachineCore.Stage.Body, ref state, -1f);
        WasConstrained |= confiner.CameraWasDisplaced(vcam);
    }
}
