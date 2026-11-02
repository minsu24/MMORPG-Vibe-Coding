import json
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

source = Path(r'C:\Users\jeonghuncomputer\Downloads\mixkit-short-fire-whoosh-1345.wav')
output = Path('Assets/_Project/Resources/Audio/ScrollClose.wav')
x, rate = sf.read(source, always_2d=True)
# Keep the main whoosh, omitting the long near-silent tail. At runtime 2x
# playback fits this 1.2-second excerpt into the 0.65-second closing motion.
x = x[round(.1*rate):round(1.3*rate)].copy()
attack, release = round(.006*rate), round(.08*rate)
x[:attack] *= np.linspace(0, 1, attack)[:, None]
x[-release:] *= np.linspace(1, 0, release)[:, None]
sf.write(output, x, rate, subtype='PCM_24')
report = dict(source=str(source), selected_range=[.1, 1.3], playback_speed=2,
              playback_seconds=len(x)/rate/2, volume=.2, output=str(output))
Path('Artifacts/AudioEditing/ScrollClose-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
