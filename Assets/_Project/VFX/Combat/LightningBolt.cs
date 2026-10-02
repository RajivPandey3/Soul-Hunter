using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// A lightning strike: a jagged bolt from the sky to the target, re-drawn each time it is used. The strike prefab is
    /// scaled by the weapon's Area (its hit zone), but the bolt keeps its own world size, and it fades out after the
    /// flash (Bug MIS v2 C04: Area-scaled, never-fading bolts read as thick white zigzags across the screen).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LightningBolt : MonoBehaviour
    {
        public float Height = 9f;
        public float Jitter = 0.45f;
        /// <summary>Seconds the bolt takes to fade out.</summary>
        public float FadeSeconds = 0.25f;
        private LineRenderer _line;
        private Color _start, _end;
        private float _age;

        private void OnEnable()
        {
            if (_line == null)
            {
                _line = GetComponent<LineRenderer>();
                _start = _line.startColor;
                _end = _line.endColor;
            }
            Vector3 scale = transform.lossyScale;
            float sx = Mathf.Max(0.001f, Mathf.Abs(scale.x)), sy = Mathf.Max(0.001f, Mathf.Abs(scale.y)), sz = Mathf.Max(0.001f, Mathf.Abs(scale.z));
            int count = _line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                var offset = (i == 0 || i == count - 1) ? Vector2.zero : Random.insideUnitCircle * Jitter;
                _line.SetPosition(i, new Vector3(offset.x / sx, Height * (1f - t) / sy, offset.y / sz));
            }
            _age = 0f;
            Apply(1f);
        }

        private void Update()
        {
            if (FadeSeconds <= 0f) return;
            _age += Time.deltaTime;
            Apply(1f - Mathf.Clamp01(_age / FadeSeconds));
        }

        private void Apply(float alpha)
        {
            _line.startColor = new Color(_start.r, _start.g, _start.b, _start.a * alpha);
            _line.endColor = new Color(_end.r, _end.g, _end.b, _end.a * alpha);
        }

        /// <summary>The bolt's current opacity (tests).</summary>
        public float Alpha => _line != null ? _line.endColor.a : 0f;
    }
}
