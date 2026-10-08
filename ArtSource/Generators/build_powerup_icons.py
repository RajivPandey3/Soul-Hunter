"""Soul Hunter PowerUp icons for the PowerUps that have no matching passive item (owner, 2026-10-07: "icons bhi banao").

Greed, Omni, Charm, Defang, Reroll, Skip, Banish and Preserve showed no icon in the Power Up shop, and Area's
(the thin Candelabrador candlestick) read too small. Each icon is the same
round dark badge as the other round item icons (build_item_icons.badge) with one glyph in the same warm gold, drawn as
filled shapes (one style for the whole set; ui-ux-pro-max: "Filled vs Outline Discipline", "Consistent Icon Sizing").
Only the rim colour changes per PowerUp. Shapes are drawn 4x large and scaled down for clean edges.
Run: python ArtSource/Generators/build_powerup_icons.py  (needs Pillow). Writes Assets/Art/UI/SH10_PowerUp_<Id>.png.
Soul Hunter's own art, not VS art.

Superseded 2026-10-08 (owner: icons without the circle): the game uses build_icons_free.py, which reuses these
glyphs and sprites without the badge. Running this script alone writes the circle versions back over them.
"""
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(__file__))
from build_item_icons import SIZE, OUT, badge  # noqa: E402

S = 4                       # supersampling
N = SIZE * S
GOLD = (255, 214, 102, 255)
GOLD_DARK = (170, 110, 30, 255)
INK = (24, 14, 10, 255)


def canvas():
    return Image.new("RGBA", (N, N), (0, 0, 0, 0))


def finish(glyph, rim, glow):
    """Glyph (drawn at N) with a soft gold glow, on the round badge."""
    small = glyph.resize((SIZE, SIZE), Image.LANCZOS)
    halo = small.copy().filter(ImageFilter.GaussianBlur(7))
    a = halo.split()[3].point(lambda v: int(v * 0.55))
    halo = Image.merge("RGBA", (*Image.new("RGB", halo.size, GOLD[:3]).split(), a))
    out = badge(rim, glow)
    out = Image.alpha_composite(out, halo)
    return Image.alpha_composite(out, small)


def c(v):
    return int(v * N)


def greed():
    g = canvas(); d = ImageDraw.Draw(g)
    # three stacked coins and one standing coin
    for i, y in enumerate((0.66, 0.58, 0.50)):
        d.ellipse((c(0.24), c(y - 0.07), c(0.62), c(y + 0.07)), fill=GOLD_DARK)
        d.ellipse((c(0.24), c(y - 0.09), c(0.62), c(y + 0.05)), fill=GOLD)
    d.ellipse((c(0.52), c(0.30), c(0.78), c(0.70)), fill=GOLD_DARK)
    d.ellipse((c(0.54), c(0.30), c(0.78), c(0.68)), fill=GOLD)
    d.ellipse((c(0.60), c(0.38), c(0.72), c(0.60)), outline=GOLD_DARK, width=c(0.02))
    return finish(g, (230, 180, 60), (120, 80, 20))


def omni():
    g = canvas(); d = ImageDraw.Draw(g)
    cx, cy = 0.5, 0.5
    big = [(cx, 0.20), (cx + 0.07, cy - 0.07), (0.80, cy), (cx + 0.07, cy + 0.07), (cx, 0.80), (cx - 0.07, cy + 0.07), (0.20, cy), (cx - 0.07, cy - 0.07)]
    d.polygon([(c(x), c(y)) for x, y in big], fill=GOLD)
    for ang in (45, 135, 225, 315):
        r = 0.25
        x, y = cx + r * math.cos(math.radians(ang)), cy + r * math.sin(math.radians(ang))
        s = 0.045
        d.polygon([(c(x), c(y - s)), (c(x + s), c(y)), (c(x), c(y + s)), (c(x - s), c(y))], fill=GOLD)
    d.ellipse((c(0.45), c(0.45), c(0.55), c(0.55)), fill=(255, 245, 210, 255))
    return finish(g, (170, 110, 255), (70, 30, 120))


def charm():
    g = canvas(); d = ImageDraw.Draw(g)
    # a bell calling the horde, with sound arcs
    d.pieslice((c(0.30), c(0.24), c(0.66), c(0.62)), 180, 360, fill=GOLD)
    d.polygon([(c(0.30), c(0.43)), (c(0.66), c(0.43)), (c(0.72), c(0.66)), (c(0.24), c(0.66))], fill=GOLD)
    d.rounded_rectangle((c(0.20), c(0.64), c(0.76), c(0.70)), radius=c(0.02), fill=GOLD_DARK)
    d.ellipse((c(0.43), c(0.68), c(0.53), c(0.78)), fill=GOLD)
    d.ellipse((c(0.45), c(0.18), c(0.51), c(0.25)), fill=GOLD)
    for r in (0.30, 0.38):
        d.arc((c(0.48 - r), c(0.45 - r), c(0.48 + r), c(0.45 + r)), 300, 345, fill=GOLD, width=c(0.03))
        d.arc((c(0.48 - r), c(0.45 - r), c(0.48 + r), c(0.45 + r)), 195, 240, fill=GOLD, width=c(0.03))
    return finish(g, (220, 50, 60), (110, 20, 30))


def defang():
    g = canvas(); d = ImageDraw.Draw(g)
    # two curved fangs under a gum line: the left one whole, the right one snapped off (defanged)
    d.rounded_rectangle((c(0.22), c(0.26), c(0.78), c(0.36)), radius=c(0.05), fill=GOLD_DARK)

    def fang(x0, x1, length, curve, broken):
        pts_out, pts_in = [], []
        steps = 12
        for i in range(steps + 1):
            t = i / steps
            w = (x1 - x0) * (1 - t) ** 1.2
            mid = x0 + (x1 - x0) / 2 + curve * t * t
            y = 0.34 + length * t
            pts_out.append((mid - w / 2, y))
            pts_in.append((mid + w / 2, y))
        if broken:  # jagged snapped end
            k = int(steps * 0.45)
            out, inn = pts_out[:k], pts_in[:k]
            yb = out[-1][1]
            jag = [(inn[-1][0], yb), (inn[-1][0] - 0.03, yb + 0.04), (out[-1][0] + 0.04, yb - 0.01), (out[-1][0], yb + 0.03)]
            poly = out + jag[::-1] + inn[::-1]
        else:
            poly = pts_out + pts_in[::-1]
        d.polygon([(c(x), c(y)) for x, y in poly], fill=GOLD)

    fang(0.27, 0.45, 0.44, 0.04, False)
    fang(0.55, 0.73, 0.44, -0.04, True)
    return finish(g, (90, 210, 110), (20, 90, 40))


def reroll():
    g = canvas(); d = ImageDraw.Draw(g)
    # two arrows chasing round a circle
    box = (c(0.24), c(0.24), c(0.76), c(0.76))
    d.arc(box, 200, 340, fill=GOLD, width=c(0.07))
    d.arc(box, 20, 160, fill=GOLD, width=c(0.07))
    def head(ang, turn):
        a = math.radians(ang)
        x, y = 0.5 + 0.26 * math.cos(a), 0.5 + 0.26 * math.sin(a)
        t = math.radians(ang + turn)
        tip = (x + 0.09 * math.cos(t), y + 0.09 * math.sin(t))
        n = math.radians(ang)
        p1 = (x + 0.08 * math.cos(n), y + 0.08 * math.sin(n))
        p2 = (x - 0.08 * math.cos(n), y - 0.08 * math.sin(n))
        d.polygon([(c(tip[0]), c(tip[1])), (c(p1[0]), c(p1[1])), (c(p2[0]), c(p2[1]))], fill=GOLD)
    head(340, 90)
    head(160, 90)
    return finish(g, (80, 200, 230), (20, 80, 110))


def skip():
    g = canvas(); d = ImageDraw.Draw(g)
    for x0 in (0.22, 0.46):
        d.polygon([(c(x0), c(0.28)), (c(x0 + 0.26), c(0.50)), (c(x0), c(0.72))], fill=GOLD)
    d.rectangle((c(0.73), c(0.28), c(0.79), c(0.72)), fill=GOLD)
    return finish(g, (255, 140, 40), (120, 55, 10))


def banish():
    g = canvas(); d = ImageDraw.Draw(g)
    # an item card sealed away by a forbidding ring
    d.rounded_rectangle((c(0.36), c(0.26), c(0.64), c(0.74)), radius=c(0.04), fill=GOLD_DARK)
    d.rounded_rectangle((c(0.39), c(0.29), c(0.61), c(0.71)), radius=c(0.03), fill=GOLD)
    d.ellipse((c(0.20), c(0.20), c(0.80), c(0.80)), outline=(255, 90, 80, 255), width=c(0.06))
    d.line([(c(0.30), c(0.70)), (c(0.70), c(0.30))], fill=(255, 90, 80, 255), width=c(0.06))
    return finish(g, (200, 40, 40), (100, 15, 15))


def preserve():
    g = canvas(); d = ImageDraw.Draw(g)
    # an hourglass: keeps what would be spent
    d.rectangle((c(0.30), c(0.22), c(0.70), c(0.27)), fill=GOLD_DARK)
    d.rectangle((c(0.30), c(0.73), c(0.70), c(0.78)), fill=GOLD_DARK)
    d.polygon([(c(0.34), c(0.27)), (c(0.66), c(0.27)), (c(0.52), c(0.50)), (c(0.66), c(0.73)), (c(0.34), c(0.73)), (c(0.48), c(0.50))], fill=GOLD)
    d.polygon([(c(0.40), c(0.32)), (c(0.60), c(0.32)), (c(0.50), c(0.46))], fill=GOLD_DARK)
    d.polygon([(c(0.50), c(0.58)), (c(0.62), c(0.70)), (c(0.38), c(0.70))], fill=GOLD_DARK)
    return finish(g, (60, 200, 180), (15, 85, 75))


def area():
    g = canvas(); d = ImageDraw.Draw(g)
    # a glowing core with rings spreading out: a bigger reach
    d.ellipse((c(0.43), c(0.43), c(0.57), c(0.57)), fill=GOLD)
    for r, w in ((0.15, 0.035), (0.23, 0.03), (0.31, 0.025)):
        d.ellipse((c(0.5 - r), c(0.5 - r), c(0.5 + r), c(0.5 + r)), outline=GOLD, width=c(w))
    for ang in (0, 90, 180, 270):
        a = math.radians(ang)
        tip = (0.5 + 0.36 * math.cos(a), 0.5 + 0.36 * math.sin(a))
        side = math.radians(ang + 90)
        p1 = (0.5 + 0.30 * math.cos(a) + 0.04 * math.cos(side), 0.5 + 0.30 * math.sin(a) + 0.04 * math.sin(side))
        p2 = (0.5 + 0.30 * math.cos(a) - 0.04 * math.cos(side), 0.5 + 0.30 * math.sin(a) - 0.04 * math.sin(side))
        d.polygon([(c(tip[0]), c(tip[1])), (c(p1[0]), c(p1[1])), (c(p2[0]), c(p2[1]))], fill=GOLD)
    return finish(g, (240, 160, 60), (110, 60, 15))


ICONS = {"Area": area, "Greed": greed, "Omni": omni, "Charm": charm, "Defang": defang,
         "Reroll": reroll, "Skip": skip, "Banish": banish, "Preserve": preserve}

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for name, make in ICONS.items():
        path = os.path.join(OUT, f"SH10_PowerUp_{name}.png")
        make().save(path)
        print("wrote", path)
