#!/usr/bin/env python3
"""Làm phẳng cao độ của một giọng nói (đọc như máy, một nốt), giữ nguyên lời và âm sắc.

Dùng: flatten_pitch.py vào.wav ra.wav [mức]   (mức 0 = phẳng hoàn toàn, 0.2 = còn chút nhấn, 1 = giữ nguyên)
Cần: pip install numpy pyworld soundfile
"""
import sys
import numpy as np
import pyworld as pw
import soundfile as sf

src, dst = sys.argv[1], sys.argv[2]
keep = float(sys.argv[3]) if len(sys.argv) > 3 else 0.0

x, fs = sf.read(src)
if x.ndim > 1:
    x = x.mean(axis=1)
x = x.astype(np.float64)

f0, t = pw.harvest(x, fs)            # cao độ theo thời gian
sp = pw.cheaptrick(x, f0, t, fs)     # âm sắc (hình dạng phổ)
ap = pw.d4c(x, f0, t, fs)            # độ ồn / hơi

voiced = f0 > 0
center = float(np.median(f0[voiced])) if voiced.any() else 150.0
flat = np.zeros_like(f0)
# keep = 0: mọi chỗ có tiếng đều đúng một nốt; keep > 0: giữ lại một phần đường cong cao độ
flat[voiced] = center * np.power(f0[voiced] / center, keep)

y = pw.synthesize(flat, sp, ap, fs)
y = y / max(1e-6, np.max(np.abs(y))) * 0.9
sf.write(dst, y, fs)
