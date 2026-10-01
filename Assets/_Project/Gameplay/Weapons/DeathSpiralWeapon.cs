using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class DeathSpiralWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Slash;
        // VS wiki Death_Spiral: it crits only with Slash (30%, 2x before Slash doubles it to 4x). The Critical_Hit page
        // leaves it out of its lists; the weapon's own page is followed.
        protected override float SlashCritChance => 0.3f;
        [Header("Death Spiral Settings (Evolved Axe)")]
        public GameObject ScythePrefab; 
        public float ExpansionSpeed = 8f; 
        private int _scytheCount = 9; // Shoots 9 scythes in all directions

        public override void LevelUp() {}

        // VS wiki (Death Spiral): 60 damage, 4 s, 9 scythes, pierce effectively unlimited. Evolutions do not level.
        public const float BaseDamage = 60f;
        public const float BaseCooldown = 4f;

        protected override void Awake()
        {
            base.Awake();
            // Damage and cooldown live in the base fields so Shadow Kael's mirroring can scale them.
            DamageAmount = BaseDamage;
            AttackCooldown = BaseCooldown;
        }

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
                
                // The scythe is the SH10 model prefab, which has no collider; Projectile requires one, so AddComponent
                // returned null and every attack threw (found in a 30-minute editor run, 2026-09-28).
                if (scythe.GetComponent<Collider>() == null)
                {
                    var trigger = scythe.AddComponent<SphereCollider>();
                    trigger.isTrigger = true;
                    trigger.radius = 0.6f;
                }
                var proj = scythe.GetComponent<Projectile>();
                if (proj == null) proj = scythe.AddComponent<Projectile>();
                if (proj == null) continue;
                // Pierces everything
                proj.Initialize(shootDir, ExpansionSpeed * SpeedMultiplier, RollDamage(DamageAmount), 6f, pierce: UnlimitedPierce);
                proj.AddBounces(ExtraBounces);
                
                var damageDealer = scythe.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = scythe.AddComponent<ProjectileDamage>();
                damageDealer.SourceWeaponName = "Death Spiral";
                AimAtHostiles(scythe);
            }
        }
    }
}
