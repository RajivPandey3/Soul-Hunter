using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// Boss ke marne par Chest (Khazana) nikalta hai.
    /// Jab player isey chhuta hai, toh ye ChestUI ko trigger karta hai.
    /// </summary>
    public class ChestPickup : MonoBehaviour
    {
        [SerializeField] private float _pickupRadius = 1.5f;
        [SerializeField] private LayerMask _playerLayer;
        private bool _isCollected = false;

        /// <summary>VS: only chests from bosses at 10:00 or later (and the 1:00 chest) can evolve a weapon. Set when dropped.</summary>
        public bool CanEvolve = true;

        /// <summary>VS Arcana mode: an Arcana chest (11:00 and 21:00) gives an Arcana choice instead of items.</summary>
        public bool IsArcana;

        private readonly Collider[] _playerHits = new Collider[1];

        private void Update()
        {
            if (_isCollected || !isActiveAndEnabled || Time.timeScale <= 0f) return;

            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, _pickupRadius, _playerHits, _playerLayer);
            if (hitCount > 0)
            {
                Collect(_playerHits[0].transform);
            }
        }

        private SoulHunter.Gameplay.Core.ChestLogicController _logicController;
        private SoulHunter.Gameplay.UI.ChestUI _chestUI;

        public void Initialize(SoulHunter.Gameplay.Core.ChestLogicController logicController, SoulHunter.Gameplay.UI.ChestUI chestUI)
        {
            _logicController = logicController;
            _chestUI = chestUI;
        }

        private void Collect(Transform playerTransform)
        {
            if (_isCollected || !isActiveAndEnabled || Time.timeScale <= 0f) return;
            _isCollected = true;

            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.ChestOpen, transform.position);

            // VS (wiki Treasure_Chest, Patch 0.2.4): "a brief moment of invulnerability after picking up a treasure"; 240 ms
            // (owner, 2026-10-01). Game time stands still while the chest screen is open, so it covers the resume.
            var pickerHealth = playerTransform != null ? playerTransform.GetComponentInParent<SoulHunter.Gameplay.Combat.HealthController>() : null;
            if (pickerHealth != null) pickerHealth.GrantInvulnerability(SoulHunter.Gameplay.Combat.HealthController.VsInvulnerabilityTime);

            // Chest UI aur Backend Logic Trigger karo
            string rewardMessage = "";
            if (_logicController != null)
            {
                // VS Arcana mode: an Arcana chest gives an Arcana instead of items.
                rewardMessage = IsArcana
                    ? _logicController.ProcessArcanaChest(CanEvolve)
                    : _logicController.ProcessChest(CanEvolve); // UI bypass / Backend processing
            }
            else
            {
                Debug.LogWarning("[ChestPickup] ChestLogicController nahi mila!");
            }

            // UI call
            if (_chestUI != null)
            {
                _chestUI.OpenChest(rewardMessage, _logicController != null ? _logicController.LastResult : null);
            }

            gameObject.SetActive(false);
        }

        public void ResetPickup()
        {
            _isCollected = false;
            CanEvolve = true;
            IsArcana = false;
        }
    }
}
