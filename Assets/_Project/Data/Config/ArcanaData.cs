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
            WaltzOfPearls   // VS XI: Magic/Holy/Fire Wand +3 enemy bounces; Hellmaw Comet 1, Cross and Heaven Sword 2 screen-edge bounces
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
