import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

folder = Path('Assets/_Project/Audio/Ambience')
folder.mkdir(parents=True, exist_ok=True)
downloads = Path(r'C:\Users\jeonghuncomputer\Downloads')

def prepare(name, source_name, start=0, duration=None, mono=False, rms_target=.07):
    source = downloads / source_name
    samples, rate = sf.read(source, always_2d=True)
    samples = samples[round(start*rate):round((start+duration)*rate) if duration else None]
    if mono:
        samples = samples.mean(axis=1, keepdims=True)
    samples -= samples.mean(axis=0)
    # Wrap tail into head; the final sample then leads naturally into the first.
    overlap = round(.8 * rate)
    weight = (.5 - .5*np.cos(np.linspace(0, np.pi, overlap)))[:, None]
    seam = samples[-overlap:]*(1-weight) + samples[:overlap]*weight
    samples = np.concatenate((samples[overlap:-overlap], seam))
    gain = min(rms_target / np.sqrt(np.mean(samples*samples)), .7 / np.max(np.abs(samples)))
    samples *= gain
    output = folder / name
    sf.write(output, samples, rate, subtype='PCM_16')
    meta = output.with_suffix('.wav.meta')
    if not meta.exists():
        template = Path('Assets/_Project/Audio/SoundEffects/Skills/Dosa_Teleport_Whoosh.wav.meta').read_text()
        guid_line = next(l for l in template.splitlines() if l.startswith('guid:'))
        template = template.replace(guid_line, 'guid: ' + uuid.uuid4().hex)
        template = template.replace('loadType: 0', 'loadType: 2').replace('preloadAudioData: 1', 'preloadAudioData: 0').replace('quality: 1', 'quality: 0.7')
        meta.write_text(template, encoding='utf-8')
    decoded, decoded_rate = sf.read(output, always_2d=True)
    assert np.max(np.abs(decoded)) < .701
    # The wrap should be no larger than ordinary sample-to-sample changes.
    wrap = float(np.max(np.abs(decoded[-1]-decoded[0])))
    ordinary_jump = float(np.quantile(np.abs(np.diff(decoded, axis=0)), .999))
    assert wrap < max(.03, ordinary_jump), f'Loop seam discontinuity: {wrap}'
    return dict(source=str(source), output=str(output), duration=len(decoded)/decoded_rate,
                sample_rate=decoded_rate, channels=decoded.shape[1], loop_wrap=wrap,
                rms=float(np.sqrt(np.mean(decoded*decoded))),
                guid=next(l.split(': ')[1] for l in meta.read_text().splitlines() if l.startswith('guid:')))

report = [prepare('Village_WindThroughTrees_Loop.wav', 'freesound_community-wind-through-trees-ambience-56038.mp3'),
          prepare('Village_Waterfall_Loop.wav', 'soundsforyou-waterfall-ambient-sounds-116395.mp3', start=8, duration=60, mono=True, rms_target=.10)]
Path('Artifacts/AudioEditing/VillageAmbience-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
