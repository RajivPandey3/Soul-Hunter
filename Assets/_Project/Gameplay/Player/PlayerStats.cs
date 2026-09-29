using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Player Stats System.
    /// Ye passive items (Spinach, Empty Tome) ke liye global multipliers store karta hai.
    /// Saare weapons damage, size, aur cooldown ke liye isey read karte hain.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        // Default modifiers (1.0 = 100%)
        public float Might { get; private set; } = 1.0f; // Damage multiplier
        public float Cooldown { get; private set; } = 1.0f; // Cooldown multiplier (lower is faster)
        public float Area { get; private set; } = 1.0f; // Size multiplier
        public float MoveSpeedMultiplier { get; private set; } = 1.0f; // Speed multiplier
        
        // --- Advanced VS Stats ---
        public int Armor { get; private set; } = 0; // Flat damage reduction
        public float Luck { get; private set; } = 1.0f; // Critical hits, better chest RNG
        public float Greed { get; private set; } = 1.0f; // Gold multiplier
        public float Curse { get; private set; } = 1.0f; // Enemy speed/health/spawn multiplier
        public int Revivals { get; private set; } = 0; // Number of times player can revive
        
        // --- Expansion 4 Stats ---
        public int Amount { get; private set; } = 0; // Extra projectiles (Duplicator)
        public float Duration { get; private set; } = 1.0f; // Weapon duration multiplier (Spellbinder)
        public float Regen { get; private set; } = 0f; // HP per second (Pummarola)
        public float Magnet { get; private set; } = 1.0f; // Pickup radius multiplier (Attractorb)
        public float ExpBonus { get; private set; } = 1.0f; // EXP multiplier (Crown)
        public float ProjectileSpeed { get; private set; } = 1.0f; // Projectile speed multiplier (Bracer)

        // Learning Comment:
        // Wandering Merchant blessings aur upgrades ke zariye permanent bonus max health track karta hai.
        public int MaxHealthBonus { get; private set; } = 0;

        private void Start()
        {
            // A mirror (Shadow Kael) already holds the player's final numbers, meta bonuses included.
            if (_isMirror) { SyncArmor(); return; }
            // Meta Progression load karein
            if (SoulHunter.Core.Services.GameServices.Instance != null)
            {
                var saveService = SoulHunter.Core.Services.GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
                if (saveService != null && saveService.CurrentData != null) ApplyPowerUps(saveService.CurrentData);
            }
            // VS Inverse mode: +20% Luck for the run, once. RunModes is filled from the save in
            // LevelProgressionManager.Awake, before any Start of the gameplay scene.
            RunModeLuckApplied = SoulHunter.Gameplay.Data.RunModes.LuckBonus;
            if (RunModeLuckApplied > 0f) AddLuck(RunModeLuckApplied);
            SyncArmor();
        }

        /// <summary>VS Charm: enemies added to every wave (flat, after Curse).</summary>
        public int Charm { get; private set; }
        /// <summary>VS Defang: share of spawned wave enemies that cannot deal damage (0-0.15).</summary>
        public float Defang { get; private set; }
        /// <summary>VS Preserve: chance that a Reroll, Skip or Banish is kept after use (0-0.5).</summary>
        public float Preserve { get; private set; }

        /// <summary>
        /// The VS PowerUps bought (PowerUps; every value from vampire.survivors.wiki/w/PowerUps and each PowerUp's page).
        /// Max Health is applied by PlayerController (it sits on HealthController) and Reroll/Skip/Banish by LevelUpManager.
        /// </summary>
        public void ApplyPowerUps(SoulHunter.Core.Persistence.GameData data)
        {
            if (data == null) return;
            int Rank(SoulHunter.Core.Persistence.PowerUpId id) => SoulHunter.Core.Persistence.PowerUps.Rank(data, id);
            float Bonus(SoulHunter.Core.Persistence.PowerUpId id) => SoulHunter.Core.Persistence.PowerUps.Bonus(id, Rank(id));
            Might += Bonus(SoulHunter.Core.Persistence.PowerUpId.Might);
            Armor += Rank(SoulHunter.Core.Persistence.PowerUpId.Armor);
            Regen += Bonus(SoulHunter.Core.Persistence.PowerUpId.Recovery);
            Cooldown = Mathf.Max(0.1f, Cooldown - Bonus(SoulHunter.Core.Persistence.PowerUpId.Cooldown));
            Area += Bonus(SoulHunter.Core.Persistence.PowerUpId.Area);
            ProjectileSpeed += Bonus(SoulHunter.Core.Persistence.PowerUpId.Speed);
            Duration += Bonus(SoulHunter.Core.Persistence.PowerUpId.Duration);
            Amount += Rank(SoulHunter.Core.Persistence.PowerUpId.Amount);
            MoveSpeedMultiplier += Bonus(SoulHunter.Core.Persistence.PowerUpId.MoveSpeed);
            Magnet *= SoulHunter.Core.Persistence.PowerUps.MagnetMultiplier(Rank(SoulHunter.Core.Persistence.PowerUpId.Magnet));
            Luck += Bonus(SoulHunter.Core.Persistence.PowerUpId.Luck);
            ExpBonus += Bonus(SoulHunter.Core.Persistence.PowerUpId.Growth);
            Greed += Bonus(SoulHunter.Core.Persistence.PowerUpId.Greed);
            Curse += Bonus(SoulHunter.Core.Persistence.PowerUpId.Curse);
            Revivals += Rank(SoulHunter.Core.Persistence.PowerUpId.Revival);
            // Omni: Might, Projectile Speed, Duration and Area +2% per rank.
            float omni = Bonus(SoulHunter.Core.Persistence.PowerUpId.Omni);
            Might += omni; ProjectileSpeed += omni; Duration += omni; Area += omni;
            Charm += Mathf.RoundToInt(Bonus(SoulHunter.Core.Persistence.PowerUpId.Charm));
            Defang += Bonus(SoulHunter.Core.Persistence.PowerUpId.Defang);
            Preserve += Bonus(SoulHunter.Core.Persistence.PowerUpId.Preserve);
            SyncArmor();
        }

        /// <summary>The run-mode Luck bonus added at the start of this run (VS Inverse: +0.2), for the HUD and tests.</summary>
        public float RunModeLuckApplied { get; private set; }

        private bool _characterApplied;
        private bool _isMirror;

        /// <summary>
        /// Level 10: Shadow Kael carries a copy of the player's stats, so every mirrored weapon scales exactly as the
        /// player's own (Might, Cooldown, Area, Amount, Duration, Speed, Luck, crits, bounces). The copy is taken once,
        /// at encounter start, and does not re-apply meta bonuses or follow the player's later level-ups.
        /// </summary>
        public void CopyFrom(PlayerStats source)
        {
            _isMirror = true;
            if (source == null) return;
            Might = source.Might; Cooldown = source.Cooldown; Area = source.Area; MoveSpeedMultiplier = source.MoveSpeedMultiplier;
            Armor = source.Armor; Luck = source.Luck; Greed = source.Greed; Curse = source.Curse; Revivals = source.Revivals;
            Amount = source.Amount; Duration = source.Duration; Regen = source.Regen; Magnet = source.Magnet;
            ExpBonus = source.ExpBonus; ProjectileSpeed = source.ProjectileSpeed; MaxHealthBonus = source.MaxHealthBonus;
            CritDamageMultiplier = source.CritDamageMultiplier; ExtraProjectileBounces = source.ExtraProjectileBounces;
            SyncArmor();
        }
        private SoulHunter.Gameplay.Data.CharacterData _character;
        private PlayerExperience _experience;

        /// <summary>Critical hit damage multiplier (VS default 2x; some characters crit harder).</summary>
        public float CritDamageMultiplier { get; private set; } = 2f;
        /// <summary>Extra bounces added to the holder's projectiles.</summary>
        public int ExtraProjectileBounces { get; private set; }
        /// <summary>Extra move speed at 0 HP, scaling linearly from full HP (0.5 = +50%).</summary>
        public float LowHealthSpeedBonus { get; private set; }
        /// <summary>Seconds of damage immunity granted when the run starts.</summary>
        public float StartingShieldSeconds { get; private set; }

        /// <summary>
        /// VS rule: each character starts with its own stat block. Applied once, on top of
        /// meta-shop bonuses (multipliers: 1.0 = no change; flat stats: 0 = no change).
        /// Also arms the character's level-up bonus and special abilities.
        /// </summary>
        public void ApplyCharacter(SoulHunter.Gameplay.Data.CharacterData character)
        {
            if (character == null || _characterApplied) return;
            _characterApplied = true;
            _character = character;
            Might += character.StartingMight - 1f;
            Area += character.StartingArea - 1f;
            Cooldown = Mathf.Max(0.1f, Cooldown + (character.StartingCooldown - 1f));
            Armor += character.StartingArmor;
            MoveSpeedMultiplier = Mathf.Max(0.1f, MoveSpeedMultiplier + (character.BaseMoveSpeed - 1f));
            Regen += character.StartingRecovery;
            ProjectileSpeed = Mathf.Max(0.1f, ProjectileSpeed + (character.StartingSpeed - 1f));
            Duration = Mathf.Max(0.1f, Duration + (character.StartingDuration - 1f));
            Amount += character.StartingAmount;
            Revivals += character.StartingRevivals;
            Magnet = Mathf.Max(0.1f, Magnet + (character.StartingMagnet - 1f));
            Luck += character.StartingLuck - 1f;
            ExpBonus = Mathf.Max(0.1f, ExpBonus + (character.StartingGrowth - 1f));
            Greed += character.StartingGreed - 1f;
            Curse = Mathf.Max(0.1f, Curse + (character.StartingCurse - 1f));
            CritDamageMultiplier = Mathf.Max(1f, character.CritDamageMultiplier);
            ExtraProjectileBounces = Mathf.Max(0, character.ExtraProjectileBounces);
            LowHealthSpeedBonus = Mathf.Max(0f, character.LowHealthSpeedBonus);
            StartingShieldSeconds = Mathf.Max(0f, character.StartingShieldSeconds);
            SyncArmor();

            if (_experience == null) _experience = GetComponent<PlayerExperience>();
            if (_experience != null) _experience.OnLevelUp += HandleLevelUp;
        }

        private void OnDestroy()
        {
            if (_experience != null) _experience.OnLevelUp -= HandleLevelUp;
        }

        private void HandleLevelUp(int level) => ApplyLevelBonus(level);

        /// <summary>
        /// VS rule: some characters grow a stat at every Nth level (e.g. +10% Might every 10 levels).
        /// Called once per level gained.
        /// </summary>
        public void ApplyLevelBonus(int level)
        {
            var c = _character;
            if (c == null || c.LevelBonus == SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.None) return;
            if (level <= 0 || level % Mathf.Max(1, c.LevelBonusEveryLevels) != 0) return;
            float amount = c.LevelBonusAmount;
            switch (c.LevelBonus)
            {
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.Might: AddMight(amount); break;
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.Armor: AddArmor(Mathf.RoundToInt(amount)); break;
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.Area: AddArea(amount); break;
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.Cooldown: ReduceCooldown(amount); break;
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.MaxHealth: AddMaxHealth(Mathf.RoundToInt(amount)); break;
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.Recovery: AddRegen(amount); break;
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.Luck: AddLuck(amount); break;
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.Growth: AddExpBonus(amount); break;
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.Greed: AddGreed(amount); break;
                case SoulHunter.Gameplay.Data.CharacterData.LevelBonusStat.MoveSpeed: AddMoveSpeed(amount); break;
            }
        }

        /// <summary>Move speed multiplier from the low-health bonus: 1 at full HP, 1 + bonus at 0 HP.</summary>
        public float LowHealthSpeedFactor(int currentHealth, int maxHealth)
        {
            if (LowHealthSpeedBonus <= 0f || maxHealth <= 0) return 1f;
            float missing = 1f - Mathf.Clamp01((float)currentHealth / maxHealth);
            return 1f + LowHealthSpeedBonus * missing;
        }

        // Armor is applied by HealthController, so push every change to it.
        private void SyncArmor()
        {
            var health = GetComponent<HealthController>();
            if (health == null) health = GetComponentInParent<HealthController>();
            if (health != null) health.Armor = Armor;
        }

        public void AddMight(float amount) { Might += amount; }
        public void ReduceCooldown(float amount) { Cooldown = Mathf.Max(0.1f, Cooldown - amount); }
        public void AddArea(float amount) { Area += amount; }
        public void AddMoveSpeed(float amount) { MoveSpeedMultiplier += amount; }

        public void AddArmor(int amount) { Armor += amount; SyncArmor(); }
        public void AddLuck(float amount) { Luck += amount; }
        public void AddGreed(float amount) { Greed += amount; }
        public void AddCurse(float amount) { Curse += amount; }
        public void AddRevival(int amount) { Revivals += amount; }
        
        public void AddAmount(int amount) { Amount += amount; }
        public void AddDuration(float amount) { Duration += amount; }
        public void AddRegen(float amount) { Regen += amount; }
        public void AddMagnet(float amount) { Magnet += amount; }
        /// <summary>VS Attractorb: its Magnet bonus multiplies (wiki: "Multiplicative").</summary>
        public void MultiplyMagnet(float factor) { if (factor > 0f) Magnet *= factor; }
        public void AddExpBonus(float amount) { ExpBonus += amount; }
        public void AddProjectileSpeed(float amount) { ProjectileSpeed += amount; }

        // Learning Comment:
        // Permanent max health bonus barhata hai aur agar HealthController present ho toh usay IncreaseMaxHealth ke zariye notify karta hai.
        public void AddMaxHealth(int amount)
        {
            MaxHealthBonus += amount;
            var health = GetComponent<HealthController>() ?? GetComponentInParent<HealthController>();
            if (health != null)
            {
                health.IncreaseMaxHealth(amount);
            }
        }
        
        public bool UseRevival() 
        {
            if (Revivals > 0)
            {
                Revivals--;
                return true;
            }
            return false;
        }
    }
}
