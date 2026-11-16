using System.Collections.Generic;
using UnityEngine;

public sealed partial class MON_SkullMonk
{
    [Header("Five ground waves")]
    [SerializeField, Min(1f)] private float waveCooldown = 12f;
    [SerializeField, Min(1f)] private float waveSpeed = 12f;
    [SerializeField, Min(.1f)] private float waveHeight = .45f;
    [SerializeField] private Collider2D waveArenaBounds;
    [SerializeField] private Collider2D waveFloor;
    [SerializeField] private Sprite[] staffSmokeFrames;
    [SerializeField] private float plantedStaffOffset = .65f;
    private enum WaveStage { None, Raising, Pulsing, Recovering }
    private WaveStage waveStage;
    private float nextWaveAt;
    private bool staffPlanted, pulseFired;
    private Vector2 plantedStaff;
    private readonly List<SkullMonkGroundWave> groundWaves = new List<SkullMonkGroundWave>();
    public bool IsWaveAttacking => waveStage != WaveStage.None;
    public int WavesReleased { get; private set; }

    protected override void Start()
    {
        base.Start();
        InitializeIntro();
        nextWaveAt = Time.time + 5f;
        nextRingAt = Time.time + 9f;
        nextPrayerAt = Time.time + 13f;
        nextGiantStaffAt = Time.time + 17f;
    }

    private bool CanStartWavePattern()
    {
        return Time.time >= nextWaveAt && waveFloor != null && waveArenaBounds != null
            && Mathf.Abs(rb.linearVelocity.y) < .4f
            && Mathf.Abs(capsuleCollider2D.bounds.min.y - waveFloor.bounds.max.y) < .4f;
    }
    private void StartWavePattern()
    {
        float facing = playerEntity.transform.position.x >= transform.position.x ? 1f : -1f;
        var scale = transform.localScale; scale.x = -Mathf.Abs(scale.x) * facing; transform.localScale = scale;
        plantedStaff = new Vector2(transform.position.x + plantedStaffOffset * facing * Mathf.Abs(scale.x), waveFloor.bounds.max.y);
        isAttacking = true; emitted = true; staffPlanted = false; pulseFired = false;
        WavesReleased = 0; waveStage = WaveStage.Raising;
        endsAt = Time.time + 10f;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);
        animator.Play("SkullMonkWavePlant", 0, 0);
    }

    // Last pose of wave attack 1: the staff first touches the floor.
    public void SkullMonkPlantStaff()
    {
        if (waveStage != WaveStage.Raising || staffPlanted || !CanDealContactDamage) return;
        staffPlanted = true;
        GiantAttackVisual.CreateDust(plantedStaff, staffSmokeFrames, spriteRenderer, 0f, 1.7f);
    }
    public void SkullMonkBeginWavePulses()
    {
        if (waveStage != WaveStage.Raising || !staffPlanted) return;
        waveStage = WaveStage.Pulsing; pulseFired = false;
        animator.Play("SkullMonkWavePulse", 0, 0);
    }
    // Lowest bowing pose of wave attack 2; exactly one event per committed cycle.
    public void SkullMonkReleaseWave()
    {
        if (waveStage != WaveStage.Pulsing || pulseFired || WavesReleased >= 5
            || !CanDealContactDamage || !HasLivingPlayer) return;
        pulseFired = true; WavesReleased++;
        groundWaves.RemoveAll(w => w == null);
        groundWaves.Add(SkullMonkGroundWave.Create(this, plantedStaff, waveArenaBounds.bounds.min.x,
            waveArenaBounds.bounds.max.x, waveSpeed, waveHeight, _playerLayer, spriteRenderer));
    }
    public void SkullMonkWaveCycleFinished()
    {
        if (waveStage != WaveStage.Pulsing || !pulseFired) return;
        if (WavesReleased >= 5)
        {
            waveStage = WaveStage.Recovering;
            animator.Play("SkullMonkWaveRecover", 0, 0);
        }
        else { pulseFired = false; animator.Play("SkullMonkWavePulse", 0, 0); }
    }
    public void SkullMonkWaveRecovered()
    {
        if (waveStage == WaveStage.Recovering) FinishAttack();
    }
    private void EndWavePattern()
    {
        if (!IsWaveAttacking) return;
        waveStage = WaveStage.None;
        nextWaveAt = Time.time + waveCooldown;
        // Released waves finish traveling to the edges even while the monk recovers.
    }
    private void CancelGroundWaves()
    {
        foreach (var wave in groundWaves) if (wave != null) wave.Cancel();
        groundWaves.Clear();
    }
}
