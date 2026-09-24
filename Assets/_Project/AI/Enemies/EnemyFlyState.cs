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

        private readonly EnemyController _controller;
        private readonly Vector3 _flyDirection;
        private readonly float _lifetime;
        private float _aliveTimer;

        public Vector3 Direction => _flyDirection;

        public EnemyFlyState(EnemyController controller, Vector3 direction, float lifetime = DefaultLifetimeSeconds)
        {
            _controller = controller;
            direction.y = 0f;
            _flyDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            _lifetime = lifetime;
        }

        public void Enter()
        {
            _aliveTimer = 0f;
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
            var velocity = _flyDirection * (_controller.MoveSpeed * SpeedMultiplier);
            velocity.y = _controller.Rigidbody.linearVelocity.y;
            _controller.Rigidbody.linearVelocity = velocity;

            if (_controller.Animator != null) _controller.Animator.UpdateSpeed(1f);
        }

        public void Exit()
        {
        }
    }
}
