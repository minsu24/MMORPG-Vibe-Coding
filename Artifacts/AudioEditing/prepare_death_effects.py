import json
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

folder = Path('Assets/_Project/Audio/SoundEffects/Impacts')
downloads = Path(r'C:\Users\jeonghuncomputer\Downloads')
report = []
for source_name, name in [
    ('dragon-studio-sand-transition-451861.mp3', 'Dosa_Death_Dissolve.wav'),
    ('universfield-body-fall-142378.mp3', 'Player_Death_BodyFall.wav'),
]:
    source = downloads/source_name
    x, rate = sf.read(source, always_2d=True)
    # Discard only leading/trailing low-level silence so animation events do not
    # trigger a silent lead-in; retain the sound's original pitch and speed.
    envelope = np.max(abs(x), axis=1)
    active = np.flatnonzero(envelope > envelope.max()*.025)
    start = max(0, int(active[0])-round(.005*rate))
    end = min(len(x), int(active[-1])+round(.06*rate))
    x = x[start:end].copy()
    fade_in = min(round(.003*rate), len(x)//4)
    fade_out = min(round(.025*rate), len(x)//4)
    x[:fade_in] *= np.linspace(0, 1, fade_in)[:, None]
    x[-fade_out:] *= np.linspace(1, 0, fade_out)[:, None]
    # Leave headroom without amplifying a quiet recording.
    gain = min(1., .7/np.max(abs(x)))
    x *= gain
    path = folder/name
    sf.write(path, x, rate, subtype='PCM_24')
    decoded, _ = sf.read(path, always_2d=True)
    assert np.isfinite(decoded).all() and abs(decoded).max() <= .701
    report.append(dict(source=str(source), output=str(path),
                       selected_range=[start/rate, end/rate], duration=len(x)/rate,
                       pitch_shift_semitones=0, gain_db=float(20*np.log10(gain))))
Path('Artifacts/AudioEditing/DeathEffects-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
