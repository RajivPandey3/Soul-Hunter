"""Soul Hunter icons without the round badge (owner, 2026-10-08: "circle wale icons bhi bina circle ke banao").

The 9 Power Up badges (build_powerup_icons.py) and the Bible, Cross, Duplicator and Spellbinder badges
(build_item_icons.py) sat on a round dark disc; the 30 approved HD item icons have none. This keeps each icon's own
picture and colour and drops the disc:
- Power Up glyphs: the same pictures (their gold and details unchanged), with a dark ink outline and a soft shadow
  in place of the disc, so they read on light cards and dark slots alike.
- Cross and Bible: their SH10 model renders with a soft glow, no disc.
- Duplicator and Spellbinder: the approved VFX v2 sprites with a dark halo behind them, so the light sprites read on
  light cards too.
Run: python ArtSource/Generators/build_icons_free.py [--out DIR]  (Pillow, numpy). Writes 512 px PNGs named like the
originals (SH10_PowerUp_<Id>.png, SH10_Icon_<Name>.png). Soul Hunter's own art.
"""
import os
import sys
import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, os.path.dirname(__file__))
import build_item_icons as items  # noqa: E402
import build_powerup_icons as pu  # noqa: E402

FINAL = 512
INK = np.array([28, 14, 10], np.float32) / 255.0


def _arr(im):
    return np.asarray(im.convert("RGBA"), np.float32) / 255.0


def _img(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "RGBA")


def glossy(glyph, colour):
    """The glyph exactly as drawn (its own gold and details), with a dark ink outline and a soft shadow instead of the
    badge, so it reads on light cards and dark slots alike. `colour` (the old badge rim) is not used: the picture stays."""
    shape = glyph.resize((FINAL, FINAL), Image.LANCZOS)
    w, h = shape.size
    a_img = shape.split()[3]
    outline_a = a_img.filter(ImageFilter.MaxFilter(13)).filter(ImageFilter.GaussianBlur(1))
    outline = Image.merge("RGBA", (*Image.new("RGB", (w, h), tuple(int(v * 255) for v in INK)).split(), outline_a))
    shadow_a = outline_a.filter(ImageFilter.GaussianBlur(10)).point(lambda v: int(v * 0.6))
    shadow = Image.merge("RGBA", (*Image.new("RGB", (w, h), (0, 0, 0)).split(), shadow_a))
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.paste(shadow, (0, 8), shadow)
    out = Image.alpha_composite(out, outline)
    return Image.alpha_composite(out, shape)


def free_powerup(fn):
    """Runs a build_powerup_icons glyph builder but keeps the glyph and its badge colour instead of the badge."""
    captured = {}

    def keep(glyph, rim, glow):
        captured["glyph"], captured["rim"] = glyph, rim
        return None

    old = pu.finish
    pu.finish = keep
    try:
        fn()
    finally:
        pu.finish = old
    return glossy(captured["glyph"], captured["rim"])


def free_model(raw_name, glow, lift):
    raw = Image.open(os.path.join(items.RAW, f"raw_{raw_name}.png")).convert("RGBA")
    s = int(FINAL * 0.9)
    raw = raw.resize((s, s), Image.LANCZOS)
    r, g, b, a = raw.split()
    rgb = Image.merge("RGB", (r, g, b)).point(lambda v: min(255, int(v * lift)))
    raw = Image.merge("RGBA", (*rgb.split(), a))
    out = Image.new("RGBA", (FINAL, FINAL), (0, 0, 0, 0))
    halo = Image.merge("RGBA", (*Image.new("RGB", raw.size, glow).split(), a.filter(ImageFilter.GaussianBlur(16)).point(lambda v: int(v * 0.8))))
    off = ((FINAL - s) // 2, (FINAL - s) // 2)
    out.paste(halo, off, halo)
    out.paste(raw, off, raw)
    return out


def free_vfx(layers):
    """Approved VFX sprites over a dark halo (so light sprites read on light cards). layers: (sprite, tint, scale, alpha, dx, dy)."""
    size = FINAL
    art = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    for name, tint, scale, alpha, dx, dy in layers:
        sp = items.sprite(name, tint, 1.0, alpha)
        s = int(size * scale)
        sp = sp.resize((s, s), Image.LANCZOS)
        art.paste(sp, ((size - s) // 2 + dx, (size - s) // 2 + dy), sp)
    a = art.split()[3]
    dark_a = a.filter(ImageFilter.MaxFilter(15)).filter(ImageFilter.GaussianBlur(14)).point(lambda v: min(255, int(v * 1.4)))
    dark = Image.merge("RGBA", (*Image.new("RGB", (size, size), (18, 10, 30)).split(), dark_a))
    return Image.alpha_composite(dark, art)


BUILDS = {
    "SH10_PowerUp_Area": lambda: free_powerup(pu.area),
    "SH10_PowerUp_Greed": lambda: free_powerup(pu.greed),
    "SH10_PowerUp_Omni": lambda: free_powerup(pu.omni),
    "SH10_PowerUp_Charm": lambda: free_powerup(pu.charm),
    "SH10_PowerUp_Defang": lambda: free_powerup(pu.defang),
    "SH10_PowerUp_Reroll": lambda: free_powerup(pu.reroll),
    "SH10_PowerUp_Skip": lambda: free_powerup(pu.skip),
    "SH10_PowerUp_Banish": lambda: free_powerup(pu.banish),
    "SH10_PowerUp_Preserve": lambda: free_powerup(pu.preserve),
    "SH10_Icon_Cross": lambda: free_model("Cross", (255, 190, 80), 1.9),
    "SH10_Icon_Bible": lambda: free_model("Bible", (200, 60, 90), 1.35),
    "SH10_Icon_Duplicator": lambda: free_vfx([("impact_star", (80, 170, 255), 0.8, 1.0, -70, 56),
                                              ("impact_star", (190, 240, 255), 1.0, 1.0, 50, -36),
                                              ("sparkle", (255, 255, 255), 0.45, 1.0, 50, -36)]),
    "SH10_Icon_Spellbinder": lambda: free_vfx([("magic_circle", (215, 150, 255), 0.9, 1.0, 0, 0),
                                               ("sparkle", (255, 230, 255), 0.42, 1.0, 0, 0)]),
}


def fill(im, pad=0.06):
    """Trims the empty space the old disc left, so the icon fills its slot like the HD item icons (same rule as their trim)."""
    a = np.asarray(im)[..., 3]
    ys, xs = np.where(a > 60)
    if len(xs) == 0:
        return im
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    side = max(x1 - x0, y1 - y0)
    p = int(side * pad)
    cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
    half = side // 2 + p
    box = Image.new("RGBA", (2 * half, 2 * half), (0, 0, 0, 0))
    box.paste(im.crop((cx - half, cy - half, cx + half, cy + half)), (0, 0))
    return box.resize((FINAL, FINAL), Image.LANCZOS)


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--out") + 1] if "--out" in sys.argv else items.OUT
    os.makedirs(out, exist_ok=True)
    for name, build in BUILDS.items():
        path = os.path.join(out, name + ".png")
        fill(build()).save(path)
        print("wrote", path)
