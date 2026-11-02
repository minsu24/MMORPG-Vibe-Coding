import json
import shutil
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

path = Path('Assets/_Project/Audio/SoundEffects/Impacts/Dosa_Death_Dissolve.wav')
backup = Path('Artifacts/DosaDeathTrim/Dosa_Death_Dissolve_before_trim.wav')
backup.parent.mkdir(parents=True, exist_ok=True)
if not backup.exists():
    shutil.copy2(path, backup)
x, rate = sf.read(backup, always_2d=True)
original_seconds = len(x)/rate
x = x[:round(1.0*rate)].copy()
fade = min(round(.18*rate), len(x))
x[-fade:] *= np.linspace(1, 0, fade)[:, None]
sf.write(path, x, rate, subtype='PCM_24')
decoded, _ = sf.read(path, always_2d=True)
assert np.isfinite(decoded).all() and abs(decoded[-1]).max() <= 1/8388608
report = dict(previous_seconds=original_seconds, new_seconds=len(x)/rate,
              tail_fade_seconds=.18, pitch_changed=False, start_timing_changed=False)
Path('Artifacts/DosaDeathTrim/Report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
