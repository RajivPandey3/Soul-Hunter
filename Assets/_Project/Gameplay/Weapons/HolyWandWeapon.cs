using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    public class HolyWandWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;
        [Header("Holy Wand Settings (Evolved Magic Wand)")]
        public GameObject ProjectilePrefab; 
        public float ProjectileSpeed = 25f; // serialized on the prefab; the VS speed below replaces it in Awake

        // VS (vampire.survivors.wiki/w/Holy_Wand, raw, fetched 2026-10-19), infobox: "damage = 30", "cooldown = 0.5
        // seconds", "interval = 0.1 seconds", "amount = 4", "speed = 2 (+100%)"; Effects: "Additional projectiles from
        // Amount will be fired in a sequence"; it has "half the Cooldown and double the [speed] of its base form"; Tips:
        // "Even if the sequence lasts longer than the Cooldown, it does not prevent another volley to start shooting."
        // CMB-11: it fired 1 + Amount bolts in one frame from one spot every 0.1 s at a hard-coded 40, and DamageAmount
        // (read by the Arcana zones) stayed at the prefab's 10.
        // Pierce (owner 2026-10-19: "SH = VS"): the infobox says "pierce = 2" and the Tips confirm it ("its damage against
        // multiple enemies is lacking even with the extra pierce"); only the Effects line says "non-piercing", so 2, plus
        // the Limit Break "Pierce +1" cards. "Ignores Duration": the bolt's 5 s flight is not scaled.
        public const float VsDamage = 30f;
        public const float VsCooldown = 0.5f;
        public const float VsInterval = 0.1f;
        public const int VsAmount = 4;
        public const int VsPierce = 2;
        /// <summary>Double the Magic Wand's bolt speed (its 20 units/s), wiki "speed = 2 (+100%)".</summary>
        public const float VsProjectileSpeed = 40f;

        protected override void Awake()
        {
            base.Awake();
            // In the base fields, so Shadow Kael's mirroring and the Arcana zones read the real numbers.
            DamageAmount = VsDamage;
            AttackCooldown = VsCooldown;
            ProjectileSpeed = VsProjectileSpeed;
        }

        public override void LevelUp() {}

        protected override void Attack()
        {
            if (ProjectilePrefab == null) return;
            if (FindNearestEnemy() == null) return;
            // One after another at the VS interval; each bolt flies at the enemy nearest when it leaves (Magic Wand rule).
            FireInSuccession(VsAmount + ExtraAmount, VsInterval, _boltShot ??= BoltShot);
        }

        private SuccessionShot _boltShot;

        private void BoltShot(int i, int count, Vector3 arg)
        {
            // Shared null-safe search (the old loop crashed on destroyed entries in ActiveEnemies).
            Transform closestEnemy = FindNearestEnemy();
            if (closestEnemy != null) FireAt(FlatDirectionTo(closestEnemy)); // on the ground plane
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
            proj.Initialize(shootDir, ProjectileSpeed * SpeedMultiplier, ScaledDamage(DamageAmount), 5f, DamageType.Holy, null, VsPierce + ExtraPierce);
            proj.AddBounces(ExtraBounces + ArcanaBounces); // + Waltz of Pearls (XI)

            var damageDealer = projObj.GetComponent<ProjectileDamage>();
            if (damageDealer == null) damageDealer = projObj.AddComponent<ProjectileDamage>();
            damageDealer.SourceWeaponName = "Holy Wand";
            damageDealer.DamageType = DamageType.Holy;
            AimAtHostiles(projObj);
        }
    }
}
