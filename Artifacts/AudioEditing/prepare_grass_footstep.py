import json
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

source = Path(r'C:\Users\jeonghuncomputer\Downloads\RPG_Essentials_Free\12_Player_Movement_SFX\03_Step_grass_03.wav')
reference = Path('Assets/_Project/Audio/SoundEffects/Footsteps/Barefoot_SoftStep.wav')
output = reference.with_name('Grass_SoftStep.wav')

def mono(path):
    samples, rate = sf.read(path, always_2d=True)
    return samples.mean(axis=1), rate

def biquad(x, b, a):
    y = np.zeros_like(x)
    x1 = x2 = y1 = y2 = 0.0
    for i, sample in enumerate(x):
        value = b[0] * sample + b[1] * x1 + b[2] * x2 - a[0] * y1 - a[1] * y2
        y[i] = value
        x2, x1, y2, y1 = x1, sample, y1, value
    return y

def weighted_energy(x, rate):
    # Compare energy per step at the same cadence, including filter decay.
    # K-weighting accounts for the very different bass/treble balance.
    count = round(len(x) * 48000 / rate)
    x = np.interp(np.arange(count) / 48000, np.arange(len(x)) / rate, x)
    x = np.pad(x, (0, 24000))
    x = biquad(x, [1.53512485958697, -2.69169618940638, 1.19839281085285], [-1.69065929318241, .73248077421585])
    x = biquad(x, [1., -2., 1.], [-1.99004745483398, .99007225036621])
    return float(np.sum(x * x))

old, old_rate = mono(reference)
new, rate = mono(source)
new = new[:round(rate * .14)].copy()
fade_in, fade_out = round(rate * .002), round(rate * .015)
new[:fade_in] *= np.linspace(0., 1., fade_in)
new[-fade_out:] *= np.linspace(1., 0., fade_out)
gain = np.sqrt(weighted_energy(old, old_rate) / weighted_energy(new, rate))
new *= gain
if np.max(np.abs(new)) > .98:
    raise RuntimeError('Matching requires additional playback headroom; do not clip.')
sf.write(output, new, rate, subtype='PCM_16')
decoded, decoded_rate = mono(output)
report = {
    'source': str(source), 'reference': str(reference), 'output': str(output),
    'duration_seconds': len(decoded) / decoded_rate,
    'sample_rate': decoded_rate, 'channels': 1, 'gain_db': float(20 * np.log10(gain)),
    'peak_dbfs': float(20 * np.log10(np.max(np.abs(decoded)))),
    'weighted_energy_difference_db': float(10 * np.log10(weighted_energy(decoded, decoded_rate) / weighted_energy(old, old_rate))),
    'match_method': 'Equal K-weighted energy per step; not a listening test or full integrated LUFS measurement.',
    'playback_volumes_preserved': {'MonkPlayer': .236, 'Player_Dosa': .25},
    'edit': 'First 140 ms; 2 ms fade in, 15 ms fade out; stereo duplicate channels collapsed to mono.'
}
Path('Artifacts/AudioEditing/GrassFootstep-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
