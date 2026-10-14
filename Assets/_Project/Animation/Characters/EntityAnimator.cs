using UnityEngine;

namespace SoulHunter.Gameplay.Animation
{
    /// <summary>
    /// Learning Comment:
    /// Decoupled Animation wrapper. Unity Animator component should not be directly 
    /// manipulated by gameplay scripts to preserve Single Responsibility.
    /// Uses Animator.StringToHash to ensure zero-allocation (Hybrid friendly).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EntityAnimator : MonoBehaviour
    {
        private Animator _animator;

        // Cached Hash IDs for Zero-Allocation
        private int _speedHash;
        private int _attackHash;
        // The last Speed sent: a horde of 300 sent the same value every physics step. An Animator forgets its
        // parameters when its object is switched off, so a pooled body sends its first value again (OnEnable).
        private float _lastSpeed = float.NaN;

        private void OnEnable() => _lastSpeed = float.NaN;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            _speedHash = Animator.StringToHash("Speed");
            _attackHash = Animator.StringToHash("Attack");
        }

        public void UpdateSpeed(float currentSpeed)
        {
            if (Mathf.Abs(currentSpeed - _lastSpeed) < 0.01f) return;
            if (_animator != null)
            {
                _animator.SetFloat(_speedHash, currentSpeed);
                _lastSpeed = currentSpeed;
            }
        }

        public void TriggerAttack()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(_attackHash);
            }
        }
    }
}
