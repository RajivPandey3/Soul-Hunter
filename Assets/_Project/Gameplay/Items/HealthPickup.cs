using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Khazana (Floor Chicken).
    /// Ye zameen par padta hai aur player ke chhoone (magnetize) hone par uski health badhata hai.
    /// </summary>
    public class HealthPickup : MonoBehaviour
    {
        public int HealAmount = 30;
        [SerializeField] private float _magnetRadius = 3f;
        [SerializeField] private float _flySpeed = 15f;
        [SerializeField] private LayerMask _playerLayer;

        private Transform _targetPlayer;
        private Transform _playerTransform;
        private bool _isMagnetized = false;

        private readonly Collider[] _playerHits = new Collider[1];

        public void Initialize(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        private void Update()
        {
            if (!_isMagnetized)
            {
                // Player ko dhoondho
                var playerStats = _playerTransform != null
                    ? _playerTransform.GetComponent<SoulHunter.Gameplay.Player.PlayerStats>()
                    : null;
                float pickupRadius = _magnetRadius * (playerStats != null ? Mathf.Max(0.1f, playerStats.Magnet) : 1f);
                int hitCount = _playerLayer.value != 0
                    ? UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, pickupRadius, _playerHits, _playerLayer)
                    : 0;
                if (hitCount > 0 && _playerHits[0] != null &&
                    _playerHits[0].GetComponentInParent<SoulHunter.Gameplay.Player.PlayerController>() != null)
                {
                    _targetPlayer = _playerHits[0].transform;
                    _isMagnetized = true;
                }
                else if (_playerTransform != null && Vector3.Distance(transform.position, _playerTransform.position) <= pickupRadius)
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

            gameObject.SetActive(false);
        }
    }
}
