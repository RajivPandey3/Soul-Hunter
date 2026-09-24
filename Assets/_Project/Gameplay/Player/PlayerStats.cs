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
            // Meta Progression load karein
            if (SoulHunter.Core.Services.GameServices.Instance != null)
            {
                var saveService = SoulHunter.Core.Services.GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
                if (saveService != null && saveService.CurrentData != null)
                {
                    Might += (saveService.CurrentData.MetaMightLevel * 0.1f);
                    Armor += saveService.CurrentData.MetaArmorLevel;
                    Greed += (saveService.CurrentData.MetaGreedLevel * 0.2f);
                    Revivals += saveService.CurrentData.MetaRevivalLevel;
                    Debug.Log("[PlayerStats] Meta Progression Loaded! Player is stronger.");
                }
            }
            SyncArmor();
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
