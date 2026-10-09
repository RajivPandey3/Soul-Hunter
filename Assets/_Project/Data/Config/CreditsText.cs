using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// The Credits screen's text, made from the third-party assets' own credit files (GameContent.Credits), so the files
    /// stay the one source (release item P3-02, 2026-10-09).
    /// </summary>
    public static class CreditsText
    {
        /// <summary>The credit files as screen text (markdown headings and tables turned into plain lines).</summary>
        public static string Compose(IEnumerable<TextAsset> files)
        {
            var text = new StringBuilder();
            if (files == null) return "";
            foreach (var file in files)
            {
                if (file == null) continue;
                if (text.Length > 0) text.Append("\n\n");
                text.Append(FromMarkdown(file.text));
            }
            return text.ToString().TrimEnd();
        }

        public static string FromMarkdown(string markdown)
        {
            var text = new StringBuilder();
            if (string.IsNullOrEmpty(markdown)) return "";
            foreach (var raw in markdown.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("#"))
                {
                    text.Append("<size=125%>").Append(SoulHunter.Gameplay.UI.UITheme.Bold(line.TrimStart('#').Trim())).Append("</size>\n");
                    continue;
                }
                if (line.StartsWith("|"))
                {
                    var cells = line.Trim('|').Split('|');
                    for (int i = 0; i < cells.Length; i++) cells[i] = cells[i].Trim();
                    // Skip the header and the |---| rule; a row is: game sound | file | pack / track | author | page.
                    if (cells.Length < 5 || cells[0] == "Game sound" || cells[0].StartsWith("---")) continue;
                    text.Append($"{cells[0]}: {cells[2]} by {cells[3]} ({cells[4]})\n");
                    continue;
                }
                text.Append(line).Append('\n');
            }
            return text.ToString().TrimEnd();
        }
    }
}
