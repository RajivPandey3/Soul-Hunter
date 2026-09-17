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
    }
}
