using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    public class ThousandEdgeWeapon : AutoAttackWeapon
    {
        public override float Knockback => 0.5f; // VS wiki Thousand Edge: knockback 0.5
        // VS wiki Thousand_Edge: it crits only with Slash (30%, 3x before Slash doubles it to 6x). The Critical_Hit
        // page leaves it out of its lists; the weapon's own page is followed.
        protected override float SlashCritChance => 0.3f;
        protected override float SlashCritMultiplier => 3f;
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

            // Amount adds knives per volley, thrown one after another (VS interval 0.05 s): all in one frame from one spot
            // they looked like a single knife, so the Duplicator seemed to do nothing (2026-10-04).
            FireInSuccession(KnivesPerAttack + ExtraAmount, VsInterval, _knifeShot ??= KnifeShot, moveDir);
        }

        private SuccessionShot _knifeShot;

        private void KnifeShot(int i, int count, Vector3 arg)
        {
            Vector3 spreadDir = Quaternion.Euler(0, Random.Range(-5f, 5f), 0) * arg;
            Vector3 spawnPos = transform.position + (Vector3)spreadDir * 0.5f;

            GameObject knife = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(KnifePrefab, spawnPos, Quaternion.identity)
                : Instantiate(KnifePrefab, spawnPos, Quaternion.identity);
            if (knife == null) return;
            ApplyArea(knife, KnifePrefab);
            knife.transform.forward = spreadDir;

            var proj = knife.GetComponent<Projectile>();
            if (proj == null) proj = knife.AddComponent<Projectile>();
            proj.Initialize(spreadDir, KnifeSpeed * SpeedMultiplier, RollDamage(BaseDamage), 3f, pierce: Pierce);
            proj.AddBounces(ExtraBounces + ArcanaBounces); // + Iron Blue Will (VII)

            var damageDealer = knife.GetComponent<ProjectileDamage>();
            if (damageDealer == null) damageDealer = knife.AddComponent<ProjectileDamage>();
            damageDealer.SourceWeaponName = "Blade Storm";
            AimAtHostiles(knife);
        }

        /// <summary>vampire.survivors.wiki/w/Thousand_Edge: "interval = 0.05 seconds" between the knives of one volley.</summary>
        public const float VsInterval = 0.05f;
    }
}
