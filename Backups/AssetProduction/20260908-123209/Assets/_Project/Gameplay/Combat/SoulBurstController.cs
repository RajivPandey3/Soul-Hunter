using UnityEngine;
using SoulHunter.Core.Events;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// GDD Feature: Soul Burst Ultimate System.
    /// Ye script souls/XP count karti hai. Jab 100 souls poori ho jayen toh Q dabane par
    /// sab dushmano par 9999 damage ka dhamaka (explosion) hota hai.
    /// </summary>
    public class SoulBurstController : MonoBehaviour
    {
        [SerializeField] private int _soulsRequired = 100;
        [SerializeField] private int _damageAmount = 9999;
        [SerializeField] private float _explosionRadius = 30f; // Screen Wipe
        [SerializeField] private LayerMask _enemyLayer;

        private int _currentSouls = 0;
        private bool _isReady = false;
        private EventBus _eventBus;
        
        // Caching
        private Collider[] _hitsBuffer = new Collider[300]; 
        private System.Action<PlayerUltimateEvent> _onUltimateHandler;

        private void Start()
        {
            if (GameServices.Instance != null)
            {
                _eventBus = GameServices.Instance.Get<EventBus>();
                if (_eventBus != null)
                {
                    _onUltimateHandler = OnUltimatePressed;
                    _eventBus.Subscribe(_onUltimateHandler);
                }
            }

            var exp = GetComponent<PlayerExperience>();
            if (exp != null)
            {
                exp.OnSoulCollected += HandleSoulCollected;
            }
        }

        private void OnDestroy()
        {
            if (_eventBus != null && _onUltimateHandler != null)
            {
                _eventBus.Unsubscribe(_onUltimateHandler);
            }

            var exp = GetComponent<PlayerExperience>();
            if (exp != null)
            {
                exp.OnSoulCollected -= HandleSoulCollected;
            }
        }

        private void HandleSoulCollected(int amount)
        {
            if (_isReady) return;

            _currentSouls += amount;
            
            // Check if meter is full
            if (_currentSouls >= _soulsRequired)
            {
                _currentSouls = _soulsRequired;
                _isReady = true;
                Debug.Log("<color=cyan>[Soul Burst] ULTIMATE IS READY! PRESS 'Q' TO UNLEASH SOUL BURST!</color>");
            }
        }

        private void OnUltimatePressed(PlayerUltimateEvent evt)
        {
            if (!_isReady)
            {
                Debug.Log($"[Soul Burst] Not ready yet. Need {_soulsRequired - _currentSouls} more souls.");
                return;
            }

            TriggerSoulBurst();
        }

        private void TriggerSoulBurst()
        {
            Debug.Log("<color=red>[Soul Burst] BOOOOOOM! SCREEN WIPE!</color>");
            
            // Screen Shake / VFX would go here
            if (SoulHunter.Gameplay.VFX.VFXPoolManager.Instance != null)
            {
                SoulHunter.Gameplay.VFX.VFXPoolManager.Instance.PlayChestOpenVFX(transform.position); // Placeholder VFX
            }

            // Zero Garbage SphereCast
            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, _explosionRadius, _hitsBuffer, _enemyLayer);
            
            for (int i = 0; i < hitCount; i++)
            {
                if (_hitsBuffer[i] == null) continue;

                var damageable = _hitsBuffer[i].GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(new DamagePacket(
                        _damageAmount,
                        _hitsBuffer[i].ClosestPoint(transform.position),
                        (_hitsBuffer[i].transform.position - transform.position).normalized * 3f
                    ));
                }
            }

            // Game Feel: Hit-Stop, Invincibility, and Camera Shake!
            if (SoulHunter.Gameplay.VFX.CameraShake.Instance != null)
            {
                SoulHunter.Gameplay.VFX.CameraShake.Instance.TriggerShake(0.5f, 1.5f);
            }
            
            StartCoroutine(SoulBurstJuiceRoutine());

            // Reset meter
            _currentSouls = 0;
            _isReady = false;
        }

        private System.Collections.IEnumerator SoulBurstJuiceRoutine()
        {
            // Player ko 2 seconds ke liye Amar (Invincible) banao
            var playerHealth = GetComponent<HealthController>();
            if (playerHealth != null) playerHealth.IsInvincible = true;

            // Hit-Stop (Slow motion effect for Anime feel)
            Time.timeScale = 0.1f;
            
            // Wait in real time so timescale doesn't freeze the wait itself
            yield return new WaitForSecondsRealtime(0.5f);
            
            Time.timeScale = 1f;

            // Wait remaining 1.5 seconds for invincibility to wear off
            yield return new WaitForSeconds(1.5f);
            if (playerHealth != null) playerHealth.IsInvincible = false;
        }
    }
}
