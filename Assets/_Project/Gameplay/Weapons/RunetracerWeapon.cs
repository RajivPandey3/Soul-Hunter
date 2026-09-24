using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class RunetracerWeapon : AutoAttackWeapon
    {
        public GameObject RunePrefab;
        public float Speed = 8f;

        private int _amount = 1;

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 5f;
            if (CurrentLevel == 3 || CurrentLevel == 6) _amount++;
        }

        protected override void Attack()
        {
            if (RunePrefab == null) return;

            for (int i = 0; i < _amount + ExtraAmount; i++)
            {
                // Owner decision: aim at the nearest enemy; random when none. Extra runes fan out.
                Vector3 aim = Quaternion.Euler(0f, (i - (_amount + ExtraAmount - 1) * 0.5f) * 15f, 0f) * AimDirection(RandomFlatDirection());
                GameObject rune = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(RunePrefab, transform.position, Quaternion.identity)
                    : Instantiate(RunePrefab, transform.position, Quaternion.identity);
                if (rune == null) continue;
                ApplyArea(rune, RunePrefab);

                var rb = rune.GetComponent<Rigidbody>();
                if (rb == null) rb = rune.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY;
                // Bouncing logic usually handled by 3D physics material on the prefab
                rb.linearVelocity = aim * (Speed * SpeedMultiplier);

                var damageDealer = rune.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = rune.AddComponent<ProjectileDamage>();
                damageDealer.DamageAmount = ScaledDamage(DamageAmount);
                damageDealer.SourceWeaponName = "Runetracer";

                var lifetime = rune.GetComponent<PooledLifetime>();
                if (lifetime != null) lifetime.Arm(5f * DurationMultiplier);
                else Destroy(rune, 5f * DurationMultiplier); // compatibility when no pool exists
            }
        }
    }
}
