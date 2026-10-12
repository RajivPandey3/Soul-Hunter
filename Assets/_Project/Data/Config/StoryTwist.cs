using System.Collections.Generic;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// "He was not alone": the story layer the owner approved on 2026-10-13 ("haan ye twist theek hai", then "haan lines
    /// theek hai, game mein jod do"). It adds to the owner's story (CampaignStory, Docs/Game_Design_Document.md) without
    /// changing a sentence of it: the other heroes are earlier hunters who made the same pact, sealed in Soul Caskets by
    /// the Death God, who was Kael; the relics are his daughter's keepsakes; with every keepsake found the ending gains one
    /// line. Every line of this layer is in this file, so the owner can change or remove any of them here.
    /// </summary>
    public static class StoryTwist
    {
        public const string CasketTitle = "A Soul Casket Opens";
        public const string KeepsakeTitle = "A Keepsake";

        /// <summary>Shown with the first casket a save ever opens.</summary>
        public const string FirstCasket = "The casket bears a seal Kael knows. It is his own.";

        /// <summary>Shown with the casket that frees the last sealed hero.</summary>
        public const string LastCasket = "Twelve hunters. Twelve pacts. One hand sealed them all.";

        private static readonly Dictionary<string, string> Hunters = new Dictionary<string, string>
        {
            { "Corvan", "He kept the graves. He asked the Death God for one more night with his dead." },
            { "Aria", "She sang to bring a voice back. The pact took hers instead." },
            { "Draven", "He counted his kills aloud, sure the next one would be enough." },
            { "Gideon", "He came down swinging and never asked who kept the tally." },
            { "Kaelen", "One clean strike, he swore, and it would be over. It was never over." },
            { "Valerius", "He wore iron so nothing could reach him. Something already had." },
            { "Isolde", "She blessed every soul she took. The water stayed holy. She did not." },
            { "Elaria", "She learned the pact's every word, and still signed it." },
            { "Seraphina", "She believed the count could be bargained down." },
            { "Lumina", "She read the names of the harvested so they would not be forgotten." },
            { "Rurik", "He ran fastest when he was closest to dying. He ran for years." },
            { "Sylas", "He guarded the others to the last. No one was left to guard him." },
        };

        /// <summary>The daughter's keepsakes, in the order they are found (one per relic).</summary>
        public static readonly IReadOnlyList<string> Keepsakes = new[]
        {
            "A wooden bird, one wing worn smooth by a small thumb.",
            "A ribbon the colour of the dress she was buried in.",
            "A page of letters, the same word written again and again: father.",
            "A candle stub. She was afraid of the dark; he used to stay until it burned out.",
            "A river stone she called a jewel. He had carried it in his pocket, once.",
            "A cup with a chipped rim. She would drink from no other.",
            "A lullaby, half remembered. He cannot find the second verse.",
            "A lock of hair, tied with thread. He does not remember cutting it.",
            // Owner-approved 2026-10-13 ("haan lines theek hain, jod do"): three more, so eleven relics have eleven lines.
            "A shoe, too small for anyone now. He looks for the other and does not find it.",
            "A drawing in charcoal: two figures, one tall, holding hands. The tall one has no face.",
            "Her name, carved low on a doorframe at the height she was. He reads it aloud. It is the first word he has spoken in years.",
        };

        /// <summary>Added under CampaignStory.FinalLine on the victory screen once every keepsake has been found.</summary>
        public const string TrueEnding =
            "She was never among the hundred thousand. She waited where he began, at the grave, for him to come home.";

        /// <summary>The line of the hunter freed from a casket, or null for a name this layer does not know.</summary>
        public static string HunterLine(string characterName) =>
            characterName != null && Hunters.TryGetValue(FirstName(characterName), out var line) ? line : null;

        /// <summary>A character's name without a bracketed note: the catalog calls Elaria "Elaria (Mage)".</summary>
        private static string FirstName(string characterName)
        {
            int bracket = characterName.IndexOf('(');
            return (bracket >= 0 ? characterName.Substring(0, bracket) : characterName).Trim();
        }

        /// <summary>
        /// The card text for a casket: the first-casket line before the hunter's own when it is the save's first, the
        /// last-casket line after it when no sealed hunter is left.
        /// </summary>
        public static string CasketText(string characterName, bool firstEver, bool lastOne)
        {
            string hunter = HunterLine(characterName);
            var parts = new List<string>();
            if (firstEver) parts.Add(FirstCasket);
            if (hunter != null) parts.Add(hunter);
            if (lastOne) parts.Add(LastCasket);
            return string.Join("\n\n", parts);
        }

        /// <summary>The keepsake line for the n-th relic a save finds (0-based); null past the last line.</summary>
        public static string KeepsakeLine(int foundBefore) =>
            foundBefore >= 0 && foundBefore < Keepsakes.Count ? Keepsakes[foundBefore] : null;

        /// <summary>True once the save has found every relic; set by the game when a run starts and when a relic is found.</summary>
        public static bool AllKeepsakesFound { get; set; }
    }
}
