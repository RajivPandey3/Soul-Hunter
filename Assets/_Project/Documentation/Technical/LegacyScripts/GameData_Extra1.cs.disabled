using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Core.Persistence
{
    /// <summary>
    /// Learning Comment:
    /// Centralized data model. Unity ke PlayerPrefs ki jagah hum ek solid object rakhenge
    /// jo JSON mein serialize hoga. Isme pure game ki state save hoti hai.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public int PlayerHealth;
        public Vector3 PlayerPosition;
        public int Currency;
        public bool SettingsMigrated;
        public float MasterVolume = 1f;
        public float HighScoreTime;

        // --- Meta Progression (Power-Ups) ---
        public int MetaMightLevel;
        public int MetaArmorLevel;
        public int MetaGreedLevel;
        public int MetaRevivalLevel;

        // --- Current Run Data ---
        public string SelectedCharacterName;
        public List<string> UnlockedAchievements = new List<string>();

        public GameData()
        {
            // Default values for a new save
            PlayerHealth = 100;
            PlayerPosition = Vector3.zero;
            Currency = 0;

            MetaMightLevel = 0;
            MetaArmorLevel = 0;
            MetaGreedLevel = 0;
            MetaRevivalLevel = 0;

            SelectedCharacterName = "Kael"; // Default Hero
        }
    }
}
