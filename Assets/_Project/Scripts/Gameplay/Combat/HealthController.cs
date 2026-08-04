using System;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Universal Health component. Implements IDamageable so anything with this script can take damage.
    /// Fires C# events for UI or death logic, keeping dependencies clean.
    /// </summary>
    public class HealthController : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _maxHealth = 100;
        private int _currentHealth;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDied;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(DamagePacket packet)
        {
            if (_currentHealth <= 0) return; // Already dead

            _currentHealth -= packet.Amount;
            _currentHealth = Mathf.Max(0, _currentHealth);

            // Optional: Apply knockback force if entity has a Rigidbody
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(packet.KnockbackDirection * 5f, ForceMode.Impulse); // Basic knockback
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth == 0)
            {
                Die();
            }
        }

        private void Die()
        {
            OnDied?.Invoke();
            Debug.Log($"[HealthController] {gameObject.name} has died.");
            
            // Basic death logic (Disable object)
            gameObject.SetActive(false);
        }
    }
}
