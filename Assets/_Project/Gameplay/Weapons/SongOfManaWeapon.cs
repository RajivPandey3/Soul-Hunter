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
        public override float LimitBreakBaseDuration => ShotLifetime;
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;
        public GameObject VerticalBeamPrefab;
        [Tooltip("Shot speed before the projectile Speed stat. Provisional.")]
        [SerializeField] private float _shotSpeed = 18f;
        [Tooltip("Enemies one shot passes through. Provisional.")]
        [SerializeField, Min(1)] private int _shotPierce = 5;

        // VS wiki (Song of Mana): 10 damage, 2.0 s cooldown, AoE, 0.5 s duration. Level-ups: L2 Area +100%;
        // L3 Duration +0.5, Cooldown +0.8; L4 Area +25%, +10 damage; L5 Duration +0.5, Cooldown +0.8;
        // L6 Area +25%, +10 damage; L7 Duration +0.5, Cooldown +0.8; L8 Area +25%, +10 damage.
        // Soul Hunter's shot flies, so its 1 s lifetime (our range tuning) stays the base and VS Duration steps add to it.
        private const float VsBaseDamage = 10f;
        private const float VsCooldown = 2.0f;
        private const float VsCooldownStep = 0.8f; // VS: cooldown goes UP at L3, L5, L7
        private const float VsDurationStep = 0.5f;
        private const float BaseShotLifetime = 1f; // Soul Hunter range tuning (kept)

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, MaxLevel);
        private int OddSteps => (Level >= 3 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 7 ? 1 : 0);
        private int EvenSteps => (Level >= 4 ? 1 : 0) + (Level >= 6 ? 1 : 0) + (Level >= 8 ? 1 : 0);
        public float BaseDamage => VsBaseDamage + 10f * EvenSteps;
        public override float ArcanaZoneBaseDamage => BaseDamage;
        public float LevelCooldown => VsCooldown + VsCooldownStep * OddSteps;
        public float ShotLifetime => BaseShotLifetime + VsDurationStep * OddSteps;
        /// <summary>Area from level-ups, on top of our shot size: 1.0, 2.0 (L2), 2.25, 2.5, 2.75 (L8).</summary>
        public float LevelAreaMultiplier => 1f + (Level >= 2 ? 1f : 0f) + 0.25f * EvenSteps;

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
            AttackCooldown = LevelCooldown;
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
            shot.transform.localScale = VerticalBeamPrefab.transform.localScale * (AreaMultiplier * LevelAreaMultiplier);

            var projectile = shot.GetComponent<Projectile>();
            if (projectile == null) projectile = shot.AddComponent<Projectile>();
            // Duration stretches how far the shot travels before it fades.
            projectile.Initialize(direction, _shotSpeed * SpeedMultiplier, ScaledDamage(BaseDamage),
                ShotLifetime * DurationMultiplier, DamageType.Magic, "Song Of Mana", _shotPierce);
            projectile.AddBounces(ExtraBounces);
            AimAtHostiles(shot);
        }
    }
}
