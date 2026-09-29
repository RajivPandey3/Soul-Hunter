using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    public class ClockLancetWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;
        public GameObject FreezeBeamPrefab;
        private int _currentDirectionIndex = 0; // Rotates 12 positions like a clock
        private readonly RaycastHit[] _hits = new RaycastHit[128];

        // VS wiki (Clock Lancet): no damage, 2.0 s cooldown, 2.0 s freeze; max level 7. Levels: L2 Dur +1; L3 CD -0.5;
        // L4 Dur +1; L5 Dur +1; L6 CD -0.5; L7 Dur +1.
        public const float VsCooldown = 2f;
        public const float VsFreezeDuration = 2f;
        /// <summary>
        /// Freeze chance compared with an enemy's freeze resistance (vampire.survivors.wiki/w/Enemies, "Resistances").
        /// The Clock Lancet page (vampire.survivors.wiki/w/Clock_Lancet, raw, read 2026-09-28) gives no number ("Chance to
        /// freeze enemies in time"), so 1.0 is assumed: every hit freezes an ordinary enemy, and VS bosses with
        /// res-freeze 1.1 resist until Garlic lowers it.
        /// </summary>
        public const float VsFreezeChance = 1f;

        public override int MaxLevel => 7;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 7);
        public float FreezeDuration => VsFreezeDuration + (Level >= 2 ? 1f : 0f) + (Level >= 4 ? 1f : 0f) + (Level >= 5 ? 1f : 0f) + (Level >= 7 ? 1f : 0f);
        public float CooldownForLevel => VsCooldown - (Level >= 3 ? 0.5f : 0f) - (Level >= 6 ? 0.5f : 0f);

        protected override void Awake()
        {
            base.Awake();
            AttackCooldown = CooldownForLevel;
        }

        public override void LevelUp()
        {
            CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1);
            AttackCooldown = CooldownForLevel;
        }

        protected override void Attack()
        {
            // 12 clock positions (0 = up, 1 = up-right, etc.)
            float angle = _currentDirectionIndex * (360f / 12f);
            // Owner decision: aim at the nearest enemy; the clock sweep when none.
            Vector3 direction = AimDirection(Quaternion.Euler(0, angle, 0) * Vector3.forward);

            _currentDirectionIndex = (_currentDirectionIndex + 1) % 12;

            if (FreezeBeamPrefab != null)
            {
                GameObject beam = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(FreezeBeamPrefab, transform.position, Quaternion.LookRotation(direction))
                    : Instantiate(FreezeBeamPrefab, transform.position, Quaternion.LookRotation(direction));
                if (beam != null)
                {
                    ApplyArea(beam, FreezeBeamPrefab);
                    var lifetime = beam.GetComponent<PooledLifetime>();
                    if (lifetime != null) lifetime.Arm(0.5f);
                    else Destroy(beam, 0.5f);
                }
            }

            int count = UnityEngine.Physics.RaycastNonAlloc(transform.position, direction, _hits, 20f * AreaMultiplier);

            // Shadow Kael's mirrored copy never freezes the boss or its horde. Owner decision (2026-09-30): a player the
            // beam crosses moves at half speed for the weapon's freeze time (VS has no player freeze).
            if (GetComponentInParent<EnemyController>() != null)
            {
                for (int i = 0; i < count; i++)
                {
                    var player = _hits[i].collider.GetComponentInParent<SoulHunter.Gameplay.Player.PlayerController>();
                    if (player == null) continue;
                    player.Slow(SoulHunter.Gameplay.Player.PlayerController.ClockLancetSlowFactor, FreezeDuration * DurationMultiplier);
                    break;
                }
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var enemy = _hits[i].collider.GetComponentInParent<EnemyController>();
                if (enemy != null) enemy.TryWeaponFreeze(VsFreezeChance, FreezeDuration * DurationMultiplier);
            }
        }
    }
}
