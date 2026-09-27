using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    public class ThousandEdgeWeapon : AutoAttackWeapon
    {
        protected override float BaseCritChance => 0.1f; // VS: this weapon can crit (provisional)
        public GameObject KnifePrefab;
        public float KnifeSpeed = 25f; // Soul Hunter speed tuning (kept)

        // VS wiki (Thousand Edge, Knife evolution): 16.5 damage, 0.35 s cooldown, 6 knives, pierce 3. It does not level.
        private const float VsDamage = 16.5f;
        private const float VsCooldown = 0.35f;
        private const int VsAmount = 6;
        private const int VsPierce = 3;
        public float BaseDamage => VsDamage;
        public int KnivesPerAttack => VsAmount;
        public int Pierce => VsPierce;

        private PlayerController _player;

        protected override void Awake()
        {
            base.Awake();
            _player = GetComponentInParent<PlayerController>();
            DamageAmount = VsDamage;
            AttackCooldown = VsCooldown; // was 0.05 s with one 35-damage knife per volley
        }

        public override void LevelUp() {}

        protected override void Attack()
        {
            // Shadow Kael's mirrored copy has no player body: it throws toward its target (the player).
            if (KnifePrefab == null) return;

            Vector3 moveDir = _player != null ? _player.Rigidbody.linearVelocity : Vector3.zero;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude < 0.001f)
                moveDir = HolderFacing;
            else moveDir.Normalize();
            moveDir = AimDirection(moveDir); // Owner decision: aim at the nearest enemy; movement direction when none

            // Amount adds knives per volley.
            for (int i = 0; i < KnivesPerAttack + ExtraAmount; i++)
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
                proj.Initialize(spreadDir, KnifeSpeed * SpeedMultiplier, RollDamage(BaseDamage), 3f, pierce: Pierce);
                proj.AddBounces(ExtraBounces);

                var damageDealer = knife.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = knife.AddComponent<ProjectileDamage>();
                damageDealer.SourceWeaponName = "Thousand Edge";
                AimAtHostiles(knife);
            }
        }
    }
}
