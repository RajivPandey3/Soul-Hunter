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

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            _speedHash = Animator.StringToHash("Speed");
            _attackHash = Animator.StringToHash("Attack");
        }

        public void UpdateSpeed(float currentSpeed)
        {
            if (_animator != null)
            {
                _animator.SetFloat(_speedHash, currentSpeed);
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
