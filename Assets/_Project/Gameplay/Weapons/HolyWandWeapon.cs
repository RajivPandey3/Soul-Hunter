using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    public class HolyWandWeapon : AutoAttackWeapon
    {
        [Header("Holy Wand Settings (Evolved Magic Wand)")]
        public GameObject ProjectilePrefab; 
        public float ProjectileSpeed = 25f; 
        
        private void Start()
        {
            AttackCooldown = 0.1f; // ALMOST ZERO COOLDOWN (VS mechanic)
        }

        public override void LevelUp() {}

        protected override void Attack()
        {
            if (ProjectilePrefab == null) return;

            EnemyController closestEnemy = null;
            float minDistance = float.MaxValue;
            var allEnemies = EnemyController.ActiveEnemies;
            
            foreach (var enemy in allEnemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy != null)
            {
                Vector3 shootDir = (closestEnemy.transform.position - transform.position).normalized;
                GameObject projObj = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(ProjectilePrefab, transform.position, Quaternion.identity)
                    : Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);
                if (projObj == null) return;
                
                var proj = projObj.GetComponent<Projectile>();
                if (proj == null) proj = projObj.AddComponent<Projectile>();
                proj.Initialize(shootDir, ProjectileSpeed, 40f, 5f, DamageType.Holy);
                
                var damageDealer = projObj.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = projObj.AddComponent<ProjectileDamage>();
                damageDealer.SourceWeaponName = "Holy Wand";
                damageDealer.DamageType = DamageType.Holy;
            }
        }
    }
}
