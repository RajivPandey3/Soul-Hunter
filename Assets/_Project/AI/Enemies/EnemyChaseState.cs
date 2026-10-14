using UnityEngine;

namespace SoulHunter.Gameplay.AI
{

    /// <summary>
    /// Learning Comment:
    /// Yeh script EnemyChaseState.cs ke core logic ko handle karti hai.
    /// </summary>
    public class EnemyChaseState : IEnemyState
    {
        private readonly EnemyController _controller;

        public EnemyChaseState(EnemyController controller)
        {
            _controller = controller;
        }

        public void Enter()
        {
        }

        public void UpdateLogic()
        {
            if (!_controller.TryGetTargetPosition(out Vector3 target))
            {
                _controller.ChangeState(new EnemyIdleState(_controller));
                return;
            }

            float sqrDist = (target - _controller.CachedTransform.position).sqrMagnitude;
            
            // If target gets too far, go back to idle (with hysteresis 1.5x)
            if (sqrDist > _controller.ChaseDistance * _controller.ChaseDistance * 1.5f)
            {
                _controller.ChangeState(new EnemyIdleState(_controller));
            }
        }

        public void UpdatePhysics(ref SoulHunter.Gameplay.Physics.EnvironmentData envData)
        {
            // The target's and this enemy's positions are read once each (they were read up to three times a step).
            if (!_controller.TryGetTargetPosition(out Vector3 target)) return;
            Vector3 position = _controller.CachedTransform.position;

            if (_controller.IsKnockedBack)
            {
                // VS knockback: movement reversed, away from the target, at move speed x weapon x knockback taken.
                Vector3 away = position - target;
                away.y = 0f;
                away = away.sqrMagnitude > 0.0001f ? away.normalized : -_controller.CachedTransform.forward;
                Vector3 push = away * (_controller.MoveSpeed * _controller.KnockbackStrength);
                _controller.Rigidbody.linearVelocity = new Vector3(push.x, _controller.VerticalVelocity, push.z);
                return;
            }
            
            Vector3 toTarget = target - position;
            float sqrDist = toTarget.sqrMagnitude;
            
            if (sqrDist > _controller.AttackDistance * _controller.AttackDistance)
            {
                Vector3 direction = toTarget.normalized;
                direction.y = 0; // Keep movement horizontal
                if (_controller.Movement == SoulHunter.Gameplay.Data.EnemyMovement.Floaty) direction = Weave(direction);

                // VS rule: the horde pushes itself apart into a wide crowd (it used to trail the player in single
                // file). A crowded enemy is slowed, never sped past its own move speed.
                Vector3 steering = direction + EnemyCrowd.Separation(_controller) * EnemyCrowd.Strength;
                Vector3 movement = Vector3.ClampMagnitude(steering, 1f) * _controller.MoveSpeed;

                // Adjust for slopes using EnvironmentData (Reusable logic!)
                float yVel = _controller.VerticalVelocity;
                if (envData.IsGrounded)
                {
                    if (envData.GroundAngle > 5f)
                    {
                        movement = Vector3.ProjectOnPlane(movement, envData.GroundNormal);
                        yVel = movement.y;
                    }
                    else
                    {
                        yVel = 0f; // Jitter prevention when overlapping on flat ground
                    }
                }

                _controller.Rigidbody.linearVelocity = new Vector3(movement.x, yVel, movement.z);
                
                // Also stop any crazy spinning caused by collisions (a body with frozen rotation cannot spin)
                if (!_controller.RotationLocked) _controller.Rigidbody.angularVelocity = Vector3.zero;
                
                if (_controller.HasAnimator)
                {
                    _controller.Animator.UpdateSpeed(movement.magnitude);
                }
            }
            else
            {
                // Next to the player: stop, but still make room for the rest of the crowd around it.
                Vector3 spread = EnemyCrowd.Separation(_controller) * (EnemyCrowd.Strength * _controller.MoveSpeed);
                _controller.Rigidbody.linearVelocity = new Vector3(spread.x, _controller.VerticalVelocity, spread.z);
                if (_controller.HasAnimator)
                {
                    _controller.Animator.UpdateSpeed(0f);
                }
            }
        }

        public void Exit()
        {
        }

        /// <summary>VS "Floaty": home on the player while weaving side to side.</summary>
        private Vector3 Weave(Vector3 direction)
        {
            var data = _controller.Data;
            float weave = data != null ? data.FloatyWeave : 0.7f;
            float period = data != null ? Mathf.Max(0.2f, data.FloatyPeriodSeconds) : 1.8f;
            return FloatyDirection(direction, weave, period, EnemyController.Now, _controller.FloatyPhase);
        }

        /// <summary>Homing direction bent sideways by weave x sin(2 pi time / period + phase), normalised, on the ground.</summary>
        public static Vector3 FloatyDirection(Vector3 homing, float weave, float periodSeconds, float time, float phase)
        {
            homing.y = 0f;
            if (homing.sqrMagnitude < 0.0001f) return homing;
            homing.Normalize();
            Vector3 side = new Vector3(-homing.z, 0f, homing.x);
            float swing = Mathf.Sin(time * 2f * Mathf.PI / Mathf.Max(0.2f, periodSeconds) + phase) * weave;
            return (homing + side * swing).normalized;
        }
    }
}
