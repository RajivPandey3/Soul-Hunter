using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    public class ClockLancetWeapon : AutoAttackWeapon
    {
        public GameObject FreezeBeamPrefab;
        private int _currentDirectionIndex = 0; // Rotates 12 positions like a clock
        private float _freezeDuration = 3f;
        private readonly RaycastHit[] _hits = new RaycastHit[128];

        public override void LevelUp()
        {
            CurrentLevel++;
            _freezeDuration += 0.5f;
            if (CurrentLevel % 2 == 0) AttackCooldown -= 0.1f;
        }

        protected override void Attack()
        {
            // 12 clock positions (0 = up, 1 = up-right, etc.)
            float angle = _currentDirectionIndex * (360f / 12f);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            _currentDirectionIndex = (_currentDirectionIndex + 1) % 12;

            if (FreezeBeamPrefab != null)
            {
                GameObject beam = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(FreezeBeamPrefab, transform.position, Quaternion.LookRotation(direction))
                    : Instantiate(FreezeBeamPrefab, transform.position, Quaternion.LookRotation(direction));
                if (beam != null)
                {
                    var lifetime = beam.GetComponent<PooledLifetime>();
                    if (lifetime != null) lifetime.Arm(0.5f);
                    else Destroy(beam, 0.5f);
                }
            }

            int count = UnityEngine.Physics.RaycastNonAlloc(transform.position, direction, _hits, 20f);
            for (int i = 0; i < count; i++)
            {
                var enemy = _hits[i].collider.GetComponentInParent<EnemyController>();
                if (enemy != null) enemy.ChangeState(new EnemyFrozenState(enemy, _freezeDuration));
            }
        }
    }
}
