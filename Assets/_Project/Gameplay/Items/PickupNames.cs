namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Soul Hunter's own names for pickups in the Results screen's pickup list (vampire.survivors.wiki/w/Results: "all the
    /// pickups and the amount of them"). The VS items they stand for: Gold Coin, Coin Bag, Rich Coin Bag, Floor Chicken,
    /// Vacuum, Orologion and Treasure Chest. Experience gems are not listed (the wiki does not say they are).
    /// </summary>
    public static class PickupNames
    {
        public const string Coin = "Soul Coin";
        public const string CoinBag = "Soul Coin Bag";
        public const string RichCoinBag = "Rich Soul Coin Bag";
        public const string Chicken = "Roast Chicken";
        public const string Magnet = "Soul Magnet";
        public const string TimeFreeze = "Time Freeze";

        /// <summary>A coin by its value (LightSourceDrops: 1, 10, 100); Gold Fever coins count as coins.</summary>
        public static string ForGold(int value) => value >= 100 ? RichCoinBag : value >= 10 ? CoinBag : Coin;

        /// <summary>A Soul Reliquary by how many items it held; an Arcana chest is its own row.</summary>
        public static string ForChest(int itemCount, bool arcana) =>
            arcana ? "Arcana Reliquary" : itemCount >= 5 ? "Soul Reliquary x5" : itemCount >= 3 ? "Soul Reliquary x3" : "Soul Reliquary";
    }
}
