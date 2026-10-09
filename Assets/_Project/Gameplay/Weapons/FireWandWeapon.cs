using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Expansion 1: Fire Wand (VS Style).
    /// Magic Wand se milta julta hai par iski aag ki goli slow hoti hai aur random dushman ko marti hai, par damage bohot ziada hota hai.
    /// </summary>
    public class FireWandWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;
        [Header("Fire Wand Settings")]
        public GameObject FireballPrefab; 
        public float ProjectileSpeed = 5f; // Soul Hunter speed tuning (kept); VS Speed level-ups scale it
        public float FireRange = 15f;

        // VS wiki (Fire Wand): 20 damage, 3.0 s cooldown, 3 fireballs, pierce 1. Level-ups: +10 damage every level
        // (L2..L8); Speed +20% at L3, L5 and L7.
        private const float VsBaseDamage = 20f;
        private const float VsDamagePerLevel = 10f;
        private const float VsCooldown = 3.0f;
        private const int VsAmount = 3;
        private const float VsSpeedStep = 0.2f;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, MaxLevel);
        public float BaseDamage => VsBaseDamage + VsDamagePerLevel * (Level - 1);
        public int FireballsPerAttack => VsAmount;
        /// <summary>Weapon speed from level-ups, on top of our base speed: 1.0, 1.2 (L3), 1.4 (L5), 1.6 (L7).</summary>
        public float LevelSpeedMultiplier => 1f + VsSpeedStep * ((Level >= 3 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 7 ? 1 : 0));

        protected override void Awake()
        {
            base.Awake();
            ApplyLevelStats();
        }

        public override void LevelUp()
        {
            CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1);
            ApplyLevelStats();
        }

        /// <summary>Keeps the base fields in step with the level (the base timer reads AttackCooldown).</summary>
        private void ApplyLevelStats()
        {
            DamageAmount = BaseDamage;
            AttackCooldown = VsCooldown;
        }

        public const float VsInterval = 0.02f;

        protected override void Attack()
        {
            if (FireballPrefab == null) return;

            var allEnemies = EnemyController.ActiveEnemies;
            if (allEnemies.Count == 0) return;

            // VS interval (wiki Fire_Wand): the Amount of shots leave 0.02s apart.
            FireInSuccession(FireballsPerAttack + ExtraAmount, VsInterval, i =>
            {
                // Fire wand hamesha completely random enemy ko target karta hai (VS rule)
                // Owner decision: aim at the nearest enemy; extra fireballs fan out around it.
                Transform target = FindNearestEnemy(FireRange);
                if (target != null)
                {
                    Vector3 fan = Quaternion.Euler(0f, (i - (FireballsPerAttack + ExtraAmount - 1) * 0.5f) * 8f, 0f) * FlatDirectionTo(target);
                    ShootFireball(transform.position + fan * 10f);
                }
            });
        }

        private void ShootFireball(Vector3 targetPosition)
        {
            Vector3 fireDir = (targetPosition - transform.position).normalized;
            GameObject fireball = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(FireballPrefab, transform.position, Quaternion.identity)
                : Instantiate(FireballPrefab, transform.position, Quaternion.identity);
            if (fireball == null) return;
            ApplyArea(fireball, FireballPrefab);
            
            var proj = fireball.GetComponent<Projectile>();
            if (proj == null) proj = fireball.AddComponent<Projectile>();
            
            proj.Initialize(fireDir, ProjectileSpeed * LevelSpeedMultiplier * SpeedMultiplier, ScaledDamage(BaseDamage), 3f, DamageType.Magic, "Fire Wand"); // Owner (2026-09-26, Level 7): weapons are typed by theme so ghosts can be fought with more than four weapons.
            proj.AddBounces(ExtraBounces + ArcanaBounces); // + Waltz of Pearls (XI)
            
            var damageDealer = fireball.GetComponent<ProjectileDamage>();
            if (damageDealer == null) damageDealer = fireball.AddComponent<ProjectileDamage>();
            damageDealer.SourceWeaponName = "Fire Wand";
            AimAtHostiles(fireball);
        }
    }
}
