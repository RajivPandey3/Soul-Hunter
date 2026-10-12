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
            // Stay frozen, except that the player can push a frozen enemy by walking into it (VS wiki Freeze: "the player can
            // still deal damage to frozen enemies and push them around by walking into them").
            Vector3 push = PushFromPlayer(_controller);
            _controller.Rigidbody.linearVelocity = new Vector3(push.x, _controller.Rigidbody.linearVelocity.y, push.z); // Keep gravity
        }

        /// <summary>The player's body radius used for pushing (its capsule is 0.4; the wiki gives no size).</summary>
        public const float PlayerPushRadius = 0.4f;

        /// <summary>
        /// The velocity that keeps a frozen enemy just outside the player's body: zero unless the player overlaps it.
        /// It never moves faster than twice the player's own speed, so a push looks like a shove, not a jump.
        /// </summary>
        public static Vector3 PushFromPlayer(EnemyController enemy)
        {
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            if (player == null || enemy == null) return Vector3.zero;
            Vector3 away = enemy.transform.position - player.transform.position;
            away.y = 0f;
            float reach = PlayerPushRadius + enemy.CrowdRadius;
            float distance = away.magnitude;
            if (distance >= reach) return Vector3.zero;
            Vector3 direction = distance > 1e-3f ? away / distance : player.FacingDirection;
            float speed = Mathf.Min((reach - distance) / Mathf.Max(Time.fixedDeltaTime, 1e-3f), Mathf.Max(1f, player.MoveSpeed) * 2f);
            return direction * speed;
        }

        public void Exit()
        {
            // Unfrozen
        }
    }
}
