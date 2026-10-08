"""Finishes the 3D icon renders (build_icons_3d.py): a thin dark outline and a soft shadow round each item, so it reads on
the light shop cells and on the dark panels alike (owner, 2026-10-07: brightness and contrast). Then, with --sample,
builds the approval sheet.
Run: python ArtSource/Generators/finish_icons_3d.py <render folder> [--sample <sheet.png>]
"""
import glob
import os
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont

INK = (20, 10, 8)


def finish(im):
    im = im.convert("RGBA")
    a = im.split()[3]
    outline = a.filter(ImageFilter.MaxFilter(7)).point(lambda v: min(255, int(v * 1.0)))
    shadow = a.filter(ImageFilter.GaussianBlur(10)).point(lambda v: int(v * 0.55))
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    sh = Image.new("RGBA", im.size, INK + (0,)); sh.putalpha(shadow)
    out.alpha_composite(sh, (6, 10))
    ol = Image.new("RGBA", im.size, INK + (0,)); ol.putalpha(outline)
    out.alpha_composite(ol)
    out.alpha_composite(im)
    return out


def font(size, bold=True):
    names = ("seguisb.ttf", "segoeuib.ttf", "arialbd.ttf") if bold else ("segoeui.ttf", "arial.ttf")
    for n in names:
        try:
            return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", n), size)
        except OSError:
            pass
    return ImageFont.load_default()


LABEL = {"Might": "Might", "Armor": "Armor", "HollowHeart": "Hollow Heart", "SkullOManiac": "Skull O'Maniac",
         "Axe": "Axe", "Garlic": "Garlic", "Greed": "Greed", "SantaWater": "Santa Water"}
OLD = {"Might": "Assets/Art/UI/sprite_105.png", "Armor": "Assets/Art/UI/sprite_104.png", "SkullOManiac": "Assets/Art/UI/sprite_102.png"}


def sample(icons, path, root):
    W, H = 1800, 1360
    sheet = Image.new("RGBA", (W, H), (34, 16, 12, 255))
    d = ImageDraw.Draw(sheet)
    d.text((40, 26), "Soul Hunter icons, 3D (Blender): sample, 512 px each, no circle", font=font(40), fill=(255, 214, 102))
    d.text((40, 80), "Real 3D models, lit and rendered; a thin dark outline and soft shadow keep them clear on light and dark.",
           font=font(26, False), fill=(240, 220, 205))
    for i, (name, im) in enumerate(icons):
        x, y = 40 + (i % 4) * 430, 130 + (i // 4) * 330
        sheet.alpha_composite(im.resize((270, 270), Image.LANCZOS), (x + 60, y))
        t = LABEL.get(name, name)
        d.text((x + 195 - d.textlength(t, font=font(26)) / 2, y + 278), t, font=font(26), fill=(250, 240, 230))
    y = 800
    d.text((40, y), "In the game at 1080p: 64 px in a shop cell (light) and on a dark panel", font=font(30), fill=(255, 214, 102))
    y += 50
    for i, (name, im) in enumerate(icons):
        x = 40 + i * 215
        small = im.resize((72, 72), Image.LANCZOS)
        d.rounded_rectangle((x, y, x + 200, y + 92), radius=10, fill=(255, 241, 226), outline=(120, 70, 40), width=2)
        sheet.alpha_composite(small, (x + 10, y + 10))
        d.text((x + 90, y + 18), LABEL.get(name, name).split(" ")[0], font=font(22), fill=(42, 12, 6))
        d.text((x + 90, y + 50), "2/5", font=font(20, False), fill=(94, 44, 30))
        d.rounded_rectangle((x, y + 104, x + 200, y + 196), radius=10, fill=(84, 34, 26))
        sheet.alpha_composite(small, (x + 10, y + 114))
        d.text((x + 90, y + 122), LABEL.get(name, name).split(" ")[0], font=font(22), fill=(255, 214, 102))
    y += 230
    d.text((40, y), "Before (old picture, stretched) and after, both at 128 px", font=font(30), fill=(255, 214, 102))
    y += 46
    lookup = dict(icons)
    for j, (name, old) in enumerate(OLD.items()):
        x = 40 + j * 600
        full = os.path.join(root, old)
        if os.path.exists(full):
            sheet.alpha_composite(Image.open(full).convert("RGBA").resize((128, 128), Image.BILINEAR), (x, y))
        sheet.alpha_composite(lookup[name].resize((128, 128), Image.LANCZOS), (x + 160, y))
        d.text((x + 310, y + 48), LABEL[name], font=font(28), fill=(250, 240, 230))
    sheet.convert("RGB").save(path)
    print("wrote", path)


if __name__ == "__main__":
    folder = sys.argv[1]
    order = ["Might", "Armor", "HollowHeart", "SkullOManiac", "Axe", "Garlic", "Greed", "SantaWater"]
    icons = []
    for name in order:
        f = os.path.join(folder, f"SH10_Icon3D_{name}.png")
        if os.path.exists(f):
            done = finish(Image.open(f))
            done.save(os.path.join(folder, f"final_{name}.png"))
            icons.append((name, done))
    if "--sample" in sys.argv:
        root = os.environ.get("SOULHUNTER_ROOT", os.path.join(os.path.dirname(__file__), "..", ".."))
        sample(icons, sys.argv[sys.argv.index("--sample") + 1], root)
