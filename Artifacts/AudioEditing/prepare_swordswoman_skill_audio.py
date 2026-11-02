import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

downloads = Path(r'C:\Users\jeonghuncomputer\Downloads')
swishes = downloads / 'Sonniss.com - GDC - Game Audio Bundle 3of5/Mechanical Wave - Hits Whoosh/Action Swish_HW 02.wav'
voice = downloads / 'Sonniss.com-GDC2026-GameAudioBundle1of5/344 Audio - Anime Fight Voices Vol. 2/VOXFem_Anime, Adult Warrior Breaths 40_344 Audio_Anime Fight Voices Vol 2.wav'
swords = downloads / 'alice_soundz-best-sword-sounds-slice-slash-crash-swoosh-etc-224090.mp3'
folder = Path('Assets/_Project/Audio/SoundEffects/Skills')
cache = {}

def read(path, start, end):
    if path not in cache:
        cache[path] = sf.read(path, always_2d=True)
    x, rate = cache[path]
    x = x[round(start*rate):round(end*rate)].copy()
    if rate == 96000:
        x = x[:len(x)//2*2].reshape(-1, 2, x.shape[1]).mean(axis=1)
        rate = 48000
    return x, rate

def fit_charge(x, rate, seconds):
    # Phase vocoder: fit the rising two-second section to the one-second wind-up without raising pitch.
    target = round(seconds * rate)
    nfft, hop = 2048, 256
    window = np.hanning(nfft)
    speed = len(x) / target
    channels = []
    for channel in x.T:
        padded = np.pad(channel, (nfft//2, nfft))
        frames = np.lib.stride_tricks.sliding_window_view(padded, nfft)[::hop]
        spectrum = np.fft.rfft(frames * window, axis=1).T
        steps = np.arange(0, spectrum.shape[1]-1, speed)
        phase = np.angle(spectrum[:, 0])
        advance = 2*np.pi*hop*np.arange(nfft//2+1)/nfft
        output = np.zeros((nfft//2+1, len(steps)), dtype=complex)
        for j, step in enumerate(steps):
            i = int(step); weight = step-i
            magnitude = (1-weight)*np.abs(spectrum[:, i]) + weight*np.abs(spectrum[:, i+1])
            output[:, j] = magnitude * np.exp(1j*phase)
            delta = np.angle(spectrum[:, i+1]) - np.angle(spectrum[:, i]) - advance
            delta -= 2*np.pi*np.round(delta/(2*np.pi))
            phase += advance + delta
        result = np.zeros(hop*(len(steps)-1)+nfft)
        norm = np.zeros_like(result)
        for j, frame in enumerate(np.fft.irfft(output.T, n=nfft, axis=1)):
            pos = j*hop
            result[pos:pos+nfft] += frame*window
            norm[pos:pos+nfft] += window*window
        result /= np.maximum(norm, 1e-9)
        result = result[nfft//2:nfft//2+target]
        channels.append(np.pad(result, (0, max(0, target-len(result))))[:target])
    return np.column_stack(channels)

def write(name, source, start, end, volume, charge=False):
    x, rate = read(source, start, end)
    if charge:
        x = fit_charge(x, rate, 1.0)
    x -= x.mean(axis=0)
    attack = round((.012 if charge else .003)*rate)
    release = min(round((.015 if charge else .035)*rate), len(x)//4)
    x[:attack] *= np.linspace(0, 1, attack)[:, None]
    x[-release:] *= np.linspace(1, 0, release)[:, None]
    x *= 10**(-6/20) / np.max(np.abs(x))
    path = folder / name
    sf.write(path, x, rate, subtype='PCM_16')
    meta = path.with_suffix('.wav.meta')
    if not meta.exists():
        template = (folder / 'Swordswoman_Dash_Whoosh.wav.meta').read_text()
        old = next(line for line in template.splitlines() if line.startswith('guid:'))
        meta.write_text(template.replace(old, 'guid: '+uuid.uuid4().hex))
    decoded, _ = sf.read(path, always_2d=True)
    assert np.max(np.abs(decoded)) < .502
    assert np.max(np.abs(decoded[[0,-1]])) <= 1/32768
    return dict(name=name, source=str(source), source_range=[start,end], duration=len(decoded)/rate,
                guid=next(line.split(': ')[1] for line in meta.read_text().splitlines() if line.startswith('guid:')),
                volume=volume, pitch_preserving_time_fit=charge)

report = [
    write('Swordswoman_DoubleSlash_Swing1.wav', swishes, .15, .21, .45),
    write('Swordswoman_DoubleSlash_Swing2.wav', swishes, 1.05, 1.11, .45),
    write('Swordswoman_HurtVoice.wav', voice, 0, .913, .35),
    write('Swordswoman_Drawing_Charge.wav', swords, 3, 5, .4, charge=True),
    write('Swordswoman_Drawing_Release.wav', swords, 5, 6, .5),
    write('Swordswoman_Drawing_Hit.wav', swords, 26.28, 27, .25),
    write('Swordswoman_DoubleSlash_Hit.wav', swords, 32.48, 32.57, .4)]
Path('Artifacts/AudioEditing/SwordswomanSkills-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
