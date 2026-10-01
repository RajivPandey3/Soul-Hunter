using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class RunetracerWeapon : AutoAttackWeapon
    {
        public override float LimitBreakBaseDuration => BaseDuration;
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;
        public GameObject RunePrefab;
        public float Speed = 8f; // Soul Hunter speed tuning (kept); VS Speed level-ups scale it

        // VS wiki (Runetracer): 10 damage, 3.0 s cooldown, 1 rune, AoE (hits every enemy it touches), 2.25 s duration.
        // Level-ups: L2 +5 damage, Speed +20%; L3 Duration +0.3, +5 damage; L4 +1 rune; L5 +5 damage, Speed +20%;
        // L6 Duration +0.3, +5 damage; L7 +1 rune; L8 Duration +0.5.
        private const float VsBaseDamage = 10f;
        private const float VsCooldown = 3.0f;
        private const float VsDuration = 2.25f;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, MaxLevel);
        public float BaseDamage => VsBaseDamage + 5f * ((Level >= 2 ? 1 : 0) + (Level >= 3 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 6 ? 1 : 0));
        public int RunesPerAttack => 1 + (Level >= 4 ? 1 : 0) + (Level >= 7 ? 1 : 0);
        public float BaseDuration => VsDuration + (Level >= 3 ? 0.3f : 0f) + (Level >= 6 ? 0.3f : 0f) + (Level >= 8 ? 0.5f : 0f);
        public float LevelSpeedMultiplier => 1f + 0.2f * ((Level >= 2 ? 1 : 0) + (Level >= 5 ? 1 : 0));

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
            if (RunePrefab == null) return;

            // VS interval (wiki Runetracer): the Amount of shots leave 0.2s apart.
            FireInSuccession(RunesPerAttack + ExtraAmount, VsInterval, i =>
            {
                // Owner decision: aim at the nearest enemy; random when none. Extra runes fan out.
                Vector3 aim = Quaternion.Euler(0f, (i - (RunesPerAttack + ExtraAmount - 1) * 0.5f) * 15f, 0f) * AimDirection(RandomFlatDirection());
                GameObject rune = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(RunePrefab, transform.position, Quaternion.identity)
                    : Instantiate(RunePrefab, transform.position, Quaternion.identity);
                if (rune == null) return;
                ApplyArea(rune, RunePrefab);

                var rb = rune.GetComponent<Rigidbody>();
                if (rb == null) rb = rune.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY;
                // Bouncing logic usually handled by 3D physics material on the prefab
                rb.linearVelocity = aim * (Speed * LevelSpeedMultiplier * SpeedMultiplier);

                var damageDealer = rune.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = rune.AddComponent<ProjectileDamage>();
                damageDealer.DamageAmount = ScaledDamage(BaseDamage);
                damageDealer.DamageType = DamageType.Magic; // Owner (2026-09-26, Level 7): weapons are typed by theme so ghosts can be fought with more than four weapons.
                damageDealer.SourceWeaponName = "Runetracer";
                AimAtHostiles(rune);

                var lifetime = rune.GetComponent<PooledLifetime>();
                if (lifetime != null) lifetime.Arm(BaseDuration * DurationMultiplier);
                else Destroy(rune, BaseDuration * DurationMultiplier); // compatibility when no pool exists
            });
        }
    }
}
