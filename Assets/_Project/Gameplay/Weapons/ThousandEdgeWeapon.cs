using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    public class ThousandEdgeWeapon : AutoAttackWeapon
    {
        protected override float BaseCritChance => 0.1f; // VS: this weapon can crit (provisional)
        public GameObject KnifePrefab;
        public float KnifeSpeed = 25f;

        private PlayerController _player;

        protected override void Awake()
        {
            base.Awake();
            _player = GetComponentInParent<PlayerController>();
        }

        private void Start()
        {
            AttackCooldown = 0.05f; // Machine-gun speed (NO cooldown basically)
        }

        public override void LevelUp() {}

        protected override void Attack()
        {
            if (KnifePrefab == null || _player == null) return;

            Vector3 moveDir = _player.Rigidbody.linearVelocity;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude < 0.001f)
                moveDir = _player.transform.localScale.x < 0f ? Vector3.left : Vector3.right;
            else moveDir.Normalize();
            moveDir = AimDirection(moveDir); // Owner decision: aim at the nearest enemy; movement direction when none

            // Amount adds knives per volley.
            for (int i = 0; i < 1 + ExtraAmount; i++)
            {
                Vector3 spreadDir = Quaternion.Euler(0, Random.Range(-5f, 5f), 0) * moveDir;
                Vector3 spawnPos = transform.position + (Vector3)spreadDir * 0.5f;

                GameObject knife = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(KnifePrefab, spawnPos, Quaternion.identity)
                    : Instantiate(KnifePrefab, spawnPos, Quaternion.identity);
                if (knife == null) continue;
                ApplyArea(knife, KnifePrefab);
                knife.transform.forward = spreadDir;

                var proj = knife.GetComponent<Projectile>();
                if (proj == null) proj = knife.AddComponent<Projectile>();
                proj.Initialize(spreadDir, KnifeSpeed * SpeedMultiplier, RollDamage(35f), 3f);

                var damageDealer = knife.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = knife.AddComponent<ProjectileDamage>();
                damageDealer.SourceWeaponName = "Thousand Edge";
            }
        }
    }
}
