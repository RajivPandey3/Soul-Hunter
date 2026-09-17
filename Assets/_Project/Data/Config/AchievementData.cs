using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Player ko naye items aur characters kholne (unlock) ke liye chote tasks karne hote hain.
    /// Ye Data object ek task (achievement) define karta hai.
    /// </summary>
    [CreateAssetMenu(fileName = "New Achievement", menuName = "Soul Hunter/Achievement")]
    public class AchievementData : ScriptableObject
    {
        public enum AchievementType
        {
            SurviveTime,    // e.g. Survive 5 minutes
            KillCount,      // e.g. Kill 1000 enemies
            WeaponLevelUp   // e.g. Get Whip to level 8
        }

        public string AchievementName;
        [TextArea]
        public string Description;
        public Sprite Icon;

        [Header("Unlock Conditions")]
        public AchievementType Type;
        public float TargetValue; // Time (seconds), Kills, or Weapon Level
        public string TargetWeaponName; // Only for WeaponLevelUp type

        [Header("Rewards (Sirf UI ke liye)")]
        public string UnlockedItemName;
    }
}
