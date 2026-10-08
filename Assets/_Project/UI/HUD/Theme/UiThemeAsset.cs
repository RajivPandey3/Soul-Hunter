using System;
using TMPro;
using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The one place the game's whole UI look lives (owner, 2026-10-04: "pure game ka single theme system ... source of
    /// truth single ... controlled by designer and layman persons, not any developer"). Every panel, button, card, HUD label
    /// and reward screen reads its colours, images, icons, fonts, motion and spacing from the active theme through
    /// <see cref="UITheme"/>; no UI script keeps its own colours. Designers edit it in Soul Hunter > Theme Studio (or the
    /// Inspector); changes show at once, also while the game is playing.
    ///
    /// Several themes can exist (presets); the game uses exactly one: the one set as active in GameContent
    /// (<see cref="SoulHunter.Gameplay.Data.GameContentCatalog.UiTheme"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Soul Hunter/UI Theme", fileName = "UiTheme")]
    public sealed class UiThemeAsset : ScriptableObject
    {
        [Tooltip("A short name designers see in Theme Studio, e.g. \"Soul Candy\".")]
        public string DisplayName = "Soul Candy";
        [TextArea(2, 4), Tooltip("What this theme is for, in plain words.")]
        public string Description = "";

        [Serializable] public sealed class SurfaceColours
        {
            [Tooltip("The dark layer drawn over the game behind menus (pause, level-up, shop). More opaque = darker.")]
            public Color Veil = new Color(0.07f, 0.03f, 0.14f, 0.82f);
            [Tooltip("Windows and panels (Options, stats, Power Up backing).")]
            public Color Panel = new Color(0.20f, 0.10f, 0.42f, 0.96f);
            [Tooltip("The border line around panels.")]
            public Color PanelEdge = new Color(0.11f, 0.04f, 0.20f, 1f);
            [Tooltip("Rows inside panels (each Options line).")]
            public Color Row = new Color(0.28f, 0.16f, 0.55f, 1f);
            [Tooltip("The full-screen background behind the Results screen.")]
            public Color Backdrop = new Color(0.10f, 0.04f, 0.22f, 1f);
        }

        [Serializable] public sealed class ButtonColours
        {
            [Tooltip("Ordinary buttons (Options, Power Up, Back...).")]
            public Color Normal = new Color(0.30f, 0.86f, 1f, 1f);
            [Tooltip("An ordinary button under the mouse.")]
            public Color Hover = new Color(0.55f, 0.94f, 1f, 1f);
            [Tooltip("The main action (Start, Resume, Confirm, Buy, Open, Done).")]
            public Color Primary = new Color(1f, 0.80f, 0.22f, 1f);
            [Tooltip("The main action under the mouse.")]
            public Color PrimaryHover = new Color(1f, 0.90f, 0.45f, 1f);
            [Tooltip("Leaving or destructive actions (Quit, Refund).")]
            public Color Danger = new Color(1f, 0.36f, 0.48f, 1f);
            [Tooltip("The button chosen with keyboard or controller. Must stand out from everything else.")]
            public Color Focus = new Color(1f, 0.95f, 0.45f, 1f);
            [Tooltip("A button being pressed.")]
            public Color Pressed = new Color(0.85f, 0.85f, 0.85f, 1f);
            [Tooltip("A button that cannot be used right now (keep it dark so its text still reads).")]
            public Color Disabled = new Color(0.12f, 0.08f, 0.20f, 0.92f);
            [Tooltip("The thin coloured line around buttons and cards. Use the outline colour for a cartoon look.")]
            public Color Edge = new Color(0.11f, 0.04f, 0.20f, 1f);
        }

        [Serializable] public sealed class CardColours
        {
            [Tooltip("An item card or Power Up cell.")]
            public Color Card = new Color(0.97f, 0.95f, 1f, 1f);
            [Tooltip("The chosen card.")]
            public Color Selected = new Color(0.60f, 0.96f, 1f, 1f);
            [Tooltip("A Power Up that is fully bought.")]
            public Color Maxed = new Color(1f, 0.85f, 0.40f, 1f);
            [Tooltip("Something still locked.")]
            public Color Locked = new Color(0.22f, 0.17f, 0.32f, 1f);
            [Tooltip("The frame around the chosen card (so the choice never depends on colour alone).")]
            public Color SelectedEdge = new Color(1f, 0.82f, 0.25f, 1f);
            [Tooltip("Thickness of that frame (0 = none).")] [Range(0f, 10f)] public float SelectedEdgeWidth = 5f;
        }

        [Serializable] public sealed class TextColours
        {
            [Tooltip("Main text on dark panels.")]
            public Color OnDark = new Color(1f, 0.98f, 1f, 1f);
            [Tooltip("Descriptions and secondary lines on dark panels.")]
            public Color SoftOnDark = new Color(0.86f, 0.82f, 0.98f, 1f);
            [Tooltip("Headers and quiet labels on dark panels.")]
            public Color DimOnDark = new Color(0.70f, 0.64f, 0.88f, 1f);
            [Tooltip("Text on light cards and buttons.")]
            public Color OnLight = new Color(0.11f, 0.04f, 0.20f, 1f);
            [Tooltip("Secondary text on light cards.")]
            public Color SoftOnLight = new Color(0.26f, 0.19f, 0.43f, 1f);
            [Tooltip("Text of locked things.")]
            public Color Locked = new Color(0.62f, 0.58f, 0.72f, 1f);
            [Tooltip("Highlights: titles, gold, changed stats, NEW! tags.")]
            public Color Accent = new Color(1f, 0.90f, 0.42f, 1f);
            [Tooltip("Warnings (Banish).")]
            public Color Danger = new Color(1f, 0.35f, 0.42f, 1f);
            [Tooltip("The NEW! / Level tags on cards.")]
            public Color Tag = new Color(0.93f, 0.17f, 0.45f, 1f);
            [Tooltip("The dark outline drawn around titles and button labels (cartoon look).")]
            public Color Outline = new Color(0.11f, 0.04f, 0.20f, 1f);
            [Tooltip("Outline thickness for titles and button labels (0 = none).")]
            [Range(0f, 0.5f)] public float OutlineWidth = 0.22f;
        }

        [Serializable] public sealed class RarityColours
        {
            [Tooltip("Common items (most passives).")] public Color Good = new Color(0.26f, 0.88f, 0.48f, 1f);
            [Tooltip("Rare items.")] public Color Rare = new Color(0.27f, 0.64f, 1f, 1f);
            [Tooltip("Epic items.")] public Color Epic = new Color(0.75f, 0.44f, 1f, 1f);
            [Tooltip("Legendary items and evolutions.")] public Color Legend = new Color(1f, 0.76f, 0.24f, 1f);
        }

        [Serializable] public sealed class HudColours
        {
            [Tooltip("Text of the in-game labels (kills, gold, time, level).")]
            public Color LabelText = new Color(1f, 1f, 1f, 1f);
            [Tooltip("The dark plate behind those labels.")]
            public Color LabelBacking = new Color(0.08f, 0.03f, 0.16f, 0.85f);
            [Tooltip("The hero's health bar.")]
            public Color HealthFill = new Color(1f, 0.25f, 0.38f, 1f);
            [Tooltip("Behind the health bar.")]
            public Color HealthBack = new Color(0.08f, 0.03f, 0.16f, 0.9f);
            [Tooltip("The experience bar's fill (top of the screen).")]
            public Color XpFill = new Color(0.25f, 0.55f, 1f, 1f);
            [Tooltip("An empty item slot.")]
            public Color SlotEmpty = new Color(0.10f, 0.05f, 0.20f, 0.9f);
            [Tooltip("A filled item slot.")]
            public Color SlotFilled = new Color(0.16f, 0.08f, 0.30f, 0.92f);
            [Tooltip("The border of an ordinary item slot.")]
            public Color SlotBorder = new Color(0.45f, 0.36f, 0.70f, 0.7f);
            [Tooltip("The border of an evolved weapon's slot.")]
            public Color SlotEvolved = new Color(1f, 0.80f, 0.25f, 1f);
            [Tooltip("The story card shown when a level starts.")]
            public Color StoryCard = new Color(0.08f, 0.03f, 0.16f, 0.78f);
            [Tooltip("Touch controls (phones): the stick base and buttons.")]
            public Color TouchPad = new Color(0f, 0f, 0f, 0.35f);
            [Tooltip("Touch controls: the stick knob.")]
            public Color TouchKnob = new Color(1f, 1f, 1f, 0.65f);
        }

        [Serializable] public sealed class RewardColours
        {
            [Tooltip("Gold numbers and reward stars on the chest screen.")]
            public Color Gold = new Color(1f, 0.85f, 0.30f, 1f);
            [Tooltip("The soul glow on chests and messages.")]
            public Color Soul = new Color(0.37f, 0.95f, 1f, 1f);
            [Tooltip("The Arcana glow.")]
            public Color Arcana = new Color(0.78f, 0.50f, 1f, 1f);
            [Tooltip("An evolution's reward frame and stars.")]
            public Color Evolution = new Color(1f, 0.45f, 0.40f, 1f);
            [Tooltip("The yellow of the best value in the Results table (a Vampire Survivors rule).")]
            public Color Best = new Color(1f, 0.84f, 0.29f, 1f);
            [Tooltip("The 'show unlocks' button on the Results screen (green, as in Vampire Survivors).")]
            public Color Unlock = new Color(0.20f, 0.72f, 0.36f, 1f);
        }

        [Serializable] public sealed class Shapes
        {
            [Tooltip("The picture used for every button (white/grey: the button colours tint it). Leave empty for flat buttons.")]
            public Sprite Button;
            [Tooltip("The picture used for cards (level-up, Power Up cells). Leave empty for flat cards.")]
            public Sprite Card;
            [Tooltip("The picture used for panels and windows. Leave empty for flat panels.")]
            public Sprite Panel;
            [Tooltip("The light band that sweeps across buttons (Shine).")]
            public Sprite Shine;
            [Tooltip("Rows, info boxes, HUD labels and item slots (a light bevel). Leave empty for flat rows.")]
            public Sprite Row;
            [Tooltip("A recessed groove: slider tracks. Leave empty for flat.")]
            public Sprite Inset;
            [Tooltip("A round raised knob: slider handles and switch knobs.")]
            public Sprite Knob;
            [Tooltip("A capsule: switch tracks.")]
            public Sprite Track;
            [Tooltip("An empty tick box with a light rim (mode chips, filters).")]
            public Sprite TickBox;
            [Tooltip("The tick drawn in a ticked box.")]
            public Sprite Tick;
            [Tooltip("Width of the coloured line around buttons and cards (0 = none).")]
            [Range(0f, 6f)] public float EdgeWidth = 0f;
        }

        [Serializable] public sealed class Icons
        {
            [Tooltip("Start / Play.")] public Sprite Start;
            [Tooltip("Power Up.")] public Sprite PowerUp;
            [Tooltip("Options.")] public Sprite Options;
            [Tooltip("Quit / leave.")] public Sprite Quit;
            [Tooltip("Back.")] public Sprite Back;
            [Tooltip("Confirm / Done / Open.")] public Sprite Confirm;
            [Tooltip("Resume.")] public Sprite Resume;
            [Tooltip("Buy.")] public Sprite Buy;
            [Tooltip("Pause / moon emblem.")] public Sprite Emblem;
            [Tooltip("The coin shown next to gold amounts and prices (in colour; the Buy button may use a one-colour coin). Empty = the Buy icon.")]
            public Sprite Coin;
        }

        [Serializable] public sealed class Fonts
        {
            [Tooltip("Titles (LEVEL UP!, RESULTS...). Leave empty to keep the scene's font.")]
            public TMP_FontAsset Title;
            [Tooltip("Buttons, cards and body text. Leave empty to keep the scene's font.")]
            public TMP_FontAsset Body;
            [Tooltip("The title look (outline and shadow) made for the Title font. Leave empty for the plain look.")]
            public Material TitleMaterial;
            [Tooltip("Light text on dark windows and buttons: raised by a dark outline and a soft shadow (made for the Body font).")]
            public Material OnDarkMaterial;
            [Tooltip("Dark text on light cards and bright buttons: embossed by a thin light edge below (made for the Body font).")]
            public Material OnLightMaterial;
            [Tooltip("Titles get a light-to-deep gradient of their colour.")]
            public bool TitleGradient;
            [Tooltip("The Title and Body fonts are already heavy: never add Unity's fake bold (it widens letters and blurs them).")]
            public bool NoFakeBold;
        }

        [Serializable] public sealed class Motion
        {
            [Tooltip("The chosen button gently grows (only that one, so the eye knows where it is).")] public bool Breathe = true;
            [Tooltip("How much it grows (0.04 = 4%).")] [Range(0f, 0.12f)] public float BreatheAmount = 0.04f;
            [Tooltip("Breaths per second.")] [Range(0.2f, 2f)] public float BreatheSpeed = 0.65f;
            [Tooltip("A light band sweeps across the chosen button now and then.")] public bool Shine = true;
            [Tooltip("Seconds between sweeps.")] [Range(0.8f, 8f)] public float ShineEvery = 3f;
            [Tooltip("Thickness of the ring around the chosen button (keyboard/controller). 0 = colour only, which some players cannot see.")]
            [Range(0f, 8f)] public float FocusRingWidth = 4f;
            [Tooltip("Buttons lift a little under the mouse.")] public bool HoverLift = true;
            [Tooltip("Buttons squash a little when pressed.")] public bool PressSquish = true;
            [Tooltip("Titles (LEVEL UP!) bounce gently. Off by default: one moving thing per screen reads best.")] public bool TitleBounce = false;
            [Tooltip("The chosen button's icon moves a little (flame flickers, scythe swings).")] public bool IconMotion = true;
        }

        [Serializable] public sealed class Layout
        {
            [Tooltip("Icon size on buttons, as a share of the button height.")] [Range(0.3f, 1f)] public float IconSize = 0.78f;
            [Tooltip("Space from the button's left edge to its icon, as a share of the button height.")] [Range(0f, 1f)] public float IconInset = 0.28f;
            [Tooltip("Where button labels sit: centred in the whole button, or centred in the space beside the icon.")]
            public bool CentreLabelBesideIcon = true;
            [Tooltip("Largest text size on buttons.")] [Range(14f, 60f)] public float ButtonTextMax = 36f;
            [Tooltip("Largest title size.")] [Range(30f, 110f)] public float TitleTextMax = 72f;

            // One size per kind of control for the whole game (owner, 2026-10-07: "button size sabka ek jaisa ... ek hee
            // standard"). Sizes are in screen units of the 1920x1080 layout every menu scales from.
            [Tooltip("Width of every action button (Start, Back, Confirm, Buy, Resume, OPEN, DONE, Reroll...).")]
            [Range(160f, 480f)] public float ButtonWidth = 300f;
            [Tooltip("Height of every action button.")] [Range(40f, 120f)] public float ButtonHeight = 68f;
            [Tooltip("Width of every tab and toggle chip (Options sections, run modes, FILTER).")]
            [Range(90f, 300f)] public float TabWidth = 150f;
            [Tooltip("Height of every tab and toggle chip.")] [Range(30f, 90f)] public float TabHeight = 54f;
            [Tooltip("Largest text size on tabs.")] [Range(12f, 40f)] public float TabTextMax = 24f;
            [Tooltip("Space between buttons placed side by side or in a column.")] [Range(4f, 40f)] public float ButtonGap = 12f;
            [Tooltip("Size of a tick box on a toggle chip, as a share of its height.")] [Range(0.3f, 0.9f)] public float TickBoxSize = 0.5f;
            [Tooltip("Buttons with an icon centre the icon and the label together as one group (instead of the label in the space beside the icon).")]
            public bool CentreIconWithLabel;
            [Tooltip("Space between a button's icon and its label, as a share of the button height.")] [Range(0f, 0.6f)] public float IconGap = 0.16f;
            [Tooltip("Button icons take the label's colour (use one-colour icons), so they always stand out like the text.")]
            public bool IconsLikeLabel;
            [Tooltip("ON/OFF switches (Options): width and height.")]
            public float SwitchWidth = 132f;
            public float SwitchHeight = 52f;
            [Tooltip("A gold bar under the open tab, so the open tab is never shown by colour alone.")]
            public bool OpenTabBar = true;
        }

        public SurfaceColours Surfaces = new SurfaceColours();
        public ButtonColours Buttons = new ButtonColours();
        public CardColours Cards = new CardColours();
        public TextColours Text = new TextColours();
        public RarityColours Rarity = new RarityColours();
        public HudColours Hud = new HudColours();
        public RewardColours Rewards = new RewardColours();
        public Shapes Shape = new Shapes();
        public Icons Icon = new Icons();
        public Fonts Font = new Fonts();
        public Motion Motions = new Motion();
        public Layout Layouts = new Layout();

        // ---------------------------------------------------------------- the active theme

        private static UiThemeAsset _active;
        private static UiThemeAsset _fallback;

        /// <summary>Raised whenever the active theme or any of its values changes; themed UI re-applies itself.</summary>
        public static event Action Changed;

        /// <summary>
        /// The theme the game uses: GameContent's <c>UiTheme</c>. Without one (tests that build UI on their own), a default
        /// theme in memory, so the UI always has a look.
        /// </summary>
        public static UiThemeAsset Active
        {
            get
            {
                if (_active != null) return _active;
                var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
                if (catalog != null && catalog.UiTheme != null) return _active = catalog.UiTheme;
                if (_fallback == null) { _fallback = CreateInstance<UiThemeAsset>(); _fallback.name = "UiTheme (default)"; _fallback.hideFlags = HideFlags.DontSave; }
                return _fallback;
            }
        }

        /// <summary>Makes <paramref name="theme"/> the active theme (Theme Studio's preset switch, tests) and re-applies the UI.</summary>
        public static void SetActive(UiThemeAsset theme)
        {
            _active = theme;
            NotifyChanged();
        }

        /// <summary>Tells every themed element to re-apply (after values were edited).</summary>
        public static void NotifyChanged() => Changed?.Invoke();

        private void OnValidate()
        {
            // Inspector or Theme Studio edits show at once.
            if (_active == this || (_active == null && SoulHunter.Gameplay.Data.GameContentCatalog.Load()?.UiTheme == this)) NotifyChanged();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _active = null; Changed = null; }
    }
}
