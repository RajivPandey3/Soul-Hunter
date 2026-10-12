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
            // Responsive (owner, 2026-10-10): the credits fill Body at one size and scroll; a narrow screen wraps them.
            var content = MakeScroll("CreditsText", Body, 0f, 0f, 1f, 1f, out _);
            Column(content, 0f, new RectOffset(12, 12, 8, 24));
            _body = FlowText(content, "Body", 28f, TextAlignmentOptions.TopLeft, ThemedElement.TextRole.Body, () => UITheme.Panel);
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
