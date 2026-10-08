"""Soul Hunter "pro" UI plates (owner, 2026-10-08: "pro type", raised 3D buttons, full brightness).

White-to-grey plates that the theme colours tint (Unity's ColorTint multiplies), so one picture serves every button
colour. Each plate is lit from above: a bright top edge, a smooth body gradient, a darker lower lip that reads as the
button's thickness, a thin dark rim, and a soft inner shadow at the bottom. Drawn at 4x and scaled down.
Run: python ArtSource/Generators/build_ui_pro_plates.py   (needs numpy, Pillow). Writes Assets/Art/UI/ThemePro/*.png.
Soul Hunter's own art.
"""
import os
import numpy as np
from PIL import Image

ROOT = os.environ.get("SOULHUNTER_ROOT", os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "UI", "ThemePro")
S = 4


def rounded_mask(w, h, r):
    """Signed distance (in pixels, negative inside) to a rounded rectangle of size w x h."""
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32) + 0.5
    qx = np.abs(xx - w / 2) - (w / 2 - r)
    qy = np.abs(yy - h / 2) - (h / 2 - r)
    outside = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    inside = np.minimum(np.maximum(qx, qy), 0)
    return outside + inside - r


def plate(w, h, r, lip, top_light=1.0, bottom_light=0.64, rim=0.30, gloss=0.20):
    W, H, R, L = w * S, h * S, r * S, lip * S
    yy = np.mgrid[0:H, 0:W][0].astype(np.float32)
    outer = rounded_mask(W, H, R)
    alpha = np.clip(0.5 - outer, 0, 1)
    # the face sits above the lip: a second rounded rect shortened at the bottom
    face_d = rounded_mask(W, H - L, R)
    face_d = np.vstack([face_d, np.full((L, W), 1e3, np.float32)]) if L > 0 else face_d
    face = np.clip(0.5 - face_d, 0, 1)
    t = yy / max(1, H - L)
    body = top_light + (bottom_light - top_light) * np.clip(t, 0, 1)
    lip_shade = 0.42  # the thickness under the face is darker
    lum = body * face + lip_shade * (1 - face)
    # bright top edge (light catching the bevel)
    edge = np.clip(1 - np.abs(face_d + 3 * S) / (3.0 * S), 0, 1) * np.clip(1 - t / 0.55, 0, 1) ** 1.5
    lum = lum + edge * 0.38
    # soft gloss across the upper part of the face, fading out smoothly (no hard line)
    band = np.clip(1 - t / 0.5, 0, 1) ** 2 * gloss * face
    lum = lum + band
    # thin dark rim round the whole plate
    rim_mask = np.clip(1 - np.abs(outer + 1.2 * S) / (1.3 * S), 0, 1)
    lum = lum * (1 - rim_mask * (1 - rim))
    lum = np.clip(lum, 0, 1)
    rgba = np.dstack([lum, lum, lum, alpha])
    img = Image.fromarray((rgba * 255).astype(np.uint8), "RGBA")
    return img.resize((w, h), Image.LANCZOS)


def panel(w, h, r):
    """A window: deep, a slightly lighter top, a gold-ready rim line inside the edge."""
    W, H, R = w * S, h * S, r * S
    yy = np.mgrid[0:H, 0:W][0].astype(np.float32)
    d = rounded_mask(W, H, R)
    alpha = np.clip(0.5 - d, 0, 1)
    t = yy / H
    lum = 1.0 - 0.18 * t
    inner_rim = np.clip(1 - np.abs(d + 7 * S) / (1.2 * S), 0, 1)
    lum = lum + inner_rim * 0.25
    outer_rim = np.clip(1 - np.abs(d + 1.5 * S) / (1.5 * S), 0, 1)
    lum = lum * (1 - outer_rim * 0.55)
    lum = np.clip(lum, 0, 1)
    img = Image.fromarray((np.dstack([lum, lum, lum, alpha]) * 255).astype(np.uint8), "RGBA")
    return img.resize((w, h), Image.LANCZOS)


def inset(w, h, r):
    """A recessed groove (slider track, empty slot): darker at the top, a light lip at the bottom edge."""
    W, H, R = w * S, h * S, r * S
    yy = np.mgrid[0:H, 0:W][0].astype(np.float32)
    d = rounded_mask(W, H, R)
    alpha = np.clip(0.5 - d, 0, 1)
    t = yy / H
    lum = 0.55 + 0.35 * t
    top_shadow = np.clip(1 - np.abs(d + 2 * S) / (3 * S), 0, 1) * np.clip(1 - t / 0.5, 0, 1)
    lum = lum - top_shadow * 0.35
    bottom_lip = np.clip(1 - np.abs(d + 1.5 * S) / (1.5 * S), 0, 1) * np.clip((t - 0.6) / 0.4, 0, 1)
    lum = np.clip(lum + bottom_lip * 0.45, 0, 1)
    img = Image.fromarray((np.dstack([lum, lum, lum, alpha]) * 255).astype(np.uint8), "RGBA")
    return img.resize((w, h), Image.LANCZOS)


def knob(size):
    """A round raised knob: light from above, a soft rim."""
    N = size * S
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32) + 0.5
    r = np.hypot(xx - N / 2, yy - N / 2) / (N / 2)
    alpha = np.clip((1 - r) * N / 2, 0, 1)
    t = yy / N
    lum = 1.0 - 0.35 * t
    hl = np.exp(-(((xx - N * 0.42) / (N * 0.22)) ** 2 + ((yy - N * 0.3) / (N * 0.14)) ** 2)) * 0.25
    rim = np.clip(1 - np.abs(r - 0.94) / 0.06, 0, 1)
    lum = np.clip((lum + hl) * (1 - rim * 0.55), 0, 1)
    img = Image.fromarray((np.dstack([lum, lum, lum, alpha]) * 255).astype(np.uint8), "RGBA")
    return img.resize((size, size), Image.LANCZOS)


def tickbox(size, r, rim_px):
    """An empty tick box: a light raised rim round a dark recessed square."""
    N, R_, RIM = size * S, r * S, rim_px * S
    yy = np.mgrid[0:N, 0:N][0].astype(np.float32)
    d = rounded_mask(N, N, R_)
    alpha = np.clip(0.5 - d, 0, 1)
    t = yy / N
    inside = 0.18 + 0.22 * t                       # dark well, lighter towards the bottom
    rim = np.clip((d + RIM) / (0.6 * S) + 0.5, 0, 1)  # 1 on the rim band, 0 inside
    rim_lum = 1.0 - 0.25 * t
    lum = inside * (1 - rim) + rim_lum * rim
    img = Image.fromarray((np.dstack([lum, lum, lum, alpha]) * 255).astype(np.uint8), "RGBA")
    return img.resize((size, size), Image.LANCZOS)


def tick(size, width):
    """A bold tick mark (white; tinted by the theme), with round ends."""
    N, Wd = size * S, width * S
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32) + 0.5
    pts = [(0.16, 0.54), (0.42, 0.78), (0.86, 0.24)]
    d = np.full((N, N), 1e9, np.float32)
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        ax, ay, bx, by = ax * N, ay * N, bx * N, by * N
        vx, vy = bx - ax, by - ay
        t = np.clip(((xx - ax) * vx + (yy - ay) * vy) / (vx * vx + vy * vy), 0, 1)
        d = np.minimum(d, np.hypot(xx - (ax + t * vx), yy - (ay + t * vy)))
    alpha = np.clip(Wd / 2 - d + 0.5, 0, 1)
    one = np.ones_like(alpha)
    img = Image.fromarray((np.dstack([one, one, one, alpha]) * 255).astype(np.uint8), "RGBA")
    return img.resize((size, size), Image.LANCZOS)


def mono_icons():
    """One-colour copies of the theme's button icons (white; the button tints them like its label).
    Inner dark lines stay as a lighter cut, so a coin or a gear keeps its detail."""
    src = os.path.join(ROOT, "Assets", "Art", "UI", "Theme")
    for f in sorted(os.listdir(src)):
        if not (f.startswith("icon_") and f.endswith(".png")):
            continue
        im = np.asarray(Image.open(os.path.join(src, f)).convert("RGBA"), np.float32) / 255.0
        lum = im[..., :3] @ np.array([0.299, 0.587, 0.114], np.float32)
        alpha = im[..., 3] * (0.4 + 0.6 * np.clip(lum / 0.55, 0, 1))
        one = np.ones_like(lum)
        out = np.dstack([one, one, one, alpha])
        path = os.path.join(OUT, "mono_" + f[len("icon_"):])
        Image.fromarray((out * 255).astype(np.uint8), "RGBA").save(path)
        print("wrote", path)


PLATES = {
    # name: (builder, args, 9-slice border l,b,r,t in px; written to plates.txt for the importer)
    "pro_button": (plate, (384, 160, 40, 12), (52, 56, 52, 52)),
    "pro_chip": (plate, (256, 112, 32, 8, 1.0, 0.74, 0.30, 0.18), (40, 40, 40, 40)),
    "pro_card": (plate, (320, 320, 30, 6, 1.0, 0.86, 0.35, 0.10), (44, 44, 44, 44)),
    "pro_panel": (panel, (512, 512, 36), (60, 60, 60, 60)),
    "pro_row": (plate, (256, 96, 18, 4, 1.0, 0.86, 0.40, 0.08), (28, 30, 28, 28)),
    "pro_inset": (inset, (128, 48, 24), (26, 22, 26, 22)),
    "pro_track": (plate, (192, 96, 48, 6, 1.0, 0.72, 0.30, 0.16), (50, 50, 50, 50)),
    "pro_knob": (knob, (96,), (0, 0, 0, 0)),
    "pro_tickbox": (tickbox, (96, 18, 9), (0, 0, 0, 0)),
    "pro_tick": (tick, (96, 16), (0, 0, 0, 0)),
}

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    lines = []
    for name, (fn, args, border) in PLATES.items():
        img = fn(*args)
        path = os.path.join(OUT, name + ".png")
        img.save(path)
        lines.append(f"{name} {border[0]} {border[1]} {border[2]} {border[3]}")
        print("wrote", path, img.size)
    open(os.path.join(OUT, "plates.txt"), "w").write("\n".join(lines))
    mono_icons()
