using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// Ground pickups bob and turn (VS items catch the eye). Only the visual child moves; the pickup root stays where
    /// the magnet and pickup code expect it. Each instance starts at a different phase so a gem field shimmers.
    /// Heavy-kill pass (2026-09-29): one PickupIdleMotionRunner moves every pickup in a single loop. With 850 pickups on
    /// the ground, 850 separate Update calls cost about 1.2 ms a frame.
    /// </summary>
    public sealed class PickupIdleMotion : MonoBehaviour
    {
        public Transform Visual;
        public float BobHeight = 0.12f;
        public float BobSpeed = 3f;
        public float SpinDegreesPerSecond = 90f;

        private Vector3 _base;
        private float _phase;
        internal int RunnerIndex = -1;
        // 60 FPS pass (2026-10-14): the root and the visual's parent rotation are read once per life, so a step is
        // three engine calls (root position, visual rotation, one write) where it was about seven.
        internal Transform Root;
        private Quaternion _parentRotation = Quaternion.identity, _parentInverse = Quaternion.identity;
        private bool _parentTurned;

        private void Awake()
        {
            if (Visual == null && transform.childCount > 0) Visual = transform.GetChild(transform.childCount - 1);
            if (Visual != null) _base = Visual.localPosition;
        }

        private void OnEnable()
        {
            _phase = Random.value * Mathf.PI * 2f;
            Root = transform;
            var parent = Visual != null ? Visual.parent : null;
            _parentRotation = parent != null ? parent.rotation : Quaternion.identity;
            _parentInverse = Quaternion.Inverse(_parentRotation);
            _parentTurned = Quaternion.Dot(_parentRotation, Quaternion.identity) < 0.99999f;
            PickupIdleMotionRunner.Add(this);
        }

        private void OnDisable() => PickupIdleMotionRunner.Remove(this);

        internal void Step(float time, float deltaTime)
        {
            if (Visual == null) return;
            Vector3 position = _base;
            position.y += (Mathf.Sin(time * BobSpeed + _phase) * 0.5f + 0.5f) * BobHeight;
            // A turn about the world's up axis, as Rotate(up, degrees, Space.World), worked out without engine calls.
            float half = SpinDegreesPerSecond * deltaTime * Mathf.Deg2Rad * 0.5f;
            var turn = new Quaternion(0f, Mathf.Sin(half), 0f, Mathf.Cos(half));
            Quaternion local = Visual.localRotation;
            local = _parentTurned ? _parentInverse * turn * _parentRotation * local : turn * local;
            local.Normalize(); // many small turns in a row must not let the rotation drift
            Visual.SetLocalPositionAndRotation(position, local);
        }
    }

    /// <summary>Moves every enabled PickupIdleMotion once a frame (created on first use, one per loaded game).</summary>
    public sealed class PickupIdleMotionRunner : MonoBehaviour
    {
        private static readonly List<PickupIdleMotion> Motions = new List<PickupIdleMotion>();
        private static PickupIdleMotionRunner _instance;

        /// <summary>Pickups being moved now (tests).</summary>
        public static int Count => Motions.Count;

        internal static void Add(PickupIdleMotion motion)
        {
            // The stored index is only trusted when the list agrees: a pooled copy can carry its template's index.
            int listed = motion.RunnerIndex;
            if (listed >= 0 && listed < Motions.Count && Motions[listed] == motion) return;
            motion.RunnerIndex = Motions.Count;
            Motions.Add(motion);
            if (_instance == null && Application.isPlaying)
            {
                var go = new GameObject("PickupIdleMotionRunner") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<PickupIdleMotionRunner>();
            }
        }

        internal static void Remove(PickupIdleMotion motion)
        {
            int i = motion.RunnerIndex;
            if (i < 0 || i >= Motions.Count || Motions[i] != motion) { motion.RunnerIndex = -1; return; }
            int last = Motions.Count - 1;
            Motions[i] = Motions[last];
            Motions[i].RunnerIndex = i;
            Motions.RemoveAt(last);
            motion.RunnerIndex = -1;
        }

        /// <summary>Pickups further than this from the hero are off screen and are not moved (the camera shows about 22 x 12 units).</summary>
        public const float ActiveRadius = 30f;

        private void Update()
        {
            float time = Time.time, deltaTime = Time.deltaTime;
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            bool near = player != null;
            Vector3 center = near ? player.transform.position : Vector3.zero;
            const float radiusSqr = ActiveRadius * ActiveRadius;
            for (int i = Motions.Count - 1; i >= 0; i--)
            {
                var motion = Motions[i];
                if (motion == null)
                {
                    // Destroyed without OnDisable (never expected): swap-remove so the others keep their indices.
                    int last = Motions.Count - 1;
                    Motions[i] = Motions[last];
                    if (Motions[i] != null) Motions[i].RunnerIndex = i;
                    Motions.RemoveAt(last);
                    continue;
                }
                if (near && (motion.Root.position - center).sqrMagnitude > radiusSqr) continue;
                motion.Step(time, deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // Domain reload may be off, so pickups from the last play session must not stay listed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Motions.Clear();
            _instance = null;
        }
    }
}
