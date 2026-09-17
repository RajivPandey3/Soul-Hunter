using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.UI
{
    /// <summary>
    /// Learning Comment:
    /// Decoupled UI script using the Observer Pattern.
    /// Ye directly game state ko modify nahi karta, balki events (OnHealthChanged) ka wait karta hai.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private Slider _healthSlider;
        
        private HealthController _boundHealthController;

        // In a real flow, a GameManager or the Player itself will call this method when spawned.
        public void BindToPlayer(HealthController playerHealth)
        {
            if (_boundHealthController != null)
            {
                _boundHealthController.OnHealthChanged -= UpdateHealthUI;
            }

            _boundHealthController = playerHealth;

            if (_boundHealthController != null)
            {
                _boundHealthController.OnHealthChanged += UpdateHealthUI;
                // Force an initial update 
                // Note: In full implementation, we'd add a getter to HealthController for current health.
            }
        }

        private void OnDestroy()
        {
            // Always unsubscribe to prevent memory leaks (2036 Test requirement)
            if (_boundHealthController != null)
            {
                _boundHealthController.OnHealthChanged -= UpdateHealthUI;
            }
        }

        private void UpdateHealthUI(int currentHealth, int maxHealth)
        {
            if (_healthSlider != null)
            {
                _healthSlider.maxValue = maxHealth;
                _healthSlider.value = currentHealth;
            }
        }
    }
}
