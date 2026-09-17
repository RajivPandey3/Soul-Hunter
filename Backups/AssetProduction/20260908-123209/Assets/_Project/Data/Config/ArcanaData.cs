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
            Awake,          // +3 Revivals, +10% MaxHealth, +1 Armor, +5% Might per revive
            Gemini,         // Hathiyaron (Weapons) ka judwa (twin) nikalta hai (Not fully implemented without UI, but backend ready)
            IronBlueWill,   // Projectiles bounce karte hain
            WaltzOfPearls   // Magic Wand aur Cross bounce karte hain
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
