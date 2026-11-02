import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

ROOT = Path('Assets/_Project/Audio/SoundEffects')
SOURCE = Path(r'C:\Users\jeonghuncomputer\Downloads')
RATE = 48000

def read(relative):
    samples, rate = sf.read(SOURCE / relative, always_2d=True)
    # Sources are 96 kHz; average each pair before decimating to 48 kHz.
    assert rate == 96000
    samples = samples[:len(samples) // 2 * 2].reshape(-1, 2, samples.shape[1]).mean(axis=1)
    return samples

def fade(samples, attack, release):
    samples = samples.copy()
    a, r = round(attack * RATE), round(release * RATE)
    samples[:a] *= np.linspace(0, 1, a)[:, None]
    samples[-r:] *= np.linspace(1, 0, r)[:, None]
    return samples

def write(relative, samples, peak_db, edit):
    path = ROOT / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    samples *= 10 ** (peak_db / 20) / np.max(np.abs(samples))
    sf.write(path, samples, RATE, subtype='PCM_16')
    meta = path.with_suffix(path.suffix + '.meta')
    if not meta.exists():
        template = (ROOT / 'Skills/Monk_EnergyWave_Charge.wav.meta').read_text()
        old_guid = next(line for line in template.splitlines() if line.startswith('guid:'))
        meta.write_text(template.replace(old_guid, 'guid: ' + uuid.uuid4().hex), encoding='utf-8')
    decoded, rate = sf.read(path, always_2d=True)
    assert rate == RATE and decoded.shape[1] == 2
    assert np.max(np.abs(decoded)) < 1
    assert np.max(np.abs(decoded[[0, -1]])) <= 1 / 32768
    guid = next(line.split(': ')[1] for line in meta.read_text().splitlines() if line.startswith('guid:'))
    return dict(path=str(path), guid=guid, duration=len(decoded)/rate,
                peak_dbfs=float(20*np.log10(np.max(np.abs(decoded)))), edit=edit)

snap = read(Path('Sonniss.com-GDC2026-GameAudioBundle2of5/Cinematic Sound Design - Colossal Impacts/Transition Frantic Shaker Snap.wav'))
snap = fade(snap[round(.40*RATE):round(.68*RATE)], .003, .055)
spacing = round(.10 * RATE)
burst = np.zeros((len(snap) + 2*spacing, 2))
for index in range(3):
    burst[index*spacing:index*spacing+len(snap)] += snap
assert len(burst) == len(snap) + 2*spacing
wind = read(Path('Sonniss.com-GDC2026-GameAudioBundle1of5/344 Audio - Elemental Palette Designed Vol. 1/WINDDsgn_Wind, Rush, Whoosh, Long x5 01_344 Audio_Elemental Palette Designed Vol 1.wav'))
wind = fade(wind[round(.06*RATE):round(.46*RATE)], .005, .065)
level = read(Path('Sonniss.com-GDC2026-GameAudioBundle2of5/Epic Stock Media - Anime Game/DSGNStngr_Power Up Bright Positive Successful Light Saturation Crash Shimmer 05_ESM_AG.wav'))
level = fade(level, .003, .025)
report = [
    write('Skills/Dosa_TripleTalisman_Burst.wav', burst, -4, 'Source 0.40–0.68 s, 3/55 ms fades; three copies at 0, 100, 200 ms.'),
    write('Skills/Dosa_Teleport_Whoosh.wav', wind, -6, 'Source 0.06–0.46 s; 5/65 ms fades.'),
    write('Skills/LevelUp_Shimmer.wav', level, -3, 'Full source; 3/25 ms fades.')]
Path('Artifacts/AudioEditing/Dosa-LevelUp-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
