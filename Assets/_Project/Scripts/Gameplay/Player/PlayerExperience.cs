using UnityEngine;
using System;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors ka Level Up System.
    /// XP collect hone par bar badhta hai. Full hone par agla level aata hai.
    /// </summary>
    public class PlayerExperience : MonoBehaviour
    {
        public int CurrentLevel { get; private set; } = 1;
        public int CurrentXP { get; private set; } = 0;
        public int XPToNextLevel { get; private set; } = 100;

        // UI ya Upgrade Menu ko batane ke liye events
        public event Action<int, int> OnXPChanged;
        public event Action<int> OnLevelUp;

        public void AddXP(int amount)
        {
            CurrentXP += amount;
            
            // Level Up logic (Vampire Survivors style: bar bar level up ho sakta hai agar XP bohot zyada ho)
            while (CurrentXP >= XPToNextLevel)
            {
                CurrentXP -= XPToNextLevel;
                CurrentLevel++;
                
                // Agle level ke liye XP requirement badhti jayegi
                XPToNextLevel = Mathf.FloorToInt(XPToNextLevel * 1.5f);
                
                // Event fire karo (Game pause karna aur Upgrade menu kholna aage banayenge)
                OnLevelUp?.Invoke(CurrentLevel);
                Debug.Log($"[PlayerExperience] Level Up! New Level: {CurrentLevel}");
            }

            OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
        }
    }
}
