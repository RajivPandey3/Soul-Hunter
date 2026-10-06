#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Theme Studio (menu Soul Hunter/Theme Studio): the admin panel where designers and non-programmers control the whole
    /// UI look (owner, 2026-10-04). Pick a look, change its colours, pictures, icons, motion and spacing in plain words,
    /// and watch the preview and the running game change at once. Every change can be undone (Ctrl+Z) and is saved
    /// automatically. There is nothing to code: the game reads only the active theme.
    /// </summary>
    public sealed class ThemeStudioWindow : EditorWindow
    {
        private enum Page { Colours, Pictures, Motion, Layout }
        private static readonly string[] PageNames = { "Colours", "Pictures & icons", "Motion", "Text & spacing" };

        private UiThemeAsset _theme;
        private SerializedObject _so;
        private Page _page;
        private Vector2 _scroll, _presetScroll;
        private double _saveAt = -1;
        private readonly Dictionary<string, bool> _open = new Dictionary<string, bool>();
        private GUIStyle _h1, _h2, _soft, _cardName, _cardText, _badge, _bigButton, _section;
        private List<UiThemeAsset> _themes = new List<UiThemeAsset>();

        // Studio chrome (the window's own look, not the game's).
        private static readonly Color Ink = new Color(0.10f, 0.07f, 0.16f), Surface = new Color(0.16f, 0.12f, 0.24f), Line = new Color(0.30f, 0.24f, 0.42f);
        private static readonly Color Good = new Color(0.35f, 0.85f, 0.50f), Warn = new Color(1f, 0.72f, 0.30f), Brand = new Color(1f, 0.80f, 0.30f);

        [MenuItem("Soul Hunter/Theme Studio", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<ThemeStudioWindow>("Theme Studio");
            window.minSize = new Vector2(860f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += AutoSave;
            Reload();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.update -= AutoSave;
            SaveNow();
        }

        private void OnFocus() => Reload();

        private void Reload()
        {
            _themes = AssetDatabase.FindAssets("t:UiThemeAsset").Select(g => AssetDatabase.LoadAssetAtPath<UiThemeAsset>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(t => t != null).OrderBy(t => t.name).ToList();
            var active = ActiveInCatalog();
            if (_theme == null || !_themes.Contains(_theme)) _theme = active != null ? active : _themes.FirstOrDefault();
            _so = _theme != null ? new SerializedObject(_theme) : null;
        }

        private static GameContentCatalog Catalog() => AssetDatabase.LoadAssetAtPath<GameContentCatalog>("Assets/Resources/GameContent.asset");
        private static UiThemeAsset ActiveInCatalog() => Catalog() != null ? Catalog().UiTheme : null;

        private void OnUndoRedo()
        {
            if (_so != null) _so.Update();
            UiThemeAsset.NotifyChanged();
            Repaint();
        }

        private void AutoSave()
        {
            if (_saveAt > 0 && EditorApplication.timeSinceStartup >= _saveAt) SaveNow();
        }

        private void SaveNow()
        {
            _saveAt = -1;
            if (_theme != null) AssetDatabase.SaveAssetIfDirty(_theme);
        }

        // ---------------------------------------------------------------- drawing

        private void Styles()
        {
            if (_h1 != null) return;
            _h1 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, normal = { textColor = Color.white } };
            _h2 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, normal = { textColor = Brand } };
            _soft = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { fontSize = 11, normal = { textColor = new Color(0.80f, 0.76f, 0.90f) } };
            _cardName = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = Color.white }, wordWrap = true };
            _cardText = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.82f, 0.78f, 0.92f) } };
            _badge = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Ink } };
            _bigButton = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, fixedHeight = 26 };
            _section = new GUIStyle(EditorStyles.foldoutHeader) { fontSize = 12, fontStyle = FontStyle.Bold };
        }

        private void OnGUI()
        {
            Styles();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), Ink);
            Header();
            if (_theme == null)
            {
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("No theme yet. Press the button to create the four starting looks.", MessageType.Info);
                if (GUILayout.Button("Create the starting looks", _bigButton)) { UiThemeBuilder.Build(); Reload(); }
                return;
            }
            Presets();
            GUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Max(420f, position.width * 0.52f))))
                {
                    _page = (Page)GUILayout.Toolbar((int)_page, PageNames, GUILayout.Height(28));
                    GUILayout.Space(4);
                    _scroll = EditorGUILayout.BeginScrollView(_scroll);
                    _so.Update();
                    EditorGUI.BeginChangeCheck();
                    switch (_page)
                    {
                        case Page.Colours: ColoursPage(); break;
                        case Page.Pictures: PicturesPage(); break;
                        case Page.Motion: Group("Motions", "Movement", "Small animations that make menus feel alive. Switch any off if it distracts.", true); break;
                        case Page.Layout: LayoutPage(); break;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        _so.ApplyModifiedProperties(); // records Undo
                        UiThemeAsset.NotifyChanged();
                        _saveAt = EditorApplication.timeSinceStartup + 0.6;
                    }
                    EditorGUILayout.EndScrollView();
                }
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope()) Preview();
            }
        }

        private void Header()
        {
            GUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("Theme Studio", _h1);
                    GUILayout.Label("One place for the whole game's menu look. Changes show at once (also while playing), are saved by themselves, and Ctrl+Z undoes them.", _soft);
                }
                GUILayout.FlexibleSpace();
                if (_theme != null && GUILayout.Button(new GUIContent("Show in project", "Select this look's file in the Project window."), GUILayout.Width(120), GUILayout.Height(24)))
                    EditorGUIUtility.PingObject(_theme);
                GUILayout.Space(12);
            }
            GUILayout.Space(6);
        }

        private void Presets()
        {
            GUILayout.Label("  1. Pick a look", _h2);
            var active = ActiveInCatalog();
            _presetScroll = EditorGUILayout.BeginScrollView(_presetScroll, GUILayout.Height(150));
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                foreach (var theme in _themes) PresetCard(theme, theme == active, theme == _theme);
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(150), GUILayout.Height(130)))
                {
                    GUILayout.Space(30);
                    if (GUILayout.Button(new GUIContent("+ New look\nfrom the selected one", "Makes a copy you can change freely; the original stays as it is."), GUILayout.Height(60)))
                        Duplicate();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void PresetCard(UiThemeAsset theme, bool inUse, bool editing)
        {
            var rect = GUILayoutUtility.GetRect(200, 130, GUILayout.Width(200), GUILayout.Height(130));
            EditorGUI.DrawRect(rect, editing ? Line : Surface);
            if (inUse) DrawFrame(rect, Good, 2);
            var swatches = new[] { theme.Surfaces.Panel, theme.Buttons.Primary, theme.Buttons.Normal, theme.Cards.Card, theme.Text.Accent };
            float w = (rect.width - 16) / swatches.Length;
            for (int i = 0; i < swatches.Length; i++) EditorGUI.DrawRect(new Rect(rect.x + 8 + i * w, rect.y + 8, w - 2, 22), swatches[i]);
            GUI.Label(new Rect(rect.x + 8, rect.y + 34, rect.width - 16, 20), theme.DisplayName, _cardName);
            GUI.Label(new Rect(rect.x + 8, rect.y + 52, rect.width - 16, 44), theme.Description, _cardText);
            if (inUse)
            {
                EditorGUI.DrawRect(new Rect(rect.xMax - 62, rect.y + 34, 54, 16), Good);
                GUI.Label(new Rect(rect.xMax - 62, rect.y + 34, 54, 16), "In game", _badge);
            }
            var left = new Rect(rect.x + 8, rect.yMax - 28, (rect.width - 20) / 2, 22);
            var right = new Rect(left.xMax + 4, left.y, left.width, 22);
            using (new EditorGUI.DisabledScope(editing)) if (GUI.Button(left, editing ? "Editing" : "Edit")) { _theme = theme; _so = new SerializedObject(theme); }
            using (new EditorGUI.DisabledScope(inUse)) if (GUI.Button(right, new GUIContent(inUse ? "In game" : "Use in game", "Makes the whole game use this look."))) UseInGame(theme);
            GUILayout.Space(8);
        }

        private void UseInGame(UiThemeAsset theme)
        {
            var catalog = Catalog();
            if (catalog == null) return;
            Undo.RecordObject(catalog, "Use UI look");
            catalog.UiTheme = theme;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            UiThemeAsset.SetActive(theme);
            _theme = theme;
            _so = new SerializedObject(theme);
        }

        private void Duplicate()
        {
            string source = AssetDatabase.GetAssetPath(_theme);
            string target = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(source), "UiTheme_MyLook.asset"));
            AssetDatabase.CopyAsset(source, target);
            var copy = AssetDatabase.LoadAssetAtPath<UiThemeAsset>(target);
            copy.DisplayName = _theme.DisplayName + " (my copy)";
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssetIfDirty(copy);
            Reload();
            _theme = copy;
            _so = new SerializedObject(copy);
        }

        // ---------------------------------------------------------------- pages

        private void ColoursPage()
        {
            GUILayout.Label("2. Change colours", _h2);
            GUILayout.Label("Click a colour to change it. Hover a name to see where it is used. The preview on the right warns when text gets hard to read.", _soft);
            GUILayout.Space(4);
            Group("Buttons", "Buttons", "Menu buttons: ordinary, main action, leaving, chosen (keyboard/controller) and unusable.");
            Group("Surfaces", "Backgrounds and windows", "The dark layer over the game, windows, their rows and borders.");
            Group("Cards", "Cards", "Level-up cards, Power Up cells and character cards.");
            Group("Text", "Text", "Text colours and the outline around titles and button labels.");
            Group("Hud", "In-game screen (HUD)", "Labels, health bar, item slots, story card and phone controls.");
            Group("Rewards", "Rewards and results", "Chest screen gold and glows, evolution frame, the Results screen's best values.");
            Group("Rarity", "Item rarity", "Colours for common, rare, epic and legendary items.");
        }

        private void PicturesPage()
        {
            GUILayout.Label("2. Pictures and icons", _h2);
            GUILayout.Label("Drag a picture (Sprite) onto a slot, or pick one with the small circle. Empty slots fall back to flat shapes.", _soft);
            GUILayout.Space(4);
            Group("Shape", "Button, card and window pictures", "White or grey pictures work best: the colours tint them.", true);
            Group("Icon", "Button icons", "The small picture shown on the left of each button.", true);
            Group("Font", "Fonts", "Optional. Leave empty to keep the game's font.", true);
        }

        private void LayoutPage()
        {
            GUILayout.Label("2. Text and spacing", _h2);
            GUILayout.Label("Where icons and labels sit on buttons, and how big the text may grow.", _soft);
            GUILayout.Space(4);
            Group("Layouts", "Icons, labels and sizes", "", true);
            Group("", "Name and description", "What this look is called in the list above.", true, new[] { "DisplayName", "Description" });
        }

        private void Group(string path, string title, string help, bool startOpen = false, string[] rootFields = null)
        {
            string key = path + title;
            if (!_open.ContainsKey(key)) _open[key] = startOpen;
            GUILayout.Space(2);
            _open[key] = EditorGUILayout.BeginFoldoutHeaderGroup(_open[key], title, _section);
            if (_open[key])
            {
                if (!string.IsNullOrEmpty(help)) GUILayout.Label(help, _soft);
                EditorGUI.indentLevel++;
                if (rootFields != null)
                    foreach (var name in rootFields) Field(_so.FindProperty(name), typeof(UiThemeAsset).GetField(name));
                else
                {
                    var group = _so.FindProperty(path);
                    var groupField = typeof(UiThemeAsset).GetField(path);
                    var end = group.GetEndProperty();
                    var child = group.Copy();
                    bool enter = true;
                    while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                    {
                        enter = false;
                        Field(child, groupField?.FieldType.GetField(child.name));
                    }
                }
                EditorGUI.indentLevel--;
                GUILayout.Space(4);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void Field(SerializedProperty property, FieldInfo field)
        {
            if (property == null) return;
            string tip = field?.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? "";
            var label = new GUIContent(ObjectNames.NicifyVariableName(property.name), tip);
            if (property.propertyType == SerializedPropertyType.ObjectReference && field != null && field.FieldType == typeof(Sprite))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, label);
                    var sprite = property.objectReferenceValue as Sprite;
                    var r = GUILayoutUtility.GetRect(36, 36, GUILayout.Width(36));
                    if (sprite != null) DrawSprite(r, sprite, Color.white);
                }
            }
            else EditorGUILayout.PropertyField(property, label, true);
        }

        // ---------------------------------------------------------------- preview

        private void Preview()
        {
            GUILayout.Label("Preview", _h2);
            var t = _theme;
            var area = GUILayoutUtility.GetRect(320, 330, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(area, new Color(0.22f, 0.30f, 0.20f)); // a stage floor behind the menu
            EditorGUI.DrawRect(area, t.Surfaces.Veil);
            var panel = new Rect(area.x + 14, area.y + 12, area.width - 28, area.height - 24);
            Plate(panel, t.Shape.Panel, t.Surfaces.Panel, 40);
            if (t.Shape.Panel == null) DrawFrame(panel, t.Surfaces.PanelEdge, 2);

            var title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            OutlinedLabel(new Rect(panel.x, panel.y + 8, panel.width, 28), "Level up!", title, t.Text.Accent, t.Text.Outline, t.Text.OutlineWidth);

            float bw = Mathf.Min(220f, panel.width * 0.5f), bh = 40f, x = panel.x + 14;
            var kinds = new[] { ("Start", t.Buttons.Primary, t.Icon.Start), ("Power Up", t.Buttons.Normal, t.Icon.PowerUp), ("Options", t.Buttons.Focus, t.Icon.Options), ("Quit", t.Buttons.Danger, t.Icon.Quit) };
            var labelStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            for (int i = 0; i < kinds.Length; i++)
            {
                var r = new Rect(x, panel.y + 46 + i * (bh + 8), bw, bh);
                Plate(r, t.Shape.Button, kinds[i].Item2, 26);
                if (t.Shape.Button == null && t.Shape.EdgeWidth > 0) DrawFrame(r, t.Buttons.Edge, Mathf.CeilToInt(t.Shape.EdgeWidth));
                float iconSize = bh * t.Layouts.IconSize, inset = bh * t.Layouts.IconInset;
                if (kinds[i].Item3 != null) DrawSprite(new Rect(r.x + inset, r.center.y - iconSize / 2, iconSize, iconSize), kinds[i].Item3, Color.white);
                float left = kinds[i].Item3 != null && t.Layouts.CentreLabelBesideIcon ? inset + iconSize : 0f;
                OutlinedLabel(new Rect(r.x + left, r.y, r.width - left, r.height), kinds[i].Item1 + (i == 2 ? "  (chosen)" : ""), labelStyle, t.Text.OnDark, t.Text.Outline, t.Text.OutlineWidth);
            }

            var card = new Rect(x + bw + 14, panel.y + 46, panel.xMax - (x + bw + 14) - 14, 2 * bh + 8);
            if (card.width > 80)
            {
                Plate(card, t.Shape.Card, t.Cards.Card, 30);
                var name = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = t.Text.OnLight } };
                var tag = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, alignment = TextAnchor.UpperRight, normal = { textColor = t.Text.Tag } };
                var desc = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = t.Text.SoftOnLight } };
                GUI.Label(new Rect(card.x + 12, card.y + 8, card.width - 24, 18), "Soul Flame", name);
                GUI.Label(new Rect(card.x + 12, card.y + 8, card.width - 24, 18), "NEW!", tag);
                GUI.Label(new Rect(card.x + 12, card.y + 28, card.width - 24, 50), "A level-up card: name, tag and what it does.", desc);
                var stats = new Rect(card.x, card.yMax + 8, card.width, 2 * bh + 8);
                Plate(stats, t.Shape.Panel, t.Surfaces.Row, 24);
                var body = new GUIStyle(EditorStyles.label) { fontSize = 12, normal = { textColor = t.Text.OnDark } };
                var soft = new GUIStyle(EditorStyles.label) { fontSize = 11, normal = { textColor = t.Text.SoftOnDark } };
                var accent = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, normal = { textColor = t.Text.Accent } };
                GUI.Label(new Rect(stats.x + 12, stats.y + 8, stats.width, 18), "Max Health   120", body);
                GUI.Label(new Rect(stats.x + 12, stats.y + 28, stats.width, 18), "Might   +10%", accent);
                GUI.Label(new Rect(stats.x + 12, stats.y + 48, stats.width, 18), "Text on windows", soft);
            }
            GUILayout.Space(6);
            ReadabilityChecks(t);
            GUILayout.Space(6);
            GUILayout.Label("Tip: press Play and open the pause menu or a level-up: every change you make here shows in the game at once.", _soft);
        }

        private void ReadabilityChecks(UiThemeAsset t)
        {
            GUILayout.Label("Readability check", EditorStyles.boldLabel);
            Check("Text on windows", t.Text.OnDark, t.Surfaces.Panel, 4.5f);
            Check("Text on cards", t.Text.OnLight, t.Cards.Card, 4.5f);
            Check("Highlights on windows", t.Text.Accent, t.Surfaces.Panel, 3f);
            Check("In-game labels", t.Hud.LabelText, t.Hud.LabelBacking, 4.5f);
            Check("Text on unusable buttons", t.Text.Locked, t.Buttons.Disabled, 3f);
            // Button labels take the light or the dark text, whichever reads best (UITheme.LightTextReads).
            Check("Button labels", Best(t, t.Buttons.Normal), t.Buttons.Normal, 4.5f);
            Check("Main button labels", Best(t, t.Buttons.Primary), t.Buttons.Primary, 4.5f);
            Check("Leaving button labels", Best(t, t.Buttons.Danger), t.Buttons.Danger, 4.5f);
            Check("Locked cards", t.Text.Locked, t.Cards.Locked, 3f);
            float focusGap = Mathf.Abs(Luminance(t.Buttons.Focus) - Luminance(t.Buttons.Normal)) + ColourDistance(t.Buttons.Focus, t.Buttons.Normal);
            Row("Chosen button stands out", focusGap > 0.25f, focusGap > 0.25f ? "yes" : "make Focus differ more from Normal");
        }

        private static Color Best(UiThemeAsset t, Color back) => Contrast(t.Text.OnDark, back) >= Contrast(t.Text.OnLight, back) ? t.Text.OnDark : t.Text.OnLight;

        private static void Check(string what, Color text, Color back, float needed)
        {
            float ratio = Contrast(text, back);
            Row(what, ratio >= needed, ratio >= needed ? $"easy to read ({ratio:0.0}:1)" : $"hard to read ({ratio:0.0}:1, aim for {needed:0.#}:1)");
        }

        private static void Row(string what, bool ok, string verdict)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var dot = GUILayoutUtility.GetRect(10, 16, GUILayout.Width(10));
                EditorGUI.DrawRect(new Rect(dot.x, dot.y + 4, 8, 8), ok ? Good : Warn);
                GUILayout.Label(what, GUILayout.Width(220));
                var style = new GUIStyle(EditorStyles.label) { normal = { textColor = ok ? Good : Warn } };
                GUILayout.Label(verdict, style);
            }
        }

        // ---------------------------------------------------------------- helpers

        private static float Luminance(Color c)
        {
            float L(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        }

        /// <summary>WCAG contrast ratio (1 to 21).</summary>
        public static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        private static float ColourDistance(Color a, Color b) => Mathf.Sqrt((a.r - b.r) * (a.r - b.r) + (a.g - b.g) * (a.g - b.g) + (a.b - b.b) * (a.b - b.b)) / 1.732f;

        private static void DrawFrame(Rect r, Color c, int w)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - w, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, w, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - w, r.y, w, r.height), c);
        }

        /// <summary>Draws a theme picture 9-sliced and tinted (flat when there is none), like the game does.</summary>
        private static void Plate(Rect r, Sprite sprite, Color tint, float border)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (sprite == null) { EditorGUI.DrawRect(r, tint); return; }
            var tex = sprite.texture;
            var tr = sprite.textureRect;
            var src = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            var b = sprite.border; // left, bottom, right, top in pixels
            float s = Mathf.Min(1f, border / Mathf.Max(1f, Mathf.Max(b.x, b.w)));
            Graphics.DrawTexture(r, tex, src, Mathf.RoundToInt(b.x * s), Mathf.RoundToInt(b.z * s), Mathf.RoundToInt(b.w * s), Mathf.RoundToInt(b.y * s), tint * 0.5f);
        }

        private static void DrawSprite(Rect r, Sprite sprite, Color tint)
        {
            if (Event.current.type != EventType.Repaint || sprite == null) return;
            var tex = sprite.texture;
            var tr = sprite.textureRect;
            var old = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(r, tex, new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
            GUI.color = old;
        }

        private static void OutlinedLabel(Rect r, string text, GUIStyle style, Color colour, Color outline, float width)
        {
            var s = new GUIStyle(style);
            if (width > 0.01f)
            {
                s.normal.textColor = outline;
                float d = Mathf.Clamp(width * 8f, 1f, 2.5f);
                foreach (var o in new[] { new Vector2(-d, 0), new Vector2(d, 0), new Vector2(0, -d), new Vector2(0, d), new Vector2(-d, -d), new Vector2(d, d), new Vector2(-d, d), new Vector2(d, -d) })
                    GUI.Label(new Rect(r.x + o.x, r.y + o.y, r.width, r.height), text, s);
            }
            s.normal.textColor = colour;
            GUI.Label(r, text, s);
        }
    }
}
#endif
