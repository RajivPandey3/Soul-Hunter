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

            if (closestEnemy == null) return;

            Vector3 aimDir = (closestEnemy.transform.position - transform.position).normalized;
            // Amount adds extra shots, fanned slightly around the target direction.
            int shots = 1 + ExtraAmount;
            for (int i = 0; i < shots; i++)
            {
                float spread = shots > 1 ? Mathf.Lerp(-5f, 5f, i / (float)(shots - 1)) : 0f;
                FireAt(Quaternion.Euler(0f, spread, 0f) * aimDir);
            }
        }

        private void FireAt(Vector3 shootDir)
        {
            GameObject projObj = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(ProjectilePrefab, transform.position, Quaternion.identity)
                : Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);
            if (projObj == null) return;
            ApplyArea(projObj, ProjectilePrefab);

            var proj = projObj.GetComponent<Projectile>();
            if (proj == null) proj = projObj.AddComponent<Projectile>();
            proj.Initialize(shootDir, ProjectileSpeed * SpeedMultiplier, ScaledDamage(40f), 5f, DamageType.Holy);

            var damageDealer = projObj.GetComponent<ProjectileDamage>();
            if (damageDealer == null) damageDealer = projObj.AddComponent<ProjectileDamage>();
            damageDealer.SourceWeaponName = "Holy Wand";
            damageDealer.DamageType = DamageType.Holy;
        }
    }
}
