using UnityEngine;

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

        private Rigidbody _rb;
        private float _currentLifetime;

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
            _currentLifetime = _lifetime;
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

        public void Initialize(Vector3 direction, float speed, float damage, float lifetime)
        {
            _speed = speed;
            _damage = Mathf.RoundToInt(damage);
            _lifetime = lifetime;
            _currentLifetime = lifetime;
            transform.forward = direction;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Apni goli se khud ko damage na ho
            if (other.CompareTag("Player")) return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                // Damage do aur khud ko pool mein waapis bhej do (Deactivate)
                Vector3 knockback = transform.forward;
                target.TakeDamage(new DamagePacket(_damage, transform.position, knockback));
                gameObject.SetActive(false);
            }
        }
    }
}
