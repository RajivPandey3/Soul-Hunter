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
        // VS stat caps (vampire.survivors.wiki/w/Player_stats, "Max value", fetched 2026-10-01): Might 1000% (not for Soul
        // Eater), Area 1000%, Speed 500%, Duration 500%, Amount 10, Armor 50. Cooldown's 10% floor is applied where it changes.
        public const float MaxMight = 10f, MaxArea = 10f, MaxProjectileSpeed = 5f, MaxDuration = 5f;
        public const int MaxAmount = 10, MaxArmor = 50;

        // Default modifiers (1.0 = 100%)
        /// <summary>The Might gathered from every source, before the VS cap (wiki Player_stats).</summary>
        public float RawMight { get; private set; } = 1.0f;
        /// <summary>Might as the game uses it: capped at the VS maximum (MaxMight).</summary>
        public float Might => Mathf.Min(RawMight + ArcanaMightBonus, MaxMight);
        /// <summary>Cooldown multiplier (lower is faster), with the Arcana reduction on top (never under the VS 10% floor).</summary>
        public float Cooldown => Mathf.Max(0.1f, _cooldown - ArcanaCooldownBonus);
        private float _cooldown = 1.0f;
        /// <summary>The Area gathered from every source, before the VS cap (wiki Player_stats).</summary>
        public float RawArea { get; private set; } = 1.0f;
        /// <summary>Area as the game uses it: capped at the VS maximum (MaxArea).</summary>
        public float Area => Mathf.Min(RawArea + ArcanaAreaBonus, MaxArea) * ArcanaAreaFactor;
        public float MoveSpeedMultiplier { get; private set; } = 1.0f; // Speed multiplier
        
        // --- Advanced VS Stats ---
        /// <summary>The Armor gathered from every source, before the VS cap (wiki Player_stats).</summary>
        public int RawArmor { get; private set; } = 0;
        /// <summary>Armor as the game uses it: capped at the VS maximum (MaxArmor).</summary>
        public int Armor => Mathf.Min(RawArmor, MaxArmor);
        public float Luck => (_luck + ArcanaLuckBonus) * ArcanaLuckFactor; // Critical hits, better chest RNG
        private float _luck = 1.0f;
        public float Greed => (_greed + ArcanaGreedBonus) * ArcanaGreedFactor; // Gold multiplier
        private float _greed = 1.0f;
        public float Curse => (_curse + ArcanaCurseBonus) * ArcanaCurseFactor; // Enemy speed/health/spawn multiplier
        private float _curse = 1.0f;
        public int Revivals { get; private set; } = 0; // Number of times player can revive
        
        // --- Expansion 4 Stats ---
        /// <summary>The Amount gathered from every source, before the VS cap (wiki Player_stats).</summary>
        public int RawAmount { get; private set; } = 0;
        /// <summary>Amount as the game uses it: capped at the VS maximum (MaxAmount).</summary>
        public int Amount => Mathf.Min(RawAmount, MaxAmount);
        /// <summary>The Duration gathered from every source, before the VS cap (wiki Player_stats).</summary>
        public float RawDuration { get; private set; } = 1.0f;
        /// <summary>Duration as the game uses it: capped at the VS maximum (MaxDuration).</summary>
        public float Duration => Mathf.Min(RawDuration + ArcanaDurationBonus, MaxDuration) * ArcanaDurationFactor;
        public float Regen { get; private set; } = 0f; // HP per second (Pummarola)
        /// <summary>
        /// VS wiki (Magnet): "Magnet = 30 * Character bonus * PowerUp multiplier", every modifier multiplies, and the
        /// Golden Egg adds to the base before them. Soul Hunter's base 1 stands for VS 30 (a Soul Egg's +0.01 is VS +0.3);
        /// Attractorb multiplies on top. No cap (wiki: "There is no upper limit").
        /// </summary>
        public float Magnet => _magnetBase * _magnetCharacter * _magnetPowerUp * _magnetItems;
        private float _magnetBase = 1f, _magnetCharacter = 1f, _magnetPowerUp = 1f, _magnetItems = 1f;
        /// <summary>
        /// World radius that Magnet 1 reaches (Soul Hunter's mapping of VS Magnet 30; the wiki gives no unit). One radius for
        /// every pickup (wiki Pickups: "The Magnet stat affects the radius pickups can be gathered from").
        /// </summary>
        public const float BaseMagnetRadius = 4f;
        /// <summary>Radius inside which gems and pickups fly to the player.</summary>
        public float MagnetRadius => BaseMagnetRadius * Mathf.Max(0.1f, Magnet);
        public float ExpBonus => (_expBonus + ArcanaGrowthBonus) * ArcanaGrowthFactor; // EXP multiplier (Crown)
        private float _expBonus = 1.0f;
        /// <summary>The ProjectileSpeed gathered from every source, before the VS cap (wiki Player_stats).</summary>
        public float RawProjectileSpeed { get; private set; } = 1.0f;
        /// <summary>ProjectileSpeed as the game uses it: capped at the VS maximum (MaxProjectileSpeed).</summary>
        public float ProjectileSpeed => Mathf.Min(RawProjectileSpeed + ArcanaSpeedBonus, MaxProjectileSpeed) * ArcanaSpeedFactor;

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
            // A maxed PowerUp switched off gives nothing (wiki PowerUps: the player "can then disable the PowerUp").
            int Rank(SoulHunter.Core.Persistence.PowerUpId id) => SoulHunter.Core.Persistence.PowerUps.ActiveRank(data, id);
            float Bonus(SoulHunter.Core.Persistence.PowerUpId id) => SoulHunter.Core.Persistence.PowerUps.Bonus(id, Rank(id));
            RawMight +=Bonus(SoulHunter.Core.Persistence.PowerUpId.Might);
            RawArmor += Rank(SoulHunter.Core.Persistence.PowerUpId.Armor);
            Regen += Bonus(SoulHunter.Core.Persistence.PowerUpId.Recovery);
            _cooldown = Mathf.Max(0.1f, _cooldown - Bonus(SoulHunter.Core.Persistence.PowerUpId.Cooldown));
            RawArea += Bonus(SoulHunter.Core.Persistence.PowerUpId.Area);
            RawProjectileSpeed += Bonus(SoulHunter.Core.Persistence.PowerUpId.Speed);
            RawDuration += Bonus(SoulHunter.Core.Persistence.PowerUpId.Duration);
            RawAmount += Rank(SoulHunter.Core.Persistence.PowerUpId.Amount);
            MoveSpeedMultiplier += Bonus(SoulHunter.Core.Persistence.PowerUpId.MoveSpeed);
            _magnetPowerUp *= SoulHunter.Core.Persistence.PowerUps.MagnetMultiplier(Rank(SoulHunter.Core.Persistence.PowerUpId.Magnet));
            _luck += Bonus(SoulHunter.Core.Persistence.PowerUpId.Luck);
            _expBonus += Bonus(SoulHunter.Core.Persistence.PowerUpId.Growth);
            _greed += Bonus(SoulHunter.Core.Persistence.PowerUpId.Greed);
            _curse += Bonus(SoulHunter.Core.Persistence.PowerUpId.Curse);
            Revivals += Rank(SoulHunter.Core.Persistence.PowerUpId.Revival);
            // Omni: Might, Projectile Speed, Duration and Area +2% per rank.
            float omni = Bonus(SoulHunter.Core.Persistence.PowerUpId.Omni);
            RawMight += omni; RawProjectileSpeed += omni; RawDuration += omni; RawArea += omni;
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
            RawMight = source.RawMight; _cooldown = source._cooldown; RawArea = source.RawArea; MoveSpeedMultiplier = source.MoveSpeedMultiplier;
            RawArmor = source.RawArmor; _luck = source._luck; _greed = source._greed; _curse = source._curse; Revivals = source.Revivals;
            RawAmount = source.RawAmount; RawDuration = source.RawDuration; Regen = source.Regen;
            _magnetBase = source._magnetBase; _magnetCharacter = source._magnetCharacter; _magnetPowerUp = source._magnetPowerUp; _magnetItems = source._magnetItems;
            _expBonus = source._expBonus; RawProjectileSpeed = source.RawProjectileSpeed; MaxHealthBonus = source.MaxHealthBonus;
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
            RawMight += character.StartingMight - 1f;
            RawArea += character.StartingArea - 1f;
            _cooldown = Mathf.Max(0.1f, _cooldown + (character.StartingCooldown - 1f));
            RawArmor += character.StartingArmor;
            MoveSpeedMultiplier = Mathf.Max(0.1f, MoveSpeedMultiplier + (character.BaseMoveSpeed - 1f));
            Regen += character.StartingRecovery;
            RawProjectileSpeed = Mathf.Max(0.1f, RawProjectileSpeed + (character.StartingSpeed - 1f));
            RawDuration = Mathf.Max(0.1f, RawDuration + (character.StartingDuration - 1f));
            RawAmount += character.StartingAmount;
            Revivals += character.StartingRevivals;
            _magnetCharacter = Mathf.Max(0.1f, character.StartingMagnet); // VS: the character bonus multiplies
            _luck += character.StartingLuck - 1f;
            _expBonus = Mathf.Max(0.1f, _expBonus + (character.StartingGrowth - 1f));
            _greed += character.StartingGreed - 1f;
            _curse = Mathf.Max(0.1f, _curse + (character.StartingCurse - 1f));
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
            if (c == null || !c.GrantsLevelBonusAt(level)) return;
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

        // ---------------------------------------------------------------- Arcanas (ArcanaManager, release item P2-01)
        // The player's Arcana bonuses and oscillations are kept apart from the raw stats, so Shadow Kael's copy (CopyFrom)
        // never carries them: "Shadow Kael's mirrored weapons never get Arcana effects". Factors multiply "on top of
        // everything else" (wiki Chaos_in_the_Dark_Night_(V), Lost_&_Found_Painting_(XVII), Boogaloo_of_Illusions_(XVIII):
        // "The oscillation is done by applying a multiplier on top of everything else"), so they come after the cap.
        /// <summary>Additive Arcana bonuses (per-level gains, Silent Old Sanctuary's empty slots); 0 without Arcanas.</summary>
        public float ArcanaMightBonus, ArcanaCooldownBonus, ArcanaAreaBonus, ArcanaDurationBonus, ArcanaSpeedBonus,
            ArcanaGrowthBonus, ArcanaLuckBonus, ArcanaGreedBonus, ArcanaCurseBonus;
        /// <summary>Arcana multipliers on top (oscillations, Wicked Season's doubling); 1 without Arcanas.</summary>
        public float ArcanaAreaFactor = 1f, ArcanaDurationFactor = 1f, ArcanaSpeedFactor = 1f,
            ArcanaGrowthFactor = 1f, ArcanaLuckFactor = 1f, ArcanaGreedFactor = 1f, ArcanaCurseFactor = 1f;

        public void AddMight(float amount) { RawMight += amount; }
        public void ReduceCooldown(float amount) { _cooldown = Mathf.Max(0.1f, _cooldown - amount); }
        public void AddArea(float amount) { RawArea += amount; }
        public void AddMoveSpeed(float amount) { MoveSpeedMultiplier += amount; }

        public void AddArmor(int amount) { RawArmor += amount; SyncArmor(); }
        public void AddLuck(float amount) { _luck += amount; }
        public void AddGreed(float amount) { _greed += amount; }
        public void AddCurse(float amount) { _curse += amount; }
        public void AddRevival(int amount) { Revivals += amount; }
        
        public void AddAmount(int amount) { RawAmount += amount; }
        public void AddDuration(float amount) { RawDuration += amount; }
        public void AddRegen(float amount) { Regen += amount; }
        /// <summary>Adds to the Magnet base, before the character, PowerUp and item multipliers (VS Golden Egg).</summary>
        public void AddMagnet(float amount) { _magnetBase += amount; }
        /// <summary>VS Attractorb: its Magnet bonus multiplies (wiki: "Multiplicative").</summary>
        public void MultiplyMagnet(float factor) { if (factor > 0f) _magnetItems *= factor; }
        public void AddExpBonus(float amount) { _expBonus += amount; }
        public void AddProjectileSpeed(float amount) { RawProjectileSpeed += amount; }

        // Learning Comment:
        // Permanent max health bonus barhata hai aur agar HealthController present ho toh usay IncreaseMaxHealth ke zariye notify karta hai.
        /// <summary>Lowers the Max Health bonus record only (SoulEggs.Remove changes the health itself).</summary>
        public void RemoveMaxHealthBonus(int amount) { MaxHealthBonus = Mathf.Max(0, MaxHealthBonus - Mathf.Max(0, amount)); }

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
