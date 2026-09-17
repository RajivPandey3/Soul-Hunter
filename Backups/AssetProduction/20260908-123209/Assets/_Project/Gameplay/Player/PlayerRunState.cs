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
            Vector3 movement = new Vector3(_controller.CurrentMoveInput.x, 0, _controller.CurrentMoveInput.y) * _controller.MoveSpeed;
            
            // Adjust movement based on ground slope if grounded
            float yVel = _controller.Rigidbody.linearVelocity.y;
            if (envData.IsGrounded)
            {
                if (envData.GroundAngle > 5f)
                {
                    movement = Vector3.ProjectOnPlane(movement, envData.GroundNormal);
                    yVel = movement.y;
                }
                else
                {
                    yVel = 0f; // Jitter prevention on flat ground
                }
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
