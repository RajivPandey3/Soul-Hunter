using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    public class KnifeWeapon : AutoAttackWeapon
    {
        protected override float BaseCritChance => 0.1f; // VS: this weapon can crit (provisional)
        [SerializeField] private GameObject _projectilePrefab;
        public float KnifeSpeed = 15f; // Soul Hunter speed tuning (kept)

        // VS wiki (Knife): 6.5 damage, 1.0 s cooldown, 1 knife, pierce 1. Level-ups: L2 +1 knife; L3 +1 knife, +5 damage;
        // L4 +1 knife; L5 +1 pierce; L6 +1 knife; L7 +1 knife, +5 damage; L8 +1 pierce.
        private const float VsBaseDamage = 6.5f;
        private const float VsCooldown = 1.0f;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, MaxLevel);
        public float BaseDamage => VsBaseDamage + (Level >= 3 ? 5f : 0f) + (Level >= 7 ? 5f : 0f);
        public int KnivesPerAttack => 1 + (Level >= 2 ? 1 : 0) + (Level >= 3 ? 1 : 0) + (Level >= 4 ? 1 : 0) + (Level >= 6 ? 1 : 0) + (Level >= 7 ? 1 : 0);
        /// <summary>Enemies each knife passes through (VS: +1 at levels 5 and 8).</summary>
        public int Pierce => 1 + (Level >= 5 ? 1 : 0) + (Level >= 8 ? 1 : 0);
        private PlayerController _player;

        protected override void Awake()
        {
            base.Awake();
            _player = GetComponentInParent<PlayerController>();
            ApplyLevelStats();
        }

        public override void LevelUp()
        {
            CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1);
            ApplyLevelStats();
        }

        /// <summary>Keeps the base fields (the base timer reads AttackCooldown) in step with the level.</summary>
        private void ApplyLevelStats()
        {
            DamageAmount = BaseDamage;
            AttackCooldown = VsCooldown;
        }

        protected override void Attack()
        {
            // Shadow Kael's mirrored copy has no player body: it throws toward its target (the player).
            if (_projectilePrefab == null) return;

            Vector3 moveDir = _player != null ? _player.Rigidbody.linearVelocity : Vector3.zero;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude < 0.001f)
                moveDir = HolderFacing;
            else moveDir.Normalize();
            moveDir = AimDirection(moveDir); // Owner decision: aim at the nearest enemy; movement direction when none

            for (int i = 0; i < KnivesPerAttack + ExtraAmount; i++)
            {
                Vector3 spreadDir = Quaternion.Euler(0, Random.Range(-10f, 10f), 0) * moveDir;
                Vector3 spawnPos = transform.position + (Vector3)spreadDir * 0.5f;

                GameObject knife = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(_projectilePrefab, spawnPos, Quaternion.identity)
                    : Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
                if (knife == null) continue;
                ApplyArea(knife, _projectilePrefab);
                knife.transform.forward = spreadDir;


                var proj = knife.GetComponent<Projectile>();
                if (proj != null) proj.Initialize(spreadDir, KnifeSpeed * SpeedMultiplier, RollDamage(BaseDamage), 2f, pierce: Pierce);
                if (proj != null) proj.AddBounces(ExtraBounces);

                var damageDealer = knife.GetComponent<ProjectileDamage>();
                if (damageDealer != null) damageDealer.SourceWeaponName = "Knife";
                AimAtHostiles(knife);
            }
        }
    }
}
