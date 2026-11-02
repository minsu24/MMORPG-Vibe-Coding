import json
import shutil
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

source = Path(r'C:\Users\jeonghuncomputer\Downloads\jesuschristisgod-4-different-types-of-women-grunting-317870.mp3')
output = Path('Assets/_Project/Audio/SoundEffects/Impacts/Swordswoman_Death.wav')
x, rate = sf.read(source, always_2d=True)
assert len(x) >= 7*rate, 'Source is shorter than seven seconds'
excerpt = x[6*rate:7*rate].copy()
assert np.isfinite(excerpt).all()
backup = Path('Artifacts/SwordswomanDeathAudio/Previous_female_hurt_original_pitch.wav')
if output.exists() and not backup.exists():
    shutil.copy2(output, backup)
# Preserve the exact selected timeline, channel count, pitch and level.
# No normalization, trimming, resampling, fades or time/pitch processing.
sf.write(output, excerpt, rate, subtype='PCM_24')
decoded, output_rate = sf.read(output, always_2d=True)
assert output_rate == rate and decoded.shape == excerpt.shape
error = float(np.max(np.abs(decoded-excerpt)))
assert error < 2**-22, 'Unexpected audio processing or clipping'
report = dict(source=str(source), range_seconds=[6, 7], output=str(output),
              duration=1.0, sample_rate=rate, channels=excerpt.shape[1],
              pitch_shift_semitones=0, gain_db=0, fades=False,
              max_pcm_quantization_error=error)
Path('Artifacts/AudioEditing/SwordswomanDeathExactCut-report.json').write_text(
    json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
