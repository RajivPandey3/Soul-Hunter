using UnityEngine;

namespace SoulHunter.Gameplay.Player
{

    /// <summary>
    /// Learning Comment:
    /// Yeh script PlayerRunState.cs ke core logic ko handle karti hai.
    /// </summary>
    public class PlayerRunState : IPlayerState
    {
        private readonly PlayerController _controller;

        public PlayerRunState(PlayerController controller)
        {
            _controller = controller;
        }

        public void Enter()
        {
            // Future: Play Run Animation
        }

        public void UpdateLogic()
        {
            if (_controller.CurrentMoveInput.sqrMagnitude <= 0.01f)
            {
                _controller.ChangeState(new PlayerIdleState(_controller));
            }
        }

        public void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData)
        {
            Vector3 movement = _controller.GetCameraRelativeMovement(_controller.CurrentMoveInput) * _controller.MoveSpeed;
            
            // Top-down Vampire-Survivors movement: horizontal speed must be
            // identical on X and Z. Ground normals from visual/level colliders
            // must never reduce forward/back movement after a level-up pause.
            float yVel = _controller.Rigidbody.linearVelocity.y;
            if (envData.IsGrounded)
            {
                yVel = 0f; // Jitter prevention; keep X/Z input unchanged.
            }
            
            _controller.Rigidbody.linearVelocity = new Vector3(movement.x, yVel, movement.z);
            
            if (_controller.Animator != null)
            {
                _controller.Animator.UpdateSpeed(movement.magnitude);
            }
        }

        public void Exit()
        {
        }
    }
}
