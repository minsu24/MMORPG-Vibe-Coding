import json
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

source = Path('Assets/_Project/Audio/SoundEffects/Interactions/homemade_sfx-equip-sound-272428.mp3')
output = source.parent/'Equip_Short.wav'
x, rate = sf.read(source, always_2d=True)
original_seconds = len(x)/rate
x = x[round(.02*rate):round(.32*rate)].copy()
attack, release = round(.003*rate), round(.025*rate)
x[:attack] *= np.linspace(0, 1, attack)[:, None]
x[-release:] *= np.linspace(1, 0, release)[:, None]
sf.write(output, x, rate, subtype='PCM_24')
decoded, _ = sf.read(output, always_2d=True)
assert np.isfinite(decoded).all() and np.max(abs(decoded[[0, -1]])) <= 1/8388608
report = dict(original_seconds=original_seconds, shortened_seconds=len(x)/rate,
              source_range=[.02, .32], gain_db=0, pitch_changed=False)
Path('Artifacts/AudioEditing/EquipShort-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
