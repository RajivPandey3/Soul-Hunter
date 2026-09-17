using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors ke 'Bat Swarm' event ke liye.
    /// Is state mein dushman player ko chase nahi karta, balki ek seedhi line mein fly karta hai.
    /// </summary>
    public class EnemyFlyState : IEnemyState
    {
        private readonly EnemyController _controller;
        private Vector3 _flyDirection;
        private float _aliveTimer = 0f;

        public EnemyFlyState(EnemyController controller, Vector3 direction)
        {
            _controller = controller;
            _flyDirection = direction.normalized;
        }

        public void Enter()
        {
            _aliveTimer = 0f;
            // Ghost mode - takraye nahi balki guzar jaye
            if (_controller.GetComponent<Collider>() != null)
            {
                _controller.GetComponent<Collider>().isTrigger = true;
            }
        }

        public void UpdateLogic()
        {
            _aliveTimer += Time.deltaTime;
            // 15 seconds ke baad agar screen se guzar jaye toh khud ko pool mein bhej de ya disable kar de
            if (_aliveTimer > 15f)
            {
                _controller.gameObject.SetActive(false);
            }
        }

        public void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData)
        {
            // Seedha tezi se fly karo (Ignore player)
            _controller.Rigidbody.linearVelocity = _flyDirection * (_controller.MoveSpeed * 1.5f);
            
            if (_controller.Animator != null)
            {
                _controller.Animator.UpdateSpeed(1f);
            }
        }

        public void Exit()
        {
            if (_controller.GetComponent<Collider>() != null)
            {
                _controller.GetComponent<Collider>().isTrigger = false; // Reset
            }
        }
    }
}
