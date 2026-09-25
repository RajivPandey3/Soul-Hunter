using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// Ground pickups bob and turn (VS items catch the eye). Only the visual child moves; the pickup root stays where
    /// the magnet and pickup code expect it. Each instance starts at a different phase so a gem field shimmers.
    /// </summary>
    public sealed class PickupIdleMotion : MonoBehaviour
    {
        public Transform Visual;
        public float BobHeight = 0.12f;
        public float BobSpeed = 3f;
        public float SpinDegreesPerSecond = 90f;

        private Vector3 _base;
        private float _phase;

        private void Awake()
        {
            if (Visual == null && transform.childCount > 0) Visual = transform.GetChild(transform.childCount - 1);
            if (Visual != null) _base = Visual.localPosition;
        }

        private void OnEnable() => _phase = Random.value * Mathf.PI * 2f;

        private void Update()
        {
            if (Visual == null) return;
            Visual.localPosition = _base + Vector3.up * (Mathf.Sin(Time.time * BobSpeed + _phase) * 0.5f + 0.5f) * BobHeight;
            Visual.Rotate(Vector3.up, SpinDegreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
