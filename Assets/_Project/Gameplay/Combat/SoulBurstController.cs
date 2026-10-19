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
            // Player.prefab and Main_Gameplay ship with an empty mask, so the burst found nothing and never hurt an
            // enemy (found by SpectralStagePlayModeTests, 2026-09-27). Default to the Enemy layer, as WhipWeapon does.
            if (_enemyLayer.value == 0) _enemyLayer = LayerMask.GetMask("Enemy");

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
            // A menu that pauses the game (level-up, chest, pause, shop) shows over the run; the burst went off behind it,
            // dealing its damage while the game stood still (Bug MIS CORE-05, 2026-10-19). Ignored while paused, as the
            // dash is (PlayerController.OnDashEvent, 2026-10-18); the meter stays full.
            if (GameTime.IsPaused) return;
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
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, transform.position);

            // Zero Garbage SphereCast
            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, _explosionRadius, _hitsBuffer, _enemyLayer);
            
            for (int i = 0; i < hitCount; i++)
            {
                if (_hitsBuffer[i] == null) continue;

                var damageable = DamageableLookup.Of(_hitsBuffer[i]);
                if (damageable != null)
                {
                    using (DamageSource.From(null, "Soul Burst")) // VS Results lists every damage source
                    damageable.TakeDamage(new DamagePacket(
                        _damageAmount,
                        _hitsBuffer[i].ClosestPoint(transform.position),
                        (_hitsBuffer[i].transform.position - transform.position).normalized * 3f,
                        DamageType.Spectral // owner (2026-09-26, Level 7): the ultimate always reaches ghosts
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
            // Its own count (CMB-10): a dash ending inside these 2 s no longer ends it, and it never clears god mode.
            if (playerHealth != null) playerHealth.BeginAbilityImmunity();

            // Hit-Stop (Slow motion effect for Anime feel)
            SoulHunter.Core.Services.GameTime.SetSlowMotion(0.1f);
            
            // Wait in real time so timescale doesn't freeze the wait itself
            yield return new WaitForSecondsRealtime(0.5f);
            
            SoulHunter.Core.Services.GameTime.SetSlowMotion(1f);

            // Wait remaining 1.5 seconds for invincibility to wear off
            yield return new WaitForSeconds(1.5f);
            if (playerHealth != null) playerHealth.EndAbilityImmunity();
        }
    }
}
