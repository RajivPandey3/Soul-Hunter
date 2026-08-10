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
        [SerializeField] private int _damageAmount = 5;
        [SerializeField] private float _damageInterval = 0.5f; // Har 0.5 sec mein damage dega

        private IDamageable _target;
        private Transform _targetTransform;
        private float _timer;

        private void OnCollisionEnter(Collision collision)
        {
            var damageable = collision.gameObject.GetComponentInParent<IDamageable>();
            if (damageable != null && collision.gameObject.CompareTag("Player"))
            {
                _target = damageable;
                _targetTransform = collision.transform;
            }
        }

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
                    _target.TakeDamage(new DamagePacket(_damageAmount, transform.position, -knockback));
                    
                    _timer = _damageInterval;
                }
            }
        }
    }
}
