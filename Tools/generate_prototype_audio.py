"""Generate original, deterministic placeholder effects; replace their AudioClips in PrototypeAudio.asset.
Python standard library only. Not recorded voices or externally sourced sound effects.
"""
from pathlib import Path
import math
import random
import struct
import wave

DEST = Path(__file__).resolve().parents[1] / 'Assets/Audio'
RATE = 24000


def generate(name, duration, kind, seed):
    rng = random.Random(seed)
    filtered = 0.0
    samples = []
    for i in range(round(duration * RATE)):
        t = i / RATE
        u = t / duration
        white = rng.uniform(-1, 1)
        filtered = filtered * .91 + white * .09
        edge = min(1, t / .006, (duration - t) / .025)
        if kind == 'step':
            value = (math.sin(2 * math.pi * 85 * t) * .55 + filtered * 2) * math.exp(-23 * t)
        elif kind == 'ghost_step':
            value = (math.sin(2 * math.pi * 58 * t) * .65 + filtered * 1.7) * math.exp(-15 * t)
        elif kind == 'loop':
            breath = (.5 + .5 * math.sin(2 * math.pi * u - math.pi / 2)) ** 1.5
            value = (filtered * 1.8 + math.sin(2 * math.pi * 60 * t) * .16 + math.sin(2 * math.pi * 90 * t) * .08) * (.18 + .82 * breath)
        elif kind == 'start':
            phase = 2 * math.pi * (180 * t - 65 * t * t)
            value = (math.sin(phase) * .35 + filtered * 1.4) * math.sin(math.pi * u) ** .6
        elif kind == 'end':
            value = math.sin(2 * math.pi * (110 * t + 90 * t * t)) * .45 * math.sin(math.pi * u)
        elif kind == 'open':
            value = (math.sin(2 * math.pi * 170 * t + 3 * math.sin(2 * math.pi * 23 * t)) * .3 + filtered * .8) * math.sin(math.pi * u)
        elif kind == 'throw':
            value = filtered * 2.5 * math.sin(math.pi * u) ** 2
        else:
            value = (math.sin(2 * math.pi * 105 * t) * .5 + filtered * 2.2) * math.exp(-19 * t)
        samples.append(value * max(0, edge))
    peak = max(abs(value) for value in samples) or 1
    pcm = b''.join(struct.pack('<h', round(value / peak * .65 * 32767)) for value in samples)
    with wave.open(str(DEST / (name + '.wav')), 'wb') as output:
        output.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        output.writeframes(pcm)


if __name__ == '__main__':
    DEST.mkdir(parents=True, exist_ok=True)
    clips = [('PlayerStep', .18, 'step'), ('GhostStep', .24, 'ghost_step'), ('GhostLoop', 3, 'loop'),
             ('HuntStart', .75, 'start'), ('HuntEnd', .45, 'end'), ('DoorOpen', .5, 'open'),
             ('DoorClose', .3, 'impact'), ('Throw', .2, 'throw'), ('Impact', .25, 'impact')]
    for seed, (name, duration, kind) in enumerate(clips):
        generate(name, duration, kind, seed)
    print('Generated nine mono 24 kHz prototype clips in Assets/Audio.')
