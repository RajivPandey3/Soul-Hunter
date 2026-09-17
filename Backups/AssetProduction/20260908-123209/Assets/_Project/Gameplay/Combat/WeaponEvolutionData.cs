using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Weapon Evolution System.
    /// Ye define karta hai ki agar BaseWeapon max level par hai, aur RequiredPassive maujood hai,
    /// toh Chest kholne par kaunsa naya (Evolved) hathiyar milega.
    /// </summary>
    [CreateAssetMenu(fileName = "New Evolution", menuName = "Soul Hunter/Weapon Evolution")]
    public class WeaponEvolutionData : ScriptableObject
    {
        [Header("Requirements")]
        public UpgradeData.UpgradeType BaseWeapon;
        public int BaseWeaponMaxLevel = 8;
        public UpgradeData.UpgradeType RequiredPassive;

        [Header("Result")]
        public GameObject EvolvedWeaponPrefab;
        public string EvolvedName;
        public Sprite EvolvedIcon;
    }
}
