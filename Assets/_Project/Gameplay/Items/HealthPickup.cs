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
        [SerializeField] private float _magnetRadius = 3f;
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
                float pickupRadius = _magnetRadius * (_playerStats != null ? Mathf.Max(0.1f, _playerStats.Magnet) : 1f);
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

        public void ResetPickup()
        {
            _isMagnetized = false;
            _targetPlayer = null;
        }

        private void Collect(Transform playerTransform)
        {
            var health = playerTransform.GetComponentInParent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                health.Heal(HealAmount);
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
