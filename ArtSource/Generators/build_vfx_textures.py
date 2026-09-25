"""Soul Hunter VFX sprites v2 (modern, Soul Hunter style) - procedural, no external art.

White-core sprites with alpha, tinted per effect in Unity (particle colour): soul flame, slash arc, magic circle,
impact star, soul wisp, lightning bolt, shockwave ring and sparkle. Also renders a tinted concept sheet for approval.

Run: blender --background --python ArtSource/Generators/build_vfx_textures.py -- <output folder> [--sheet]
"""
import math
import os
import sys

import bpy
import numpy as np

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = args[0] if args else os.path.join(os.getcwd(), 'vfx_v2')
N = 256
u = (np.arange(N) + 0.5) / N * 2 - 1
X, Y = np.meshgrid(u, -u)   # Y up
R = np.sqrt(X * X + Y * Y)
A = np.arctan2(Y, X)
rng = np.random.default_rng(7)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def noise(scale, seed):
    r = np.random.default_rng(seed)
    g = r.random((scale + 1, scale + 1))
    fx, fy = (X + 1) / 2 * scale, (Y + 1) / 2 * scale
    x0, y0 = np.floor(fx).astype(int).clip(0, scale - 1), np.floor(fy).astype(int).clip(0, scale - 1)
    tx, ty = fx - x0, fy - y0
    tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
    return (g[y0, x0] * (1 - tx) + g[y0, x0 + 1] * tx) * (1 - ty) + (g[y0 + 1, x0] * (1 - tx) + g[y0 + 1, x0 + 1] * tx) * ty


def fbm(seed, base=4):
    return sum(noise(base * 2 ** o, seed + o) * 0.5 ** o for o in range(4)) / 1.875


# Each sprite returns (core, alpha): core 0..1 = how white-hot the centre is; alpha = coverage.

def soul_flame():
    # Teardrop pointing up, with licking edges and a hot core low in the flame.
    y = (Y + 0.55) / 1.4
    width = 0.42 * np.sqrt(np.clip(1 - y, 0, 1)) * np.clip(y * 3, 0, 1) ** 0.5
    wobble = (fbm(11, 3) - 0.5) * 0.35 * y
    d = np.abs(X - wobble * 0.6) / np.maximum(width, 1e-3)
    body = smooth(1.0, 0.55, d) * (y > 0) * (y < 1)
    tongues = smooth(0.62, 0.8, fbm(3, 5) + (1 - y) * 0.35)
    alpha = np.clip(body * (0.55 + 0.45 * tongues), 0, 1)
    core = smooth(0.7, 0.0, np.sqrt((X / 0.22) ** 2 + ((Y + 0.2) / 0.35) ** 2)) * alpha
    return core, alpha


def slash_arc():
    # A crescent sweep: bright leading edge on the outside, fading tail along the angle.
    ring = smooth(0.12, 0.0, np.abs(R - 0.72)) * (R < 0.92)
    thickness = smooth(0.25, 0.0, np.abs(R - 0.68 + 0.08 * (A / math.pi)))
    sweep = np.clip((A + 0.2) / 2.6, 0, 1) * (A > -0.2) * (A < 2.6)
    tail = sweep ** 1.6
    alpha = np.clip((ring * 0.7 + thickness * 0.6) * tail, 0, 1)
    core = smooth(0.05, 0.0, np.abs(R - 0.78)) * tail
    return core, alpha


def magic_circle():
    rings = smooth(0.025, 0.0, np.abs(R - 0.92)) + smooth(0.02, 0.0, np.abs(R - 0.8)) + smooth(0.015, 0.0, np.abs(R - 0.46))
    # Rune ticks between the outer rings.
    ticks = (smooth(0.05, 0.0, np.abs(((A / (2 * math.pi) * 24) % 1) - 0.5) - 0.42) * (R > 0.81) * (R < 0.91))
    # Hexagram: two triangles.
    star = np.zeros_like(R)
    for k in range(6):
        a1 = math.pi / 2 + k * math.pi / 3
        a2 = a1 + 2 * math.pi / 3
        p1 = np.array([math.cos(a1), math.sin(a1)]) * 0.78
        p2 = np.array([math.cos(a2), math.sin(a2)]) * 0.78
        d = p2 - p1
        t = np.clip(((X - p1[0]) * d[0] + (Y - p1[1]) * d[1]) / (d @ d), 0, 1)
        dist = np.hypot(X - (p1[0] + t * d[0]), Y - (p1[1] + t * d[1]))
        star = np.maximum(star, smooth(0.018, 0.0, dist))
    glow = smooth(0.95, 0.3, R) * 0.15
    alpha = np.clip(rings + ticks * 0.9 + star * 0.8 + glow, 0, 1) * (R < 0.97)
    core = np.clip(rings * 0.6 + star * 0.4, 0, 1)
    return core, alpha


def impact_star():
    spikes = 0
    for k, (n, length, width) in enumerate(((4, 0.95, 0.07), (4, 0.6, 0.09))):
        a = (A + (math.pi / 4 if k else 0)) % (math.pi / 2) - math.pi / 4
        spike = smooth(width, 0.0, np.abs(np.sin(a)) * R) * smooth(length, 0.0, R)
        spikes = np.maximum(spikes, spike)
    core_disc = smooth(0.35, 0.0, R)
    alpha = np.clip(spikes + core_disc, 0, 1)
    core = smooth(0.2, 0.0, R)
    return core, alpha


def soul_wisp():
    # Comet: orb at the top, tail trailing down with a slight curl.
    head = smooth(0.32, 0.05, np.hypot(X, Y - 0.35))
    curl = X - 0.12 * np.sin((0.35 - Y) * 3)
    tail = smooth(0.18, 0.0, np.abs(curl) / np.clip((0.35 - Y) * 0.5 + 0.12, 0.05, 1)) * (Y < 0.35) * smooth(-0.95, -0.2, Y)
    alpha = np.clip(head + tail * 0.7, 0, 1)
    core = smooth(0.15, 0.0, np.hypot(X, Y - 0.35))
    return core, alpha


def lightning():
    path = np.zeros(N)
    x = 0.0
    for i in range(N):
        if i % 18 == 0:
            x = np.clip(x + rng.uniform(-0.25, 0.25), -0.45, 0.45)
        path[i] = x
    path = np.convolve(path, np.ones(9) / 9, mode='same')
    d = np.abs(X - path[:, None])
    bolt = smooth(0.05, 0.0, d)
    glow = smooth(0.35, 0.0, d) * 0.45
    alpha = np.clip(bolt + glow, 0, 1)
    core = smooth(0.02, 0.0, d)
    return core, alpha


def shock_ring():
    ring = smooth(0.14, 0.0, np.abs(R - 0.8)) * smooth(0.98, 0.9, R)
    inner = smooth(0.8, 0.2, R) * 0.2
    alpha = np.clip(ring + inner * (R < 0.8), 0, 1)
    core = smooth(0.03, 0.0, np.abs(R - 0.82))
    return core, alpha


def sparkle():
    a = np.abs(np.sin(A * 2))
    rays = smooth(0.08, 0.0, a * R) * smooth(1.0, 0.0, R)
    alpha = np.clip(rays + smooth(0.25, 0.0, R), 0, 1)
    return smooth(0.12, 0.0, R), alpha


SPRITES = dict(soul_flame=soul_flame, slash_arc=slash_arc, magic_circle=magic_circle, impact_star=impact_star,
               soul_wisp=soul_wisp, lightning=lightning, shock_ring=shock_ring, sparkle=sparkle)
TINTS = dict(soul_flame=(0.35, 0.9, 1.0), slash_arc=(0.75, 0.9, 1.0), magic_circle=(0.6, 0.45, 1.0), impact_star=(1.0, 0.85, 0.5),
             soul_wisp=(0.55, 0.75, 1.0), lightning=(0.7, 0.85, 1.0), shock_ring=(1.0, 0.8, 0.35), sparkle=(0.5, 1.0, 0.9))


def save(rgba, path):
    img = bpy.data.images.new(os.path.basename(path), rgba.shape[1], rgba.shape[0], alpha=True)
    img.pixels.foreach_set(rgba[::-1].astype(np.float32).ravel())
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    tiles = []
    for name, fn in SPRITES.items():
        core, alpha = fn()
        # Sprite: white-hot core blended to a light rim; Unity tints it by particle colour.
        rgba = np.stack([np.clip(0.75 + 0.25 * core, 0, 1)] * 3 + [np.clip(alpha, 0, 1)], axis=-1)
        save(rgba, os.path.join(OUT, f'SH10_Vfx2_{name}.png'))
        tint = np.array(TINTS[name])
        colour = (tint[None, None, :] * (1 - core[..., None]) + core[..., None]) * alpha[..., None]
        bg = np.array([0.04, 0.045, 0.06])
        tile = bg * (1 - alpha[..., None]) + colour
        tiles.append(np.concatenate([np.clip(tile, 0, 1), np.ones((N, N, 1))], axis=-1))
        print('SPRITE', name, flush=True)
    if '--sheet' in args:
        cols = 4
        rows = (len(tiles) + cols - 1) // cols
        sheet = np.zeros((rows * N, cols * N, 4))
        for i, t in enumerate(tiles):
            r, c = i // cols, i % cols
            sheet[r * N:(r + 1) * N, c * N:(c + 1) * N] = t
        save(sheet, os.path.join(OUT, 'vfx_v2_sheet.png'))
        print('SHEET', flush=True)
