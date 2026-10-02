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
        [SerializeField] private float _flashBrightness = 1.8f;

        /// <summary>
        /// Bug MIS B02 (2026-09-30): enemies (and the boss) looked like flat white silhouettes. A 3x flash saturated the model
        /// to pure white, and every hit restarted it, so an enemy under constant fire (Garlic, several weapons) stayed white;
        /// the timer also stood still while the game was paused. Now the flash is 1.8x, runs on real time, and after one
        /// ends the next may start only after this gap, so a much-hit enemy blinks instead of staying white.
        /// </summary>
        public const float FlashGap = 0.25f;

        /// <summary>
        /// Bug MIS v2 C06 (2026-10-02): even after B02, the boss and elites under constant hits read as flat white in 4 of
        /// 8 frames (pale models x 1.8 saturate). The flash is now a short warm tint (at most 1.35x, 0.07 s) with a 0.25 s
        /// gap, so a much-hit body shows its own look at least three quarters of the time.
        /// </summary>
        public const float MaxBrightness = 1.35f;
        public const float MaxDuration = 0.07f;
        public static readonly Color FlashTint = new Color(1f, 0.82f, 0.78f);
        private float _nextFlashAllowed = -1f;

        /// <summary>True while the white hit flash is showing (tests).</summary>
        public bool IsFlashing => _isFlashing;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private MaterialPropertyBlock _propBlock;
        private float _flashTimer;
        private bool _isFlashing;
        private Color? _tint;

        /// <summary>
        /// A lasting tint (VS Defang's green) that the flash returns to instead of the material's own colour; null clears it.
        /// </summary>
        public void SetTint(Color? tint)
        {
            _tint = tint;
            if (!_isFlashing) ApplyRest();
        }

        private void ApplyRest()
        {
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            _propBlock.Clear();
            if (_tint.HasValue)
            {
                _propBlock.SetColor(BaseColorId, _tint.Value);
                _propBlock.SetColor(ColorId, _tint.Value);
            }
            foreach (var target in _renderers)
                if (target != null) target.SetPropertyBlock(_propBlock);
        }

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

        /// <summary>
        /// Re-collects the model renderers after the visual was swapped at runtime (the player's
        /// selected character model replaces the authored one).
        /// </summary>
        public void RefreshRenderers()
        {
            if (_renderer != null) return;
            _isFlashing = false; // old renderers may already be destroyed
            _renderers.Clear();
            foreach (var candidate in GetComponentsInChildren<Renderer>(true))
            {
                if (candidate.enabled && candidate.gameObject.activeInHierarchy && !(candidate is ParticleSystemRenderer))
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
            if (!SoulHunter.Core.Persistence.GameSettings.FlashingVfx) return; // Options: flashing VFX off
            if (_isFlashing || Time.unscaledTime < _nextFlashAllowed) return; // a hit does not stretch or chain the flash
            _flashTimer = Mathf.Min(_flashDuration, MaxDuration);

            _isFlashing = true;
            Color flash = _flashColor * FlashTint * Mathf.Min(_flashBrightness, MaxBrightness);
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

            _flashTimer -= Time.unscaledDeltaTime; // real time: a flash never freezes white during a pause
            if (_flashTimer <= 0f) EndFlash();
        }

        private void EndFlash()
        {
            _isFlashing = false;
            _nextFlashAllowed = Time.unscaledTime + FlashGap;
            // Back to the material's own colour, or to the lasting tint (Defang).
            ApplyRest();
        }
    }
}
