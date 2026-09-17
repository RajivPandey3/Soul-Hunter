using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Garlic weapon. Ye player ke aas paas ek aura (circle) banata hai.
    /// Har X seconds mein us aura ke andar aane wale sabhi dushmano ko damage lagta hai.
    /// Ye kisi ko point nahi karta, bas area of effect (AoE) mein damage deta hai.
    /// </summary>
    public class GarlicWeapon : MonoBehaviour
    {
        [Header("Soul Hunter Theme: Aura of Death (Garlic)")]
        [Tooltip("Kitne meter ke daayre (radius) mein aura asar karega")]
        [SerializeField] private float _damageRadius = 2.5f;
        
        [Tooltip("Har kitne second mein aura damage dega")]
        [SerializeField] private float _damageInterval = 1f;
        
        [Tooltip("Ek baar mein kitna damage padega")]
        [SerializeField] private int _damageAmount = 5;
        
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

        private float _timer;
        private Collider[] _hitsBuffer = new Collider[200]; // 100% VS Quality: Zero Garbage Allocation
        private SoulHunter.Gameplay.Player.PlayerStats _stats;

        private void Awake()
        {
            _stats = GetComponentInParent<SoulHunter.Gameplay.Player.PlayerStats>();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            
            // Har X second ke baad aas-paas ke dushmano par hamla karo
            if (_timer <= 0f)
            {
                ApplyDamageToEnemies();
                float currentCooldown = _stats != null ? _damageInterval * _stats.Cooldown : _damageInterval;
                _timer = currentCooldown;
            }
        }

        private void ApplyDamageToEnemies()
        {
            float areaMult = _stats != null ? _stats.Area : 1f;
            float actualRadius = _damageRadius * areaMult;
            int actualDamage = _stats != null ? Mathf.RoundToInt(_damageAmount * _stats.Might) : _damageAmount;

            // Player ke center se ek bada gola (Sphere) phek kar usme aaye dushmano ko dhoondho (NonAlloc for max performance)
            int hitsCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, actualRadius, _hitsBuffer, _enemyLayer);
            
            for (int i = 0; i < hitsCount; i++)
            {
                var damageable = _hitsBuffer[i].GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
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
        }

        // Scene window mein Garlic ka size dekhne ke liye visual guide (White circle)
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, _damageRadius);
        }
    }
}
