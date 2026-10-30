# Soul Hunter: AI content disclosure (for the store page)

Owner, 2026-10-30: keep the art and disclose it ("option 1"). The pictures are the owner's own (`Assets/Art/UI/CREDITS.md`). This page lists what was made with AI tools, so the Steam AI-content form and store page say it correctly.

Before submitting, check the current wording of Steam's AI-content survey in Steamworks. This text follows its two parts: content made with AI before release, and content generated live while the game runs.

## Draft text for the store page

> Some of Soul Hunter's 2D artwork was created by the developer with the help of AI image tools: the title logo and splash picture, the main menu and loading backgrounds, one ground texture, and most item and upgrade icons. Some icons were then enlarged with an AI upscaler. The developer also used an AI coding assistant to write game code and the scripts that build the 3D models, effects and generated icons. The game does not generate any content with AI while it is played. All music and sound effects are human-made recordings released under CC0 (see the in-game Credits).

## Pre-generated content: what and where

| Content | Made with | Where the player sees it |
|---|---|---|
| Title logo `Assets/Art/UI/SH10_Logo.png`, from `sprite_4` and `sprite_5` | AI image generation, then AI upscaling (Real-ESRGAN, 2026-10-08) | Main menu |
| Splash and app icon source `Assets/sprite_126.png`, `sprite_130.png`; app icon `Assets/_Project/Art/AppIcon/SH10_AppIcon.png` | AI image generation, then AI upscaling (2026-10-12) | Splash screen, app icon (Windows and Android) |
| `ChatGPT Image Aug 16, 2026, 02_09_53 PM.png`, `… 06_10_52 PM.png` | AI image generation | MainMenu scene (background) |
| `ChatGPT Image Aug 16, 2026, 02_15_37 PM.png` | AI image generation | Bootstrap / loading scene |
| `Assets/Art/Materials/ChatGPT Image Aug 10 … 01_44_13 AM.png`, `… Aug 17 … 01_45_17 PM.png`, `Art/UI/ChatGPT Image Aug 13 … 12_51_14 AM.png` | AI image generation | Materials (one is `Ground_Mat`) |
| 30 item and upgrade icons `Assets/Art/UI/sprite_*.png` (sprite_16, 32, 34-38, 40, 44, 45, 48, 77, 87-90, 92, 93, 97, 98, 101-107, 109, 110, 113), plus sprite_53 in `Main_Gameplay` and sprite_56 inside another UI asset | AI image generation; 30 of them upscaled with Real-ESRGAN (2026-10-07; originals in `ArtSource/SH10_OldIcons_Original`) | Level-up cards, HUD item slots, character select, chests, Collection, Results |
| Icons `SH10_Icon_Bible/Cross/Duplicator/Spellbinder/SoulEgg/GoldPouch`, `SH10_PowerUp_*` | Drawn by scripts the AI coding assistant wrote (`ArtSource/Generators/build_icons_free.py`, `build_powerup_icons.py`) and renders of the game's own models; no image model | Level-up cards, Power Up shop |
| 3D models (heroes, enemies, merchant, pickups, chest, stage floors and props), VFX sprites, UI plates | Blender and Python generator scripts written with the AI coding assistant (`ArtSource/Generators/`); no image model | Everywhere in play |
| Game code | Written with an AI coding assistant | n/a |

The list of every used picture and the asset that uses it is in `D:/SoulHunter-Validation/run-20261030-audit/ai-art-usage.txt`.

## Not AI

- Music and sound effects: CC0 recordings by human authors, credited in `Assets/_Project/Audio/CC0/CREDITS.md`. The synthesised fallback clips are made by code and are not used while the CC0 files exist.
- Fonts: Cinzel and Barlow (SIL OFL), credited in `Assets/Art/Fonts/CREDITS.md`.
- Story and text of the levels: the owner's `Docs/Game_Design_Document.md`.

## Live-generated content

None. Nothing in the game calls an AI service or model while it runs.
