using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// A projectile that bounces off the edges of the screen (VS NO FUTURE: "bounces off the edge of the screen or
    /// obstacles"). Each physics step, a part of the velocity that carries it out of the camera's view is reversed.
    /// </summary>
    public sealed class ScreenBounce : MonoBehaviour
    {
        private Rigidbody _body;

        /// <summary>Bounces so far (tests).</summary>
        public int Bounces { get; private set; }

        private void OnEnable()
        {
            _body = GetComponent<Rigidbody>();
            Bounces = 0;
        }

        // A pooled projectile reused by another weapon must not bounce: off until a weapon turns it on again.
        private void OnDisable() => enabled = false;

        private void FixedUpdate()
        {
            if (_body == null) return;
            Vector3 velocity = _body.linearVelocity;
            if (!ScreenEdge.Reflect(transform.position, ref velocity)) return;
            _body.linearVelocity = velocity;
            Bounces++;
        }
    }

    /// <summary>The screen-edge bounce shared by projectiles that bounce off the edges of the view.</summary>
    public static class ScreenEdge
    {
        /// <summary>
        /// When <paramref name="position"/> is outside the main camera's view and <paramref name="motion"/> carries it
        /// further out, reverses the outward part of the motion (on the ground plane) and returns true.
        /// </summary>
        public static bool Reflect(Vector3 position, ref Vector3 motion)
        {
            var camera = Camera.main;
            if (camera == null) return false;
            Vector3 v = camera.WorldToViewportPoint(position);
            Vector3 right = camera.transform.right; right.y = 0f; right.Normalize();
            Vector3 up = Vector3.ProjectOnPlane(camera.transform.up, Vector3.up).normalized;
            bool bounced = false;
            if ((v.x < 0f && Vector3.Dot(motion, right) < 0f) || (v.x > 1f && Vector3.Dot(motion, right) > 0f))
            {
                motion -= 2f * Vector3.Dot(motion, right) * right;
                bounced = true;
            }
            if ((v.y < 0f && Vector3.Dot(motion, up) < 0f) || (v.y > 1f && Vector3.Dot(motion, up) > 0f))
            {
                motion -= 2f * Vector3.Dot(motion, up) * up;
                bounced = true;
            }
            return bounced;
        }
    }
}
