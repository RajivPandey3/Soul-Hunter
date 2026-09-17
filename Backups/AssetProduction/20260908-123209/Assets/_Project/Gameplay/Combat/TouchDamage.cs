using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors style me dushman touch hone par lagatar (continuous) damage deta hai.
    /// Ye script Dushman (Enemy) par lagegi aur jab tak Player touch mein rahega, usko nuksaan pahunchayegi.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TouchDamage : MonoBehaviour
    {
        [SerializeField] private float _damageAmount = 5f;
        [SerializeField] private float _damageInterval = 0.5f; // Har 0.5 sec mein damage dega

        public float DamageAmount { get => _damageAmount; set => _damageAmount = value; }
        public float DamageInterval { get => _damageInterval; set => _damageInterval = value; }
        public string SourceWeaponName = "Unknown";

        private IDamageable _target;
        private Transform _targetTransform;
        private float _timer;

        private void OnEnable() { _timer = 0f; _target = null; _targetTransform = null; }

        private void OnCollisionEnter(Collision collision)
        {
            var damageable = collision.gameObject.GetComponentInParent<IDamageable>();
            if (damageable != null && collision.gameObject.CompareTag("Player"))
            {
                _target = damageable;
                _targetTransform = collision.transform;
            }
        }

        private void OnDisable() { _target = null; _targetTransform = null; } 
        
        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                _target = null;
                _targetTransform = null;
            }
        }

        private void Update()
        {
            if (_target != null && _targetTransform != null)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    // Player ko damage do
                    Vector3 knockback = (transform.position - _targetTransform.position).normalized;
                    _target.TakeDamage(new DamagePacket(Mathf.RoundToInt(_damageAmount), transform.position, -knockback));
                    
                    if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null && SourceWeaponName != "Unknown")
                    {
                        SoulHunter.Gameplay.Core.RunStatsTracker.Instance.RecordDamage(SourceWeaponName, Mathf.RoundToInt(_damageAmount));
                    }
                    
                    _timer = _damageInterval;
                }
            }
        }
    }
}
