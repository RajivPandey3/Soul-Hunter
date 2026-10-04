"""Soul Hunter UI theme art: high-quality icons and button/panel plates for the theme system.

Each piece is drawn as SVG (Soul Hunter's own art, the "Option B story edition" the owner chose on 2026-10-04) and
rendered to a transparent PNG by Microsoft Edge in headless mode, so the edges stay sharp at any size.

    python build_ui_theme_art.py [output_dir]

Default output: <repo>/Assets/Art/UI/Theme. The plates are white/grey so the theme tints them (the designer picks the
colour in Theme Studio); the icons keep their own colours. Unity imports them through UiThemeBuilder (sprite settings,
9-slice borders).
"""
import os
import subprocess
import sys
import tempfile
import time

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Assets", "Art", "UI", "Theme")
EDGE = [p for p in (r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe") if os.path.exists(p)]

INK = "#1d0b33"  # the outline colour of the whole set

# ---------------------------------------------------------------- icons (48 x 48 design grid, rendered at 512 px)
ICONS = {
    # Start: the soul flame Kael gathers.
    "icon_soul_flame": f"""
<defs><radialGradient id="g" cx="50%" cy="65%" r="60%"><stop offset="0" stop-color="#ffffff"/><stop offset="0.35" stop-color="#a8fbff"/><stop offset="0.75" stop-color="#3fe3ff"/><stop offset="1" stop-color="#0f9cc4"/></radialGradient>
<radialGradient id="glow" cx="50%" cy="60%" r="50%"><stop offset="0" stop-color="#5ef2ff" stop-opacity="0.5"/><stop offset="1" stop-color="#5ef2ff" stop-opacity="0"/></radialGradient></defs>
<circle cx="24" cy="28" r="22" fill="url(#glow)"/>
<path d="M24 3c3.4 8.6 13 12.7 13 25a13 13 0 0 1-26 0c0-6.6 3.3-9.9 5.5-14 1.1 4.3 3.2 6.5 5.4 6.5C20.8 14 21.8 9 24 3z" fill="url(#g)" stroke="{INK}" stroke-width="2.6" stroke-linejoin="round"/>
<path d="M24 21.5c2.2 4.4 6.6 6.5 6.6 12a6.6 6.6 0 0 1-13.2 0c0-3.3 2.2-5.5 3.3-7.7 1.1 2.2 2.2 3.3 3.3 3.3z" fill="#f2feff"/>
<ellipse cx="19.5" cy="27" rx="1.6" ry="3.2" fill="#ffffff" opacity="0.75" transform="rotate(-18 19.5 27)"/>""",
    # Power Up: a cut soul gem.
    "icon_soul_gem": f"""
<defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff2a8"/><stop offset="0.5" stop-color="#ffc23d"/><stop offset="1" stop-color="#e07b0e"/></linearGradient>
<radialGradient id="glow" cx="50%" cy="50%" r="50%"><stop offset="0" stop-color="#ffd76a" stop-opacity="0.45"/><stop offset="1" stop-color="#ffd76a" stop-opacity="0"/></radialGradient></defs>
<circle cx="24" cy="24" r="22" fill="url(#glow)"/>
<path d="M24 5l17 14-17 25L7 19z" fill="url(#g)" stroke="{INK}" stroke-width="2.6" stroke-linejoin="round"/>
<path d="M7 19h34M24 5l-6.5 14L24 44l6.5-25z" fill="none" stroke="{INK}" stroke-width="1.8" stroke-linejoin="round"/>
<path d="M17.5 19L24 5l6.5 14z" fill="#fff8d2"/><path d="M9.5 19.5l8 0 6.5 22z" fill="#ffffff" opacity="0.28"/>""",
    # Options: a turning gear.
    "icon_gear": f"""
<defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#eef3ff"/><stop offset="1" stop-color="#9fb2e6"/></linearGradient></defs>
<path d="M21 4h6l1 5 4 2 4-3 4 4-3 4 2 4 5 1v6l-5 1-2 4 3 4-4 4-4-3-4 2-1 5h-6l-1-5-4-2-4 3-4-4 3-4-2-4-5-1v-6l5-1 2-4-3-4 4-4 4 3 4-2z" fill="url(#g)" stroke="{INK}" stroke-width="2.6" stroke-linejoin="round"/>
<circle cx="24" cy="24" r="7" fill="{INK}"/><circle cx="24" cy="24" r="3.2" fill="#9fb2e6"/>""",
    # Quit: the Reaper's scythe (the pact).
    "icon_scythe": f"""
<defs><linearGradient id="b" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#ffffff"/><stop offset="1" stop-color="#b9c8ef"/></linearGradient></defs>
<path d="M31 5L14 45" stroke="{INK}" stroke-width="7" stroke-linecap="round"/><path d="M31 5L14 45" stroke="#c98a4a" stroke-width="3" stroke-linecap="round"/>
<path d="M30 7C18.5 4.5 8 10.5 4.5 21c6.4-6.2 14.6-8.4 23-7.2z" fill="url(#b)" stroke="{INK}" stroke-width="2.6" stroke-linejoin="round"/>
<path d="M10 15c4-3 9-4.4 14-4" stroke="#ffffff" stroke-width="1.4" fill="none" stroke-linecap="round" opacity="0.8"/>""",
    # The moon over the graveyard (pause, stage card).
    "icon_moon": f"""
<defs><radialGradient id="g" cx="40%" cy="40%" r="70%"><stop offset="0" stop-color="#fffbe0"/><stop offset="1" stop-color="#ffd85a"/></radialGradient>
<radialGradient id="glow" cx="50%" cy="50%" r="50%"><stop offset="0" stop-color="#ffe58a" stop-opacity="0.45"/><stop offset="1" stop-color="#ffe58a" stop-opacity="0"/></radialGradient></defs>
<circle cx="24" cy="24" r="22" fill="url(#glow)"/>
<path d="M30 7a17 17 0 1 0 11.5 28.5A14 14 0 1 1 30 7z" fill="url(#g)" stroke="{INK}" stroke-width="2.6" stroke-linejoin="round"/>
<circle cx="19" cy="27" r="2" fill="#e8c24a" opacity="0.6"/><circle cx="24" cy="35" r="1.4" fill="#e8c24a" opacity="0.6"/>""",
    # Back: a curved arrow.
    "icon_back": f"""
<path d="M20 9L6 22l14 13v-8c9 0 15 3 20 11 0-12-7-21-20-22z" fill="#ffffff" stroke="{INK}" stroke-width="2.8" stroke-linejoin="round"/>""",
    # Confirm / Done: a check in a soul ring.
    "icon_check": f"""
<circle cx="24" cy="24" r="19" fill="#42e07a" stroke="{INK}" stroke-width="2.8"/>
<path d="M14 24.5l7 7 13-14" fill="none" stroke="{INK}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>
<path d="M14 24.5l7 7 13-14" fill="none" stroke="#ffffff" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"/>""",
    # Resume / Play: a play triangle.
    "icon_play": f"""
<path d="M15 8l26 16-26 16z" fill="#ffffff" stroke="{INK}" stroke-width="2.8" stroke-linejoin="round"/>""",
    # Buy: a soul coin.
    "icon_coin": f"""
<defs><radialGradient id="g" cx="40%" cy="35%" r="70%"><stop offset="0" stop-color="#fff4b8"/><stop offset="1" stop-color="#f0a21c"/></radialGradient></defs>
<circle cx="24" cy="24" r="18" fill="url(#g)" stroke="{INK}" stroke-width="2.8"/><circle cx="24" cy="24" r="12.5" fill="none" stroke="#c97a0a" stroke-width="2"/>
<path d="M24 14c1.6 4 6 6 6 11.5a6 6 0 0 1-12 0c0-3 1.5-4.5 2.5-6.5.5 2 1.5 3 2.5 3-.5-3 0-5.5 1-8z" fill="#5ef2ff" stroke="{INK}" stroke-width="1.6" stroke-linejoin="round"/>""",
}

# ---------------------------------------------------------------- plates (white/grey: the theme tints them)
# 9-slice: the corners keep their shape at any button size. Size and border (px) are read by UiThemeBuilder.
PLATES = {
    # Glossy chunky button: dark outline, a darker lip below, a gloss band on top.
    "plate_button": (256, 128, f"""
<rect x="6" y="18" width="244" height="104" rx="34" fill="{INK}"/>
<rect x="6" y="6" width="244" height="104" rx="34" fill="#c9c9c9" stroke="{INK}" stroke-width="10"/>
<rect x="11" y="11" width="234" height="94" rx="29" fill="url(#body)"/>
<rect x="26" y="16" width="204" height="30" rx="15" fill="#ffffff" opacity="0.55"/>""",
                     """<linearGradient id="body" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffffff"/><stop offset="1" stop-color="#d4d4d4"/></linearGradient>"""),
    # Card / panel: rounded, outlined, soft top light.
    "plate_card": (256, 256, f"""
<rect x="6" y="14" width="244" height="236" rx="36" fill="{INK}"/>
<rect x="6" y="6" width="244" height="236" rx="36" fill="#e9e9e9" stroke="{INK}" stroke-width="10"/>
<rect x="11" y="11" width="234" height="226" rx="31" fill="url(#body)"/>""",
                   """<linearGradient id="body" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffffff"/><stop offset="1" stop-color="#e3e3e3"/></linearGradient>"""),
    # Panel: a big window frame for menus.
    "plate_panel": (256, 256, f"""
<rect x="5" y="5" width="246" height="246" rx="30" fill="#f2f2f2" stroke="{INK}" stroke-width="10"/>
<rect x="18" y="18" width="220" height="220" rx="20" fill="none" stroke="#ffffff" stroke-width="4" opacity="0.6"/>""", ""),
    # Shine: a soft diagonal band for the shine sweep.
    "plate_shine": (128, 256, """
<rect x="40" y="-40" width="48" height="336" fill="url(#s)" transform="rotate(18 64 128)"/>""",
                    """<linearGradient id="s" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#ffffff" stop-opacity="0"/><stop offset="0.5" stop-color="#ffffff" stop-opacity="0.85"/><stop offset="1" stop-color="#ffffff" stop-opacity="0"/></linearGradient>"""),
}


def render(name, width, height, view, body, defs=""):
    html = (f'<!doctype html><html><head><style>html,body{{margin:0;background:transparent}}svg{{display:block}}</style></head>'
            f'<body><svg width="{width}" height="{height}" viewBox="{view}" xmlns="http://www.w3.org/2000/svg">'
            f'<defs>{defs}</defs>{body}</svg></body></html>')
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False, encoding="utf-8") as f:
        f.write(html)
        page = f.name
    target = os.path.join(OUT, name + ".png")
    if os.path.exists(target):
        os.remove(target)
    subprocess.run([EDGE[0], "--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000",
                    f"--window-size={width},{height}", "--force-device-scale-factor=1", f"--screenshot={target}",
                    "file:///" + page.replace("\\", "/")], capture_output=True, timeout=60)
    for _ in range(40):
        if os.path.exists(target):
            break
        time.sleep(0.1)
    os.remove(page)
    if not os.path.exists(target):
        raise RuntimeError("Edge did not render " + name)
    print("wrote", target)


def main():
    if not EDGE:
        raise SystemExit("Microsoft Edge not found (needed to render the SVG art).")
    os.makedirs(OUT, exist_ok=True)
    for name, body in ICONS.items():
        render(name, 512, 512, "0 0 48 48", body)
    for name, (w, h, body, defs) in PLATES.items():
        render(name, w, h, f"0 0 {w} {h}", body, defs)


if __name__ == "__main__":
    main()
