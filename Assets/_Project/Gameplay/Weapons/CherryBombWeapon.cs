using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class CherryBombWeapon : AutoAttackWeapon
    {
        public GameObject BombPrefab; 
        public GameObject ExplosionPrefab; // What spawns when it hits
        
        private int _amount = 1; 

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 8f; 
            if (CurrentLevel % 3 == 0) _amount++; 
        }

        protected override void Attack()
        {
            if (BombPrefab == null) return;

            for (int i = 0; i < _amount + ExtraAmount; i++)
            {
                // Owner decision: aim at the nearest enemy; random when none. On the ground plane (a Vector2 here sent bombs vertically).
                Vector3 randomDir = Quaternion.Euler(0f, (i - (_amount + ExtraAmount - 1) * 0.5f) * 15f, 0f) * AimDirection(RandomFlatDirection());
                GameObject bomb = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(BombPrefab, transform.position, Quaternion.identity)
                    : Instantiate(BombPrefab, transform.position, Quaternion.identity);
                if (bomb == null) continue;
                ApplyArea(bomb, BombPrefab);
                
                var proj = bomb.GetComponent<Projectile>();
                if (proj == null) proj = bomb.AddComponent<Projectile>();
                
                proj.Initialize(randomDir, 7f * SpeedMultiplier, ScaledDamage(DamageAmount), 4f);
                
                // Note: Needs a specialized collision script to spawn ExplosionPrefab on bounce.
                // This is a simplified projectile implementation.
                var damageDealer = bomb.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = bomb.AddComponent<ProjectileDamage>();
                damageDealer.SourceWeaponName = "Cherry Bomb";
            }
        }
    }
}
