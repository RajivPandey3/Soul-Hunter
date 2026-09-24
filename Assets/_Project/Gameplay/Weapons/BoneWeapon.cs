using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class BoneWeapon : AutoAttackWeapon
    {
        public GameObject BonePrefab;
        public float ThrowSpeed = 10f;
        private int _amount = 1;

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 3f;
            if (CurrentLevel % 3 == 0) _amount++;
        }

        protected override void Attack()
        {
            if (BonePrefab == null) return;

            for (int i = 0; i < _amount + ExtraAmount; i++)
            {
                // Owner decision: aim at the nearest enemy; random when none. Extra bones fan out.
                Vector3 aim = Quaternion.Euler(0f, (i - (_amount + ExtraAmount - 1) * 0.5f) * 15f, 0f) * AimDirection(RandomFlatDirection());
                GameObject bone = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(BonePrefab, transform.position, Quaternion.identity)
                    : Instantiate(BonePrefab, transform.position, Quaternion.identity);
                if (bone == null) continue;
                ApplyArea(bone, BonePrefab);

                var rb = bone.GetComponent<Rigidbody>();
                if (rb == null) rb = bone.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY;
                // Bone bounces heavily, relies on physics material
                rb.linearVelocity = aim * (ThrowSpeed * SpeedMultiplier);

                var damageDealer = bone.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = bone.AddComponent<ProjectileDamage>();
                damageDealer.DamageAmount = ScaledDamage(DamageAmount);
                damageDealer.SourceWeaponName = "Bone";

                var lifetime = bone.GetComponent<PooledLifetime>();
                if (lifetime != null) lifetime.Arm(6f);
                else Destroy(bone, 6f); // compatibility when no pool exists
            }
        }
    }
}
