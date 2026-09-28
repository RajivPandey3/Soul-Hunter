"""Installs Soul Hunter's CC0 sound effects and music (owner choice 2026-09-25: "CC0 sound packs").

Every file comes from a pack whose licence page says CC0 (checked 2026-09-25, see SOURCES). The script picks one
file per game sound, loudness-normalises it (so packs from different authors sit at one level), writes it as Ogg
Vorbis to Assets/_Project/Audio/CC0/ and writes CREDITS.md there. AudioManagerPrefabBuilder prefers these files
over the older synthesised ones (Assets/_Project/Audio/SFX, Music), which stay as a fallback.

Run: blender --background --python ArtSource/Generators/install_cc0_audio.py -- <downloads folder> [--only A,B]
--only installs just the named sounds and appends their rows to CREDITS.md (the other files stay untouched).
The downloads folder holds the extracted packs listed in SOURCES (not kept in the repository).
Set SOULHUNTER_ROOT to run against an isolated copy of the project.
"""
import os
import sys
from pathlib import Path

import aud
import numpy as np

ROOT = Path(os.environ.get("SOULHUNTER_ROOT") or Path(__file__).resolve().parent.parent.parent)
OUT = ROOT / 'Assets' / '_Project' / 'Audio' / 'CC0'

SOURCES = {
    'kenney-impact': ('Impact Sounds', 'Kenney', 'https://kenney.nl/assets/impact-sounds'),
    'kenney-interface': ('Interface Sounds', 'Kenney', 'https://kenney.nl/assets/interface-sounds'),
    'kenney-rpg': ('RPG Audio', 'Kenney', 'https://kenney.nl/assets/rpg-audio'),
    'kenney-scifi': ('Sci-fi Sounds', 'Kenney', 'https://kenney.nl/assets/sci-fi-sounds'),
    'kenney-jingles': ('Music Jingles', 'Kenney', 'https://kenney.nl/assets/music-jingles'),
    'kenney-digital': ('Digital Audio', 'Kenney', 'https://kenney.nl/assets/digital-audio'),
    'oga-rpg': ('RPG Sound Pack', 'artisticdude', 'https://opengameart.org/content/rpg-sound-pack'),
    'oga-swishes': ('Swishes Sound Pack', 'artisticdude', 'https://opengameart.org/content/swishes-sound-pack'),
    'oga-creature': ('80 CC0 creature SFX', 'rubberduck', 'https://opengameart.org/content/80-cc0-creature-sfx'),
    'menu': ('At the End of Hope', 'emmama', 'https://opengameart.org/content/at-the-end-of-hope'),
    'l01': ('Battle Theme A', 'cynicmusic', 'https://opengameart.org/content/battle-theme-a'),
    'l02': ('Dark Forest Theme', 'cynicmusic', 'https://opengameart.org/content/dark-forest-theme'),
    'l03': ('Town Theme RPG', 'cynicmusic', 'https://opengameart.org/content/town-theme-rpg'),
    'l04': ('Castle Stage', 'mintodog', 'https://opengameart.org/content/castle-stage'),
    'l05': ('Dungeon Theme', 'katana', 'https://opengameart.org/content/dungeon-theme-0'),
    'l06': ('Crystal Cave (song18)', 'cynicmusic', 'https://opengameart.org/content/crystal-cave-song18'),
    'l07': ("Apparition's Ball", 'bobjt', 'https://opengameart.org/content/apparitions-ball'),
    'l08': ('Battle Theme B', 'cynicmusic', 'https://opengameart.org/content/battle-theme-b-for-rpg'),
    'l09': ('Cursed Tower Veranda', 'bobjt', 'https://opengameart.org/content/cursed-tower-veranda'),
    'l10': ('Final Hour', 'isaiah658', 'https://opengameart.org/content/final-hour'),
}

# Game sound -> (source, path inside the downloads folder)
SFX = {
    'GemPickup': ('kenney-interface', 'kenney/interface-sounds/Audio/glass_001.ogg'),
    'LevelUp': ('kenney-jingles', 'kenney/music-jingles/Audio/Hit jingles/jingles_HIT10.ogg'),
    'EnemyHit': ('kenney-impact', 'kenney/impact-sounds/Audio/impactPunch_medium_000.ogg'),
    'ChestOpen': ('kenney-rpg', 'kenney/rpg-audio/Audio/metalLatch.ogg'),
    'Heal': ('oga-rpg', 'oga/rpg_sound_pack/RPG Sound Pack/inventory/bubble.wav'),
    'EnemyDeath': ('oga-creature', 'oga/80-CC0-creature-SFX_0/hurt_02.ogg'),
    'PlayerHurt': ('kenney-impact', 'kenney/impact-sounds/Audio/impactPunch_heavy_000.ogg'),
    'GameOver': ('kenney-jingles', 'kenney/music-jingles/Audio/Steel jingles/jingles_STEEL01.ogg'),
    'StageClear': ('kenney-jingles', 'kenney/music-jingles/Audio/Hit jingles/jingles_HIT11.ogg'),
    'BossWarning': ('oga-creature', 'oga/80-CC0-creature-SFX_0/roar_01.ogg'),
    'UIClick': ('kenney-interface', 'kenney/interface-sounds/Audio/select_007.ogg'),
    'UIConfirm': ('kenney-interface', 'kenney/interface-sounds/Audio/confirmation_002.ogg'),
    'Unlock': ('kenney-jingles', 'kenney/music-jingles/Audio/Hit jingles/jingles_HIT06.ogg'),
    'Shoot': ('oga-swishes', 'oga/swishes/swishes/swish-10.wav'),
    'Slash': ('oga-swishes', 'oga/swishes/swishes/swish-1.wav'),
    'Magic': ('oga-rpg', 'oga/rpg_sound_pack/RPG Sound Pack/battle/magic1.wav'),
    'Explosion': ('kenney-scifi', 'kenney/sci-fi-sounds/Audio/explosionCrunch_000.ogg'),
    # VS wiki (Treasure_Chest, Audio): "Treasure Found" when a chest is found, then a jingle for a 1-, 3- or 5-item
    # chest. Picked 2026-09-28 from the unused jingles by measured length (short to long) and rising pitch.
    'TreasureFound': ('kenney-jingles', 'kenney/music-jingles/Audio/Hit jingles/jingles_HIT12.ogg'),
    'TreasureA': ('kenney-jingles', 'kenney/music-jingles/Audio/Hit jingles/jingles_HIT01.ogg'),
    'TreasureB': ('kenney-jingles', 'kenney/music-jingles/Audio/8-Bit jingles/jingles_NES13.ogg'),
    'TreasureC': ('kenney-jingles', 'kenney/music-jingles/Audio/8-Bit jingles/jingles_NES00.ogg'),
}

MUSIC = {'Music_Menu': ('menu', 'oga/music/menu.mp3')}
MUSIC.update({f'Music_Stage{i:02}': (f'l{i:02}', f'oga/music/l{i:02}.' + ('ogg' if i in (4, 5, 7, 9, 10) else 'mp3'))
              for i in range(1, 11)})

SFX_RMS, MUSIC_RMS, PEAK = 0.14, 0.11, 0.95


def install(name, source, rel, downloads, folder, target_rms):
    src = downloads / rel
    sound = aud.Sound(str(src))
    data = sound.data()
    rate = int(sound.specs[0])
    mono = data.mean(axis=1) if data.ndim > 1 else data
    rms = float(np.sqrt((mono ** 2).mean()))
    peak = float(np.abs(data).max())
    gain = min(target_rms / max(rms, 1e-6), PEAK / max(peak, 1e-6))
    out = OUT / folder / f'{name}.ogg'
    out.parent.mkdir(parents=True, exist_ok=True)
    channels = data.shape[1] if data.ndim > 1 else 1
    sound.volume(gain).write(str(out), rate, channels, aud.FORMAT_FLOAT32, aud.CONTAINER_OGG, aud.CODEC_VORBIS, 160000)
    print(f'AUDIO_DONE {name} {len(mono) / rate:.2f}s rms {rms:.3f} gain {gain:.2f} <- {src.name}', flush=True)
    return name, source, src.name, len(mono) / rate


def main():
    args = sys.argv[sys.argv.index('--') + 1:]
    downloads = Path(args[0])
    if '--only' in args:
        only = args[args.index('--only') + 1].split(',')
        credits = OUT / 'CREDITS.md'
        text = credits.read_text(encoding='utf-8')
        for name in only:
            source, rel = SFX[name]
            name, source, file, _ = install(name, source, rel, downloads, 'SFX', SFX_RMS)
            title, author, url = SOURCES[source]
            row = f'| {name} | {file} | {title} | {author} | {url} |'
            if f'| {name} |' not in text:
                text = text.rstrip('\n') + '\n' + row + '\n'
        credits.write_text(text, encoding='utf-8')
        print('AUDIO_COMPLETE', len(only), flush=True)
        return
    rows = []
    for name, (source, rel) in SFX.items():
        rows.append(install(name, source, rel, downloads, 'SFX', SFX_RMS))
    for name, (source, rel) in MUSIC.items():
        rows.append(install(name, source, rel, downloads, 'Music', MUSIC_RMS))
    lines = ['# Soul Hunter CC0 audio', '',
             'All files are CC0 1.0 (public domain dedication, https://creativecommons.org/publicdomain/zero/1.0/),',
             'checked on each source page on 2026-09-25. Attribution is not required; it is given here as thanks.',
             'Files were loudness-normalised and re-encoded by ArtSource/Generators/install_cc0_audio.py.', '',
             '| Game sound | Source file | Pack / track | Author | Page |', '|---|---|---|---|---|']
    for name, source, file, _ in rows:
        title, author, url = SOURCES[source]
        lines.append(f'| {name} | {file} | {title} | {author} | {url} |')
    (OUT / 'CREDITS.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('AUDIO_COMPLETE', len(rows), flush=True)


if __name__ == '__main__':
    main()
