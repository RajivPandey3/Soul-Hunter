"""The main menu's HD logo (owner review 2026-10-08: the logo was two tiny sprites, 85x45 and 130x46, stretched about 3x).

Steps (the owner chose the illustration model for HD art, 2026-10-07):
1. Upscale Assets/Art/UI/sprite_5.png (SOUL) and sprite_4.png (HUNTER) 4x with Real-ESRGAN, model realesrgan-x4plus-anime:
   realesrgan-ncnn-vulkan.exe -i <in> -o <out> -n realesrgan-x4plus-anime -f png
2. Run: python ArtSource/Generators/build_logo_hd.py <out folder>
   It trims each word, joins them on one baseline with a tight word gap (0.16 x the letter height) and writes
   Assets/Art/UI/SH10_Logo.png. The picture itself is unchanged.
"""
import os
import sys
import numpy as np
from PIL import Image

ROOT = os.environ.get("SOULHUNTER_ROOT", os.path.join(os.path.dirname(__file__), "..", ".."))


def trimmed(path):
    im = Image.open(path).convert("RGBA")
    a = np.asarray(im)[..., 3]
    ys, xs = np.where(a > 20)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


if __name__ == "__main__":
    src = sys.argv[1]
    soul, hunter = trimmed(os.path.join(src, "sprite_5.png")), trimmed(os.path.join(src, "sprite_4.png"))
    h = max(soul.height, hunter.height)
    gap, pad = int(h * 0.16), int(h * 0.03)   # a tight word gap and almost no margin (owner review 2026-10-08)
    logo = Image.new("RGBA", (soul.width + gap + hunter.width + 2 * pad, h + 2 * pad), (0, 0, 0, 0))
    logo.paste(soul, (pad, pad + h - soul.height), soul)
    logo.paste(hunter, (pad + soul.width + gap, pad + h - hunter.height), hunter)
    out = os.path.join(ROOT, "Assets", "Art", "UI", "SH10_Logo.png")
    logo.save(out)
    print("wrote", out, logo.size)
