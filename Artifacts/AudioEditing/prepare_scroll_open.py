import json
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

source = Path(r'C:\Users\jeonghuncomputer\Downloads\mixkit-single-book-paging-1101.wav')
output = Path('Assets/_Project/Resources/Audio/ScrollOpen.wav')
x, rate = sf.read(source, always_2d=True)
# First page turn only; omit the second turn later in the recording.
x = x[round(.035*rate):round(.59*rate)].copy()
attack, release = round(.004*rate), round(.03*rate)
x[:attack] *= np.linspace(0, 1, attack)[:, None]
x[-release:] *= np.linspace(1, 0, release)[:, None]
output.parent.mkdir(parents=True, exist_ok=True)
sf.write(output, x, rate, subtype='PCM_24')
decoded, _ = sf.read(output, always_2d=True)
assert np.isfinite(decoded).all() and np.max(abs(decoded[[0, -1]])) <= 1/8388608
report = dict(source=str(source), selected_range=[.035, .59], duration=len(x)/rate,
              output=str(output), gain_db=0, pitch_changed=False)
Path('Artifacts/AudioEditing/ScrollOpen-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
