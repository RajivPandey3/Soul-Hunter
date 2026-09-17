using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public class LaurelWeapon : AutoAttackWeapon
    {
        public GameObject ShieldVisualPrefab;
        private GameObject _activeShield;
        private HealthController _playerHealth;
        
        private int _maxCharges = 1;
        private int _currentCharges = 1;

        private void Start()
        {
            _playerHealth = GetComponentInParent<HealthController>();
            if (_playerHealth != null)
            {
                // Hook into damage taken event to absorb damage
                // Note: HealthController would need an OnTakeDamage event or interceptor for full Laurel effect.
                // For now, this is simulated logic.
            }
        }

        public override void LevelUp()
        {
            CurrentLevel++;
            if (CurrentLevel % 2 == 0) _maxCharges++;
            AttackCooldown -= 0.5f; // Faster recharge
        }

        protected override void Attack()
        {
            // Laurel "Attack" is actually recharging the shield
            if (_currentCharges < _maxCharges)
            {
                _currentCharges++;
                UpdateVisual();
            }
        }

        private void UpdateVisual()
        {
            if (_currentCharges > 0 && _activeShield == null && ShieldVisualPrefab != null)
            {
                _activeShield = Instantiate(ShieldVisualPrefab, transform);
            }
            else if (_currentCharges <= 0 && _activeShield != null)
            {
                Destroy(_activeShield);
            }
        }
    }
}
