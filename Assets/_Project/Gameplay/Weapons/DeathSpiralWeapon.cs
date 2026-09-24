using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class DeathSpiralWeapon : AutoAttackWeapon
    {
        protected override float BaseCritChance => 0.1f; // VS: this weapon can crit (provisional)
        [Header("Death Spiral Settings (Evolved Axe)")]
        public GameObject ScythePrefab; 
        public float ExpansionSpeed = 8f; 
        private int _scytheCount = 9; // Shoots 9 scythes in all directions

        public override void LevelUp() {}

        // VS: Death Spiral scythes pass through every enemy in their path.
        private const int UnlimitedPierce = 9999;

        protected override void Attack()
        {
            if (ScythePrefab == null) return;

            int scytheCount = _scytheCount + ExtraAmount;
            Vector3 aim = AimDirection(Vector3.forward);
            float angleStep = 360f / scytheCount;

            for (int i = 0; i < scytheCount; i++)
            {
                float currentAngle = i * angleStep;
                // Ring on the ground plane (a Vector2 here fired scythes vertically), first scythe at the nearest enemy.
                Vector3 shootDir = Quaternion.Euler(0f, currentAngle, 0f) * aim;
                
                GameObject scythe = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(ScythePrefab, transform.position, Quaternion.identity)
                    : Instantiate(ScythePrefab, transform.position, Quaternion.identity);
                if (scythe == null) continue;
                ApplyArea(scythe, ScythePrefab);
                
                var proj = scythe.GetComponent<Projectile>();
                if (proj == null) proj = scythe.AddComponent<Projectile>();
                // Pierces everything
                proj.Initialize(shootDir, ExpansionSpeed * SpeedMultiplier, RollDamage(60f), 6f, pierce: UnlimitedPierce);
                
                var damageDealer = scythe.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = scythe.AddComponent<ProjectileDamage>();
                damageDealer.SourceWeaponName = "Death Spiral";
            }
        }
    }
}
