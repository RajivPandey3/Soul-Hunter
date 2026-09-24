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
        [Header("Fire Wand Settings")]
        public GameObject FireballPrefab; 
        public float ProjectileSpeed = 5f; // Slow speed
        public float FireRange = 15f;
        
        private int _fireballsPerAttack = 1; 

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 20f; // Bohat heavy damage
            
            if (CurrentLevel == 3 || CurrentLevel == 6) 
            {
                _fireballsPerAttack++; // Ek sath mazeed aag ke gole
            }
        }

        protected override void Attack()
        {
            if (FireballPrefab == null) return;

            var allEnemies = EnemyController.ActiveEnemies;
            if (allEnemies.Count == 0) return;

            for (int i = 0; i < _fireballsPerAttack + ExtraAmount; i++)
            {
                // Fire wand hamesha completely random enemy ko target karta hai (VS rule)
                // Owner decision: aim at the nearest enemy; extra fireballs fan out around it.
                Transform target = FindNearestEnemy(FireRange);
                if (target != null)
                {
                    Vector3 fan = Quaternion.Euler(0f, (i - (_fireballsPerAttack + ExtraAmount - 1) * 0.5f) * 8f, 0f) * FlatDirectionTo(target);
                    ShootFireball(transform.position + fan * 10f);
                }
            }
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
            
            proj.Initialize(fireDir, ProjectileSpeed * SpeedMultiplier, ScaledDamage(DamageAmount), 3f);
            
            var damageDealer = fireball.GetComponent<ProjectileDamage>();
            if (damageDealer == null) damageDealer = fireball.AddComponent<ProjectileDamage>();
            damageDealer.SourceWeaponName = "Fire Wand";
        }
    }
}
