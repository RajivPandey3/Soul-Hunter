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
        public bool IsMagnetized => _isMagnetized;

        private void Update()
        {
            if (_isShot)
            {
                _shotLeft -= Time.deltaTime;
                if (SoulHunter.Gameplay.Core.ArcanaManager.TickGemShot(transform, _shotLeft)) { _isShot = false; gameObject.SetActive(false); }
                return;
            }
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
            _isShot = false;
            if (!ActiveGems.Contains(this)) ActiveGems.Add(this);
            _spawnTime = Time.time;
        }

        private void OnDisable() { ActiveGems.Remove(this); }

        public void Magnetize(Transform playerTransform)
        {
            // 0.5 second tak magnetize nahi ho sakta taake player usko girta hua dekh sake
            if (!_isMagnetized && !_isShot && Time.time >= _spawnTime + 0.5f)
            {
                _targetPlayer = playerTransform;
                _isMagnetized = true;
            }
        }

        public void ResetPickup()
        {
            _isShot = false;
            _isMagnetized = false;
            _targetPlayer = null;
        }

        // VS Game Killer (0) (wiki Game_Killer_(0), raw, fetched 2026-10-09): "all experience gained from Experience Gems
        // is halted, and instead, Experience Gems act as exploding projectiles that hurl themselves towards the closest
        // enemy when collected." The gem itself flies on (ArcanaManager.TickGemShot) and explodes, then goes back to its pool.
        private bool _isShot;
        private float _shotLeft;
        /// <summary>True while this gem flies as a Soul Barrage (Game Killer) shot.</summary>
        public bool IsShot => _isShot;

        private void Collect(Transform playerTransform)
        {
            var playerExperience = playerTransform.GetComponentInParent<SoulHunter.Gameplay.Player.PlayerExperience>();
            if (SoulHunter.Gameplay.Core.ArcanaManager.GameKillerActive)
            {
                _isShot = true;
                _isMagnetized = false;
                _targetPlayer = null;
                _shotLeft = SoulHunter.Gameplay.Core.ArcanaManager.GameKillerFlightSeconds;
                SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.GemPickup, playerTransform.position + Vector3.up * 0.8f);
                return;
            }
            if (playerExperience != null)
            {
                playerExperience.AddXP(XPValue);
            }

            if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
            {
                SoulHunter.Gameplay.Audio.AudioManager.Instance.PlaySFX(SoulHunter.Gameplay.Audio.AudioManager.Instance.GemPickupSound);
            }
            
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.GemPickup, playerTransform.position + Vector3.up * 0.8f);
            gameObject.SetActive(false); // AutoReturnToPool will push it to stack automatically
        }
    }
}
