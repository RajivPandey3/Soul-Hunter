using TMPro;
using UnityEngine;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.UI
{
    /// <summary>
    /// The Credits screen (release item P3-02, 2026-10-09). wiki Main_menu: Credits "Lists everyone who has contributed to
    /// the development and growth of the game", "Available by default"; the wiki gives no rules for its layout. Soul Hunter's
    /// credits are the third-party assets' own credit files (GameContent.Credits: the CC0 audio CREDITS.md and the OFL fonts
    /// CREDITS.md), shown as plain text: headings larger, each sound as "Game sound: track by author", and the
    /// licence lines as written. Nothing is invented; the files stay the one source.
    /// </summary>
    public sealed class CreditsMenuUI : MenuSectionScreen
    {
        private TextMeshProUGUI _body;

        public string BodyText => _body != null ? _body.text : null;

        protected override void Build()
        {
            var content = MakeScroll("CreditsText", Panel, 0.08f, 0.08f, 0.92f, 0.82f, out _);
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(12, 12, 8, 24);
            _body = EntryText(content, "Body", 28f, TextAlignmentOptions.TopLeft, ThemedElement.TextRole.Body, () => UITheme.Panel);
            _body.enableAutoSizing = false; // a long text keeps one size and scrolls
            _body.fontSize = 28f;
            _body.textWrappingMode = TextWrappingModes.Normal;
        }

        public override void Refresh()
        {
            if (_body == null) return;
            var catalog = GameContentCatalog.Load();
            _body.text = CreditsText.Compose(catalog != null ? catalog.Credits : null);
            CountText.text = "";
        }
    }
}
