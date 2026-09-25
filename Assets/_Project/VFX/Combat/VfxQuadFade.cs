using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// A one-shot sprite on a quad (the Whip's slash arc): it grows a little and fades out, then hides itself so the
    /// VFX pool can reuse it. Uses a property block, so the shared material is never changed.
    /// </summary>
    public sealed class VfxQuadFade : MonoBehaviour
    {
        public float Duration = 0.22f;
        public float StartScale = 0.85f;
        public float EndScale = 1.1f;
        public Color Tint = Color.white;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private Vector3 _baseScale;
        private Vector3 _rendererScale = Vector3.one;
        private float _started;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
            if (_renderer != null) _rendererScale = _renderer.transform.localScale;
        }

        private void OnEnable()
        {
            _started = Time.time;
            _baseScale = transform.localScale;
            Apply(0f);
        }

        private void Update()
        {
            float t = (Time.time - _started) / Mathf.Max(0.01f, Duration);
            if (t >= 1f) { gameObject.SetActive(false); return; }
            Apply(t);
        }

        private void Apply(float t)
        {
            float s = Mathf.Lerp(StartScale, EndScale, Mathf.Sqrt(t));
            if (_renderer != null)
            {
                _renderer.transform.localScale = _rendererScale * s;
                var c = Tint;
                c.a *= 1f - t * t;
                _block.SetColor(ColorId, c);
                _renderer.SetPropertyBlock(_block);
            }
        }
    }
}
