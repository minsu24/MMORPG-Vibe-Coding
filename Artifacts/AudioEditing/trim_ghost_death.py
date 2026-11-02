import json
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

source = Path(r'C:\Users\jeonghuncomputer\Downloads\유령 죽는 소리.mp3')
output = Path('Assets/_Project/Resources/Audio/GhostDeathCut.wav')
x, rate = sf.read(source, always_2d=True)
assert len(x) >= 5*rate
excerpt = x[round(3.7*rate):round(4.7*rate)]
sf.write(output, excerpt, rate, subtype='PCM_24')
decoded, output_rate = sf.read(output, always_2d=True)
assert output_rate == rate and decoded.shape == excerpt.shape
assert np.isfinite(decoded).all() and np.max(abs(decoded-excerpt)) < 2**-22
report = dict(source=str(source), range_seconds=[3.7, 4.7], duration_seconds=1,
              output=str(output), pitch_changed=False, gain_db=0)
Path('Artifacts/AudioEditing/GhostDeathCut-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(dict(duration=1, sample_rate=rate, channels=excerpt.shape[1])))
