"""Builds the Soul Hunter app icon (512 px): the hooded hunter of the splash logo on a dark plate.
Owner (2026-10-12): the installed app showed Unity's icon; "start ke baad unity logo ke saath jo hoodie wala picture
hai wahi bana do".
1. Upscale the splash logo Assets/sprite_126.png (199x177) 4x with Real-ESRGAN, model realesrgan-x4plus-anime (as the
   HD item icons and the menu logo were):  realesrgan-ncnn-vulkan.exe -i Assets/sprite_126.png
   -o ArtSource/SH10_AppIcon/splash_logo_x4.png -n realesrgan-x4plus-anime -f png
2. python ArtSource/Generators/build_app_icon.py"""
import os
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LOGO = os.path.join(ROOT, "ArtSource", "SH10_AppIcon", "splash_logo_x4.png")
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "AppIcon", "SH10_AppIcon.png")
SIZE = 512

# Dark plate with an ember glow from below. Square to the edges: the phone's launcher cuts its own shape.
plate = Image.new("RGB", (SIZE, SIZE), (12, 7, 11))
glow = Image.new("RGB", (SIZE, SIZE), (0, 0, 0))
ImageDraw.Draw(glow).ellipse((-80, 260, SIZE + 80, SIZE + 280), fill=(120, 26, 14))
plate = Image.blend(plate, glow.filter(ImageFilter.GaussianBlur(80)), 0.6)

# The whole logo, trimmed to its drawn pixels, inside the launcher's safe middle.
logo = Image.open(LOGO).convert("RGBA")
logo = logo.crop(logo.getbbox())
fit = SIZE * 0.86 / max(logo.size)
logo = logo.resize((round(logo.width * fit), round(logo.height * fit)), Image.LANCZOS)

# Owner (2026-10-12): "icon full bright, full contrast bala banao". The picture's own colours are pushed to full
# brightness, contrast and colour; the plate stays dark, and a warm halo behind the logo lifts it off the plate.
alpha = logo.getchannel("A")
rgb = logo.convert("RGB")
rgb = ImageEnhance.Brightness(rgb).enhance(1.45)
rgb = ImageEnhance.Contrast(rgb).enhance(1.4)
rgb = ImageEnhance.Color(rgb).enhance(1.35)
rgb = ImageEnhance.Sharpness(rgb).enhance(1.5)
logo = Image.merge("RGBA", (*rgb.split(), alpha))

at = ((SIZE - logo.width) // 2, (SIZE - logo.height) // 2)
icon = plate.convert("RGBA")
halo = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
halo.paste(Image.new("RGBA", logo.size, (255, 96, 32, 255)), at, alpha)
halo = halo.filter(ImageFilter.GaussianBlur(22))
halo.putalpha(halo.getchannel("A").point(lambda a: int(a * 0.55)))
icon.alpha_composite(halo)
icon.alpha_composite(logo, at)
icon.convert("RGB").save(OUT)
print("wrote", OUT)
