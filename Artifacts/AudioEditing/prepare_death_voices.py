import sys, json
from pathlib import Path
sys.path.insert(0, 'Artifacts/AudioTools/python')
import numpy as np
import soundfile as sf

def weighted_rms(x, sr):
    n = round(len(x) * 48000 / sr)
    y = np.interp(np.arange(n) / 48000, np.arange(len(x)) / sr, x)
    y = np.pad(y, (0, 24000))
    for b, a in [([1.53512485958697, -2.69169618940638, 1.19839281085285], [-1.69065929318241, .73248077421585]), ([1., -2., 1.], [-1.99004745483398, .99007225036621])]:
        z = np.zeros_like(y)
        x1 = x2 = y1 = y2 = 0.
        for i, sample in enumerate(y):
            value = b[0]*sample+b[1]*x1+b[2]*x2-a[0]*y1-a[1]*y2
            z[i] = value
            x2,x1,y2,y1 = x1,sample,y1,value
        y = z
    return np.sqrt(np.sum(y*y)/n)

clips = []
for character, name in [('Dosa', 'yell9'), ('Monk', 'yell6')]:
    source = Path('C:/Users/jeonghuncomputer/Downloads/yelling sounds') / (name+'.wav')
    x,sr = sf.read(source, always_2d=True)
    x = x.mean(axis=1)
    w = round(sr*.01)
    env = np.array([np.sqrt(np.mean(x[i:i+w]**2)) for i in range(0,len(x),w)])
    active = np.where(env > env.max()*.06)[0]
    start = max(0,active[0]*w-round(sr*.015))
    end = min(len(x),(active[-1]+1)*w+round(sr*.045))
    x = x[start:end].copy()
    fi,fo = round(sr*.003),round(sr*.025)
    x[:fi] *= np.linspace(0,1,fi)
    x[-fo:] *= np.linspace(1,0,fo)
    clips.append(dict(character=character,source=str(source),sr=sr,x=x,start=start/sr,end=end/sr,rms=weighted_rms(x,sr)))

target = 10**(-22/20)
largest_peak = max(abs(c['x']).max()*target/c['rms'] for c in clips)
target *= min(1., 10**(-12/20)/largest_peak)
report = []
for c in clips:
    gain = target/c['rms']
    out = Path('Assets/_Project/Audio/SoundEffects/Impacts')/(c['character']+'_Death.wav')
    sf.write(out,c['x']*gain,c['sr'],subtype='PCM_16')
    report.append(dict(character=c['character'],source=c['source'],output=str(out),source_range=[c['start'],c['end']],duration=len(c['x'])/c['sr'],gain_db=float(20*np.log10(gain)),weighted_rms_db=float(20*np.log10(target)),peak_dbfs=float(20*np.log10(abs(c['x']).max()*gain)),volume=.45))
Path('Artifacts/AudioEditing/DeathVoices-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
