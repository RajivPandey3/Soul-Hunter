namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// The owner's story (Docs/Game_Design_Document.md), word for word: owner, 2026-09-27, "MERE STORY KO FOLLOW KARO"
    /// and "STORY MEIN NO CHANGES". Each stage's line is CampaignLevelDefinition.Narrative; the premise opens the run
    /// and the twist opens Level 10. StoryCardUI shows them when a stage starts.
    /// </summary>
    public static class CampaignStory
    {
        public const string Premise =
            "Kael makes a pact with the Death God to harvest 100,000 souls from the underworld to bring his dead daughter back to life.";

        public const string Twist =
            "Upon collecting the final soul, the mirror of truth is revealed. The Death God was Kael himself. " +
            "His obsession with harvesting souls corrupted him entirely, trapping him in purgatory. " +
            "The final boss is his own shadow. To free his daughter's soul, he must destroy himself.";

        /// <summary>The story told when <paramref name="stage"/> starts.</summary>
        public static string CardText(int stage)
        {
            var level = CampaignLevelCatalog.Get(stage);
            if (level.Number == 1) return Premise + "\n\n" + level.Narrative;
            if (level.Number == LevelCount) return Twist;
            return level.Narrative;
        }

        private const int LevelCount = 10;
    }
}
