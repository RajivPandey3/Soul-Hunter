using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.CameraSystem
{
    /// <summary>
    /// Follows the player with a tilted top-down view (owner decision 2026-09-25: ~55 degrees instead of straight
    /// down, so characters' bodies, attack swings and facing are visible). The camera stays locked on the player
    /// as in VS; smoothing is frame-rate independent so it feels the same at 30 or 240 FPS.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public const float DefaultPitch = 55f;

        [SerializeField] private Transform _target;
        [Tooltip("Degrees below the horizon; 90 = straight down.")]
        [SerializeField, Range(30f, 90f)] private float _pitch = DefaultPitch;
        [SerializeField] private float _distance = 30f;
        [Tooltip("Higher = tighter follow. 0 = locked exactly on the player.")]
        [SerializeField] private float _followSharpness = 25f;

        public float Pitch => _pitch;

        private void Start()
        {
            // Learning Comment: Auto-assign player if it was not set in the inspector
            if (_target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                }
            }
            if (_target != null) SnapToTarget();
        }

        public void SnapToTarget()
        {
            transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
            transform.position = DesiredPosition();
        }

        private Vector3 DesiredPosition() => _target.position - transform.rotation * Vector3.forward * _distance;

        private void LateUpdate()
        {
            if (_target == null) return;
            transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
            Vector3 desired = DesiredPosition();
            transform.position = _followSharpness <= 0f
                ? desired
                : Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-_followSharpness * Time.unscaledDeltaTime));
        }
    }
}
