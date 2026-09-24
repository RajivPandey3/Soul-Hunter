using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    public class KnifeWeapon : AutoAttackWeapon
    {
        protected override float BaseCritChance => 0.1f; // VS: this weapon can crit (provisional)
        [SerializeField] private GameObject _projectilePrefab;
        public float KnifeSpeed = 15f;

        private int _knivesPerAttack = 1;
        /// <summary>Enemies each knife passes through (VS: +1 at levels 5 and 8).</summary>
        public int Pierce { get; private set; } = 1;
        private PlayerController _player;

        protected override void Awake()
        {
            base.Awake();
            _player = GetComponentInParent<PlayerController>();
        }

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 5f;

            if (CurrentLevel % 2 == 0) _knivesPerAttack++;
            if (CurrentLevel == 5) AttackCooldown -= 0.2f;
            if (CurrentLevel == 5 || CurrentLevel == 8) Pierce++;
        }

        protected override void Attack()
        {
            if (_player == null || _projectilePrefab == null) return;

            Vector3 moveDir = _player.Rigidbody.linearVelocity;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude < 0.001f)
                moveDir = _player.transform.localScale.x < 0f ? Vector3.left : Vector3.right;
            else moveDir.Normalize();
            moveDir = AimDirection(moveDir); // Owner decision: aim at the nearest enemy; movement direction when none

            for (int i = 0; i < _knivesPerAttack + ExtraAmount; i++)
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
                if (proj != null) proj.Initialize(spreadDir, KnifeSpeed * SpeedMultiplier, RollDamage(DamageAmount), 2f, pierce: Pierce);

                var damageDealer = knife.GetComponent<ProjectileDamage>();
                if (damageDealer != null) damageDealer.SourceWeaponName = "Knife";
            }
        }
    }
}
