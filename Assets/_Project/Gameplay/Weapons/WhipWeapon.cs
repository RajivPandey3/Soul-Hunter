using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Weapons
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Whip weapon hamesha horizontally attack karta hai (left ya right).
    /// Ye ek rectangular box area mein aaye hue sabhi dushmano ko ek saath damage deta hai.
    /// Isme koi goli (projectile) travel nahi karti, ye instant hit (OverlapBox) karta hai.
    /// </summary>
    public class WhipWeapon : MonoBehaviour
    {
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

        private float _timer;
        private Collider[] _hitsBuffer = new Collider[50]; // Limits piercing to 50 enemies per whip, zero GC allocation
        private SoulHunter.Gameplay.Player.PlayerStats _stats;

        private void Awake()
        {
            _stats = GetComponentInParent<SoulHunter.Gameplay.Player.PlayerStats>();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f)
            {
                FireWhip();
                float currentCooldown = _stats != null ? _cooldown * _stats.Cooldown : _cooldown;
                _timer = currentCooldown;
            }
        }

        private void FireWhip()
        {
            float sign = Mathf.Sign(transform.root.localScale.x);
            Vector3 attackDirection = new Vector3(sign, 0, 0);

            float areaMult = _stats != null ? _stats.Area : 1f;
            float actualRange = _attackRange * areaMult;
            float actualWidth = _attackWidth * areaMult;
            int actualDamage = _stats != null ? Mathf.RoundToInt(_damage * _stats.Might) : _damage;

            // Box ka center point nikalna (player se thoda aage)
            Vector3 boxCenter = transform.position + (attackDirection * (actualRange / 2f));
            Vector3 halfExtents = new Vector3(actualRange / 2f, 1f, actualWidth / 2f);

            // NonAlloc for 100% performance (no garbage generation)
            int hitsCount = UnityEngine.Physics.OverlapBoxNonAlloc(boxCenter, halfExtents, _hitsBuffer, Quaternion.identity, _enemyLayer);

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
