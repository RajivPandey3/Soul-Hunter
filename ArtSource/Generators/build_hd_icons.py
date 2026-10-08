"""Soul Hunter HD item icons, v2 (owner, 2026-10-07: "icon full hd mein hona chahiye", "brightness or contrast ka kami").

One look for every icon, made at 512 px:
- an obsidian badge with a raised (bevelled) rim; the rim colour says what it is: ember red = weapon, soul cyan = passive
  item, gold = Power Up;
- the item itself in its own colour (quick to recognise), shaded light-to-dark so it reads as 3D, with a thin dark
  outline so it stays crisp at 64 px;
- a soft ember glow behind the item.
Shapes are drawn at 2048 px and scaled down. Soul Hunter's own art (no VS art).
Run: python ArtSource/Generators/build_hd_icons.py --sample <out.png>   (the approval sheet; writes nothing in Assets)
"""
import math
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

OUT_SIZE = 512
BIG = 2048
K = BIG / 1.0

RIM = {"weapon": (230, 70, 45), "passive": (70, 210, 230), "powerup": (245, 190, 60)}
INK = (18, 10, 8)


# ------------------------------------------------------------------ drawing helpers (masks at BIG, shading at OUT_SIZE)

def mask():
    return Image.new("L", (BIG, BIG), 0)


def p(x, y):
    return (x * BIG, y * BIG)


def pts(seq):
    return [p(x, y) for x, y in seq]


def small(m):
    return m.resize((OUT_SIZE, OUT_SIZE), Image.LANCZOS)


def arr(im):
    return np.asarray(im, dtype=np.float32) / 255.0


def shade(m, colour, light=1.35, dark=0.62):
    """A part filled with its colour, lighter at the top and darker at the bottom, with a bright top-left rim."""
    a = arr(small(m))
    ys = np.linspace(0.0, 1.0, OUT_SIZE, dtype=np.float32)[:, None]
    rows = np.where(a.any(axis=1))[0]
    top, bottom = (rows.min(), rows.max()) if len(rows) else (0, OUT_SIZE - 1)
    t = np.clip((np.arange(OUT_SIZE)[:, None] - top) / max(1, bottom - top), 0, 1)
    factor = light + (dark - light) * t
    rgb = np.clip(np.array(colour, np.float32)[None, None, :] / 255.0 * factor[..., None], 0, 1)
    # bevel: where the mask ends a few pixels down-right, the edge catches light
    shifted = np.roll(np.roll(a, 4, axis=0), 4, axis=1)
    edge = np.clip(a - shifted, 0, 1)[..., None]
    rgb = rgb * (1 - edge * 0.6) + edge * 0.6
    out = np.dstack([rgb, a])
    return Image.fromarray((out * 255).astype(np.uint8), "RGBA")


def outline(parts_mask, width=9):
    """A dark outline around everything (keeps the icon crisp at small sizes)."""
    m = small(parts_mask).filter(ImageFilter.MaxFilter(width))
    ink = Image.new("RGBA", (OUT_SIZE, OUT_SIZE), INK + (0,))
    ink.putalpha(m)
    return ink


class Glyph:
    """Collects coloured parts; renders them shaded with one outline."""

    def __init__(self):
        self.parts = []
        self.union = mask()

    def part(self, colour, draw_fn, **shade_args):
        m = mask()
        draw_fn(ImageDraw.Draw(m))
        self.parts.append((m, colour, shade_args))
        self.union = Image.fromarray(np.maximum(np.asarray(self.union), np.asarray(m)))
        return self

    def detail(self, colour, draw_fn, alpha=1.0):
        """A flat detail line or shape on top (engraving, highlight), not outlined."""
        m = mask()
        draw_fn(ImageDraw.Draw(m))
        self.parts.append((m, colour, {"flat": alpha}))
        return self

    def render(self):
        out = Image.new("RGBA", (OUT_SIZE, OUT_SIZE), (0, 0, 0, 0))
        out = Image.alpha_composite(out, outline(self.union))
        for m, colour, args in self.parts:
            if "flat" in args:
                layer = Image.new("RGBA", (OUT_SIZE, OUT_SIZE), tuple(colour) + (0,))
                layer.putalpha(small(m).point(lambda v: int(v * args["flat"])))
            else:
                layer = shade(m, colour, **args)
            out = Image.alpha_composite(out, layer)
        return out


def badge(kind):
    """The obsidian badge with a raised rim in the category colour."""
    s = OUT_SIZE
    yy, xx = np.mgrid[0:s, 0:s].astype(np.float32)
    cx = cy = (s - 1) / 2
    r = np.hypot(xx - cx, yy - cy) / (s / 2)
    rim_col = np.array(RIM[kind], np.float32) / 255.0
    img = np.zeros((s, s, 4), np.float32)
    # face: obsidian, a little lighter towards the top
    face = r < 0.86
    base = np.array([0.075, 0.05, 0.06], np.float32)
    lift = np.clip(1.25 - (yy / s) * 0.5, 0.8, 1.3)[..., None]
    img[face, :3] = (base * lift)[face]
    img[face, 3] = 1
    # rim: bevel, light from the top-left
    ring = (r >= 0.86) & (r < 0.985)
    ang = np.arctan2(yy - cy, xx - cx)
    light = 0.55 + 0.45 * np.cos(ang + math.radians(135))
    img[ring, :3] = np.clip(rim_col * (0.45 + 0.75 * light[..., None]), 0, 1)[ring]
    img[ring, 3] = 1
    # inner groove between face and rim
    groove = (r >= 0.84) & (r < 0.87)
    img[groove, :3] = 0.03
    img[groove, 3] = 1
    # soft anti-aliased edge
    edge = np.clip((0.995 - r) / 0.012, 0, 1)
    img[..., 3] *= edge
    out = Image.fromarray((img * 255).astype(np.uint8), "RGBA")
    # ember glow in the middle of the face
    glow = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((s * 0.22, s * 0.22, s * 0.78, s * 0.78), fill=(255, 120, 40, 70))
    glow = glow.filter(ImageFilter.GaussianBlur(s * 0.08))
    face_mask = Image.fromarray(((r < 0.84) * 255).astype(np.uint8))
    glow.putalpha(Image.fromarray(np.minimum(np.asarray(glow.split()[3]), np.asarray(face_mask))))
    return Image.alpha_composite(out, glow)


GLYPH_SCALE = 1.3  # the item fills about three quarters of the badge face, so it reads at 64 px


def icon(kind, glyph):
    g = glyph.render()
    s = int(OUT_SIZE * GLYPH_SCALE)
    g = g.resize((s, s), Image.LANCZOS)
    o = (s - OUT_SIZE) // 2
    g = g.crop((o, o, o + OUT_SIZE, o + OUT_SIZE))
    # keep the item inside the face (it never covers the rim)
    yy, xx = np.mgrid[0:OUT_SIZE, 0:OUT_SIZE]
    r = np.hypot(xx - (OUT_SIZE - 1) / 2, yy - (OUT_SIZE - 1) / 2) / (OUT_SIZE / 2)
    face = np.clip((0.84 - r) / 0.02, 0, 1)
    alpha = (np.asarray(g.split()[3], np.float32) * face).astype(np.uint8)
    g.putalpha(Image.fromarray(alpha))
    return Image.alpha_composite(badge(kind), g)


# ------------------------------------------------------------------ the sample glyphs

def whip():
    g = Glyph()
    def lash(d):
        prev = None
        for i in range(60):
            t = i / 59
            x = 0.30 + 0.42 * t
            y = 0.70 - 0.42 * t + 0.10 * math.sin(t * math.pi * 2.2)
            w = 0.075 * (1 - t) + 0.025
            if prev: d.line([p(*prev), p(x, y)], fill=255, width=int(w * BIG))
            d.ellipse((p(x - w / 2, y - w / 2)[0], p(x - w / 2, y - w / 2)[1], p(x + w / 2, y + w / 2)[0], p(x + w / 2, y + w / 2)[1]), fill=255)
            prev = (x, y)
    g.part((150, 70, 40), lash)
    g.part((240, 190, 90), lambda d: d.rounded_rectangle((*p(0.20, 0.66), *p(0.36, 0.78)), radius=40, fill=255))
    g.part((200, 40, 40), lambda d: d.ellipse((*p(0.17, 0.68), *p(0.23, 0.76)), fill=255))
    return icon("weapon", g)


def magic_wand():
    g = Glyph()
    g.part((120, 70, 200), lambda d: d.line([p(0.26, 0.76), p(0.58, 0.44)], fill=255, width=int(0.06 * BIG)))
    g.part((240, 200, 110), lambda d: d.line([p(0.24, 0.78), p(0.32, 0.70)], fill=255, width=int(0.075 * BIG)))
    def star(d):
        cx, cy, R, r = 0.64, 0.36, 0.17, 0.07
        pt = []
        for i in range(10):
            a = math.radians(-90 + i * 36)
            rad = R if i % 2 == 0 else r
            pt.append((cx + rad * math.cos(a), cy + rad * math.sin(a)))
        d.polygon(pts(pt), fill=255)
    g.part((255, 225, 90), star)
    g.detail((255, 255, 230), lambda d: d.ellipse((*p(0.61, 0.33), *p(0.67, 0.39)), fill=255), 0.9)
    return icon("weapon", g)


def knife():
    g = Glyph()
    g.part((215, 225, 235), lambda d: d.polygon(pts([(0.33, 0.62), (0.70, 0.25), (0.76, 0.24), (0.75, 0.30), (0.38, 0.67)]), fill=255))
    g.part((235, 185, 80), lambda d: d.polygon(pts([(0.27, 0.58), (0.31, 0.54), (0.46, 0.69), (0.42, 0.73)]), fill=255))
    g.part((90, 45, 30), lambda d: d.line([p(0.34, 0.66), p(0.22, 0.78)], fill=255, width=int(0.06 * BIG)))
    g.detail((255, 255, 255), lambda d: d.line([p(0.42, 0.58), p(0.70, 0.30)], fill=255, width=int(0.012 * BIG)), 0.7)
    return icon("weapon", g)


def axe():
    g = Glyph()
    g.part((130, 80, 45), lambda d: d.line([p(0.30, 0.80), p(0.62, 0.24)], fill=255, width=int(0.055 * BIG)))
    blade = [(0.50, 0.30), (0.60, 0.24), (0.74, 0.18), (0.80, 0.30), (0.80, 0.46), (0.74, 0.58), (0.62, 0.50), (0.56, 0.42)]
    g.part((200, 210, 222), lambda d: d.polygon(pts(blade), fill=255))
    g.detail((255, 255, 255), lambda d: d.line(pts([(0.76, 0.21), (0.79, 0.32), (0.79, 0.45), (0.74, 0.55)]), fill=255, width=int(0.016 * BIG)), 0.85)
    g.part((110, 115, 125), lambda d: d.rounded_rectangle((*p(0.49, 0.30), *p(0.59, 0.46)), radius=20, fill=255))
    return icon("weapon", g)


def garlic():
    g = Glyph()
    for x0, x1 in ((0.24, 0.50), (0.50, 0.76), (0.34, 0.66)):
        g.part((245, 238, 222), lambda d, x0=x0, x1=x1: d.ellipse((*p(x0, 0.40), *p(x1, 0.80)), fill=255))
    g.part((245, 238, 222), lambda d: d.polygon(pts([(0.43, 0.46), (0.47, 0.22), (0.53, 0.22), (0.57, 0.46)]), fill=255))
    g.part((200, 180, 140), lambda d: d.rounded_rectangle((*p(0.36, 0.78), *p(0.64, 0.82)), radius=10, fill=255))
    for x in (0.40, 0.50, 0.60):
        g.detail((175, 150, 125), lambda d, x=x: d.line([p(x, 0.47), p(x + (x - 0.5) * 0.4, 0.77)], fill=255, width=int(0.012 * BIG)), 0.9)
    g.detail((210, 130, 180), lambda d: d.ellipse((*p(0.28, 0.52), *p(0.38, 0.66)), fill=255), 0.35)
    return icon("weapon", g)


def santa_water():
    g = Glyph()
    g.part((170, 215, 240), lambda d: d.ellipse((*p(0.26, 0.38), *p(0.74, 0.84)), fill=255), light=1.2)
    g.part((170, 215, 240), lambda d: d.rectangle((*p(0.42, 0.22), *p(0.58, 0.44)), fill=255), light=1.2)
    g.part((60, 140, 255), lambda d: d.chord((*p(0.29, 0.41), *p(0.71, 0.81)), 15, 165, fill=255))
    g.part((140, 90, 50), lambda d: d.rounded_rectangle((*p(0.40, 0.15), *p(0.60, 0.24)), radius=30, fill=255))
    g.detail((255, 255, 255), lambda d: d.arc((*p(0.31, 0.43), *p(0.69, 0.79)), 200, 250, fill=255, width=int(0.02 * BIG)), 0.8)
    return icon("weapon", g)


def might():
    g = Glyph()
    g.part((70, 200, 80), lambda d: d.ellipse((*p(0.28, 0.40), *p(0.72, 0.84)), fill=255))
    g.part((180, 220, 190), lambda d: d.rectangle((*p(0.43, 0.24), *p(0.57, 0.44)), fill=255))
    g.part((150, 95, 55), lambda d: d.rounded_rectangle((*p(0.40, 0.16), *p(0.60, 0.26)), radius=30, fill=255))
    g.detail((255, 255, 255), lambda d: d.arc((*p(0.33, 0.45), *p(0.67, 0.79)), 200, 250, fill=255, width=int(0.022 * BIG)), 0.85)
    g.detail((255, 255, 200), lambda d: d.polygon(pts([(0.50, 0.50), (0.56, 0.62), (0.51, 0.62), (0.55, 0.74), (0.44, 0.59), (0.49, 0.59)]), fill=255), 0.9)
    return icon("passive", g)


def armor():
    g = Glyph()
    shield = [(0.28, 0.24), (0.50, 0.17), (0.72, 0.24), (0.70, 0.52), (0.50, 0.82), (0.30, 0.52)]
    g.part((60, 120, 230), lambda d: d.polygon(pts(shield), fill=255))
    g.detail((230, 200, 110), lambda d: d.line(pts(shield + [shield[0]]), fill=255, width=int(0.03 * BIG), joint="curve"), 1.0)
    g.detail((230, 200, 110), lambda d: d.line([p(0.50, 0.24), p(0.50, 0.74)], fill=255, width=int(0.025 * BIG)), 0.9)
    return icon("passive", g)


def hollow_heart():
    g = Glyph()
    def heart(d):
        d.ellipse((*p(0.24, 0.24), *p(0.52, 0.52)), fill=255)
        d.ellipse((*p(0.48, 0.24), *p(0.76, 0.52)), fill=255)
        d.polygon(pts([(0.255, 0.43), (0.745, 0.43), (0.50, 0.80)]), fill=255)
    g.part((230, 45, 60), heart)
    g.detail((80, 10, 20), lambda d: d.line(pts([(0.50, 0.33), (0.46, 0.45), (0.54, 0.52), (0.48, 0.64)]), fill=255, width=int(0.02 * BIG)), 0.85)
    g.detail((255, 220, 220), lambda d: d.ellipse((*p(0.31, 0.30), *p(0.39, 0.38)), fill=255), 0.8)
    return icon("passive", g)


def empty_tome():
    g = Glyph()
    g.part((150, 60, 50), lambda d: d.rounded_rectangle((*p(0.28, 0.20), *p(0.72, 0.80)), radius=40, fill=255))
    g.part((245, 235, 210), lambda d: d.rectangle((*p(0.66, 0.24), *p(0.72, 0.76)), fill=255))
    g.part((235, 190, 80), lambda d: d.rounded_rectangle((*p(0.62, 0.44), *p(0.76, 0.56)), radius=20, fill=255))
    g.detail((235, 190, 80), lambda d: d.rectangle((*p(0.34, 0.30), *p(0.58, 0.34)), fill=255), 0.9)
    g.detail((235, 190, 80), lambda d: d.ellipse((*p(0.40, 0.42), *p(0.52, 0.56)), outline=255, width=int(0.014 * BIG)), 0.9)
    return icon("passive", g)


def clover():
    g = Glyph()
    def leaves(d):
        for a in (0, 90, 180, 270):
            r = math.radians(a + 45)
            cx, cy = 0.50 + 0.13 * math.cos(r), 0.46 + 0.13 * math.sin(r)
            d.ellipse((*p(cx - 0.13, cy - 0.13), *p(cx + 0.13, cy + 0.13)), fill=255)
    g.part((60, 190, 80), leaves)
    g.part((60, 150, 60), lambda d: d.line([p(0.50, 0.50), p(0.58, 0.82)], fill=255, width=int(0.035 * BIG)))
    g.detail((200, 255, 200), lambda d: d.line([p(0.50, 0.30), p(0.50, 0.62)], fill=255, width=int(0.01 * BIG)), 0.6)
    g.detail((200, 255, 200), lambda d: d.line([p(0.34, 0.46), p(0.66, 0.46)], fill=255, width=int(0.01 * BIG)), 0.6)
    return icon("passive", g)


def skull():
    g = Glyph()
    g.part((235, 225, 205), lambda d: d.ellipse((*p(0.27, 0.20), *p(0.73, 0.64)), fill=255))
    g.part((235, 225, 205), lambda d: d.rounded_rectangle((*p(0.36, 0.54), *p(0.64, 0.78)), radius=30, fill=255))
    g.detail(INK, lambda d: d.ellipse((*p(0.34, 0.38), *p(0.46, 0.52)), fill=255), 1.0)
    g.detail(INK, lambda d: d.ellipse((*p(0.54, 0.38), *p(0.66, 0.52)), fill=255), 1.0)
    g.detail(INK, lambda d: d.polygon(pts([(0.50, 0.55), (0.46, 0.62), (0.54, 0.62)]), fill=255), 1.0)
    for x in (0.43, 0.50, 0.57):
        g.detail(INK, lambda d, x=x: d.line([p(x, 0.66), p(x, 0.77)], fill=255, width=int(0.015 * BIG)), 0.8)
    g.detail((255, 80, 60), lambda d: d.ellipse((*p(0.37, 0.41), *p(0.43, 0.47)), fill=255), 0.9)
    g.detail((255, 80, 60), lambda d: d.ellipse((*p(0.57, 0.41), *p(0.63, 0.47)), fill=255), 0.9)
    return icon("passive", g)


def greed():
    g = Glyph()
    for y in (0.66, 0.58, 0.50):
        g.part((240, 185, 60), lambda d, y=y: d.ellipse((*p(0.22, y - 0.08), *p(0.60, y + 0.06)), fill=255))
    g.part((250, 205, 80), lambda d: d.ellipse((*p(0.52, 0.28), *p(0.80, 0.70)), fill=255))
    g.detail((150, 95, 20), lambda d: d.ellipse((*p(0.59, 0.37), *p(0.73, 0.61)), outline=255, width=int(0.018 * BIG)), 1.0)
    return icon("powerup", g)


SAMPLE = [("Whip", whip), ("Magic Wand", magic_wand), ("Knife", knife), ("Axe", axe), ("Garlic", garlic), ("Santa Water", santa_water),
          ("Might", might), ("Armor", armor), ("Hollow Heart", hollow_heart), ("Empty Tome", empty_tome), ("Clover", clover),
          ("Skull O'Maniac", skull), ("Greed", greed)]



def font(size, bold=True):
    for name in ("seguisb.ttf", "segoeuib.ttf", "arialbd.ttf") if bold else ("segoeui.ttf", "arial.ttf"):
        try:
            return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name), size)
        except OSError:
            continue
    return ImageFont.load_default()


def sample_sheet(path, root):
    icons = [(name, make()) for name, make in SAMPLE]
    W = 1800
    sheet = Image.new("RGBA", (W, 1500), (34, 16, 12, 255))
    d = ImageDraw.Draw(sheet)
    d.text((40, 28), "Soul Hunter icons v2: sample (512 px each, shown smaller)", font=font(40), fill=(255, 214, 102))
    d.text((40, 82), "Rim: ember red = weapon, soul cyan = passive item, gold = Power Up.", font=font(26, False), fill=(240, 220, 205))
    # big row(s)
    x0, y0, cell = 40, 140, 250
    for i, (name, im) in enumerate(icons):
        x = x0 + (i % 7) * cell
        y = y0 + (i // 7) * 300
        sheet.alpha_composite(im.resize((220, 220), Image.LANCZOS), (x, y))
        tw = d.textlength(name, font=font(24))
        d.text((x + 110 - tw / 2, y + 228), name, font=font(24), fill=(250, 240, 230))
    # in-game size on the cream cell and on the dark panel
    y = 760
    d.text((40, y), "In the game at 1080p: 64 px in a shop cell, 128 px in the info box", font=font(30), fill=(255, 214, 102))
    y += 56
    for j, (name, im) in enumerate(icons[:8]):
        cx = 40 + j * 215
        d.rounded_rectangle((cx, y, cx + 200, y + 96), radius=10, fill=(255, 241, 226), outline=(120, 70, 40), width=2)
        sheet.alpha_composite(im.resize((64, 64), Image.LANCZOS), (cx + 14, y + 16))
        d.text((cx + 88, y + 22), name.split(" ")[0], font=font(22), fill=(42, 12, 6))
        d.text((cx + 88, y + 52), "2/5", font=font(20, False), fill=(94, 44, 30))
    y += 130
    d.rounded_rectangle((40, y, 1760, y + 170), radius=12, fill=(84, 34, 26), outline=(255, 214, 102), width=3)
    for j, (name, im) in enumerate([icons[0], icons[7], icons[12]]):
        sheet.alpha_composite(im.resize((128, 128), Image.LANCZOS), (70 + j * 560, y + 20))
        d.text((220 + j * 560, y + 50), name, font=font(34), fill=(255, 214, 102))
        d.text((220 + j * 560, y + 98), "Rank 0/5", font=font(24, False), fill=(242, 207, 184))
    # before / after
    y += 210
    d.text((40, y), "Before (old 40-87 px picture, stretched) and after, both at 128 px", font=font(30), fill=(255, 214, 102))
    y += 50
    pairs = [("Might", "Assets/Art/UI/sprite_105.png"), ("Armor", "Assets/Art/UI/sprite_104.png"), ("Skull O'Maniac", "Assets/Art/UI/sprite_102.png")]
    for j, (name, old_path) in enumerate(pairs):
        x = 40 + j * 600
        full = os.path.join(root, old_path)
        if os.path.exists(full):
            old = Image.open(full).convert("RGBA").resize((128, 128), Image.BILINEAR)
            sheet.alpha_composite(old, (x, y))
        new = dict(icons)[name].resize((128, 128), Image.LANCZOS)
        sheet.alpha_composite(new, (x + 170, y))
        d.text((x + 320, y + 50), name, font=font(28), fill=(250, 240, 230))
    sheet.convert("RGB").save(path)
    print("wrote", path)


if __name__ == "__main__":
    root = os.environ.get("SOULHUNTER_ROOT", os.path.join(os.path.dirname(__file__), "..", ".."))
    if "--sample" in sys.argv:
        sample_sheet(sys.argv[sys.argv.index("--sample") + 1], root)
