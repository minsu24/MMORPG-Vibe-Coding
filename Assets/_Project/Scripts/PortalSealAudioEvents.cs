using System.Collections.Generic;
using UnityEngine;

public sealed class PortalSealAudioEvents : MonoBehaviour
{
    private bool armed, tensionPlayed, chainsPlayed, lockPlayed;
    private readonly List<PortalAudioCue> cues = new List<PortalAudioCue>();

    public void Begin()
    {
        StopSounds();
        armed = true;
        tensionPlayed = chainsPlayed = lockPlayed = false;
    }

    public void PortalSealTension()
    {
        if (!armed || tensionPlayed) return;
        tensionPlayed = true;
        cues.Add(PortalAudioCue.Play("Audio/PortalSealTension", .4f, 2.05f, .32f, transform));
    }

    public void PortalSealChainsBreak()
    {
        if (!armed || chainsPlayed) return;
        chainsPlayed = true;
        cues.Add(PortalAudioCue.Play("Audio/PortalChainsBreak", .4f, .75f, owner: transform));
    }

    public void PortalSealLockBreak()
    {
        if (!armed || lockPlayed) return;
        lockPlayed = true;
        cues.Add(PortalAudioCue.Play("Audio/PortalSealBreak", .45f, 1.02f, owner: transform));
    }

    public void StopSounds()
    {
        armed = false;
        foreach (var cue in cues) if (cue != null) cue.Stop();
        cues.Clear();
    }

    private void OnDisable() => StopSounds();
}
