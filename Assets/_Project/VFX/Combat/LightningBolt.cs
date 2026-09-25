using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>A lightning strike: a jagged bolt from the sky to the target, re-drawn each time it is used.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LightningBolt : MonoBehaviour
    {
        public float Height = 9f;
        public float Jitter = 0.45f;
        private LineRenderer _line;

        private void OnEnable()
        {
            if (_line == null) _line = GetComponent<LineRenderer>();
            int count = _line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                var offset = (i == 0 || i == count - 1) ? Vector2.zero : Random.insideUnitCircle * Jitter;
                _line.SetPosition(i, new Vector3(offset.x, Height * (1f - t), offset.y));
            }
        }
    }
}
