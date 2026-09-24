using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class GunWeapon : AutoAttackWeapon
    {
        public GameObject BulletPrefab; 
        public bool ShootCorners = true; // Phiera shoots corners
        
        private int _bulletsPerShot = 1; 

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 5f; 
            AttackCooldown -= 0.1f; // Guns shoot fast
            if (CurrentLevel % 3 == 0) _bulletsPerShot++; 
        }

        protected override void Attack()
        {
            if (BulletPrefab == null) return;

            // Guns shoot in 4 diagonal directions
            Vector2[] directions = {
                new Vector2(1, 1).normalized,
                new Vector2(-1, 1).normalized,
                new Vector2(1, -1).normalized,
                new Vector2(-1, -1).normalized
            };

            foreach (var dir in directions)
            {
                for (int i = 0; i < _bulletsPerShot + ExtraAmount; i++)
                {
                    // Slight spread
                    Vector2 spreadDir = Quaternion.Euler(0, 0, Random.Range(-5f, 5f)) * dir;
                    GameObject bullet = WeaponPoolManager.Instance != null
                        ? WeaponPoolManager.Instance.GetFromPool(BulletPrefab, transform.position, Quaternion.identity)
                        : Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                    if (bullet == null) continue;
                    ApplyArea(bullet, BulletPrefab);
                    
                    var proj = bullet.GetComponent<Projectile>();
                    if (proj == null) proj = bullet.AddComponent<Projectile>();
                    proj.Initialize(spreadDir, 20f * SpeedMultiplier, ScaledDamage(DamageAmount), 2f);
                    
                    var damageDealer = bullet.GetComponent<ProjectileDamage>();
                    if (damageDealer == null) damageDealer = bullet.AddComponent<ProjectileDamage>();
                    damageDealer.SourceWeaponName = "Gun";
                }
            }
        }
    }
}
