using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Whip weapon hamesha horizontally attack karta hai (left ya right).
    /// Ye ek rectangular box area mein aaye hue sabhi dushmano ko ek saath damage deta hai.
    /// Isme koi goli (projectile) travel nahi karti, ye instant hit (OverlapBox) karta hai.
    /// Level table (1-8): L2 dusri taraf ek aur whip, L3-L8 damage, L4/L6 area.
    /// </summary>
    public class WhipWeapon : AutoAttackWeapon
    {
        protected override float BaseCritChance => 0.1f; // VS: this weapon can crit (provisional)
        [Header("Soul Hunter Theme: Shadow Scythe (Whip)")]
        [Tooltip("Whip ka attack kitni door tak jayega")]
        [SerializeField] private float _attackRange = 4f;

        [Tooltip("Whip ka hitbox kitna chouda (wide) hoga")]
        [SerializeField] private float _attackWidth = 1.5f;

        [Tooltip("Kitni der baad agla chabuk (whip) chalega")]
        [SerializeField] private float _cooldown = 1.5f;

        [SerializeField] private int _damage = 10;
        [SerializeField] private LayerMask _enemyLayer;

        /// <summary>
        /// Learning Comment:
        /// TargetLayer property allows external reconfiguration of the detection mask.
        /// When mirrored on Shadow Kael, this is reconfigured to target the Player layer.
        /// </summary>
        public LayerMask TargetLayer
        {
            get => _enemyLayer;
            set => _enemyLayer = value;
        }

        public LayerMask EnemyLayer
        {
            get => _enemyLayer;
            set => _enemyLayer = value;
        }

        public override int MaxLevel => 8;
        /// <summary>Whips per attack from levels (before the Amount stat).</summary>
        public int LevelWhipCount { get; private set; } = 1;
        /// <summary>Area bonus from levels (multiplies the Area stat).</summary>
        public float LevelAreaMultiplier { get; private set; } = 1f;

        private Collider[] _hitsBuffer = new Collider[50]; // Limits piercing to 50 enemies per whip, zero GC allocation

        protected override void Awake()
        {
            base.Awake();
            // Whip_Weapon.prefab ships with an empty mask, so the hitbox found nothing and the
            // whip never hit. Default to the Enemy layer; Shadow Kael's mirror still overrides it.
            if (_enemyLayer.value == 0) _enemyLayer = LayerMask.GetMask("Enemy");
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
                case 2: LevelWhipCount++; break;
                case 4:
                case 6: DamageAmount += 5f; LevelAreaMultiplier += 0.1f; break;
                default: DamageAmount += 5f; break; // 3, 5, 7, 8
            }
        }

        protected override void Attack()
        {
            // Owner decision: lash toward the nearest enemy (facing side when none).
            // The second whip strikes the opposite side, then they alternate.
            float facing = Mathf.Sign(transform.root.localScale.x);
            Vector3 aim = AimDirection(new Vector3(facing, 0f, 0f));
            int whips = LevelWhipCount + ExtraAmount;
            for (int i = 0; i < whips; i++)
            {
                FireWhip(i % 2 == 0 ? aim : -aim);
            }
        }

        private void FireWhip(Vector3 attackDirection)
        {

            float areaMult = AreaMultiplier * LevelAreaMultiplier;
            float actualRange = _attackRange * areaMult;
            float actualWidth = _attackWidth * areaMult;
            int actualDamage = Mathf.RoundToInt(RollDamage(DamageAmount));

            // Box ka center point nikalna (player se thoda aage)
            Vector3 boxCenter = transform.position + (attackDirection * (actualRange / 2f));
            Vector3 halfExtents = new Vector3(actualRange / 2f, 1f, actualWidth / 2f);
            // The box is long along local X; turn it to face the lash direction.
            Quaternion boxRotation = Quaternion.FromToRotation(Vector3.right, attackDirection);

            // NonAlloc for 100% performance (no garbage generation)
            int hitsCount = UnityEngine.Physics.OverlapBoxNonAlloc(boxCenter, halfExtents, _hitsBuffer, boxRotation, _enemyLayer);

            for (int i = 0; i < hitsCount; i++)
            {
                var damageable = _hitsBuffer[i].GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    var packet = new DamagePacket
                    {
                        Amount = actualDamage,
                        HitPoint = _hitsBuffer[i].ClosestPoint(transform.position),
                        KnockbackDirection = attackDirection * 2f // Whip dushman ko thoda peeche dhakelta hai
                    };
                    damageable.TakeDamage(packet);
                }
            }

            // Learning Comment: Har frame empty swing log karne se console choke ho jata hai.
            if (hitsCount > 0)
            {
                Debug.Log($"[Shadow Scythe] Slashed {hitsCount} enemies!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            float sign = Mathf.Sign(transform.root.localScale.x);
            Vector3 attackDirection = new Vector3(sign, 0, 0);
            Vector3 boxCenter = transform.position + (attackDirection * (_attackRange / 2f));
            Vector3 halfExtents = new Vector3(_attackRange / 2f, 1f, _attackWidth / 2f);

            Gizmos.DrawWireCube(boxCenter, halfExtents * 2);
        }
    }
}
