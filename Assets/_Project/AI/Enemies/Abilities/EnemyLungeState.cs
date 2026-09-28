using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// A short dash along a fixed line toward where the player was (VS Orochimario's heads "lunge at the player",
    /// vampire.survivors.wiki/w/Orochimario; The Stalker "will speed up ... appearing to chase the player",
    /// /w/The_Stalker). Afterwards the enemy chases again. A freeze or knockback does not resume the dash.
    /// </summary>
    public class EnemyLungeState : IEnemyState
    {
        private readonly EnemyController _controller;
        private readonly Vector3 _direction;
        private readonly float _speed;
        private readonly float _duration;
        private float _elapsed;

        public Vector3 Direction => _direction;
        public float Speed => _speed;

        public EnemyLungeState(EnemyController controller, Vector3 direction, float speed, float duration)
        {
            _controller = controller;
            direction.y = 0f;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            _speed = Mathf.Max(0f, speed);
            _duration = Mathf.Max(0.05f, duration);
        }

        public void Enter()
        {
            _elapsed = 0f;
            _controller.FaceDirection(_direction);
        }

        public void UpdateLogic()
        {
            _elapsed += Time.deltaTime; // game time: a pause holds the dash
            if (_elapsed >= _duration) _controller.ChangeState(new EnemyChaseState(_controller));
        }

        public void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData)
        {
            if (_controller.Rigidbody == null) return;
            var velocity = _direction * _speed;
            velocity.y = _controller.Rigidbody.linearVelocity.y;
            _controller.Rigidbody.linearVelocity = velocity;
            if (_controller.Animator != null) _controller.Animator.UpdateSpeed(1f);
        }

        public void Exit()
        {
            if (_controller.Rigidbody != null)
                _controller.Rigidbody.linearVelocity = new Vector3(0f, _controller.Rigidbody.linearVelocity.y, 0f);
        }
    }
}
