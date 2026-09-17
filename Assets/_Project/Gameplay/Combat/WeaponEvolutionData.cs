using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    [System.Serializable]
    public struct PassiveEvolutionRequirement
    {
        public UpgradeData.UpgradeType Type;
        [Min(1)] public int MinimumLevel;
    }

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
        [Tooltip("Minimum level of the required passive. Standard evolutions normally use 1.")]
        public int RequiredPassiveMaxLevel = 1;
        public PassiveEvolutionRequirement[] AdditionalPassives = new PassiveEvolutionRequirement[0];

        public bool RequirementsMet(WeaponManager inventory)
        {
            if (inventory == null || inventory.GetWeaponLevel(BaseWeapon) < Mathf.Max(1, BaseWeaponMaxLevel) ||
                inventory.GetWeaponLevel(RequiredPassive) < Mathf.Max(1, RequiredPassiveMaxLevel)) return false;
            if (AdditionalPassives != null)
                foreach (var requirement in AdditionalPassives)
                    if (inventory.GetWeaponLevel(requirement.Type) < Mathf.Max(1, requirement.MinimumLevel)) return false;
            return true;
        }

        [Header("Result")]
        public GameObject EvolvedWeaponPrefab;
        public string EvolvedName;
        public Sprite EvolvedIcon;
    }
}
