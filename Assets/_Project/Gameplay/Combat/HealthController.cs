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
        private float _preciseMaxHealth;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDied;
        public event Action OnDamaged; // For Hit Flash VFX
        public Func<DamagePacket, bool> DamageInterceptor { get; set; }
        public Func<int, int> DamageModifier { get; set; }

        public bool IsInvincible { get; set; } = false;
        private bool _keepAliveAfterDeath;

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            if (_preciseMaxHealth <= 0f) _preciseMaxHealth = _maxHealth;
            _currentHealth = _maxHealth;
        }

        public void Initialize(int maxHealth)
        {
            _maxHealth = maxHealth;
            _preciseMaxHealth = maxHealth;
            ResetHealth();
        }

        [Tooltip("0 = Full Knockback, 1 = Immune (Bosses)")]
        [Range(0f, 1f)]
        public float KnockbackResistance = 0f;
        private bool _isKnockedBack = false;

        public void TakeDamage(DamagePacket packet)
        {
            if (_currentHealth <= 0 || IsInvincible) return; // Already dead or invincible
            if (packet.Amount <= 0 || (DamageInterceptor != null && DamageInterceptor(packet))) return;

            int incomingDamage = packet.Amount;
            var enemy = GetComponentInParent<SoulHunter.Gameplay.AI.EnemyController>();
            if (enemy != null)
            {
                // Learning Comment:
                // Authoritative Level 7 Spectral Damage Whitelist (AGENTS.md Section 5 & Docs/Requirements/P1-07-reconciliation.md Section 3.2):
                // Spectral/ghost enemies are intangible to physical and non-holy elemental damage.
                // Reject Normal, Fire, Poison, and Shadow damage, permitting only Holy (2x vulnerability), Magic (1x), and Spectral (1x).
                if (enemy.IsSpectral)
                {
                    bool isWhitelisted = packet.Type == DamageType.Holy ||
                                         packet.Type == DamageType.Magic ||
                                         packet.Type == DamageType.Spectral;
                    if (!isWhitelisted) return;
                }

                incomingDamage = Mathf.Max(1, Mathf.RoundToInt(incomingDamage * enemy.GetDamageMultiplier(packet.Type)));
            }
            if (DamageModifier != null) incomingDamage = Mathf.Max(0, DamageModifier(incomingDamage));
            if (incomingDamage == 0) return;
            _currentHealth -= incomingDamage;
            
            // Kami #3 Fix: Damage Number dikhana
            if (SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
            {
                // Thoda sa upar dikhate hain taaki enemy ke sir par aaye
                Vector3 popupPos = transform.position + Vector3.up * 1.5f; 
                SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(incomingDamage, popupPos);
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

        /// <summary>
        /// Arena cleanup/boss transition ke liye silent instant death. Isse
        /// gameplay damage popup mein fake 99999 number nahi dikhta.
        /// </summary>
        public void KillSilently()
        {
            if (_currentHealth <= 0) return;
            _currentHealth = 0;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            Die();
        }

        /// <summary>
        /// Player revival hook. PlayerController calls this from OnDied when a
        /// revival is available; the current death notification still fires,
        /// but HealthController keeps the player object alive and restores HP.
        /// </summary>
        public void ReviveFromDeath(int restoredHealth = -1)
        {
            _keepAliveAfterDeath = true;
            _currentHealth = restoredHealth > 0 ? Mathf.Min(restoredHealth, _maxHealth) : Mathf.Max(1, Mathf.CeilToInt(_maxHealth * 0.5f));
            IsInvincible = true;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void IncreaseMaxHealth(int amount)
        {
            if (_preciseMaxHealth <= 0f) _preciseMaxHealth = _maxHealth;
            _preciseMaxHealth += amount;
            _maxHealth += amount;
            _currentHealth += amount; // Asli VS ki tarah Max Health barhne par player heal bhi hota hai
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            Debug.Log($"[HealthController] Max Health increased by {amount}! New Max: {_maxHealth}");
        }

        public void MultiplyMaxHealth(float multiplier)
        {
            if (multiplier <= 0f) return;
            if (_preciseMaxHealth <= 0f) _preciseMaxHealth = _maxHealth;
            int previous = _maxHealth;
            _preciseMaxHealth *= multiplier;
            _maxHealth = Mathf.Max(1, Mathf.RoundToInt(_preciseMaxHealth));
            if (_currentHealth > 0) _currentHealth = Mathf.Clamp(_currentHealth + _maxHealth - previous, 1, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Die()
        {
            OnDied?.Invoke();
            // Learning Comment:
            // Generic cloned enemies (horde) ke marne par Debug.Log karne se Unity console freeze ho jata tha.
            // Ab sirf unique entities (jaise Player ya Boss) ka death log hoga.
            if (!gameObject.name.Contains("(Clone)"))
            {
                Debug.Log($"[HealthController] {gameObject.name} has died.");
            }
            
            // Basic death logic (Disable object)
            if (_keepAliveAfterDeath)
            {
                _keepAliveAfterDeath = false;
                StartCoroutine(EndRevivalInvulnerability());
            }
            else gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator EndRevivalInvulnerability()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            IsInvincible = false;
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
