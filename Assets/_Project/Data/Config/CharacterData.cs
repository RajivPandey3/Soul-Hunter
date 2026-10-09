using UnityEngine;
using SoulHunter.Gameplay.Combat; // For UpgradeType

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Har character ki apni khasiyat hoti hai (starting weapon, extra health, etc).
    /// Ye ScriptableObject humein naye characters asani se banane ki taqat deta hai.
    /// Multipliers: 1.0 = no change. Flat values (Recovery, Armor, Amount, Revival): 0 = no change.
    /// </summary>
    [CreateAssetMenu(fileName = "New Character", menuName = "Soul Hunter/Character Data")]
    public class CharacterData : ScriptableObject
    {
        /// <summary>Stats a character can grow as it levels up (VS "+10% Might every 10 levels").</summary>
        public enum LevelBonusStat { None, Might, Armor, Area, Cooldown, MaxHealth, Recovery, Luck, Growth, Greed, MoveSpeed }

        public string CharacterName;
        [TextArea]
        public string Description;
        public Sprite CharacterIcon;
        public GameObject CharacterModelPrefab; // 3D ya 2D model

        [Header("Starting Loadout")]
        public UpgradeData.UpgradeType StartingWeapon;

        [Header("Base Stats Modifiers")]
        public int BaseMaxHealth = 100;
        [Tooltip("Move speed multiplier (owner decision 2026-09-25): 1.0 = the player's base speed.")]
        public float BaseMoveSpeed = 1f;
        public float StartingMight = 1.0f;
        public float StartingArea = 1.0f;
        public float StartingCooldown = 1.0f;
        public int StartingArmor = 0;

        [Header("More Stats (VS stat block)")]
        [Tooltip("HP regenerated per second.")]
        public float StartingRecovery = 0f;
        [Tooltip("Projectile speed multiplier.")]
        public float StartingSpeed = 1f;
        public float StartingDuration = 1f;
        [Tooltip("Extra projectiles.")]
        public int StartingAmount = 0;
        public int StartingRevivals = 0;
        public float StartingMagnet = 1f;
        public float StartingLuck = 1f;
        [Tooltip("Experience multiplier.")]
        public float StartingGrowth = 1f;
        public float StartingGreed = 1f;
        public float StartingCurse = 1f;

        [Header("Level-Up Bonus")]
        public LevelBonusStat LevelBonus = LevelBonusStat.None;
        [Tooltip("Bonus is granted at every multiple of this player level.")]
        [Min(1)] public int LevelBonusEveryLevels = 10;
        [Tooltip("Multiplier stats: fraction (0.1 = +10%). Cooldown: fraction removed. Armor/MaxHealth: flat.")]
        public float LevelBonusAmount = 0.1f;
        [Tooltip("Last player level that grants the bonus (0 = no cap). VS wiki Antonio_Belpaese: \"+10% Might every 10 levels until level 50. The maximum Might gained this way is +50%.\"")]
        [Min(0)] public int LevelBonusUntilLevel = 0;

        /// <summary>True when reaching <paramref name="level"/> grants the level-up bonus: every Nth level, up to the cap (VS).</summary>
        public bool GrantsLevelBonusAt(int level) =>
            LevelBonus != LevelBonusStat.None && level > 0 && level % Mathf.Max(1, LevelBonusEveryLevels) == 0
            && (LevelBonusUntilLevel <= 0 || level <= LevelBonusUntilLevel);

        [Header("Special Abilities")]
        [Tooltip("Critical hit damage multiplier (default 2x).")]
        public float CritDamageMultiplier = 2f;
        [Tooltip("Extra bounces for the character's projectiles.")]
        public int ExtraProjectileBounces = 0;
        [Tooltip("Extra move speed at 0 HP, scaling linearly from full HP (0.5 = +50%).")]
        public float LowHealthSpeedBonus = 0f;
        [Tooltip("Seconds of full damage immunity when the run starts.")]
        public float StartingShieldSeconds = 0f;
        [Tooltip("Shown around the player while the starting shield lasts (blinks near the end).")]
        public GameObject ShieldVisualPrefab;

        /// <summary>VS rule: characters unlock by meeting a condition in a run, then are bought with gold.</summary>
        public enum UnlockCondition { None, ClearStage, SurviveInStage, ReachPlayerLevel, KillsInRun, WeaponLevel }

        [Header("Unlock (VS rule)")]
        [Tooltip("Level this character comes from; the select screen filter groups characters by it.")]
        [Range(1, 10)] public int OriginStage = 1;
        [Tooltip("None = available from the start.")]
        public UnlockCondition Unlock = UnlockCondition.None;
        [Tooltip("ClearStage / SurviveInStage: the stage number.")]
        public int UnlockStage = 1;
        [Tooltip("SurviveInStage: seconds. ReachPlayerLevel: level. KillsInRun: kills. WeaponLevel: weapon level.")]
        public float UnlockTarget;
        [Tooltip("WeaponLevel: the weapon.")]
        public UpgradeData.UpgradeType UnlockWeapon;
        [Tooltip("Gold to buy once unlocked; 0 = free.")]
        [Min(0)] public int GoldPrice;
    }
}
