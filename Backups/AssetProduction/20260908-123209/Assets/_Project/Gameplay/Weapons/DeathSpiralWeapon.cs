using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class DeathSpiralWeapon : AutoAttackWeapon
    {
        [Header("Death Spiral Settings (Evolved Axe)")]
        public GameObject ScythePrefab; 
        public float ExpansionSpeed = 8f; 
        private int _scytheCount = 9; // Shoots 9 scythes in all directions

        public override void LevelUp() {}

        protected override void Attack()
        {
            if (ScythePrefab == null) return;

            float angleStep = 360f / _scytheCount;

            for (int i = 0; i < _scytheCount; i++)
            {
                float currentAngle = i * angleStep;
                Vector2 shootDir = Quaternion.Euler(0, 0, currentAngle) * Vector2.up;
                
                GameObject scythe = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(ScythePrefab, transform.position, Quaternion.identity)
                    : Instantiate(ScythePrefab, transform.position, Quaternion.identity);
                if (scythe == null) continue;
                
                var proj = scythe.GetComponent<Projectile>();
                if (proj == null) proj = scythe.AddComponent<Projectile>();
                // Pierces everything
                proj.Initialize(shootDir, ExpansionSpeed, 60f, 6f);
                
                var damageDealer = scythe.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = scythe.AddComponent<ProjectileDamage>();
                damageDealer.SourceWeaponName = "Death Spiral";
            }
        }
    }
}
