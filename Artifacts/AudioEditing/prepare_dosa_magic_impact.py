import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

source = Path(r'C:\Users\jeonghuncomputer\Downloads\humordome-magic-burst-452852.mp3')
output = Path('Assets/_Project/Audio/SoundEffects/Skills/Dosa_MagicBurst_Hit.wav')
samples, rate = sf.read(source, always_2d=True)
start_seconds = .30
samples = samples[round(start_seconds * rate):round((start_seconds + .52) * rate)].copy()
samples -= samples.mean(axis=0)
pitch_semitones = -1.0
pitch_ratio = 2 ** (pitch_semitones / 12)
positions = np.arange(round(len(samples) / pitch_ratio)) * pitch_ratio
samples = np.column_stack([
    np.interp(positions, np.arange(len(samples)), samples[:, channel])
    for channel in range(samples.shape[1])
])
attack, release = round(.003 * rate), round(.14 * rate)
samples[:attack] *= np.linspace(0, 1, attack)[:, None]
samples[-release:] *= np.linspace(1, 0, release)[:, None]
# The MP3 decodes above 0 dBFS. Leave headroom before three talismans overlap.
samples *= 10 ** (-6 / 20) / np.max(np.abs(samples))
sf.write(output, samples, rate, subtype='PCM_16')
meta = output.with_suffix('.wav.meta')
if not meta.exists():
    template = output.with_name('Dosa_Teleport_Whoosh.wav.meta').read_text()
    old_guid = next(line for line in template.splitlines() if line.startswith('guid:'))
    meta.write_text(template.replace(old_guid, 'guid: ' + uuid.uuid4().hex), encoding='utf-8')
decoded, decoded_rate = sf.read(output, always_2d=True)
assert abs(len(decoded) / decoded_rate - .52 / pitch_ratio) < 1 / decoded_rate
assert np.max(np.abs(decoded)) < .502
assert np.max(np.abs(decoded[[0, -1]])) <= 1 / 32768
report = dict(source=str(source), output=str(output), duration=len(decoded)/decoded_rate,
              peak_dbfs=float(20*np.log10(np.max(np.abs(decoded)))),
              source_start_seconds=start_seconds,
              pitch_semitones=pitch_semitones,
              edit='Source 300-820 ms; lowered one semitone; 3 ms attack / 140 ms release; stereo PCM16, -6 dBFS peak.',
              playback_volume=.25,
              guid=next(line.split(': ')[1] for line in meta.read_text().splitlines() if line.startswith('guid:')))
Path('Artifacts/AudioEditing/DosaMagicImpact-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
