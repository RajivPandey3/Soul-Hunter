"""Soul Hunter item icons composed from the owner-approved VFX v2 sprites (Bug MIS v2 C18, 2026-10-02).

The Duplicator and Spellbinder cards showed opaque squares (sprite_11 / sprite_12, plain glow tiles). These icons put
the approved sprites (Assets/Art/Textures/VFX/SH10_Vfx2_*.png) on a round dark badge like the other round item icons.
The Cross and Bible icons are their SH10 models (rendered by ItemIconBuilder step 1) on the same badges.
Run: python ArtSource/Generators/build_item_icons.py  (needs Pillow). Writes Assets/Art/UI/SH10_Icon_<Name>.png.

Superseded 2026-10-08 (owner: icons without the circle): the game uses build_icons_free.py, which reuses these
glyphs and sprites without the badge. Running this script alone writes the circle versions back over them.
"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

ROOT = os.environ.get("SOULHUNTER_ROOT", os.path.join(os.path.dirname(__file__), "..", ".."))
VFX = os.path.join(ROOT, "Assets", "Art", "Textures", "VFX")
OUT = os.path.join(ROOT, "Assets", "Art", "UI")
RAW = os.path.join(ROOT, "ArtSource", "SH10_ItemIcons")  # model renders from Unity (ItemIconBuilder step 1)
SIZE = 256


def sprite(name, tint, scale=1.0, alpha=1.0):
    im = Image.open(os.path.join(VFX, f"SH10_Vfx2_{name}.png")).convert("RGBA")
    if scale != 1.0:
        s = int(SIZE * scale)
        im = im.resize((s, s), Image.LANCZOS)
    r, g, b, a = im.split()
    lum = Image.merge("RGB", (r, g, b)).convert("L")
    colour = Image.new("RGB", im.size, tint)
    lit = ImageChops.multiply(colour, Image.merge("RGB", (lum, lum, lum)))
    a = a.point(lambda v: int(v * alpha))
    return Image.merge("RGBA", (*lit.split(), a))


def badge(rim, glow):
    """A round dark badge with a coloured rim and an inner glow."""
    im = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pad = 10
    d.ellipse((pad, pad, SIZE - pad, SIZE - pad), fill=(14, 10, 24, 255))
    inner = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ImageDraw.Draw(inner).ellipse((48, 48, SIZE - 48, SIZE - 48), fill=glow + (150,))
    im = Image.alpha_composite(im, inner.filter(ImageFilter.GaussianBlur(28)))
    ring = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ImageDraw.Draw(ring).ellipse((pad, pad, SIZE - pad, SIZE - pad), outline=rim + (255,), width=9)
    im = Image.alpha_composite(im, ring.filter(ImageFilter.GaussianBlur(1)))
    hl = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ImageDraw.Draw(hl).arc((pad + 6, pad + 6, SIZE - pad - 6, SIZE - pad - 6), 200, 300, fill=(255, 255, 255, 110), width=5)
    return Image.alpha_composite(im, hl)


def place(base, layer, dx=0, dy=0):
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    x = (SIZE - layer.size[0]) // 2 + dx
    y = (SIZE - layer.size[1]) // 2 + dy
    canvas.paste(layer, (x, y), layer)
    return Image.alpha_composite(base, canvas)


def mask_round(im):
    m = Image.new("L", (SIZE, SIZE), 0)
    ImageDraw.Draw(m).ellipse((8, 8, SIZE - 8, SIZE - 8), fill=255)
    out = im.copy()
    out.putalpha(ImageChops.multiply(im.split()[3], m))
    return out


def duplicator():
    # +1 projectile for every weapon: two soul flames, side by side, one echoing the other.
    im = badge((90, 200, 255), (40, 120, 255))
    # +1 projectile for every weapon: a bright star and its echo behind it.
    im = place(im, sprite("impact_star", (80, 170, 255), 0.62, 0.85), -30, 22)
    im = place(im, sprite("impact_star", (190, 240, 255), 0.78), 22, -14)
    im = place(im, sprite("sparkle", (255, 255, 255), 0.34), 22, -14)
    return mask_round(im)


def spellbinder():
    # +10% weapon duration: a binding rune circle holding a spark.
    im = badge((200, 120, 255), (120, 40, 220))
    im = place(im, sprite("magic_circle", (215, 150, 255), 0.86, 1.0))
    im = place(im, sprite("sparkle", (255, 230, 255), 0.42))
    return mask_round(im)


def model_icon(raw_name, rim, glow, lift=1.25, scale=0.78):
    """A rendered SH10 model on a round badge, brightened, with a soft glow behind it."""
    raw = Image.open(os.path.join(RAW, f"raw_{raw_name}.png")).convert("RGBA")
    s = int(SIZE * scale)
    raw = raw.resize((s, s), Image.LANCZOS)
    r, g, b, a = raw.split()
    rgb = Image.merge("RGB", (r, g, b)).point(lambda v: min(255, int(v * lift)))
    raw = Image.merge("RGBA", (*rgb.split(), a))
    im = badge(rim, glow)
    halo = Image.new("RGBA", raw.size, glow + (0,))
    halo.putalpha(a.filter(ImageFilter.GaussianBlur(10)).point(lambda v: int(v * 0.9)))
    im = place(im, halo)
    im = place(im, raw)
    return mask_round(im)


def cross():
    return model_icon("Cross", (255, 205, 90), (255, 190, 80), lift=1.9)


def bible():
    return model_icon("Bible", (220, 70, 70), (200, 60, 90), lift=1.35)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    builds = [("Duplicator", duplicator), ("Spellbinder", spellbinder)]
    for name, build in (("Cross", cross), ("Bible", bible)):
        if os.path.exists(os.path.join(RAW, f"raw_{name}.png")):
            builds.append((name, build))
        else:
            print("skipped", name, "(render it first: ItemIconBuilder step 1)")
    for name, build in builds:
        path = os.path.join(OUT, f"SH10_Icon_{name}.png")
        build().save(path)
        print("wrote", path)
