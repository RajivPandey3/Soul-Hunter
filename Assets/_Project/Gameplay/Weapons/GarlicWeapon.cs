using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Garlic weapon. Ye player ke aas paas ek aura (circle) banata hai.
    /// Har X seconds mein us aura ke andar aane wale sabhi dushmano ko damage lagta hai.
    /// Ye kisi ko point nahi karta, bas area of effect (AoE) mein damage deta hai.
    /// Level table (1-8): har level damage; L2/L4/L6/L8 area, L3/L5/L7 tez interval.
    /// </summary>
    public class GarlicWeapon : AutoAttackWeapon
    {
        [Header("Soul Hunter Theme: Aura of Death (Garlic)")]
        [Tooltip("Kitne meter ke daayre (radius) mein aura asar karega")]
        [SerializeField] private float _damageRadius = 2.5f;

        [Tooltip("Har kitne second mein aura damage dega")]
        [SerializeField] private float _damageInterval = 1f;

        [Tooltip("Ek baar mein kitna damage padega")]
        [SerializeField] private int _damageAmount = 5;

        [SerializeField] private LayerMask _enemyLayer;

        [Header("Life steal (Soul Eater evolution)")]
        [Tooltip("HP healed per enemy hit (fractions carry over). 0 for plain Garlic.")]
        [SerializeField, Min(0f)] private float _lifeStealPerHit = 0f;
        [Tooltip("Most HP healed by one aura pulse")]
        [SerializeField, Min(0)] private int _maxHealPerPulse = 5;

        private float _healBank;
        private HealthController _ownerHealth;

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
        /// <summary>Area bonus from levels (multiplies the Area stat).</summary>
        public float LevelAreaMultiplier { get; private set; } = 1f;

        private Collider[] _hitsBuffer = new Collider[200]; // 100% VS Quality: Zero Garbage Allocation

        protected override void Awake()
        {
            base.Awake();
            // Damage and interval live in the base fields so levelling and
            // Shadow Kael's mirroring can scale them.
            DamageAmount = _damageAmount;
            AttackCooldown = _damageInterval;
        }

        public override void LevelUp()
        {
            if (CurrentLevel >= MaxLevel) return;
            CurrentLevel++;
            switch (CurrentLevel)
            {
                case 2: LevelAreaMultiplier += 0.4f; DamageAmount += 2f; break;
                case 3:
                case 7: AttackCooldown *= 0.9f; DamageAmount += 1f; break;
                case 5: AttackCooldown *= 0.9f; DamageAmount += 2f; break;
                default: LevelAreaMultiplier += 0.2f; DamageAmount += 1f; break; // 4, 6, 8
            }
        }

        protected override void Attack()
        {
            ApplyDamageToEnemies();
        }

        private void ApplyDamageToEnemies()
        {
            float actualRadius = _damageRadius * AreaMultiplier * LevelAreaMultiplier;
            int actualDamage = Mathf.RoundToInt(ScaledDamage(DamageAmount));

            // Player ke center se ek bada gola (Sphere) phek kar usme aaye dushmano ko dhoondho (NonAlloc for max performance)
            int hitsCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, actualRadius, _hitsBuffer, _enemyLayer);
            int enemiesHit = 0;

            for (int i = 0; i < hitsCount; i++)
            {
                var damageable = _hitsBuffer[i].GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    enemiesHit++;
                    // Aura of death doesn't really knockback, just drains soul/health
                    var packet = new DamagePacket
                    {
                        Amount = actualDamage,
                        HitPoint = _hitsBuffer[i].ClosestPoint(transform.position),
                        KnockbackDirection = (_hitsBuffer[i].transform.position - transform.position).normalized * 0.1f // Tiny knockback
                    };
                    damageable.TakeDamage(packet);
                }
            }

            StealLife(enemiesHit);
        }

        /// <summary>
        /// Soul Eater: heals the owner for each enemy the aura hit. Fractions carry over between
        /// pulses; one pulse heals at most _maxHealPerPulse.
        /// </summary>
        public int StealLife(int enemiesHit)
        {
            if (_lifeStealPerHit <= 0f || enemiesHit <= 0) return 0;

            _healBank = Mathf.Min(_healBank + enemiesHit * _lifeStealPerHit, _maxHealPerPulse);
            int heal = Mathf.FloorToInt(_healBank + 0.0001f); // tolerate float drift (0.6 + 0.4 < 1)
            if (heal <= 0) return 0;
            _healBank -= heal;

            if (_ownerHealth == null) _ownerHealth = GetComponentInParent<HealthController>();
            if (_ownerHealth != null) _ownerHealth.Heal(heal);
            return heal;
        }

        // Scene window mein Garlic ka size dekhne ke liye visual guide (White circle)
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, _damageRadius);
        }
    }
}
