import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

source = Path(r'C:\Users\jeonghuncomputer\Downloads\150gameeffectsounds\03_explosions_destruction\ElectricExplosion_s01.wav')
folder = Path('Assets/_Project/Audio/SoundEffects/Skills')
output = folder / 'Dosa_ElectricExplosion_Hit.wav'
reference = folder / 'Dosa_MagicBurst_Hit.wav'
samples, rate = sf.read(source, always_2d=True)
samples = samples[:round(.2 * rate)].copy()
samples -= samples.mean(axis=0)
attack, release = round(.0015 * rate), round(.045 * rate)
samples[:attack] *= np.linspace(0, 1, attack)[:, None]
samples[-release:] *= np.linspace(1, 0, release)[:, None]
previous, _ = sf.read(reference, always_2d=True)
reference_rms = np.sqrt(np.mean(previous * previous))
gain = min(reference_rms / np.sqrt(np.mean(samples * samples)),
           10 ** (-6 / 20) / np.max(np.abs(samples)))
samples *= gain
sf.write(output, samples, rate, subtype='PCM_16')
meta = output.with_suffix('.wav.meta')
if not meta.exists():
    template = reference.with_suffix('.wav.meta').read_text()
    old_guid = next(line for line in template.splitlines() if line.startswith('guid:'))
    meta.write_text(template.replace(old_guid, 'guid: ' + uuid.uuid4().hex), encoding='utf-8')
guid = next(line.split(': ')[1] for line in meta.read_text().splitlines() if line.startswith('guid:'))
decoded, decoded_rate = sf.read(output, always_2d=True)
assert len(decoded) == round(.2 * decoded_rate)
assert np.max(np.abs(decoded)) < .502
assert np.max(np.abs(decoded[[0, -1]])) <= 1 / 32768
for name in ['Projectile', 'TalismanProjectile']:
    prefab = Path('Assets/_Project/Prefabs/Player') / (name + '.prefab')
    text = prefab.read_text(encoding='utf-8')
    old = 'hitSound: {fileID: 8300000, guid: 21114dab25164be082a87c61582410ec, type: 3}'
    new = f'hitSound: {{fileID: 8300000, guid: {guid}, type: 3}}'
    assert text.count(old) + text.count(new) == 1
    assert 'hitSoundVolume: 0.15' in text
    prefab.write_text(text.replace(old, new), encoding='utf-8')
report = dict(source=str(source), output=str(output), guid=guid, duration=len(decoded)/decoded_rate,
              peak_dbfs=float(20*np.log10(np.max(np.abs(decoded)))),
              rms_difference_db=float(20*np.log10(np.sqrt(np.mean(decoded*decoded))/reference_rms)),
              edit='First 200 ms; 1.5 ms attack, 45 ms release; RMS matched to previous impact with -6 dBFS peak ceiling.',
              playback_volume=.15, prefabs=['Projectile', 'TalismanProjectile'])
Path('Artifacts/AudioEditing/DosaElectricImpact-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
