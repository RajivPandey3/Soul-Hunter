using UnityEngine;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors ke 'Magic Wand' jaisi projectile script.
    /// Ye sidha aage move karti hai aur kisi enemy se takrane par use damage deti hai.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 10f;
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private DamageType _damageType = DamageType.Normal;

        private Rigidbody _rb;
        private float _currentLifetime;
        private int _bounceCount;
        private int _maxBounces;
        private string _sourceWeaponName;
        public event System.Action<Projectile> Deactivated;

        private void OnDisable()
        {
            _recentHits.Clear();
            Deactivated?.Invoke(this);
            Deactivated = null;
        }
        private int _remainingHits;
        private bool _blockedByWalls;
        private readonly System.Collections.Generic.Dictionary<IDamageable, float> _recentHits = new();

        public void ConfigureContacts(int pierce, int bounces, bool blockedByWalls)
        {
            _maxBounces = Mathf.Max(0, bounces);
            _bounceCount = 0;
            _remainingHits = Mathf.Max(1, pierce) + _maxBounces;
            _blockedByWalls = blockedByWalls;
            _recentHits.Clear();
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.isKinematic = true; // Use trigger collisions
            
            var col = GetComponent<Collider>();
            col.isTrigger = true; // Important for trigger detection
        }

        private void OnEnable()
        {
            _sourceWeaponName = null;
            _currentLifetime = _lifetime;
            _bounceCount = 0;
            _maxBounces = ArcanaManager.Instance != null ? ArcanaManager.Instance.ProjectileBounceCount : 0;
            ConfigureContacts(1, _maxBounces, false);
        }

        private void Update()
        {
            transform.position += transform.forward * (_speed * Time.deltaTime);

            _currentLifetime -= Time.deltaTime;
            if (_currentLifetime <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        public void Initialize(Vector3 direction, float speed, float damage, float lifetime, DamageType damageType = DamageType.Normal, string sourceWeaponName = null)
        {
            _sourceWeaponName = sourceWeaponName;
            _speed = speed;
            _damage = Mathf.RoundToInt(damage);
            _lifetime = lifetime;
            _damageType = damageType;
            _currentLifetime = lifetime;
            _bounceCount = 0;
            _maxBounces = ArcanaManager.Instance != null ? ArcanaManager.Instance.ProjectileBounceCount : 0;
            ConfigureContacts(1, _maxBounces, false);
            transform.forward = direction;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActiveAndEnabled) return;
            // Apni goli se khud ko damage na ho
            if (other.CompareTag("Player") || other.GetComponentInParent<SoulHunter.Gameplay.Player.PlayerController>() != null) return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                if (_recentHits.TryGetValue(target, out float lastHit) && Time.time - lastHit < 0.1f) return;
                _recentHits[target] = Time.time;
                Vector3 knockback = transform.forward;
                var health = target as HealthController;
                int healthBefore = health != null ? health.CurrentHealth : 0;
                target.TakeDamage(new DamagePacket(_damage, transform.position, knockback, _damageType));
                if (health != null && RunStatsTracker.Instance != null)
                {
                    int applied = Mathf.Max(0, healthBefore - health.CurrentHealth);
                    if (applied > 0) RunStatsTracker.Instance.RecordDamage(_sourceWeaponName, applied);
                }
                _remainingHits--;
                if (_remainingHits <= 0) { gameObject.SetActive(false); return; }
                if (_bounceCount < _maxBounces)
                {
                    _bounceCount++;
                    Vector3 normal = other.transform.position - transform.position;
                    normal.y = 0f;
                    Vector3 reflected = Vector3.Reflect(transform.forward, normal.normalized);
                    reflected.y = 0f;
                    if (reflected.sqrMagnitude < 0.001f) reflected = -transform.forward;
                    transform.forward = reflected.normalized;
                }
            }
            else if (_blockedByWalls && !other.isTrigger) gameObject.SetActive(false);
        }
    }
}
