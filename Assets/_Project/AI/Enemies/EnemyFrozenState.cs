using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// Time Freeze (Orologion) state. Dushman apni jagah barf ki tarah jam jayega.
    /// Freeze khatam hone par wapas apni purani aadat par: stage-event flier apne raaste par, baaki Chase.
    /// Use EnemyController.Freeze, which remembers the state to resume.
    /// </summary>
    public class EnemyFrozenState : IEnemyState
    {
        private readonly EnemyController _controller;
        private float _freezeDuration;
        private float _timer;

        /// <summary>State to return to when the freeze ends; null means chase.</summary>
        public IEnemyState ResumeState { get; }

        public EnemyFrozenState(EnemyController controller, float duration, IEnemyState resumeState = null)
        {
            _controller = controller;
            _freezeDuration = duration;
            ResumeState = resumeState;
        }

        public void Enter()
        {
            _timer = 0f;
            // Freeze physics
            _controller.Rigidbody.linearVelocity = Vector3.zero;
            
            // Stop animation
            if (_controller.Animator != null)
            {
                _controller.Animator.UpdateSpeed(0f);
            }
        }

        public void UpdateLogic()
        {
            _timer += Time.deltaTime;
            if (_timer >= _freezeDuration)
            {
                // Unfreeze: resume the stage-event flight, otherwise go back to chase.
                _controller.ChangeState(ResumeState ?? new EnemyChaseState(_controller));
            }
        }

        public void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData)
        {
            // Do absolutely nothing. Stay frozen.
            _controller.Rigidbody.linearVelocity = new Vector3(0, _controller.Rigidbody.linearVelocity.y, 0); // Keep gravity
        }

        public void Exit()
        {
            // Unfrozen
        }
    }
}
