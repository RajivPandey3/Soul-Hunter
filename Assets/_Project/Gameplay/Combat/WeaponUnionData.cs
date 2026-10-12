using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Jab 2 hathiyar (weapons) max level par ponch jatay hain, toh wo aapas mein 
    /// combine ho kar ek (Union) ban jate hain. Misal ke tor par: Phiera Der Tuphello + Eight The Sparrow = Phieraggi.
    /// Ye Data file us milap (Union) ke rules batati hai.
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon Union", menuName = "Soul Hunter/Weapon Union")]
    public class WeaponUnionData : ScriptableObject
    {
        [Header("Requirements (Dono Hathiyar Max Level hone chahiye)")]
        public UpgradeData.UpgradeType WeaponA;
        public int WeaponAMaxLevel = 8;
        
        public UpgradeData.UpgradeType WeaponB;
        public int WeaponBMaxLevel = 8;

        [Header("Result (In dono ko mila kar kya banega)")]
        public GameObject UnionWeaponPrefab;
        public string UnionName;
        public Sprite UnionIcon;

        // VS unions built 2026-10-12 (vampire.survivors.wiki/w/Evolution, raw, fetched 2026-10-12: "In case of union, all
        // base weapons must be fully leveled up. This may also require a passive item counterpart.").
        [Header("Optional passive (VS Phieraggi needs Tirajisu)")]
        public bool NeedsPassive;
        public UpgradeData.UpgradeType RequiredPassive;
        [Min(1)] public int RequiredPassiveMinLevel = 1;

        [Header("Optional: the union is a weapon type with levels of its own (VS Vandalier has 8 levels)")]
        public bool HasResultType;
        public UpgradeData.UpgradeType ResultType;

        /// <summary>True when the union needs no passive, or the inventory holds it at the needed level.</summary>
        public bool PassiveMet(WeaponManager inventory) =>
            !NeedsPassive || (inventory != null && inventory.GetWeaponLevel(RequiredPassive) >= Mathf.Max(1, RequiredPassiveMinLevel));

        /// <summary>
        /// VS (wiki Evolution): a union is never offered as a new item; it only comes from a chest. Once held, a union
        /// with levels of its own is offered like any owned weapon. True for a type that is a union's result.
        /// </summary>
        public static bool IsUnionResult(UpgradeData.UpgradeType type) => type == UpgradeData.UpgradeType.TwilightFlock;
    }
}
