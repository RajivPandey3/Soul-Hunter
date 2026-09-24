using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Song of Mana (owner decision 2026-09-24): har attack sabse nazdeek dushman ki taraf ek tez,
    /// piercing magic shot chalata hai. Pehle ye ek chhoti si goli player ke upar hi spawn hoti thi aur
    /// player ke saath chalti thi, isliye sirf player ke bilkul upar khade dushman ko lagti thi.
    /// Magic damage Level 7 ke spectral ghosts ko bhi lagta hai.
    /// </summary>
    public class SongOfManaWeapon : AutoAttackWeapon
    {
        public GameObject VerticalBeamPrefab;
        private float _beamDuration = 1f;

        [Tooltip("Shot speed before the projectile Speed stat. Provisional.")]
        [SerializeField] private float _shotSpeed = 18f;
        [Tooltip("Enemies one shot passes through. Provisional.")]
        [SerializeField, Min(1)] private int _shotPierce = 5;

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 10f;
            if (CurrentLevel == 4 || CurrentLevel == 8) _beamDuration += 0.5f;
            if (CurrentLevel % 2 == 0) AttackCooldown -= 0.2f;
        }

        protected override void Attack()
        {
            if (VerticalBeamPrefab == null) return;

            // Fire at the nearest enemy; with nothing to aim at, hold fire.
            Transform target = FindNearestEnemy();
            if (target == null) return;
            Vector3 direction = FlatDirectionTo(target);

            GameObject shot = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(VerticalBeamPrefab, transform.position, Quaternion.identity)
                : Instantiate(VerticalBeamPrefab, transform.position, Quaternion.identity);
            if (shot == null) return;
            ApplyArea(shot, VerticalBeamPrefab);

            var projectile = shot.GetComponent<Projectile>();
            if (projectile == null) projectile = shot.AddComponent<Projectile>();
            // Duration stretches how far the shot travels before it fades.
            projectile.Initialize(direction, _shotSpeed * SpeedMultiplier, ScaledDamage(DamageAmount),
                _beamDuration * DurationMultiplier, DamageType.Magic, "Song Of Mana", _shotPierce);
        }
    }
}
