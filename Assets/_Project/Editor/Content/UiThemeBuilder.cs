#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Sets up the UI theme system once (owner, 2026-10-04): imports the theme art made by
    /// ArtSource/Generators/build_ui_theme_art.py (icons and 9-slice plates in Assets/Art/UI/Theme), creates the four
    /// preset themes and makes Ember Pact (owner choice, 2026-10-07) the active one in GameContent. After this, designers only use Theme Studio.
    /// Menu Soul Hunter/Content/Build UI Themes; batch: -executeMethod SoulHunter.Editor.Content.UiThemeBuilder.BuildFromCommandLine.
    /// Existing preset assets are left as the designers edited them; only missing ones are created.
    /// </summary>
    public static class UiThemeBuilder
    {
        public const string ArtFolder = "Assets/Art/UI/Theme";
        public const string ThemeFolder = "Assets/_Project/Data/Config/UiThemes";

        // 9-slice borders in pixels (left, bottom, right, top), from the plate drawings in build_ui_theme_art.py.
        private static readonly (string Name, Vector4 Border)[] Plates =
        {
            ("plate_button", new Vector4(46f, 34f, 46f, 52f)),
            ("plate_card", new Vector4(46f, 46f, 46f, 46f)),
            ("plate_panel", new Vector4(40f, 40f, 40f, 40f)),
            ("plate_shine", Vector4.zero),
        };

        private static readonly string[] IconNames =
            { "icon_soul_flame", "icon_soul_gem", "icon_gear", "icon_scythe", "icon_moon", "icon_back", "icon_check", "icon_play", "icon_coin" };

        [MenuItem("Soul Hunter/Content/Build UI Themes")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            foreach (var plate in Plates) ImportSprite($"{ArtFolder}/{plate.Name}.png", plate.Border);
            foreach (var icon in IconNames) ImportSprite($"{ArtFolder}/{icon}.png", Vector4.zero);

            Directory.CreateDirectory(ThemeFolder);
            Preset("UiTheme_SoulCandy", SoulCandy);
            Preset("UiTheme_MoonlitReaper", MoonlitReaper);
            var emberPact = Preset("UiTheme_EmberPact", EmberPact);
            Preset("UiTheme_ClassicCrimson", ClassicCrimson);

            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>("Assets/Resources/GameContent.asset");
            if (catalog != null && catalog.UiTheme == null)
            {
                catalog.UiTheme = emberPact;
                EditorUtility.SetDirty(catalog);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiThemeBuilder] Themes ready in {ThemeFolder}; active: {(catalog != null && catalog.UiTheme != null ? catalog.UiTheme.DisplayName : "none")}.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
            EditorApplication.Exit(0);
        }

        private static void ImportSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning($"[UiThemeBuilder] {path} missing: run ArtSource/Generators/build_ui_theme_art.py."); return; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; // sharp edges on the plates and icons
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }

        private static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");

        private static UiThemeAsset Preset(string file, System.Action<UiThemeAsset> fill)
        {
            string path = $"{ThemeFolder}/{file}.asset";
            var theme = AssetDatabase.LoadAssetAtPath<UiThemeAsset>(path);
            if (theme != null) return theme; // designers own it now
            theme = ScriptableObject.CreateInstance<UiThemeAsset>();
            theme.Icon.Start = Art("icon_play");
            theme.Icon.PowerUp = Art("icon_soul_gem");
            theme.Icon.Options = Art("icon_gear");
            theme.Icon.Quit = Art("icon_scythe");
            theme.Icon.Back = Art("icon_back");
            theme.Icon.Confirm = Art("icon_check");
            theme.Icon.Resume = Art("icon_soul_flame");
            theme.Icon.Buy = Art("icon_coin");
            theme.Icon.Emblem = Art("icon_moon");
            // Main menu sections (P3-02): icons already drawn for the theme (owner: no new art).
            theme.Icon.Collection = Art("icon_soul_gem");
            theme.Icon.Bestiary = Art("icon_soul_flame");
            theme.Icon.Unlocks = Art("icon_check");
            theme.Icon.Credits = Art("icon_moon");
            theme.Shape.Button = Art("plate_button");
            theme.Shape.Card = Art("plate_card");
            theme.Shape.Panel = Art("plate_panel");
            theme.Shape.Shine = Art("plate_shine");
            fill(theme);
            AssetDatabase.CreateAsset(theme, path);
            return theme;
        }

        private static Color C(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = alpha;
            return c;
        }

        // ---------------------------------------------------------------- presets (the B1-B3 samples the owner saw)

        /// <summary>B1 Soul Candy: bright soul-candy buttons on violet night, cartoon outlines (the default until 2026-10-07).</summary>
        private static void SoulCandy(UiThemeAsset t)
        {
            t.DisplayName = "Soul Candy";
            t.Description = "Bright, friendly and bouncy: candy-coloured buttons with thick outlines on a violet night. Made to catch young players.";
            // The class defaults are Soul Candy.
        }

        /// <summary>B2 Moonlit Reaper: silver and moon gold on deep night blue; calm, elegant.</summary>
        private static void MoonlitReaper(UiThemeAsset t)
        {
            t.DisplayName = "Moonlit Reaper";
            t.Description = "Calm and elegant: silver buttons and moon gold on a deep night blue, like Kael hunting under the moon.";
            var s = t.Surfaces; s.Veil = C("#060a1c", 0.84f); s.Panel = C("#1b2650", 0.96f); s.PanelEdge = C("#070b1e"); s.Row = C("#26346a"); s.Backdrop = C("#0b1230");
            var b = t.Buttons; b.Normal = C("#b8c6e8"); b.Hover = C("#d9e3ff"); b.Primary = C("#ffd36b"); b.PrimaryHover = C("#ffe39a");
            b.Danger = C("#e8728a"); b.Focus = C("#fff1a8"); b.Pressed = C("#a8a8b8"); b.Disabled = C("#151b33", 0.92f); b.Edge = C("#070b1e");
            var k = t.Cards; k.Card = C("#eef2ff"); k.Selected = C("#ffe39a"); k.Maxed = C("#ffd36b"); k.Locked = C("#262e4f"); k.Off = C("#2b2e38");
            var x = t.Text; x.OnDark = C("#f4f7ff"); x.SoftOnDark = C("#c9d3f2"); x.DimOnDark = C("#8f9cc8"); x.OnLight = C("#0d1433"); x.SoftOnLight = C("#33406e");
            x.Locked = C("#8a93b5"); x.Accent = C("#ffd36b"); x.Danger = C("#ff7a8f"); x.Tag = C("#3a59c9"); x.Outline = C("#070b1e"); x.OutlineWidth = 0.2f;
            var h = t.Hud; h.LabelBacking = C("#070b1e", 0.85f); h.HealthBack = C("#070b1e", 0.9f); h.SlotEmpty = C("#0b1230", 0.9f); h.SlotFilled = C("#1b2650", 0.92f);
            h.SlotBorder = C("#6f7fb8", 0.7f); h.StoryCard = C("#070b1e", 0.78f);
            t.Rewards.Soul = C("#9fe3ff");
            t.Motions.BreatheAmount = 0.03f; t.Motions.BreatheSpeed = 0.5f; t.Motions.ShineEvery = 4f; t.Motions.TitleBounce = false;
        }

        /// <summary>B3 Ember Pact: fiery orange and blood red on charcoal; bold, intense.</summary>
        private static void EmberPact(UiThemeAsset t)
        {
            t.DisplayName = "Ember Pact";
            t.Description = "Bold and fiery: ember orange and blood red on charcoal, the pact Kael sealed to save his daughter.";
            var s = t.Surfaces; s.Veil = C("#120604", 0.86f); s.Panel = C("#3a1610", 0.96f); s.PanelEdge = C("#140604"); s.Row = C("#55211a"); s.Backdrop = C("#1e0b07");
            var b = t.Buttons; b.Normal = C("#ff8a3d"); b.Hover = C("#ffab70"); b.Primary = C("#ffd23f"); b.PrimaryHover = C("#ffe27a");
            b.Danger = C("#b8202c"); b.Focus = C("#fff07a"); b.Pressed = C("#b0a090"); b.Disabled = C("#26140f", 0.92f); b.Edge = C("#140604");
            var k = t.Cards; k.Card = C("#fff1e2"); k.Selected = C("#ffd9a8"); k.Maxed = C("#ffd23f"); k.Locked = C("#3a2620"); k.Off = C("#33302e");
            var x = t.Text; x.OnDark = C("#fff6ee"); x.SoftOnDark = C("#f2cfb8"); x.DimOnDark = C("#c79a82"); x.OnLight = C("#2a0c06"); x.SoftOnLight = C("#5e2c1e");
            x.Locked = C("#a08478"); x.Accent = C("#ffd23f"); x.Danger = C("#ff5a5a"); x.Tag = C("#d8323c"); x.Outline = C("#140604"); x.OutlineWidth = 0.22f;
            var h = t.Hud; h.LabelBacking = C("#140604", 0.85f); h.HealthBack = C("#140604", 0.9f); h.SlotEmpty = C("#1e0b07", 0.9f); h.SlotFilled = C("#3a1610", 0.92f);
            h.SlotBorder = C("#a0503a", 0.7f); h.StoryCard = C("#140604", 0.78f);
            t.Rewards.Soul = C("#ffb35e");
        }

        /// <summary>The dark crimson look of 2026-10-04 (b1a7ed27), flat, without plates.</summary>
        private static void ClassicCrimson(UiThemeAsset t)
        {
            t.DisplayName = "Classic Crimson";
            t.Description = "The earlier dark, high-contrast crimson look: flat buttons with a red edge, orange focus.";
            t.Shape.Button = t.Shape.Card = t.Shape.Panel = null; t.Shape.EdgeWidth = 2f;
            var s = t.Surfaces; s.Veil = new Color(0f, 0f, 0f, 0.9f); s.Panel = new Color(0.035f, 0.022f, 0.028f, 0.97f); s.PanelEdge = new Color(0.52f, 0.08f, 0.09f);
            s.Row = new Color(0.09f, 0.05f, 0.06f); s.Backdrop = new Color(0.012f, 0.008f, 0.01f);
            var b = t.Buttons; b.Normal = new Color(0.17f, 0.04f, 0.05f); b.Hover = new Color(0.30f, 0.07f, 0.08f); b.Primary = new Color(0.46f, 0.07f, 0.08f);
            b.PrimaryHover = new Color(0.60f, 0.10f, 0.10f); b.Danger = new Color(0.30f, 0.05f, 0.06f); b.Focus = new Color(0.85f, 0.45f, 0.08f);
            b.Pressed = new Color(0.65f, 0.32f, 0.06f); b.Disabled = new Color(0.07f, 0.05f, 0.06f, 0.85f); b.Edge = new Color(0.78f, 0.14f, 0.12f, 0.9f);
            var k = t.Cards; k.Card = new Color(0.10f, 0.05f, 0.06f); k.Selected = new Color(0.55f, 0.16f, 0.07f); k.Maxed = new Color(0.36f, 0.26f, 0.05f); k.Locked = new Color(0.06f, 0.05f, 0.06f); k.Off = new Color(0.17f, 0.16f, 0.16f);
            var x = t.Text; x.OnDark = new Color(0.96f, 0.92f, 0.86f); x.SoftOnDark = new Color(0.86f, 0.80f, 0.74f); x.DimOnDark = new Color(0.62f, 0.56f, 0.52f);
            x.OnLight = x.OnDark; x.SoftOnLight = x.SoftOnDark; x.Locked = new Color(0.45f, 0.42f, 0.40f); x.Accent = new Color(1f, 0.80f, 0.30f);
            x.Danger = new Color(1f, 0.38f, 0.32f); x.Tag = x.Accent; x.Outline = Color.black; x.OutlineWidth = 0f;
            t.Motions.Breathe = false; t.Motions.Shine = false; t.Motions.TitleBounce = false; t.Motions.IconMotion = false;
        }
    }
}
#endif
