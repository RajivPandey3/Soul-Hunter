using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors stage events (swarm, wall, closing ring) ke liye.
    /// Is state mein dushman player ko chase nahi karta, balki ek seedhi line mein fly karta hai,
    /// aur kuch der baad bina drop ke pool mein wapas chala jata hai (screen se nikal gaya).
    /// </summary>
    public class EnemyFlyState : IEnemyState
    {
        public const float DefaultLifetimeSeconds = 15f;
        public const float SpeedMultiplier = 1.5f;
        /// <summary>The period of an event flier's own weave: the Floaty default (EnemyData.FloatyPeriodSeconds).</summary>
        public const float WeavePeriodSeconds = 1.8f;

        private readonly EnemyController _controller;
        private readonly Vector3 _flyDirection;
        private readonly float _lifetime;
        private readonly float _speedMultiplier;
        private readonly float _weave;
        private float _aliveTimer;

        public Vector3 Direction => _flyDirection;

        /// <param name="speedMultiplier">Stage-event formations fly at 1.5x; a Fixed Direction enemy at its own speed (1).</param>
        /// <param name="weave">Above 0, the flier weaves along its line whatever its type (Omens' Serpent Streams: VS Shrimp
        /// Swarm "fly in wave-like patterns"), with the Floaty period.</param>
        public EnemyFlyState(EnemyController controller, Vector3 direction, float lifetime = DefaultLifetimeSeconds, float speedMultiplier = SpeedMultiplier, float weave = 0f)
        {
            _speedMultiplier = speedMultiplier;
            _weave = Mathf.Max(0f, weave);
            _controller = controller;
            direction.y = 0f;
            _flyDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            _lifetime = lifetime;
            _aliveTimer = 0f; // not reset in Enter: a flier resumed after a Time Freeze keeps its lifetime
        }

        public void Enter()
        {
            // Enemy bodies are always triggers (EnemyController.Awake), so fliers pass
            // through each other and the player. The old Exit made the collider solid again,
            // which would have turned every pooled flier into a wall on reuse.
            _controller.FaceDirection(_flyDirection);
        }

        public void UpdateLogic()
        {
            _aliveTimer += Time.deltaTime;
            // Screen se guzar gaya: bina kill/drop ke pool mein wapas.
            if (_aliveTimer > _lifetime) _controller.gameObject.SetActive(false);
        }

        public void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData)
        {
            // Seedha fly karo (player ko ignore), ground plane par hi.
            // VS "[[Floaty]], [[Fixed Direction]]" (Sneaky Heads): keep the line, weave along it.
            Vector3 direction = _flyDirection;
            if (_weave > 0f)
                direction = EnemyChaseState.FloatyDirection(direction, _weave, WeavePeriodSeconds, EnemyController.Now, _controller.FloatyPhase);
            else if (_controller.Movement == SoulHunter.Gameplay.Data.EnemyMovement.FloatyFixedDirection)
            {
                var data = _controller.Data;
                direction = EnemyChaseState.FloatyDirection(direction, data != null ? data.FloatyWeave : 0.7f,
                    data != null ? data.FloatyPeriodSeconds : 1.8f, EnemyController.Now, _controller.FloatyPhase);
            }
            var velocity = direction * (_controller.MoveSpeed * _speedMultiplier);
            velocity.y = _controller.VerticalVelocity;
            _controller.Rigidbody.linearVelocity = velocity;

            if (_controller.HasAnimator) _controller.Animator.UpdateSpeed(1f);
        }

        public void Exit()
        {
        }
    }
}
