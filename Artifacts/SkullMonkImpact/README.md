# Skull Monk impact revision

Saved to the existing Skull in `Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity`.

Character pixels, texture resolution, import scale, rear-foot pivots, and actor scale are unchanged. Source SHA256 remains `237A76E6FA4085E1B189D2A404BFAF874D4C23D93B35F6D6CFA74793EAFC6ABD`. No image generation was needed.

- Raised-foot anticipation holds from 0.26 to 0.43 seconds.
- Plant and drive: poses 4 to 7 at 0.43, 0.45, 0.47, 0.49 seconds. Extension takes 60 ms.
- Impact at 0.49 seconds; full extension holds until 0.72 seconds.
- Recovery uses retracted poses 2, 1, 0 instead of reversing the lunge/raised foot; ends at 1.10 seconds.
- Existing effect opens brightly in 25 ms, surrounds the shaft, moves rapidly over the tip and fades out by 220 ms.
- Quiet existing foot-plant and swing cues accompany the drive. Successful damage adds impact sound, 40 ms hit stop and a small camera impulse through the existing CombatImpactFeedback system. A miss does not stop the game.

`attack-preview.png` is a lossless looping APNG from Unity-rendered frames at 60 FPS. This visual-only preview shows the animation and burst; victim feedback is conditional and is not included in the preview. `impact.png` is the peak still. `timing.json` records the actual keys and events.

Runtime tests passed for both directions: no early damage, one hit, burst active, hit stop, resume path, attack completion. Whiff and interruption checks passed. See `runtime.json`. This deterministic probe advanced Animator.Update and the stop expiry explicitly. Temporary players were removed and Play Mode stopped.

A pre-existing missing PlayerIdle8Frames.png setup error was present before these edits. Additive test scenes produced duplicate EventSystem/AudioListener notices; the temporary Center scene was closed afterward. Those unrelated systems were not changed.
