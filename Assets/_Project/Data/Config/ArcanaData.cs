using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Arcanas (Tarot Cards) game ko puri tarah badal dete hain.
    /// Ye ScriptableObject ek Card define karta hai. Har card ka ek 'Type' hota hai 
    /// jise ArcanaManager parh kar uski makhsoos (specific) taqat ko jagata hai.
    /// </summary>
    [CreateAssetMenu(fileName = "New Arcana", menuName = "Soul Hunter/Arcana Card")]
    public class ArcanaData : ScriptableObject
    {
        public enum ArcanaType
        {
            // Stored as an int in the assets: add new cards at the END only.
            Awake,          // VS IV: +3 Revivals; each Revival used: Max Health x1.1, +1 Armor, +5% Might/Area/Duration/Speed
            Gemini,         // VS I: Peachone and Gun get a counterpart (no slot, levels with them); Hellfire Breath a back stream
            IronBlueWill,   // VS VII: Knife, Thousand Edge, Axe, Gun +3 enemy bounces; Death Spiral +1 screen-edge bounce
            WaltzOfPearls,  // VS XI: Magic/Holy/Fire Wand +3 enemy bounces; Hellmaw Comet 1, Cross and Heaven Sword 2 screen-edge bounces
            // The other 18 base cards (release item P2-01, 2026-10-09). Soul Hunter names them itself (owner, 2026-10-09);
            // the enum keeps the VS card for mapping only and is never shown. Each card's rules: ArcanaCatalog.
            GameKiller,          // VS 0    -> "Soul Barrage"
            TwilightRequiem,     // VS II   -> "Final Echo"
            TragicPrincess,      // VS III  -> "Restless Pilgrim"
            ChaosInTheDarkNight, // VS V    -> "Wavering Winds"
            SarabandeOfHealing,  // VS VI   -> "Mending Wrath"
            MadGroove,           // VS VIII -> "Gathering Call"
            DivineBloodline,     // VS IX   -> "Ironblood Oath"
            Beginning,           // VS X    -> "First Blade"
            OutOfBounds,         // VS XII  -> "Shattering Frost"
            WickedSeason,        // VS XIII -> "Changing Fortunes"
            JailOfCrystal,       // VS XIV  -> "Icebound Snare"
            DiscoOfGold,         // VS XV   -> "Midas Pulse"
            Slash,               // VS XVI  -> "Keen Edge"
            LostAndFoundPainting,// VS XVII -> "Ebbing Hours"
            BoogalooOfIllusions, // VS XVIII-> "Swelling Shadows"
            HeartOfFire,         // VS XIX  -> "Burning Core"
            SilentOldSanctuary,  // VS XX   -> "Empty Hands"
            BloodAstronomia      // VS XXI  -> "Crimson Orbit"
        }

        [Header("Display Info")]
        public string CardNumber; // e.g. "IV"
        public string CardName;   // e.g. "Awake"
        [TextArea]
        public string Description;
        public Sprite CardIcon;
        
        [Header("Logic Info")]
        public ArcanaType Type;
    }
}
