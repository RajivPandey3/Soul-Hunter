using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class BoneWeapon : AutoAttackWeapon
    {
        public GameObject BonePrefab;
        public float ThrowSpeed = 10f; // Soul Hunter speed tuning (kept); VS Speed level-ups scale it

        // VS wiki (Bone): 5 damage, 3.0 s cooldown, 1 bone, bounces, 2.0 s duration. Level-ups: L2 Area +20%,
        // Duration +0.2; L3 +1 bone, +20 damage; L4 Area +20%, Speed +50%; L5 +1 bone, +20 damage; L6 Duration +0.2;
        // L7 +20 damage; L8 Duration +0.2, Speed +50%.
        private const float VsBaseDamage = 5f;
        private const float VsCooldown = 3.0f;
        private const float VsDuration = 2.0f;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, MaxLevel);
        public float BaseDamage => VsBaseDamage + 20f * ((Level >= 3 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 7 ? 1 : 0));
        public int BonesPerAttack => 1 + (Level >= 3 ? 1 : 0) + (Level >= 5 ? 1 : 0);
        public float BaseDuration => VsDuration + 0.2f * ((Level >= 2 ? 1 : 0) + (Level >= 6 ? 1 : 0) + (Level >= 8 ? 1 : 0));
        /// <summary>Area from level-ups, on top of our bone size: 1.0, 1.2 (L2), 1.4 (L4).</summary>
        public float LevelAreaMultiplier => 1f + 0.2f * ((Level >= 2 ? 1 : 0) + (Level >= 4 ? 1 : 0));
        public float LevelSpeedMultiplier => 1f + 0.5f * ((Level >= 4 ? 1 : 0) + (Level >= 8 ? 1 : 0));

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

        protected override void Attack()
        {
            if (BonePrefab == null) return;

            for (int i = 0; i < BonesPerAttack + ExtraAmount; i++)
            {
                // Owner decision: aim at the nearest enemy; random when none. Extra bones fan out.
                Vector3 aim = Quaternion.Euler(0f, (i - (BonesPerAttack + ExtraAmount - 1) * 0.5f) * 15f, 0f) * AimDirection(RandomFlatDirection());
                GameObject bone = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(BonePrefab, transform.position, Quaternion.identity)
                    : Instantiate(BonePrefab, transform.position, Quaternion.identity);
                if (bone == null) continue;
                bone.transform.localScale = BonePrefab.transform.localScale * (AreaMultiplier * LevelAreaMultiplier);

                var rb = bone.GetComponent<Rigidbody>();
                if (rb == null) rb = bone.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY;
                // Bone bounces heavily, relies on physics material
                rb.linearVelocity = aim * (ThrowSpeed * LevelSpeedMultiplier * SpeedMultiplier);

                var damageDealer = bone.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = bone.AddComponent<ProjectileDamage>();
                damageDealer.DamageAmount = ScaledDamage(BaseDamage);
                damageDealer.SourceWeaponName = "Bone";
                AimAtHostiles(bone);

                var lifetime = bone.GetComponent<PooledLifetime>();
                if (lifetime != null) lifetime.Arm(BaseDuration * DurationMultiplier);
                else Destroy(bone, BaseDuration * DurationMultiplier); // compatibility when no pool exists
            }
        }
    }
}
