using System.Collections;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(PortalManager))]
public sealed class EnemyDefeatPortalUnlock : MonoBehaviour
{
    [SerializeField] private EnemyController requiredEnemy;
    [SerializeField] private Animator sealAnimator;
    [SerializeField] private SpriteRenderer sealRenderer;
    [SerializeField] private AnimationClip unlockAnimation;

    private PortalManager portal;
    private Coroutine unlockRoutine;
    private bool enemyDefeated, unlocked;

    private void Awake() => portal = GetComponent<PortalManager>();

    private void OnEnable()
    {
        if (requiredEnemy != null) requiredEnemy.Defeated += OnEnemyDefeated;
        if (unlocked) ApplyUnlockedState();
        else if (enemyDefeated) BeginUnlock();
        else portal.SetLocked(true);
    }

    private void Start()
    {
        // Start runs after both the portal and enemy have initialized their state.
        if (!enemyDefeated && !unlocked)
        {
            portal.SetLocked(true);
            if (sealRenderer != null) sealRenderer.enabled = true;
            if (requiredEnemy != null && requiredEnemy.HP <= 0f)
                OnEnemyDefeated(requiredEnemy);
        }
    }

    private void OnEnemyDefeated(EnemyController enemy)
    {
        if (enemy != requiredEnemy || enemyDefeated || unlocked) return;
        enemyDefeated = true;
        BeginUnlock();
    }

    private void BeginUnlock()
    {
        if (unlockRoutine == null && isActiveAndEnabled)
            unlockRoutine = StartCoroutine(UnlockSeal());
    }

    private IEnumerator UnlockSeal()
    {
        portal.SetLocked(true);
        if (sealAnimator != null && sealAnimator.isActiveAndEnabled
            && sealAnimator.HasState(0, Animator.StringToHash("Unlock")))
        {
            if (sealRenderer != null) sealRenderer.enabled = true;
            sealAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            sealAnimator.Play("Unlock", 0, 0f);
            sealAnimator.Update(0f);
            yield return new WaitForSecondsRealtime(unlockAnimation != null ? unlockAnimation.length : 1.65f);
        }
        unlocked = true;
        ApplyUnlockedState();
        unlockRoutine = null;
    }

    private void ApplyUnlockedState()
    {
        if (sealRenderer != null) sealRenderer.enabled = false;
        portal.SetLocked(false);
    }

    private void OnDisable()
    {
        if (requiredEnemy != null) requiredEnemy.Defeated -= OnEnemyDefeated;
        if (unlockRoutine != null)
        {
            StopCoroutine(unlockRoutine);
            unlockRoutine = null;
        }
    }
}
