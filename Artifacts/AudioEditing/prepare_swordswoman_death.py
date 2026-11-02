import json
import sys
from pathlib import Path

sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

SOURCE = Path(r'C:\Users\jeonghuncomputer\Downloads\1_dying.ogg')
HURT = Path('Assets/_Project/Audio/SoundEffects/Skills/Swordswoman_HurtVoice.wav')
JUMP = Path('Assets/_Project/Audio/SoundEffects/Movement/Swordswoman_JumpVoice.wav')
OUTPUT = Path('Assets/_Project/Audio/SoundEffects/Impacts/Swordswoman_Death.wav')


def mono(path, target_rate=48000):
    x, rate = sf.read(path, always_2d=True)
    x = x.mean(axis=1)
    if rate != target_rate:
        # All inputs are at 44.1/48/96 kHz. Pair-average 96 kHz before decimation.
        if rate == target_rate * 2:
            x = x[:len(x)//2*2].reshape(-1, 2).mean(axis=1)
        else:
            x = np.interp(np.arange(round(len(x)*target_rate/rate))*rate/target_rate,
                          np.arange(len(x)), x)
    return x, target_rate


def pitch_summary(x, rate):
    # YIN on 40 ms frames; exclude breath/noise and retain only periodic frames.
    x = x[::3]
    rate = rate / 3
    size, hop = round(.04*rate), round(.01*rate)
    low, high = round(rate/650), round(rate/85)
    frames = np.lib.stride_tricks.sliding_window_view(x, size)[::hop]
    rms = np.sqrt(np.mean(frames**2, axis=1))
    pitches = []
    confidences = []
    for frame, energy in zip(frames, rms):
        if energy < max(.002, float(rms.max())*.1):
            continue
        frame = frame-frame.mean()
        diff = np.array([np.sum((frame[:size-high]-frame[t:t+size-high])**2)
                         for t in range(1, high+1)])
        normalized = diff*np.arange(1, high+1)/np.maximum(np.cumsum(diff), 1e-12)
        minima = [i for i in range(low-1, high-1)
                  if normalized[i] < .35 and normalized[i] <= normalized[i-1]
                  and normalized[i] <= normalized[i+1]]
        if not minima:
            continue
        i = minima[0]
        lag = float(i+1)
        a, b, c = normalized[i-1:i+2]
        if abs(a-2*b+c) > 1e-12:
            lag += .5*(a-c)/(a-2*b+c)
        pitches.append(rate/lag)
        confidences.append(1-float(normalized[i]))
    if not pitches:
        return dict(median_hz=None, voiced_frames=0)
    return dict(median_hz=float(np.median(pitches)),
                quartiles_hz=np.percentile(pitches, [25, 75]).tolist(),
                voiced_frames=len(pitches), confidence=float(np.median(confidences)))


def autocorrelation_summary(x, rate):
    x = x[::3]
    rate = rate/3
    size, hop = round(.04*rate), round(.01*rate)
    frames = np.lib.stride_tricks.sliding_window_view(x, size)[::hop]
    energy = np.sqrt(np.mean(frames**2, axis=1))
    frequencies, quality = [], []
    for frame, rms in zip(frames, energy):
        if rms < energy.max()*.15:
            continue
        frame = (frame-frame.mean())*np.hanning(size)
        spectrum = np.fft.rfft(frame, n=2048)
        corr = np.fft.irfft(abs(spectrum)**2, n=2048)[:size]
        corr /= max(corr[0], 1e-12)
        low, high = round(rate/600), round(rate/100)
        peaks = [i for i in range(low, high) if corr[i]>corr[i-1] and corr[i]>corr[i+1]]
        if not peaks:
            continue
        lag = max(peaks, key=lambda i: corr[i])
        if corr[lag] < .08:
            continue
        frequencies.append(rate/lag)
        quality.append(corr[lag])
    return dict(median_hz=float(np.median(frequencies)), periodicity=float(np.median(quality)),
                frames=len(frequencies), note='Approximate pitch proxy for breathy/unvoiced reference')


def stretch_to(x, target):
    # Phase-vocoder time stretch to retain the death performance's duration.
    nfft, hop = 2048, 256
    window = np.hanning(nfft)
    padded = np.pad(x, (nfft//2, nfft))
    frames = np.lib.stride_tricks.sliding_window_view(padded, nfft)[::hop]
    spectrum = np.fft.rfft(frames*window, axis=1).T
    steps = np.arange(0, spectrum.shape[1]-1, len(x)/target)
    phase = np.angle(spectrum[:, 0])
    advance = 2*np.pi*hop*np.arange(nfft//2+1)/nfft
    output = np.zeros((nfft//2+1, len(steps)), dtype=complex)
    for j, step in enumerate(steps):
        i = int(step)
        weight = step-i
        magnitude = (1-weight)*abs(spectrum[:, i])+weight*abs(spectrum[:, i+1])
        output[:, j] = magnitude*np.exp(1j*phase)
        delta = np.angle(spectrum[:, i+1])-np.angle(spectrum[:, i])-advance
        delta -= 2*np.pi*np.round(delta/(2*np.pi))
        phase += advance+delta
    result = np.zeros(hop*(len(steps)-1)+nfft)
    norm = np.zeros_like(result)
    for j, frame in enumerate(np.fft.irfft(output.T, n=nfft, axis=1)):
        pos = j*hop
        result[pos:pos+nfft] += frame*window
        norm[pos:pos+nfft] += window*window
    result /= np.maximum(norm, 1e-9)
    result = result[nfft//2:nfft//2+target]
    return np.pad(result, (0, max(0, target-len(result))))[:target]


def active_rms(x, rate):
    size = round(.02*rate)
    frames = x[:len(x)//size*size].reshape(-1, size)
    energy = np.mean(frames**2, axis=1)
    return float(np.sqrt(np.mean(energy[energy > energy.max()*.04])))


source, rate = mono(SOURCE)
hurt, _ = mono(HURT)
jump, _ = mono(JUMP)
report = {name: pitch_summary(x, rate) for name, x in [('source', source), ('hurt', hurt), ('jump', jump)]}
report['autocorrelation'] = {name: autocorrelation_summary(x, rate)
    for name, x in [('source', source), ('hurt', hurt), ('jump', jump)]}
if '--write' in sys.argv:
    target_hz = report['autocorrelation']['jump']['median_hz']
    ratio = target_hz/report['source']['median_hz']
    # Match the processed voiced region, since removing the leading breath changes
    # which source frames contribute to the measured median.
    candidates = []
    for semitone_offset in [0, -.5, -1, -1.5, -2]:
        trial_ratio = ratio*2**(semitone_offset/12)
        trial = stretch_to(np.interp(np.arange(0, len(source), trial_ratio),
                                    np.arange(len(source)), source), len(source))
        active = np.flatnonzero(abs(trial) > abs(trial).max()*.012)
        a = max(0, int(active[0])-round(.015*rate))
        b = min(len(trial), int(active[-1])+round(.1*rate))
        measured = pitch_summary(trial[a:b], rate)['median_hz']
        if measured is not None:
            candidates.append((abs(np.log2(measured/target_hz)), trial_ratio))
    ratio = min(candidates)[1]
    shifted = np.interp(np.arange(0, len(source), ratio), np.arange(len(source)), source)
    output = stretch_to(shifted, len(source))
    active = np.flatnonzero(abs(output) > abs(output).max()*.012)
    start = max(0, int(active[0]) - round(.015*rate))
    end = min(len(output), int(active[-1])+round(.1*rate))
    output = output[start:end].copy()
    output -= output.mean()
    attack, release = round(.008*rate), round(.07*rate)
    output[:attack] *= np.linspace(0, 1, attack)
    output[-release:] *= np.linspace(1, 0, release)
    target_rms = np.sqrt(active_rms(hurt, rate)*active_rms(jump, rate))
    gain = min(target_rms/active_rms(output, rate), .5/abs(output).max())
    output *= gain
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sf.write(OUTPUT, output, rate, subtype='PCM_16')
    decoded, _ = sf.read(OUTPUT)
    assert np.all(np.isfinite(decoded)) and abs(decoded).max() <= .501
    assert abs(decoded[0]) <= 1/32768 and abs(decoded[-1]) <= 1/32768
    report['processing'] = dict(output=str(OUTPUT), source=str(SOURCE),
        pitch_shift_semitones=float(12*np.log2(ratio)), target_hz=target_hz,
        duration_preserved_before_silence_trim=True, trimmed_source_seconds=[start/rate, end/rate],
        duration_seconds=len(output)/rate, sample_rate=rate, gain_db=float(20*np.log10(gain)),
        active_rms_db=float(20*np.log10(active_rms(output, rate))),
        peak_dbfs=float(20*np.log10(abs(output).max())), output_pitch=pitch_summary(output, rate),
        limitation='Breathy existing voices have no reliable YIN fundamental. Jump autocorrelation is an approximate register target, not an exact identity or listening match.')
    Path('Artifacts/AudioEditing/SwordswomanDeath-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
