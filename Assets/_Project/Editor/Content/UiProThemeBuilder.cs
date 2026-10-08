#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds the "Ember Pact Pro" look for the owner's approval (2026-10-08: "pro type", full brightness and contrast,
    /// raised sharp text, Full HD): sharp font assets from Assets/Art/Fonts (2048 atlas, 90 pt sampling), three raised-
    /// text materials, the 3D plates from ArtSource/Generators/build_ui_pro_plates.py, and a brighter copy of Ember Pact
    /// with them. It goes into GameContent.UiThemePreview only; the game's own look (UiTheme) is not changed.
    /// Menu Soul Hunter/Content/Build Pro Look (preview); batch: -executeMethod SoulHunter.Editor.Content.UiProThemeBuilder.BuildFromCommandLine.
    /// </summary>
    public static class UiProThemeBuilder
    {
        private const string Fonts = "Assets/Art/Fonts";
        private const string Plates = "Assets/Art/UI/ThemePro";
        private const string Out = "Assets/_Project/Data/Config/UiThemes";
        private const string Characters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            " °±·×–—‘’“”•…→←";

        [MenuItem("Soul Hunter/Content/Build Pro Look (preview)")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var title = FontAsset("Cinzel-ExtraBold");
            var body = FontAsset("BarlowSemiCondensed-Bold");

            var ember = AssetDatabase.LoadAssetAtPath<UiThemeAsset>($"{Out}/UiTheme_EmberPact.asset");
            if (ember == null) throw new FileNotFoundException("Ember Pact theme missing");
            string path = $"{Out}/UiTheme_EmberPactPro.asset";
            var pro = AssetDatabase.LoadAssetAtPath<UiThemeAsset>(path);
            if (pro == null)
            {
                pro = Object.Instantiate(ember);
                AssetDatabase.CreateAsset(pro, path);
            }
            else EditorUtility.CopySerialized(ember, pro); // the preview always starts from Ember Pact's colours

            pro.name = "UiTheme_EmberPactPro";
            pro.DisplayName = "Ember Pact Pro";
            pro.Description = "Ember Pact with raised 3D buttons, embossed sharp text (Cinzel titles, Barlow labels), brighter windows and Full HD icons.";

            // Brighter windows and rows (owner: "brightness or contrast full"), same ember family.
            // Review 2026-10-08 (10): a deeper outer backdrop, a lighter window, brighter section headings.
            pro.Surfaces.Backdrop = new Color(0.07f, 0.025f, 0.02f, 1f);
            pro.Surfaces.Panel = new Color(0.40f, 0.16f, 0.10f, 1f); // opaque: nothing behind a window shows through
            pro.Surfaces.Row = new Color(0.55f, 0.23f, 0.15f, 1f);
            pro.Text.DimOnDark = new Color(1f, 0.80f, 0.62f, 1f);
            // (11) selection is the gold frame only; the chosen cell keeps its own colour.
            pro.Cards.Selected = pro.Cards.Card;
            pro.Surfaces.PanelEdge = new Color(1f, 0.74f, 0.32f, 1f);
            pro.Text.OnDark = new Color(1f, 0.98f, 0.95f, 1f);
            pro.Text.SoftOnDark = new Color(1f, 0.88f, 0.78f, 1f);
            pro.Text.OutlineWidth = 0f; // the materials carry outline and shadow

            pro.Shape.Button = Plate("pro_button");
            pro.Shape.Card = Plate("pro_card");
            pro.Shape.Panel = Plate("pro_panel");
            // (5) rows, boxes, HUD plates and slots get a light bevel; the slider has a groove and a round knob.
            pro.Shape.Row = Plate("pro_row");
            pro.Shape.Inset = Plate("pro_inset");
            pro.Shape.Knob = Plate("pro_knob");
            pro.Shape.Track = Plate("pro_track");
            pro.Shape.TickBox = Plate("pro_tickbox");
            pro.Shape.Tick = Plate("pro_tick");
            pro.Shape.EdgeWidth = 0f; // the plates have their own rim

            pro.Font.Title = title;
            pro.Font.Body = body;
            Color ink = pro.Text.Outline;
            pro.Font.TitleMaterial = TextMaterial(title, "Title_Raised", ink, outline: 0.22f, shadow: new Color(0f, 0f, 0f, 0.85f),
                offset: new Vector2(1f, -1f), dilate: 0.25f, softness: 0.35f);
            pro.Font.OnDarkMaterial = TextMaterial(body, "Body_OnDark", ink, outline: 0.17f, shadow: new Color(0f, 0f, 0f, 0.8f),
                offset: new Vector2(0.7f, -0.7f), dilate: 0.2f, softness: 0.3f);
            var light = pro.Text.OnDark; light.a = 0.55f;
            pro.Font.OnLightMaterial = TextMaterial(body, "Body_OnLight", ink, outline: 0f, shadow: light,
                offset: new Vector2(0f, -0.8f), dilate: 0.05f, softness: 0.05f);
            pro.Font.TitleGradient = true;
            pro.Font.NoFakeBold = true; // (1) both fonts are heavy already
            // ...and <b> tags or a Bold style anywhere (Results, cards) add nothing to them either.
            foreach (var font in new[] { title, body })
            {
                font.boldStyle = 0f;
                font.boldSpacing = 0f;
                font.material.SetFloat(ShaderUtilities.ID_WeightBold, font.normalStyle);
                EditorUtility.SetDirty(font);
            }

            // (8) one-colour button icons that take the label's colour, so they stand out on every button.
            pro.Icon.Start = Plate("mono_play");
            pro.Icon.PowerUp = Plate("mono_soul_gem");
            pro.Icon.Options = Plate("mono_gear");
            pro.Icon.Quit = Plate("mono_scythe");
            pro.Icon.Back = Plate("mono_back");
            pro.Icon.Confirm = Plate("mono_check");
            pro.Icon.Resume = Plate("mono_play");
            pro.Icon.Buy = Plate("mono_coin");
            pro.Icon.Emblem = Plate("mono_moon");
            pro.Icon.Coin = ember.Icon.Buy; // gold amounts and prices keep the coloured coin
            pro.Layouts.IconsLikeLabel = true;

            // (9) no light streak through the words; the focus ring and the gentle breath mark the chosen button.
            pro.Motions.Shine = false;
            // Owner 2026-10-08: every button must look the same size, so the chosen one no longer grows; the gold ring marks it.
            pro.Motions.Breathe = false;
            pro.Motions.FocusRingWidth = 3f;
            // Owner 2026-10-08: the near-black disabled Buy looked bad. A muted stone plate with light text reads as "not now".
            pro.Buttons.Disabled = new Color(0.40f, 0.31f, 0.27f, 1f);
            pro.Text.Locked = new Color(0.90f, 0.83f, 0.77f, 1f);

            pro.Layouts.CentreIconWithLabel = true;
            pro.Layouts.IconSize = 0.62f;
            pro.Layouts.IconGap = 0.16f;
            pro.Layouts.ButtonTextMax = 34f;
            pro.Layouts.TabTextMax = 26f;
            pro.Layouts.TabWidth = 150f; // (2) room for "Gameplay" and "Controls"

            EditorUtility.SetDirty(pro);
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>("Assets/Resources/GameContent.asset");
            // Owner 2026-10-08: "ok pass now apply it in project every where" - Ember Pact Pro is the game's look.
            if (catalog != null) { catalog.UiTheme = pro; catalog.UiThemePreview = pro; EditorUtility.SetDirty(catalog); }
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiProThemeBuilder] {pro.DisplayName} is the game's look ({path}).");
        }

        public static void BuildFromCommandLine()
        {
            int code = 0;
            try { Build(); }
            catch (System.Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        /// <summary>A static SDF font asset (2048 atlas, 90 pt sampling: sharp at every UI size) next to its font file.</summary>
        private static TMP_FontAsset FontAsset(string name)
        {
            string assetPath = $"{Fonts}/{name} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{Fonts}/{name}.ttf");
            if (font == null) throw new FileNotFoundException("Font missing", $"{Fonts}/{name}.ttf");
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            asset.name = name + " SDF";
            asset.TryAddCharacters(Characters, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.Log($"[UiProThemeBuilder] {name}: no glyphs for '{missing}'.");
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset, assetPath);
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                asset.atlasTextures[i].name = $"{name} Atlas {i}";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
            }
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>A material preset of the font's own material: an outline and a shadow (underlay) for raised text.</summary>
        private static Material TextMaterial(TMP_FontAsset font, string name, Color ink, float outline, Color shadow, Vector2 offset,
            float dilate, float softness)
        {
            string path = $"{Fonts}/{font.name} {name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(font.material) { name = $"{font.name} {name}" };
                AssetDatabase.CreateAsset(m, path);
            }
            else m.CopyPropertiesFromMaterial(font.material);
            m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.06f);
            m.SetFloat(ShaderUtilities.ID_WeightBold, font.normalStyle); // bold adds no weight to these heavy fonts
            if (outline > 0f)
            {
                m.EnableKeyword(ShaderUtilities.Keyword_Outline);
                m.SetFloat(ShaderUtilities.ID_OutlineWidth, outline);
                m.SetColor(ShaderUtilities.ID_OutlineColor, ink);
            }
            else m.DisableKeyword(ShaderUtilities.Keyword_Outline);
            m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            m.SetColor(ShaderUtilities.ID_UnderlayColor, shadow);
            m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, offset.x);
            m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, offset.y);
            m.SetFloat(ShaderUtilities.ID_UnderlayDilate, dilate);
            m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A 3D plate imported as a sharp, uncompressed 9-sliced sprite (borders from plates.txt).</summary>
        private static Sprite Plate(string name)
        {
            string path = $"{Plates}/{name}.png";
            var border = Vector4.zero;
            string table = $"{Plates}/plates.txt";
            if (File.Exists(table))
                foreach (var line in File.ReadAllLines(table))
                {
                    var parts = line.Split(' ');
                    if (parts.Length == 5 && parts[0] == name)
                        border = new Vector4(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]));
                }
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }
    }
}
#endif
