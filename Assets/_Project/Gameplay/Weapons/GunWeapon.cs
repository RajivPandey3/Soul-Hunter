using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class GunWeapon : AutoAttackWeapon
    {
        public override bool UsesPierce => true;
        public override float Knockback => 0.1f; // VS wiki Phiera Der Tuphello: knockback 0.1
        public GameObject BulletPrefab; 
        public bool ShootCorners = true; // Phiera shoots corners
        /// <summary>
        /// Turns the whole 4-way pattern by this many degrees. 0 for the Gun; a Gemini (I) counterpart uses
        /// <see cref="GeminiArmOffsetDegrees"/> (wiki Gemini_(I): Phiera's counterpart "fires at a slightly different angle").
        /// </summary>
        [System.NonSerialized] public float ArmOffsetDegrees;
        /// <summary>The wiki gives no number for "slightly different": 15 degrees is Soul Hunter's choice, far enough apart to
        /// read as a second gun while both still sweep the same lanes.</summary>
        public const float GeminiArmOffsetDegrees = 15f;
        
        // VS wiki (Gun = Phiera Der Tuphello): 5 damage, 1.4 s cooldown, 1 bullet, pierce 1. Level-ups: L2 +1 bullet;
        // L3 +2 pierce; L4 +1 bullet; L5 +5 damage; L6 +2 pierce; L7 +5 damage, Speed +50%; L8 +2 pierce.
        // Soul Hunter keeps its 4-way pattern, so Amount counts bullets per arm.
        private const float VsBaseDamage = 5f;
        private const float VsCooldown = 1.4f;
        private const float BulletSpeed = 20f; // Soul Hunter speed tuning (kept)
        private const float BulletLifetime = 2f; // Soul Hunter range tuning (kept)

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, MaxLevel);
        public float BaseDamage => VsBaseDamage + (Level >= 5 ? 5f : 0f) + (Level >= 7 ? 5f : 0f);
        public int BulletsPerArm => 1 + (Level >= 2 ? 1 : 0) + (Level >= 4 ? 1 : 0);
        public int Pierce => 1 + 2 * ((Level >= 3 ? 1 : 0) + (Level >= 6 ? 1 : 0) + (Level >= 8 ? 1 : 0)) + ExtraPierce;
        public float LevelSpeedMultiplier => Level >= 7 ? 1.5f : 1f;

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

        public const float VsInterval = 0.2f;

        protected override void Attack()
        {
            if (BulletPrefab == null) return;

            // Guns shoot in 4 diagonal directions
            // Owner decision: aim at the nearest enemy; the other three arms keep the 4-way pattern.
            // Built on the ground plane: Vector2 directions became (x, y, 0) and fired vertically.
            Vector3 aim = Quaternion.Euler(0f, ArmOffsetDegrees, 0f) * AimDirection(new Vector3(1f, 0f, 1f));
            Vector3[] directions = {
                aim,
                Quaternion.Euler(0f, 90f, 0f) * aim,
                Quaternion.Euler(0f, 180f, 0f) * aim,
                Quaternion.Euler(0f, 270f, 0f) * aim
            };

            // VS (wiki Phiera Der Tuphello): "Amount affects all four bullet streams"; interval 0.2 s, so each point of
            // Amount is one more volley of four, 0.2 s after the last (they used to leave in one frame, on top of each other).
            FireInSuccession(BulletsPerArm + ExtraAmount, VsInterval, volley =>
            {
                foreach (var dir in directions)
                {
                    // Slight spread
                    Vector3 spreadDir = Quaternion.Euler(0f, Random.Range(-5f, 5f), 0f) * dir;
                    GameObject bullet = WeaponPoolManager.Instance != null
                        ? WeaponPoolManager.Instance.GetFromPool(BulletPrefab, transform.position, Quaternion.identity)
                        : Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                    if (bullet == null) continue;
                    ApplyArea(bullet, BulletPrefab);
                    
                    var proj = bullet.GetComponent<Projectile>();
                    if (proj == null) proj = bullet.AddComponent<Projectile>();
                    proj.Initialize(spreadDir, BulletSpeed * LevelSpeedMultiplier * SpeedMultiplier, ScaledDamage(BaseDamage), BulletLifetime, pierce: Pierce);
                    proj.AddBounces(ExtraBounces + ArcanaBounces); // + Iron Blue Will (VII)
                    
                    var damageDealer = bullet.GetComponent<ProjectileDamage>();
                    if (damageDealer == null) damageDealer = bullet.AddComponent<ProjectileDamage>();
                    damageDealer.SourceWeaponName = "Gun";
                    AimAtHostiles(bullet);
                }
            });
        }
    }
}
