using UnityEngine;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// O(1) Performance Optimized XP Gem (Soul). 
    /// Ye ab apne aap ko magnetize nahi karta (issey CPU cycles bachti hain).
    /// Jab player pass aata hai, toh player isey magnetize karta hai.
    /// </summary>
    public class XPGem : MonoBehaviour
    {
        public static readonly List<XPGem> ActiveGems = new List<XPGem>();
        public int XPValue = 10;
        [SerializeField] private float _flySpeed = 15f;

        private Transform _targetPlayer;
        private bool _isMagnetized = false;

        private void Update()
        {
            if (_isMagnetized && _targetPlayer != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, _targetPlayer.position, _flySpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, _targetPlayer.position) < 0.5f)
                {
                    Collect(_targetPlayer);
                }
            }
        }

        private float _spawnTime;

        private void OnEnable()
        {
            if (!ActiveGems.Contains(this)) ActiveGems.Add(this);
            _spawnTime = Time.time;
        }

        private void OnDisable() { ActiveGems.Remove(this); }

        public void Magnetize(Transform playerTransform)
        {
            // 0.5 second tak magnetize nahi ho sakta taake player usko girta hua dekh sake
            if (!_isMagnetized && Time.time >= _spawnTime + 0.5f)
            {
                _targetPlayer = playerTransform;
                _isMagnetized = true;
            }
        }

        public void ResetPickup()
        {
            _isMagnetized = false;
            _targetPlayer = null;
        }

        private void Collect(Transform playerTransform)
        {
            var playerExperience = playerTransform.GetComponentInParent<SoulHunter.Gameplay.Player.PlayerExperience>();
            if (playerExperience != null)
            {
                playerExperience.AddXP(XPValue);
            }

            if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
            {
                SoulHunter.Gameplay.Audio.AudioManager.Instance.PlaySFX(SoulHunter.Gameplay.Audio.AudioManager.Instance.GemPickupSound);
            }
            
            gameObject.SetActive(false); // AutoReturnToPool will push it to stack automatically
        }
    }
}
