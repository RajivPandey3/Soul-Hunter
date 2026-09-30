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
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Slash;
        protected override float BaseCritChance => 0.1f; // VS: this weapon can crit (provisional)
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
            FireInSuccession(LevelAxeCount + ExtraAmount, VsInterval, i => ThrowSingleAxe(1f + i * 0.35f));
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
            pierce.Arm(Pierce, projectileDamage);

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

            // 5 second baad axe wapas pool mein chala jayega (Destroy nahi hoga)
            StartCoroutine(DisableAxeRoutine(axeObj));
        }

        private System.Collections.IEnumerator DisableAxeRoutine(GameObject axe)
        {
            yield return new WaitForSeconds(5f);
            if (axe != null && axe.activeSelf) axe.SetActive(false);
        }
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
        private bool _usedUp;

        public int HitCount => _hit.Count;

        public void Arm(int pierce, ProjectileDamage damage)
        {
            _pierce = Mathf.Max(1, pierce);
            _damage = damage;
            _hit.Clear();
            _usedUp = false;
        }

        private void OnDisable() { _hit.Clear(); _usedUp = false; }

        private void OnTriggerEnter(Collider other) => Count(other);
        private void OnCollisionEnter(Collision collision) => Count(collision.collider);

        private void Count(Collider other)
        {
            if (_usedUp || other == null || _damage == null || !other.CompareTag(_damage.TargetTag)) return;
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;
            _hit.Add(damageable is Component body ? body.GetInstanceID() : other.GetInstanceID());
            if (_hit.Count >= _pierce) _usedUp = true;
        }

        private void LateUpdate()
        {
            if (_usedUp) gameObject.SetActive(false);
        }
    }
}
