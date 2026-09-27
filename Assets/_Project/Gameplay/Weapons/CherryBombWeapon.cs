using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class CherryBombWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Explosion;
        public GameObject BombPrefab; 
        public GameObject ExplosionPrefab; // What spawns when it hits
        
        // VS wiki (Cherry Bomb): 10 damage, 3.0 s cooldown, 1 bomb, 2.0 s duration. Levels: L2 Area +25%, Spd +30%;
        // L3 50% explode chance; L4 +1 bomb; L5 Area +25%, +5; L6 60% explode chance; L7 Area +25%, Spd +30%;
        // L8 Area +25%, +5. Area and Speed bonuses scale our own bomb size and throw speed (7).
        public const float VsBaseDamage = 10f;
        public const float VsCooldown = 3f;
        public const int VsAmount = 1;
        public const float VsDuration = 2f;
        public const float ThrowSpeed = 7f;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public float BaseDamage => DamageAmount + (Level >= 5 ? 5f : 0f) + (Level >= 8 ? 5f : 0f);
        public int BombCount => VsAmount + (Level >= 4 ? 1 : 0);
        public float LevelArea => 1f + 0.25f * ((Level >= 2 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 7 ? 1 : 0) + (Level >= 8 ? 1 : 0));
        public float LevelSpeed => 1f + 0.3f * ((Level >= 2 ? 1 : 0) + (Level >= 7 ? 1 : 0));

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsBaseDamage;
            AttackCooldown = VsCooldown;
        }

        public override void LevelUp() { CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1); }

        protected override void Attack()
        {
            if (BombPrefab == null) return;

            for (int i = 0; i < BombCount + ExtraAmount; i++)
            {
                // Owner decision: aim at the nearest enemy; random when none. On the ground plane (a Vector2 here sent bombs vertically).
                Vector3 randomDir = Quaternion.Euler(0f, (i - (BombCount + ExtraAmount - 1) * 0.5f) * 15f, 0f) * AimDirection(RandomFlatDirection());
                GameObject bomb = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(BombPrefab, transform.position, Quaternion.identity)
                    : Instantiate(BombPrefab, transform.position, Quaternion.identity);
                if (bomb == null) continue;
                bomb.transform.localScale = BombPrefab.transform.localScale * AreaMultiplier * LevelArea;
                
                var proj = bomb.GetComponent<Projectile>();
                if (proj == null) proj = bomb.AddComponent<Projectile>();
                
                proj.Initialize(randomDir, ThrowSpeed * LevelSpeed * SpeedMultiplier, ScaledDamage(BaseDamage), VsDuration * DurationMultiplier);
                proj.AddBounces(ExtraBounces);
                
                // Note: Needs a specialized collision script to spawn ExplosionPrefab on bounce.
                // This is a simplified projectile implementation.
                var damageDealer = bomb.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = bomb.AddComponent<ProjectileDamage>();
                damageDealer.SourceWeaponName = "Cherry Bomb";
                AimAtHostiles(bomb);
            }
        }
    }
}
