using UnityEngine;

public sealed partial class MON_Frog
{
    private AudioSource movementAudio;

    private void LateUpdate()
    {
        bool moving = CanDealContactDamage && !isAttacking && !isKnockback && Time.timeScale > 0f
            && rb != null && rb.simulated && Mathf.Abs(rb.linearVelocity.x) > .1f
            && Mathf.Abs(rb.linearVelocity.y) < .2f;
        if (!moving) { StopMovementAudio(); return; }
        if (movementAudio == null)
        {
            var clip = Resources.Load<AudioClip>("Audio/FrogMove");
            if (clip == null) return;
            movementAudio = gameObject.AddComponent<AudioSource>();
            movementAudio.playOnAwake = false;
            movementAudio.loop = true;
            movementAudio.spatialBlend = 0f;
            movementAudio.volume = .2f;
            movementAudio.clip = clip;
        }
        if (!movementAudio.isPlaying) movementAudio.Play();
    }

    private void StopMovementAudio()
    {
        if (movementAudio != null) movementAudio.Stop();
    }

    protected override void OnDisable()
    {
        StopMovementAudio();
        HideWarnings();
        RestoreAnimatorSpeed();
        base.OnDisable();
    }
}
