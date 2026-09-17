using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// Learning Comment:
    /// Dushman ko hit par White Flash karne ke liye script.
    /// MaterialPropertyBlock use kiya hai taake hazaron dushman flash hon tab bhi
    /// Garbage Collector (GC) call na ho aur game 60 FPS par chale.
    /// </summary>
    [RequireComponent(typeof(HealthController))]
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _flashDuration = 0.1f;

        private MaterialPropertyBlock _propBlock;
        private int _colorPropertyID;
        private Color _originalColor = Color.white;
        private float _flashTimer = 0f;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            
            _propBlock = new MaterialPropertyBlock();
            // Standard Unity shader color property is _Color or _BaseColor
            _colorPropertyID = Shader.PropertyToID("_Color"); 
            
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                // Agar material ka color read ho sake (Note: Sometimes requires material setup)
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

        private void TriggerFlash()
        {
            _flashTimer = _flashDuration;
        }

        private void Update()
        {
            if (_renderer == null) return;

            if (_flashTimer > 0)
            {
                _flashTimer -= Time.deltaTime;
                
                // Flash it
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(_colorPropertyID, _flashColor);
                _renderer.SetPropertyBlock(_propBlock);
            }
            else if (_flashTimer > -1f) // Just a flag to revert once
            {
                _flashTimer = -2f; // mark as done
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(_colorPropertyID, _originalColor);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }
    }
}
