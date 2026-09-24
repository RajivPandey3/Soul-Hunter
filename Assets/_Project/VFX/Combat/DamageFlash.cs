using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// Learning Comment:
    /// Dushman ko hit par White Flash karne ke liye script.
    /// MaterialPropertyBlock use kiya hai taake hazaron dushman flash hon tab bhi
    /// Garbage Collector (GC) call na ho aur game 60 FPS par chale.
    /// URP Lit colour ko texture se multiply karta hai, isliye white tint se kuch nahi badalta;
    /// flash ke liye tint ko 1 se upar le jaate hain (model white ki taraf chamakta hai).
    /// </summary>
    [RequireComponent(typeof(HealthController))]
    public class DamageFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit
        private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in / legacy shaders

        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _flashDuration = 0.1f;
        [Tooltip("Tint multiplier during the flash. Values above 1 brighten the textured model toward white.")]
        [SerializeField] private float _flashBrightness = 3f;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private MaterialPropertyBlock _propBlock;
        private float _flashTimer;
        private bool _isFlashing;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();

            if (_renderer != null)
            {
                _renderers.Add(_renderer);
                return;
            }

            // Gameplay roots carry a disabled, mesh-less MeshRenderer; flash the
            // visible model renderers under SH10_Visual instead.
            foreach (var candidate in GetComponentsInChildren<Renderer>(true))
            {
                if (candidate.enabled && !(candidate is ParticleSystemRenderer))
                    _renderers.Add(candidate);
            }
        }

        private void Start()
        {
            var health = GetComponent<HealthController>();
            if (health != null)
            {
                health.OnDamaged += TriggerFlash;
            }
        }

        private void OnDestroy()
        {
            var health = GetComponent<HealthController>();
            if (health != null)
            {
                health.OnDamaged -= TriggerFlash;
            }
        }

        private void OnDisable()
        {
            // Pooled enemies must not come back still flashing.
            if (_isFlashing) EndFlash();
        }

        private void TriggerFlash()
        {
            _flashTimer = _flashDuration;
            if (_isFlashing) return;

            _isFlashing = true;
            Color flash = _flashColor * _flashBrightness;
            flash.a = _flashColor.a;
            foreach (var target in _renderers)
            {
                if (target == null) continue;
                target.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, flash);
                _propBlock.SetColor(ColorId, flash);
                target.SetPropertyBlock(_propBlock);
            }
        }

        private void Update()
        {
            if (!_isFlashing) return;

            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f) EndFlash();
        }

        private void EndFlash()
        {
            _isFlashing = false;
            // Clearing the block restores each material's own colour.
            _propBlock.Clear();
            foreach (var target in _renderers)
            {
                if (target != null) target.SetPropertyBlock(_propBlock);
            }
        }
    }
}
