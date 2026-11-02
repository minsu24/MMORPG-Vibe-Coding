import ast
import json
import shutil
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

# Reuse analysis functions without running the earlier source conversion.
tree = ast.parse(Path('Artifacts/AudioEditing/prepare_swordswoman_death.py').read_text(encoding='utf-8'))
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n, ast.FunctionDef)], type_ignores=[]),
             '<audio-helpers>', 'exec'))
source_path = Path(r'C:\Users\jeonghuncomputer\Downloads\female_hurt_grunts_groans_1.ogg')
output_path = Path('Assets/_Project/Audio/SoundEffects/Impacts/Swordswoman_Death.wav')
source, rate = mono(source_path)
references = [mono(Path(p))[0] for p in [
    'Assets/_Project/Audio/SoundEffects/Skills/Swordswoman_HurtVoice.wav',
    'Assets/_Project/Audio/SoundEffects/Movement/Swordswoman_JumpVoice.wav']]


def spectrum_profile(x):
    n = 1024
    frames = np.lib.stride_tricks.sliding_window_view(x, n)[::256]
    power = abs(np.fft.rfft(frames*np.hanning(n), axis=1))**2
    frequencies = np.fft.rfftfreq(n, 1/rate)
    bands = [80, 200, 400, 800, 1600, 3200, 6400, 12000]
    result = np.array([power[:, (frequencies>=a)&(frequencies<b)].sum()
                       for a, b in zip(bands[:-1], bands[1:])])
    result = np.log10(result/result.sum()+1e-6)
    return result-result.mean()


size = round(rate*.01)
energy = np.sqrt(np.mean(source[:len(source)//size*size].reshape(-1, size)**2, axis=1))
active = energy > max(.002, energy.max()*.045)
indices = np.flatnonzero(active)
groups = np.split(indices, np.flatnonzero(np.diff(indices) > 20)+1)
reference_profile = np.mean([spectrum_profile(x) for x in references], axis=0)
reference_pitch = float(np.mean([autocorrelation_summary(x, rate)['median_hz'] for x in references]))
segments = []
for group in groups:
    start = max(0, (group[0]*size)/rate-.025)
    end = min(len(source)/rate, ((group[-1]+1)*size)/rate+.09)
    duration = end-start
    if duration < .2:
        continue
    x = source[round(start*rate):round(end*rate)]
    proxy = autocorrelation_summary(x, rate)
    pitch = pitch_summary(x, rate)
    timbre_distance = float(np.sqrt(np.mean((spectrum_profile(x)-reference_profile)**2)))
    pitch_distance = abs(float(np.log2(proxy['median_hz']/reference_pitch)))
    # Prefer a fuller utterance over a tiny hit grunt, without merging different takes.
    duration_penalty = max(0, .8-duration)*2 + max(0, duration-2)*2
    score = timbre_distance+.5*pitch_distance+duration_penalty
    segments.append(dict(start=float(start), end=float(end), duration=float(duration),
                         pitch=pitch, proxy=proxy, timbre_distance=timbre_distance, score=score))

report = dict(source=str(source_path), source_seconds=len(source)/rate,
              reference_pitch_proxy_hz=reference_pitch, segments=segments,
              selection_basis='Acoustic similarity and utterance length; no listening/emotion assessment.')
if '--write' in sys.argv:
    selected = min(segments, key=lambda s: s['score'])
    start, end = selected['start'], min(len(source)/rate, selected['end']+.08)
    take = source[round(start*rate):round(end*rate)]
    semitones = 0.0
    ratio = 2**(semitones/12)
    pitched = np.interp(np.arange(0, len(take), ratio), np.arange(len(take)), take)
    output = take.copy() if semitones == 0 else stretch_to(pitched, len(take))
    output -= output.mean()
    attack, release = round(.008*rate), round(.08*rate)
    output[:attack] *= np.linspace(0, 1, attack)
    output[-release:] *= np.linspace(1, 0, release)
    target_rms = float(np.exp(np.mean([np.log(active_rms(x, rate)) for x in references])))
    gain = min(target_rms/active_rms(output, rate), .5/max(abs(output)))
    output *= gain
    backup = Path('Artifacts/SwordswomanDeathAudio/Previous_1_dying_Adjusted.wav')
    backup.parent.mkdir(parents=True, exist_ok=True)
    if output_path.exists() and not backup.exists():
        shutil.copy2(output_path, backup)
    sf.write(output_path, output, rate, subtype='PCM_16')
    decoded, _ = sf.read(output_path)
    assert np.isfinite(decoded).all() and abs(decoded).max() <= .501
    assert abs(decoded[0]) <= 1/32768 and abs(decoded[-1]) <= 1/32768
    report['processing'] = dict(selected_range=[start, end], output=str(output_path),
        backup=str(backup), pitch_shift_semitones=semitones, preserved_performance_duration=True,
        duration_seconds=len(decoded)/rate, sample_rate=rate,
        gain_db=float(20*np.log10(gain)), peak_dbfs=float(20*np.log10(abs(decoded).max())),
        active_rms_db=float(20*np.log10(active_rms(decoded, rate))),
        output_pitch=pitch_summary(decoded, rate))
print(json.dumps(report, indent=2))
Path('Artifacts/AudioEditing/SwordswomanDeathSelection-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
