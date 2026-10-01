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
    /// VS wiki (Garlic): 5 damage, 1.3 s; L2 Area +40%, +2; L3/L7 -0.1 s, +1; L5 -0.1 s, +2; L4/L6 Area +20%, +1; L8 Area +20%, +2.
    /// VS wiki (Soul Eater, uses this script): 20 damage, 1.0 s, Area 300% of Garlic (SoulEater.prefab radius 7.5).
    /// </summary>
    public class GarlicWeapon : AutoAttackWeapon
    {
        /// <summary>Garlic's own knockback (weak; provisional). Its VS effect is raising knockback taken.</summary>
        public const float GarlicKnockback = 0.3f;

        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.None;
        [Header("Soul Hunter Theme: Aura of Death (Garlic)")]
        [Tooltip("Kitne meter ke daayre (radius) mein aura asar karega")]
        [SerializeField] private float _damageRadius = 2.5f;

        [Tooltip("Har kitne second mein aura damage dega")]
        [SerializeField] private float _damageInterval = 1.3f;

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

        /// <summary>Soul Eater (this script with life steal) is not held to the Might cap (wiki Player_stats).</summary>
        protected override bool IgnoresMightCap => _lifeStealPerHit > 0f;

        public override int MaxLevel => 8;
        /// <summary>Area bonus from levels (multiplies the Area stat).</summary>
        public float LevelAreaMultiplier { get; private set; } = 1f;

        private Collider[] _hitsBuffer = new Collider[200]; // 100% VS Quality: Zero Garbage Allocation

        protected override void Awake()
        {
            base.Awake();
            // An empty mask would make the aura hit nothing (the Whip prefab shipped that way).
            if (_enemyLayer.value == 0) _enemyLayer = LayerMask.GetMask("Enemy");
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
                case 7: AttackCooldown -= 0.1f; DamageAmount += 1f; break;
                case 5: AttackCooldown -= 0.1f; DamageAmount += 2f; break;
                case 8: LevelAreaMultiplier += 0.2f; DamageAmount += 2f; break;
                default: LevelAreaMultiplier += 0.2f; DamageAmount += 1f; break; // 4, 6
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
                    // A weak push (provisional), but VS Garlic makes each enemy it touches take more knockback.
                    var packet = new DamagePacket
                    {
                        Amount = actualDamage,
                        HitPoint = _hitsBuffer[i].ClosestPoint(transform.position),
                        KnockbackDirection = (_hitsBuffer[i].transform.position - transform.position).normalized * 0.1f,
                        WeaponKnockback = GarlicKnockback
                    };
                    damageable.TakeDamage(packet);
                    var enemy = _hitsBuffer[i].GetComponentInParent<SoulHunter.Gameplay.AI.EnemyController>();
                    // VS Garlic debuffs (+0.3 knockback taken, -0.1 freeze resistance), blocked by debuff resistance.
                    if (enemy != null) enemy.ApplyGarlicDebuff();
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
