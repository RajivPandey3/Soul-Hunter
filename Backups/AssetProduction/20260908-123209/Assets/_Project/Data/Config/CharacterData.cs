using UnityEngine;
using SoulHunter.Gameplay.Combat; // For UpgradeType

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Har character ki apni khasiyat hoti hai (starting weapon, extra health, etc).
    /// Ye ScriptableObject humein naye characters asani se banane ki taqat deta hai.
    /// </summary>
    [CreateAssetMenu(fileName = "New Character", menuName = "Soul Hunter/Character Data")]
    public class CharacterData : ScriptableObject
    {
        public string CharacterName;
        [TextArea]
        public string Description;
        public Sprite CharacterIcon;
        public GameObject CharacterModelPrefab; // 3D ya 2D model

        [Header("Starting Loadout")]
        public UpgradeData.UpgradeType StartingWeapon;
        
        [Header("Base Stats Modifiers")]
        public int BaseMaxHealth = 100;
        public float BaseMoveSpeed = 3f;
        public float StartingMight = 1.0f;
        public float StartingArea = 1.0f;
        public float StartingCooldown = 1.0f;
        public int StartingArmor = 0;
    }
}
