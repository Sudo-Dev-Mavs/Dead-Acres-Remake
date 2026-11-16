#!/usr/bin/env python3
"""Genera los sonidos y la música provisionales de Dead Acres (WAV mono de 16 bits).

Todo es síntesis: no hay grabaciones ni material de terceros. Sirve para tener el juego
sonando mientras no haya audio definitivo; para cambiar un sonido basta con sustituir su
.wav en Assets/_Project/Audio conservando el nombre.

Uso:  python3 Tools/generate_placeholder_audio.py
"""
import os
import wave

import numpy as np

RATE = 22050
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Audio")
rng = np.random.default_rng(7)


def t(seconds):
    return np.arange(int(RATE * seconds)) / RATE


def sine(freq, time, phase=0.0):
    return np.sin(2 * np.pi * freq * time + phase)


def sweep(f0, f1, time):
    """Seno con la frecuencia yendo de f0 a f1 de forma lineal."""
    duration = time[-1] if len(time) > 1 else 1.0
    freq = f0 + (f1 - f0) * time / duration
    return np.sin(2 * np.pi * np.cumsum(freq) / RATE)


def noise(time):
    return rng.uniform(-1.0, 1.0, len(time))


def lowpass(signal, amount):
    """Filtro de un polo: amount cerca de 1 = más grave."""
    out = np.empty_like(signal)
    acc = 0.0
    for i, sample in enumerate(signal):
        acc = amount * acc + (1.0 - amount) * sample
        out[i] = acc
    return out


def fade(signal, attack=0.005, release=0.02):
    n = len(signal)
    a = min(n // 2, int(RATE * attack))
    r = min(n // 2, int(RATE * release))
    env = np.ones(n)
    if a > 0:
        env[:a] = np.linspace(0.0, 1.0, a)
    if r > 0:
        env[-r:] = np.linspace(1.0, 0.0, r)
    return signal * env


def save(name, signal, peak=0.8):
    signal = np.asarray(signal, dtype=np.float64)
    top = np.max(np.abs(signal))
    if top > 0:
        signal = signal / top * peak
    data = (signal * 32767).astype("<i2")
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(data.tobytes())
    print(f"{name}.wav  {len(data) / RATE:.2f} s")


def pluck(freq, seconds, decay):
    time = t(seconds)
    return fade((sine(freq, time) + 0.3 * sine(freq * 2, time)) * np.exp(-time * decay))


def sequence(parts):
    return np.concatenate(parts)


# ---------- efectos

time = t(0.28)
save("shot", fade(noise(time) * np.exp(-time * 38) + 0.9 * sine(85, time) * np.exp(-time * 22), 0.001, 0.03), 0.9)

time = t(0.06)
save("hit", fade(sine(1250, time) * np.exp(-time * 60), 0.001, 0.01), 0.5)

time = t(0.55)
growl = sweep(190, 60, time) + 0.5 * sweep(285, 90, time)
save("zombie_death", fade(growl * (0.7 + 0.3 * sine(23, time)) * np.exp(-time * 3.5) + 0.15 * lowpass(noise(time), 0.9), 0.01, 0.1), 0.7)

time = t(0.22)
save("till", fade(lowpass(noise(time), 0.82) * np.exp(-time * 12), 0.005, 0.05), 0.7)

time = t(0.12)
save("plant", fade(sweep(480, 720, time) * np.exp(-time * 14)), 0.5)

save("harvest", sequence([pluck(660, 0.09, 22), pluck(880, 0.16, 16)]), 0.6)

time = t(0.16)
save("place", fade(sine(115, time) * np.exp(-time * 28) + 0.4 * noise(time) * np.exp(-time * 90), 0.001, 0.03), 0.8)

save("coin", sequence([pluck(1318, 0.07, 18), pluck(1760, 0.28, 12)]), 0.5)

time = t(0.09)
save("blip", fade(sine(880, time) * np.exp(-time * 20)), 0.4)

time = t(0.24)
save("hurt", fade(sweep(210, 110, time) * np.exp(-time * 9) + 0.5 * lowpass(noise(time), 0.6) * np.exp(-time * 25), 0.002, 0.05), 0.8)

time = t(0.45)
clang = sum(sine(f, time) * np.exp(-time * d) for f, d in ((310, 9), (517, 12), (811, 16), (1240, 22)))
save("rv_hit", fade(clang + 0.3 * noise(time) * np.exp(-time * 70), 0.001, 0.05), 0.7)

time = t(2.6)
swell = np.minimum(time / 1.2, 1.0) * np.exp(-np.maximum(time - 1.4, 0) * 1.6)
night = sweep(58, 44, time) + 0.5 * sine(110, time) + 0.35 * sine(130.8, time) + 0.25 * sine(164.8, time)
save("night_start", fade(night * swell, 0.05, 0.3), 0.8)

save("dawn", sequence([pluck(523.25, 0.22, 5), pluck(659.25, 0.22, 5), pluck(783.99, 0.22, 5), pluck(1046.5, 0.9, 3)]), 0.5)

time = t(1.5)
saw = 2 * ((time * 68) % 1.0) - 1
save("strong", fade(lowpass(saw, 0.75) * (0.6 + 0.4 * sine(11, time)) * np.minimum(time / 0.3, 1.0), 0.02, 0.4), 0.8)

time = t(0.14)
save("turret_shot", fade(noise(time) * np.exp(-time * 55) + 0.7 * sine(160, time) * np.exp(-time * 40), 0.001, 0.02), 0.8)

time = t(1.0)
boom = sweep(95, 35, time) * np.exp(-time * 4.5) + 0.8 * lowpass(noise(time), 0.93) * np.exp(-time * 6)
save("explosion", fade(boom + 0.5 * noise(time) * np.exp(-time * 45), 0.001, 0.2), 0.95)

time = t(0.11)
save("step", fade(lowpass(noise(time), 0.9) * np.exp(-time * 38) + 0.5 * sine(70, time) * np.exp(-time * 45), 0.002, 0.03), 0.5)


def click(freq, seconds):
    time = t(seconds)
    return fade(sine(freq, time) * np.exp(-time * 70) + 0.5 * noise(time) * np.exp(-time * 120), 0.001, 0.01)


save("reload", sequence([click(900, 0.07), np.zeros(int(RATE * 0.16)), click(1400, 0.06), np.zeros(int(RATE * 0.05)), click(650, 0.09)]), 0.6)

# ---------- música (bucles que empalman: cada frecuencia da un número entero de ciclos)

LOOP = 12.0


def loopable(freq):
    return round(freq * LOOP) / LOOP


def note_at(buffer, start, freq, seconds, volume, decay):
    freq = loopable(freq)
    time = t(seconds)
    voice = fade((sine(freq, time) + 0.25 * sine(freq * 2, time)) * np.exp(-time * decay), 0.01, 0.05) * volume
    first = int(start * RATE)
    # Lo que se sale por el final vuelve al principio: el bucle no corta ninguna nota.
    index = (first + np.arange(len(voice))) % len(buffer)
    np.add.at(buffer, index, voice)


# Día: arpegio en Do mayor pentatónico, tranquilo.
day = np.zeros(int(RATE * LOOP))
loop_time = t(LOOP)
beat = 0.5
pattern = [261.63, 392.00, 329.63, 392.00, 440.00, 392.00, 329.63, 293.66]
for i in range(int(LOOP / beat)):
    note_at(day, i * beat, pattern[i % len(pattern)], 0.9, 0.5, 3.5)
    if i % 8 == 0:
        note_at(day, i * beat, 130.81 if (i // 8) % 2 == 0 else 98.00, 3.6, 0.6, 0.8)
day += 0.12 * (sine(loopable(196.00), loop_time) + sine(loopable(261.63), loop_time)) * (0.6 + 0.4 * sine(loopable(0.25), loop_time))
save("music_day", day, 0.6)

# Noche: pedal grave en La menor con campanas sueltas.
night_loop = 0.6 * sine(loopable(55.0), loop_time) * (0.7 + 0.3 * sine(loopable(0.5), loop_time))
night_loop += 0.35 * sine(loopable(82.41), loop_time) * (0.6 + 0.4 * sine(loopable(1 / 3), loop_time, 1.3))
night_loop += 0.2 * sine(loopable(130.81), loop_time) * (0.5 + 0.5 * sine(loopable(1 / 6), loop_time, 2.1))
for start, freq in ((1.0, 659.25), (3.5, 622.25), (6.0, 523.25), (8.5, 440.0), (10.0, 493.88)):
    note_at(night_loop, start, freq, 2.4, 0.22, 1.6)
save("music_night", night_loop, 0.6)
