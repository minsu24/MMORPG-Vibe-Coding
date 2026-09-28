using Unity.Cinemachine;
using UnityEngine;

// Shared combat feedback; the active Cinemachine camera receives a short impulse.
public sealed class CombatImpactFeedback : MonoBehaviour
{
    private static CombatImpactFeedback instance;
    private CinemachineImpulseSource impulseSource;

    public static void Play(Vector3 hitPosition, bool critical)
    {
        if (Time.timeScale <= 0f)
            return;

        GameTimeController.RequestHitStop(critical ? 0.06f : 0.04f);
        if (instance == null)
        {
            GameObject controller = new GameObject("Combat Impact Feedback");
            instance = controller.AddComponent<CombatImpactFeedback>();
            DontDestroyOnLoad(controller);
        }
        instance.Shake(hitPosition, critical);
    }

    private void Awake()
    {
        impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        impulseSource.ImpulseDefinition.ImpulseType =
            CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        impulseSource.ImpulseDefinition.ImpulseShape =
            CinemachineImpulseDefinition.ImpulseShapes.Bump;
        impulseSource.ImpulseDefinition.ImpulseDuration = 0.12f;
        impulseSource.ImpulseDefinition.ImpulseChannel = 1;
    }

    private void Shake(Vector3 hitPosition, bool critical)
    {
        CinemachineCamera camera = FindFirstObjectByType<CinemachineCamera>();
        if (camera == null)
            return;

        CinemachineImpulseListener listener = camera.GetComponent<CinemachineImpulseListener>();
        if (listener == null)
        {
            listener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.ChannelMask = 1;
            listener.Gain = 1f;
            listener.Use2DDistance = true;
        }

        float strength = critical ? 0.14f : 0.07f;
        impulseSource.GenerateImpulseAtPositionWithVelocity(hitPosition,
            new Vector3(Random.Range(-1f, 1f), 1f, 0f).normalized * strength);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
