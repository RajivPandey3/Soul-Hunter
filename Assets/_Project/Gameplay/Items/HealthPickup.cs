using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Khazana (Floor Chicken).
    /// Ye zameen par padta hai aur player ke chhoone (magnetize) hone par uski health badhata hai.
    /// </summary>
    public class HealthPickup : MonoBehaviour, ITickedPickup
    {
        public int HealAmount = 30;
        /// <summary>How close the hero must come to a stage-placed chicken (the walk-over distance of every Pickup).</summary>
        public const float StageItemTouchRadius = 1.5f;
        [SerializeField] private float _flySpeed = 15f;
        [SerializeField] private LayerMask _playerLayer;

        private Transform _targetPlayer;
        private Transform _playerTransform;
        private bool _isMagnetized = false;

        private SoulHunter.Gameplay.Player.PlayerStats _playerStats;

        public void Initialize(Transform playerTransform)
        {
            _playerTransform = playerTransform;
            _playerStats = null;
        }

        public int TickerIndex { get; set; } = -1;

        // Heavy-kill pass (2026-09-29): PickupTicker calls Tick for every pickup in one loop instead of an Update each.
        private void OnEnable() => PickupTicker.Add(this);
        private void OnDisable() => PickupTicker.Remove(this);

        public void Tick()
        {
            if (!_isMagnetized)
            {
                // Player ko dhoondho. Heavy-kill pass: no physics query or component lookup per chicken per frame.
                if (_playerStats == null && _playerTransform != null) _playerStats = _playerTransform.GetComponent<SoulHunter.Gameplay.Player.PlayerStats>();
                // VS wiki (Magnet, Attractorb): one Magnet radius for every pickup, the Floor Chicken included. A chicken the
                // stage placed is a stage item: only walking over it picks it up (wiki Mad_Groove_(VIII): "the only way").
                float pickupRadius = TryGetComponent<StageItemMark>(out _) ? StageItemTouchRadius
                    : _playerStats != null ? _playerStats.MagnetRadius : SoulHunter.Gameplay.Player.PlayerStats.BaseMagnetRadius;
                var hit = PlayerReach.Within(transform.position, pickupRadius);
                if (hit != null)
                {
                    _targetPlayer = hit.transform;
                    _isMagnetized = true;
                }
                else if (_playerTransform != null && (transform.position - _playerTransform.position).sqrMagnitude <= pickupRadius * pickupRadius)
                {
                    // Layer mask galat/old prefab par ho tab bhi health pickup
                    // Injected player reference se reliably magnetize ho.
                    _targetPlayer = _playerTransform;
                    _isMagnetized = true;
                }
            }
            else if (_targetPlayer != null)
            {
                // Player ki taraf udo
                transform.position = Vector3.MoveTowards(transform.position, _targetPlayer.position, _flySpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, _targetPlayer.position) < 0.5f)
                {
                    Collect(_targetPlayer);
                }
            }
        }

        /// <summary>
        /// VS wiki (Recovery): "Recovery also boosts healing from pickups, each 0.1 translates to an additive 10% boost":
        /// the heal is multiplied by 1 + Recovery (Soul Hunter's Regen, HP per second).
        /// </summary>
        public static int HealWithRecovery(int amount, float recovery) => Mathf.RoundToInt(amount * (1f + Mathf.Max(0f, recovery)));

        public void ResetPickup()
        {
            _isMagnetized = false;
            _targetPlayer = null;
        }

        private void Collect(Transform playerTransform)
        {
            if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null) SoulHunter.Gameplay.Core.RunStatsTracker.Instance.RecordPickup(PickupNames.Chicken);
            var health = playerTransform.GetComponentInParent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                var stats = playerTransform.GetComponentInParent<SoulHunter.Gameplay.Player.PlayerStats>();
                health.Heal(HealWithRecovery(HealAmount, stats != null ? stats.Regen : 0f));
                // VS: a Floor Chicken is a source of immunity (wiki Invulnerability_Time); 240 ms (owner, 2026-10-01).
                health.GrantInvulnerability(SoulHunter.Gameplay.Combat.HealthController.VsInvulnerabilityTime);
            }

            if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
            {
                SoulHunter.Gameplay.Audio.AudioManager.Instance.PlaySFX(SoulHunter.Gameplay.Audio.AudioManager.Instance.HealSound);
            }
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Heal, playerTransform.position);

            gameObject.SetActive(false);
        }
    }
}
