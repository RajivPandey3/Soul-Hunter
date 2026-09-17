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
        [Tooltip("Asli Vampire Survivors mein early dushmano ki HP bahut kam (10-15) hoti hai taaki wo 1 hit mein marein")]
        [SerializeField] private int _maxHealth = 100;
        private int _currentHealth;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDied;
        public event Action OnDamaged; // For Hit Flash VFX

        public bool IsInvincible { get; set; } = false;

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            _currentHealth = _maxHealth;
        }

        public void Initialize(int maxHealth)
        {
            _maxHealth = maxHealth;
            ResetHealth();
        }

        [Tooltip("0 = Full Knockback, 1 = Immune (Bosses)")]
        [Range(0f, 1f)]
        public float KnockbackResistance = 0f;
        private bool _isKnockedBack = false;

        public void TakeDamage(DamagePacket packet)
        {
            if (_currentHealth <= 0 || IsInvincible) return; // Already dead or invincible

            _currentHealth -= packet.Amount;
            
            // Kami #3 Fix: Damage Number dikhana
            if (SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
            {
                // Thoda sa upar dikhate hain taaki enemy ke sir par aaye
                Vector3 popupPos = transform.position + Vector3.up * 1.5f; 
                SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(packet.Amount, popupPos);
            }

            _currentHealth = Mathf.Max(0, _currentHealth);

            OnDamaged?.Invoke();

            // 100% VS Quality: Kinematic smooth slide (No physics jitter)
            if (KnockbackResistance < 1f && !_isKnockedBack)
            {
                StartCoroutine(KinematicKnockback(packet.KnockbackDirection * 2f * (1f - KnockbackResistance)));
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth == 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            if (_currentHealth <= 0) return; // Dead things don't heal
            
            _currentHealth += amount;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth); // Cap to max
            
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            
            Debug.Log($"[HealthController] Healed for {amount}! Current: {_currentHealth}");
        }

        public void IncreaseMaxHealth(int amount)
        {
            _maxHealth += amount;
            _currentHealth += amount; // Asli VS ki tarah Max Health barhne par player heal bhi hota hai
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            Debug.Log($"[HealthController] Max Health increased by {amount}! New Max: {_maxHealth}");
        }

        private void Die()
        {
            OnDied?.Invoke();
            Debug.Log($"[HealthController] {gameObject.name} has died.");
            
            // Basic death logic (Disable object)
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator KinematicKnockback(Vector3 slideVector)
        {
            _isKnockedBack = true;
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // Smoothly push the enemy without breaking physics swarms
                transform.position += slideVector * (Time.deltaTime / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _isKnockedBack = false;
        }
    }
}
