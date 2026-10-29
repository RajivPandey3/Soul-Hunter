using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Axe weapon umeed se uthta hai (Parabola arc) aur wapas neeche girti hai.
    /// Ye script simply ek Axe Projectile banati hai aur usay upar ki taraf dhakka (force) deti hai.
    /// Level table (1-8): L2/L5 ek aur axe, L3/L6/L8 damage, L4/L7 pierce.
    /// VS wiki (Axe): 20 damage, 4 s, 1 axe, pierce 3; L2/L5 +1 axe; L3/L6/L8 +20 damage; L4/L7 pierce +2.
    /// </summary>
    public class AxeWeapon : AutoAttackWeapon
    {
        public override bool UsesPierce => true;
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Slash;
        // VS wiki Critical_Hit: the Axe crits only with Slash (30%, 2x before Slash doubles it).
        protected override float SlashCritChance => 0.3f;
        [Tooltip("Axe ki tasveer/mesh jisme Rigidbody laga ho")]
        [SerializeField] private GameObject _axePrefab;

        [Tooltip("Kitni der baad agla axe fainka jayega")]
        [SerializeField] private float _cooldown = 4f;

        [Tooltip("Ground speed of a thrown axe (before the projectile Speed stat). Provisional.")]
        [SerializeField] private float _groundSpeed = 12f;

        [SerializeField] private int _damage = 20;

        public override int MaxLevel => 8;
        /// <summary>Axes per attack from levels (before the Amount stat).</summary>
        public int LevelAxeCount { get; private set; } = 1;
        /// <summary>Area bonus from levels (multiplies the Area stat). VS Axe levels add no Area, so it stays 1.</summary>
        public float LevelAreaMultiplier { get; private set; } = 1f;
        /// <summary>Enemies one axe can hit before it is used up (VS: 3, +2 at L4 and L7).</summary>
        public int Pierce { get; private set; } = BasePierce;
        private const int BasePierce = 3;

        protected override void Awake()
        {
            // Learning Comment:
            // Infinite Recursion Safeguard:
            // Agar ye script kisi spawned axe projectile par lag gayi ho, toh turant disable karein.
            // Held by the player or by Shadow Kael (mirrored copy) is fine; anything else is a stray copy.
            if (transform.parent != null && transform.parent.GetComponent<SoulHunter.Gameplay.Player.PlayerController>() == null &&
                transform.parent.GetComponent<SoulHunter.Gameplay.AI.EnemyController>() == null)
            {
                enabled = false;
                return;
            }

            base.Awake();
            // Damage and cooldown live in the base fields so levelling and
            // Shadow Kael's mirroring can scale them.
            DamageAmount = _damage;
            AttackCooldown = _cooldown;
        }

        public override void LevelUp()
        {
            if (CurrentLevel >= MaxLevel) return;
            CurrentLevel++;
            switch (CurrentLevel)
            {
                case 2:
                case 5: LevelAxeCount++; break;
                case 4:
                case 7: Pierce += 2; break;
                default: DamageAmount += 20f; break; // 3, 6, 8
            }
        }

        protected override void Attack()
        {
            ThrowAxe();
        }

        private void ThrowAxe()
        {
            if (_axePrefab == null || WeaponPoolManager.Instance == null) return;

            // Learning Comment:
            // Self-Reference Guard:
            if (_axePrefab == gameObject || _axePrefab.GetComponent<AxeWeapon>() != null)
            {
                Debug.LogError("[AxeWeapon] Infinite loop blocked! _axePrefab cannot be AxeWeapon itself.");
                return;
            }

            // VS (wiki Axe): "subsequent axes from Amount are thrown in quick succession ... in progressively wider arcs";
            // interval 0.2 s. Each later axe goes a little further (Soul Hunter's arc along the ground).
            FireInSuccession(LevelAxeCount + ExtraAmount, VsInterval, _throwAxeShot ??= ThrowAxeShot);
        }

        private SuccessionShot _throwAxeShot;

        private void ThrowAxeShot(int i, int count, Vector3 arg)
        {
            ThrowSingleAxe(1f + i * 0.35f);
        }

        public const float VsInterval = 0.2f;

        private void ThrowSingleAxe(float forwardScale)
        {
            // Axe banao O(1) performance ke sath
            GameObject axeObj = WeaponPoolManager.Instance.GetFromPool(_axePrefab, transform.position, Quaternion.identity);
            if (axeObj == null) return;
            axeObj.transform.localScale = _axePrefab.transform.localScale * (AreaMultiplier * LevelAreaMultiplier);

            int actualDamage = Mathf.RoundToInt(RollDamage(DamageAmount));

            var projectileDamage = axeObj.GetComponent<ProjectileDamage>();
            if (projectileDamage == null)
            {
                projectileDamage = axeObj.AddComponent<ProjectileDamage>();
            }
            projectileDamage.DamageAmount = actualDamage;
            AimAtHostiles(axeObj);

            // VS pierce: the axe is used up after hitting Pierce enemies.
            var pierce = axeObj.GetComponent<AxePierce>();
            if (pierce == null) pierce = axeObj.AddComponent<AxePierce>();
            // Iron Blue Will (VII): up to 3 bounces off enemies on top of the pierce.
            // The axe's 5 s life is kept on the axe itself, per throw (CMB-06): a coroutine on the weapon switched a recycled
            // axe off mid-flight when its earlier throw's 5 s ran out, and stopped when the Axe evolved or was switched off,
            // so the axes then in flight never went back to the pool.
            pierce.Arm(Pierce + ExtraPierce, projectileDamage, ArcanaBounces, AxeLifetimeSeconds);

            // Rigidbody ko dhakka do
            Rigidbody rb = axeObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Reset velocity kyunki ye pool se wapas aayi hai
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Owner decision: throw toward the nearest enemy (facing side when none), still in an upward arc.
                // The player no longer mirrors itself (localScale.x stays 1), so fall back to where the hero faces.
                Vector3 aim = AimDirection(HolderFacing);
                // Top-down adaptation: VS's axe arcs up the *screen*. Built as a 3D height arc it flew
                // ~7 units over nearby enemies and landed ~15 units away. Fly along the ground instead.
                rb.useGravity = false;
                Vector3 throwDirection = aim * (_groundSpeed * forwardScale * SpeedMultiplier);

                rb.AddForce(throwDirection, ForceMode.VelocityChange);
                rb.AddTorque(Vector3.Cross(aim, Vector3.up) * 10f, ForceMode.VelocityChange); // tumble end over end along the throw
            }

        }

        /// <summary>A thrown axe goes back to the pool this long after its throw (game time; Soul Hunter's number).</summary>
        public const float AxeLifetimeSeconds = 5f;
    }

    /// <summary>
    /// Counts the different hostiles a thrown axe touches and returns it to the pool once it has hit
    /// its pierce count. The axe is hidden after the frame's physics callbacks, so the last hit still
    /// deals its damage whichever component Unity calls first.
    /// </summary>
    public class AxePierce : MonoBehaviour
    {
        private readonly System.Collections.Generic.HashSet<int> _hit = new();
        private ProjectileDamage _damage;
        private int _pierce = int.MaxValue;
        private int _bouncesLeft;
        private bool _usedUp;
        private float _expiresAt = float.PositiveInfinity;

        public int HitCount => _hit.Count;
        /// <summary>Hits this axe can make before it is used up: its pierce plus its Arcana bounces (tests).</summary>
        public int HitLimit => _pierce;
        /// <summary>Enemy bounces left (VS Iron Blue Will; tests).</summary>
        public int BouncesLeft => _bouncesLeft;

        /// <param name="bounces">VS Iron Blue Will (VII): extra hits, each turning the axe away from the enemy it hit.</param>
        /// <param name="lifetime">Game seconds from this throw until the axe returns to the pool (0 or less = no limit).</param>
        public void Arm(int pierce, ProjectileDamage damage, int bounces = 0, float lifetime = 0f)
        {
            _expiresAt = lifetime > 0f ? Time.time + lifetime : float.PositiveInfinity;
            _bouncesLeft = Mathf.Max(0, bounces);
            _pierce = Mathf.Max(1, pierce) + _bouncesLeft;
            _damage = damage;
            _hit.Clear();
            _usedUp = false;
            // The damage itself asks this axe first (bug hunt 2026-10-29): before, the count and the damage were separate
            // trigger callbacks, so enemies entered in the step the pierce ran out were still hurt, and an enemy that left
            // and came back was hurt again.
            if (damage != null) damage.HitGate = _gate ??= Allow;
        }

        private System.Func<Component, bool> _gate;

        private void OnDisable() { _hit.Clear(); _usedUp = false; _expiresAt = float.PositiveInfinity; }

        /// <summary>True when the axe may hurt <paramref name="body"/>: once per enemy, and not after its pierce is used up.</summary>
        private bool Allow(Component body)
        {
            if (_usedUp || body == null) return false;
            if (!_hit.Add(body.GetInstanceID())) return false;
            if (_hit.Count >= _pierce) _usedUp = true;
            else if (_bouncesLeft > 0) Bounce(body.transform.position);
            return true;
        }

        /// <summary>Reflects the axe's flight off the enemy it hit, as Projectile bounces do.</summary>
        private void Bounce(Vector3 enemyPosition)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null) return;
            Vector3 velocity = rb.linearVelocity;
            Vector3 normal = enemyPosition - transform.position;
            normal.y = 0f;
            if (velocity.sqrMagnitude < 0.0001f || normal.sqrMagnitude < 0.0001f) return;
            Vector3 reflected = Vector3.Reflect(velocity, normal.normalized);
            reflected.y = 0f;
            if (reflected.sqrMagnitude < 0.0001f) reflected = -velocity;
            rb.linearVelocity = reflected;
            _bouncesLeft--;
        }

        private void LateUpdate()
        {
            if (_usedUp || Time.time >= _expiresAt) gameObject.SetActive(false);
        }
    }
}
