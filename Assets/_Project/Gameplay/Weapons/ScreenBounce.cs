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
            var camera = Camera.main;
            if (camera == null || _body == null) return;
            Vector3 v = camera.WorldToViewportPoint(transform.position);
            Vector3 velocity = _body.linearVelocity;
            Vector3 right = camera.transform.right; right.y = 0f; right.Normalize();
            Vector3 up = Vector3.ProjectOnPlane(camera.transform.up, Vector3.up).normalized;
            bool bounced = false;
            if ((v.x < 0f && Vector3.Dot(velocity, right) < 0f) || (v.x > 1f && Vector3.Dot(velocity, right) > 0f))
            {
                velocity -= 2f * Vector3.Dot(velocity, right) * right;
                bounced = true;
            }
            if ((v.y < 0f && Vector3.Dot(velocity, up) < 0f) || (v.y > 1f && Vector3.Dot(velocity, up) > 0f))
            {
                velocity -= 2f * Vector3.Dot(velocity, up) * up;
                bounced = true;
            }
            if (!bounced) return;
            _body.linearVelocity = velocity;
            Bounces++;
        }
    }
}
