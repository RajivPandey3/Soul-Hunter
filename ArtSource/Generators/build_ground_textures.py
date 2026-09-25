"""Soul Hunter stylized ground textures: seamless 512x512 detail textures (grass, dirt, cobble, flagstone, mud, planks,
snow, ice, sand, marble, crystal plates, carpet, stone, leaves, water, grain), generated procedurally with numpy so they
are Soul Hunter's own art. Each texture is a near-neutral detail map with a mean brightness of MEAN; the stage floor
materials tint it with the level palette colour (StageFloorImporter: BaseColor = palette / MEAN).

Run: blender --background --python ArtSource/Generators/build_ground_textures.py
Set SOULHUNTER_ROOT to run against an isolated copy of the project.
"""
import os
from pathlib import Path

import bpy
import numpy as np

ROOT = Path(os.environ.get("SOULHUNTER_ROOT") or Path(__file__).resolve().parent.parent.parent)
OUT = ROOT / 'Assets' / 'Art' / 'Textures' / 'Ground'
PREVIEW = ROOT / 'ArtSource' / 'SH10_Floors_v02' / 'Previews' / 'Textures'
N = 512
MEAN = 0.8
_u = np.linspace(0, 1, N, endpoint=False)
X, Y = np.meshgrid(_u, _u)


def value_noise(rng, cells, cells_y=None):
    """Tileable smooth value noise in [0, 1]; `cells_y` != `cells` stretches it (strokes, grain)."""
    cy = cells_y or cells
    grid = rng.random((cy, cells))
    fx, fy = X * cells, Y * cy
    x0, y0 = np.floor(fx).astype(int), np.floor(fy).astype(int)
    tx, ty = fx - x0, fy - y0
    tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
    x1, y1 = (x0 + 1) % cells, (y0 + 1) % cy
    x0, y0 = x0 % cells, y0 % cy
    a, b = grid[y0, x0], grid[y0, x1]
    c, d = grid[y1, x0], grid[y1, x1]
    return (a * (1 - tx) + b * tx) * (1 - ty) + (c * (1 - tx) + d * tx) * ty


def fbm(rng, cells=4, octaves=5, gain=0.5):
    total, amp, norm = np.zeros((N, N)), 1.0, 0.0
    for o in range(octaves):
        total += value_noise(rng, cells * 2 ** o) * amp
        norm += amp
        amp *= gain
    return total / norm


def voronoi(rng, count, jitter=0.8):
    """Tileable Voronoi on a jittered grid (even, stone-like cells): (F1, F2, cell id) in texture units."""
    side = max(2, int(round(np.sqrt(count))))
    gy, gx = np.meshgrid(np.arange(side), np.arange(side), indexing='ij')
    pts = np.stack([(gx.ravel() + 0.5 + (rng.random(side * side) - 0.5) * jitter) / side,
                    (gy.ravel() + 0.5 + (rng.random(side * side) - 0.5) * jitter) / side], axis=-1) % 1.0
    f1 = np.full((N, N), 9.0)
    f2 = np.full((N, N), 9.0)
    cid = np.zeros((N, N), dtype=int)
    for i, (px, py) in enumerate(pts):
        dx = np.abs(X - px); dx = np.minimum(dx, 1 - dx)
        dy = np.abs(Y - py); dy = np.minimum(dy, 1 - dy)
        d = np.sqrt(dx * dx + dy * dy)
        closer = d < f1
        f2 = np.where(closer, f1, np.minimum(f2, d))
        cid = np.where(closer, i, cid)
        f1 = np.where(closer, d, f1)
    return f1, f2, cid


def speckles(rng, density, size_cells):
    """Sparse bright/dark dots (pebbles, sparkles)."""
    n = value_noise(rng, size_cells)
    return np.clip((n - (1 - density)) / density, 0, 1)


def smooth_step(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def tint_variation(rng, amount, cells=3):
    """Slight warm/cool colour drift so a tinted texture is not a single flat hue."""
    w = (fbm(rng, cells, 3) - 0.5) * 2 * amount
    return np.stack([1 + w, np.ones_like(w), 1 - w], axis=-1)


# ---------- patterns: return luminance (N, N) and optional colour drift ----------

def grass(rng):
    # Hand-painted grass: soft light/dark clumps, short vertical blade strokes, a few light tips.
    clumps = fbm(rng, 3, 4)
    strokes = value_noise(rng, 96, 24) * 0.6 + value_noise(rng, 160, 40) * 0.4
    lum = 0.74 + (clumps - 0.5) * 0.5 + (strokes - 0.5) * 0.22
    lum += speckles(rng, 0.025, 72) * 0.12
    return lum, tint_variation(rng, 0.12)


def dirt(rng):
    base = fbm(rng, 5, 5)
    f1, f2, cid = voronoi(rng, 60)
    keep = np.random.default_rng(1).random(cid.max() + 1)[cid] > 0.6
    pebble = smooth_step(0.022, 0.012, f1) * keep
    rim = (smooth_step(0.03, 0.022, f1) - pebble) * keep
    lum = 0.76 + (base - 0.5) * 0.45 + pebble * 0.14 - rim * 0.18 - speckles(rng, 0.05, 128) * 0.08
    return lum, tint_variation(rng, 0.06)


def cobble(rng):
    f1, f2, cid = voronoi(rng, 70)
    edge = f2 - f1
    mortar = smooth_step(0.004, 0.018, edge)
    per = np.random.default_rng(int(rng.integers(1 << 30))).random(cid.max() + 1)[cid]
    dome = np.clip(1 - f1 / 0.07, 0, 1) ** 0.5
    lum = (0.55 + per * 0.3 + dome * 0.18 + (fbm(rng, 8, 4) - 0.5) * 0.2) * (0.35 + 0.65 * mortar)
    return lum, tint_variation(rng, 0.05)


def flagstone(rng):
    rows = 4
    ry = Y * rows
    row = np.floor(ry).astype(int)
    cols = 3
    rx = X * cols + (row % 2) * 0.5
    col = np.floor(rx).astype(int) % cols
    fx, fy = rx - np.floor(rx), ry - np.floor(ry)
    seam = np.minimum(np.minimum(fx, 1 - fx) * 3.0, np.minimum(fy, 1 - fy) * 4.0)
    grout = smooth_step(0.01, 0.05, seam)
    per = np.random.default_rng(int(rng.integers(1 << 30))).random((rows, cols))[row % rows, col]
    wear = fbm(rng, 6, 5)
    cracks = smooth_step(0.004, 0.0, np.abs(fbm(rng, 3, 4) - 0.5) - 0.002) * 0.25
    lum = (0.6 + per * 0.25 + (wear - 0.5) * 0.35 - cracks) * (0.4 + 0.6 * grout) + (1 - grout) * 0.05
    return lum, tint_variation(rng, 0.04)


def mud(rng):
    base = fbm(rng, 4, 5)
    wet = smooth_step(0.5, 0.7, fbm(rng, 4, 4))
    lum = 0.8 + (base - 0.5) * 0.35 - wet * 0.14 + speckles(rng, 0.03, 96) * wet * 0.15
    return lum, tint_variation(rng, 0.05)


def planks(rng):
    boards = 6
    by = Y * boards
    board = np.floor(by).astype(int)
    fy = by - board
    offset = np.random.default_rng(int(rng.integers(1 << 30))).random(boards)[board % boards]
    gx = (X + offset) % 1.0
    grain = value_noise(rng, 6, 96) * 0.7 + value_noise(rng, 12, 160) * 0.3
    gap = smooth_step(0.0, 0.06, np.minimum(fy, 1 - fy))
    joint = smooth_step(0.485, 0.498, np.abs(((gx * 2) % 1.0) - 0.5))   # board ends: dark seams
    end = 1.0 - joint
    per = np.random.default_rng(3).random(boards)[board % boards]
    lum = (0.62 + per * 0.22 + (grain - 0.5) * 0.25) * (0.35 + 0.65 * gap) * (0.6 + 0.4 * end)
    return lum, tint_variation(rng, 0.05)


def snow(rng):
    drifts = fbm(rng, 3, 5)
    sparkle = speckles(rng, 0.02, 256)
    lum = 0.9 + (drifts - 0.5) * 0.25 + sparkle * 0.2
    cool = (fbm(rng, 3, 3) - 0.5) * 0.12
    return lum, np.stack([1 - cool, np.ones_like(cool), 1 + cool], axis=-1)


def ice(rng):
    f1, f2, _ = voronoi(rng, 25)
    crack = smooth_step(0.006, 0.0, f2 - f1)
    streak = value_noise(rng, 8) * 0.5 + fbm(rng, 6, 4) * 0.5
    lum = 0.82 + (streak - 0.5) * 0.3 + crack * 0.25
    return lum, tint_variation(rng, 0.04)


def sand(rng):
    ripple = np.sin((Y * 14 + fbm(rng, 3, 4) * 1.5) * np.pi * 2) * 0.5 + 0.5
    grain = value_noise(rng, 256)
    lum = 0.8 + (ripple - 0.5) * 0.14 + (grain - 0.5) * 0.18 + (fbm(rng, 4, 4) - 0.5) * 0.2
    return lum, tint_variation(rng, 0.05)


def marble(rng):
    tiles = 2
    tx, ty = (X * tiles) % 1.0, (Y * tiles) % 1.0
    seam = smooth_step(0.0, 0.012, np.minimum(np.minimum(tx, 1 - tx), np.minimum(ty, 1 - ty)))
    veins = np.abs(np.sin((X * 3 + Y * 2 + fbm(rng, 3, 6) * 2.5) * np.pi))
    vein = smooth_step(0.08, 0.0, veins)
    cloud = fbm(rng, 4, 5)
    lum = (0.78 + (cloud - 0.5) * 0.3 - vein * 0.35) * (0.5 + 0.5 * seam)
    return lum, tint_variation(rng, 0.06)


def plates(rng):
    cells = 4
    fx, fy = (X * cells) % 1.0, (Y * cells) % 1.0
    edge = np.minimum(np.minimum(fx, 1 - fx), np.minimum(fy, 1 - fy))
    bevel = smooth_step(0.0, 0.06, edge)
    highlight = smooth_step(0.06, 0.03, edge) * (fx + fy < 1.0) * 0.3
    per = np.random.default_rng(int(rng.integers(1 << 30))).random((cells, cells))[
        (np.floor(Y * cells).astype(int)) % cells, (np.floor(X * cells).astype(int)) % cells]
    lum = (0.65 + per * 0.2 + (fbm(rng, 8, 4) - 0.5) * 0.15) * (0.35 + 0.65 * bevel) + highlight
    return lum, tint_variation(rng, 0.04)


def carpet(rng):
    weave = (np.sin(X * N * np.pi / 2) * np.sin(Y * N * np.pi / 2)) * 0.5 + 0.5
    diamond = np.abs(((X * 4) % 1.0) - 0.5) + np.abs(((Y * 4) % 1.0) - 0.5)
    motif = smooth_step(0.30, 0.26, np.abs(diamond - 0.35))
    lum = 0.78 + (weave - 0.5) * 0.08 + motif * 0.15 + (fbm(rng, 6, 3) - 0.5) * 0.12
    return lum, tint_variation(rng, 0.03)


def stone(rng):
    base = fbm(rng, 4, 6)
    f1, f2, _ = voronoi(rng, 18)
    crack = smooth_step(0.005, 0.0, f2 - f1) * 0.3
    lum = 0.78 + (base - 0.5) * 0.5 - crack - speckles(rng, 0.05, 160) * 0.12 + speckles(rng, 0.03, 200) * 0.12
    return lum, tint_variation(rng, 0.04)


def leaves(rng):
    f1, f2, cid = voronoi(rng, 90)
    per = np.random.default_rng(int(rng.integers(1 << 30))).random(cid.max() + 1)[cid]
    lum = 0.6 + per * 0.35 + smooth_step(0.03, 0.0, f1) * 0.1 - smooth_step(0.006, 0.0, f2 - f1) * 0.25
    return lum, tint_variation(rng, 0.12)


def water(rng):
    waves = np.sin((X * 10 + fbm(rng, 3, 4) * 2) * np.pi * 2) * np.sin((Y * 7 + fbm(rng, 3, 4)) * np.pi * 2)
    lum = 0.8 + waves * 0.08 + (fbm(rng, 4, 4) - 0.5) * 0.2 + speckles(rng, 0.02, 128) * 0.2
    return lum, tint_variation(rng, 0.05)


def grain(rng):
    lum = 0.8 + (fbm(rng, 6, 5) - 0.5) * 0.25 + (value_noise(rng, 256) - 0.5) * 0.08
    return lum, tint_variation(rng, 0.04)


PATTERNS = dict(grass=grass, dirt=dirt, cobble=cobble, flagstone=flagstone, mud=mud, planks=planks, snow=snow, ice=ice,
                sand=sand, marble=marble, plates=plates, carpet=carpet, stone=stone, leaves=leaves, water=water, grain=grain)


def save_png(rgb, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    img = bpy.data.images.new(path.stem, N, N, alpha=False)
    rgba = np.ones((N, N, 4), dtype=np.float32)
    rgba[..., :3] = np.clip(rgb, 0, 1)
    img.pixels.foreach_set(rgba[::-1].ravel())  # Blender stores rows bottom-up
    img.filepath_raw = str(path)
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)


def build(name, seed):
    rng = np.random.default_rng(seed)
    lum, drift = PATTERNS[name](rng)
    lum = lum * (MEAN / lum.mean())
    rgb = np.clip(lum[..., None] * drift, 0, 1)
    rgb *= MEAN / rgb.mean()
    save_png(rgb, OUT / f'SH10_Tex_{name}.png')
    print('TEXTURE_DONE', name, round(float(rgb.mean()), 3), flush=True)


if __name__ == '__main__':
    for i, name in enumerate(PATTERNS):
        build(name, 700 + i)
    print('TEXTURES_COMPLETE', len(PATTERNS), flush=True)
