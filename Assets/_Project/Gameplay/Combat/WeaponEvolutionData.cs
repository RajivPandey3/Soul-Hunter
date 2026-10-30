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

        /// <summary>
        /// The player-facing name of an evolution or union ID: "Evo_BloodyTear" -> "Bloody Tear". The chest reel showed the
        /// raw IDs (Bug MIS B03, 2026-09-30); the IDs themselves stay as they are, since other code keys on them.
        /// </summary>
        public static string DisplayName(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            string name = id;
            foreach (var prefix in new[] { "Evo_", "Union_" })
                if (name.StartsWith(prefix)) { name = name.Substring(prefix.Length); break; }
            name = name.Replace('_', ' ');
            var text = new System.Text.StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i > 0 && char.IsUpper(c) && name[i - 1] != ' ' && !char.IsUpper(name[i - 1])) text.Append(' ');
                text.Append(c);
            }
            return SoulHunter.Gameplay.Data.SoulHunterNames.For(text.ToString()); // VS names are shown with Soul Hunter's (owner, 2026-10-30)
        }
        public Sprite EvolvedIcon;
    }
}
