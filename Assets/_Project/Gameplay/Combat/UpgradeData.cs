using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Har upgrade ek data file hoti hai. 
    /// Ye ScriptableObject humein Unity Editor mein naye upgrades (jaise Magic Wand Lv2, Whip Lv1) 
    /// asani se banane ki taqat deta hai, bina naya code likhe.
    /// </summary>
    [CreateAssetMenu(fileName = "New Upgrade", menuName = "Soul Hunter/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        public string UpgradeName;
        [TextArea]
        public string Description;
        public Sprite Icon;

        // Upgrade kis cheez ka hai?
        public enum UpgradeType { 
            MagicWand, Whip, Garlic, PlayerSpeed, MaxHealth, Axe, Bible, Cross,
            LightningRing, FireWand, SantaWater, 
            EmptyTome, Spinach, Bracer, 
            Knife, Runetracer, Pentagram, ClockLancet, Laurel, 
            Bone, Peachone, SongOfMana, CherryBomb, Gun,
            Candelabrador, Spellbinder, Duplicator, Armor, Pummarola, // Expansion 4
            Attractorb, Clover, Crown, SkullOManiac, Tiragisu, // Expansion 4
            MetaglioLeft, MetaglioRight,
            // VS items added 2026-10-12 (NewVsItemsBuilder), Soul Hunter's own names; the VS source is in the comment.
            // New values go at the END only: the enum is stored as an int in every UpgradeData asset.
            MisersIdol,        // VS Stone Mask (passive, Greed)
            RingOfLongHours,   // VS Silver Ring (stage-item passive, Duration and Area)
            RingOfDarkHours,   // VS Gold Ring (stage-item passive, Curse)
            DuskRaven,         // VS Ebony Wings (weapon)
            AzureRepeater,     // VS Eight The Sparrow (weapon)
            TwilightFlock      // VS Vandalier (union of Peachone and Dusk Raven; it has levels of its own)
        }
        public UpgradeType Type;
        
        // Upgrade ka level kya hai (Level 1 naya weapon deta hai, Level 2 purane ki taqat badhata hai)
        public int Level;
    }
}
