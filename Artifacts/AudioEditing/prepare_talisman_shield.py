import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

source = Path(r'C:\Users\jeonghuncomputer\Downloads\Sonniss.com-GDC2026-GameAudioBundle2of5\Epic Stock Media - Halloween Game - Haunted House and Horror Audio Scare Kit\UIAlert_Alert Designed Transition Swell Creature Strange Alien Layered 01_ESM_HALG.wav')
output = Path('Assets/_Project/Audio/SoundEffects/Skills/Dosa_TalismanShield.wav')
samples, rate = sf.read(source, always_2d=True)
assert rate == 96000
samples = samples[:round(.65 * rate)]
samples = samples.reshape(-1, 2, samples.shape[1]).mean(axis=1)
rate = 48000

# Gentle 100 Hz high-pass removes sustained sub-bass from the horror source.
alpha = np.exp(-2 * np.pi * 100 / rate)
filtered = np.zeros_like(samples)
for i in range(1, len(samples)):
    filtered[i] = alpha * (filtered[i-1] + samples[i] - samples[i-1])
attack, release = round(.005 * rate), round(.16 * rate)
filtered[:attack] *= np.linspace(0, 1, attack)[:, None]
filtered[-release:] *= np.linspace(1, 0, release)[:, None]
filtered *= 10 ** (-6 / 20) / np.max(np.abs(filtered))
sf.write(output, filtered, rate, subtype='PCM_16')

meta = output.with_suffix('.wav.meta')
if not meta.exists():
    template = output.with_name('Dosa_Teleport_Whoosh.wav.meta').read_text()
    old_guid = next(line for line in template.splitlines() if line.startswith('guid:'))
    meta.write_text(template.replace(old_guid, 'guid: ' + uuid.uuid4().hex), encoding='utf-8')
decoded, decoded_rate = sf.read(output, always_2d=True)
assert decoded_rate == rate and decoded.shape == (31200, 2)
assert np.max(np.abs(decoded)) < 1
assert np.max(np.abs(decoded[[0, -1]])) <= 1/32768
report = dict(source=str(source), output=str(output), duration=len(decoded)/rate,
              peak_dbfs=float(20*np.log10(np.max(np.abs(decoded)))),
              edit='First 650 ms; 100 Hz high-pass; 5 ms attack and 160 ms release; 48 kHz stereo PCM16.',
              listening_verified=False)
Path('Artifacts/AudioEditing/TalismanShield-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(meta.read_text().splitlines()[1])
print(json.dumps(report, indent=2))
