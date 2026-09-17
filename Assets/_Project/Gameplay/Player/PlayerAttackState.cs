using UnityEngine;

namespace SoulHunter.Gameplay.Player
{

    /// <summary>
    /// Learning Comment:
    /// Yeh script PlayerAttackState.cs ke core logic ko handle karti hai.
    /// </summary>
    public class PlayerAttackState : IPlayerState
    {
        private readonly PlayerController _controller;
        private float _attackTimer;
        private const float ATTACK_DURATION = 0.5f; // Should be tied to animation length in a real scenario

        public PlayerAttackState(PlayerController controller)
        {
            _controller = controller;
        }

        public void Enter()
        {
            _attackTimer = 0f;
            
            // Halt movement during attack
            _controller.Rigidbody.linearVelocity = Vector3.zero;

            if (_controller.Animator != null)
            {
                _controller.Animator.TriggerAttack();
            }
        }

        public void UpdateLogic()
        {
            _attackTimer += Time.deltaTime;
            
            // When animation is done, go back to Idle
            if (_attackTimer >= ATTACK_DURATION)
            {
                _controller.ChangeState(new PlayerIdleState(_controller));
            }
        }

        public void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData)
        {
            // VS-style attacks do not lock the survivor in place. This also
            // prevents a stale attack state after level-up from making movement
            // appear slow on one axis.
            Vector3 movement = _controller.GetCameraRelativeMovement(_controller.CurrentMoveInput) * _controller.MoveSpeed;
            float yVelocity = envData.IsGrounded ? 0f : _controller.Rigidbody.linearVelocity.y;
            _controller.Rigidbody.linearVelocity = new Vector3(
                movement.x,
                yVelocity,
                movement.z);
        }

        public void Exit()
        {
        }
    }
}
