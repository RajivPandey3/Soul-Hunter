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

        private readonly Collider[] _playerHits = new Collider[1];

        private void Update()
        {
            if (_isCollected) return;

            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, _pickupRadius, _playerHits, _playerLayer);
            if (hitCount > 0)
            {
                Collect(_playerHits[0].transform);
            }
        }

        private void Collect(Transform playerTransform)
        {
            _isCollected = true;

            if (SoulHunter.Gameplay.VFX.VFXPoolManager.Instance != null)
            {
                SoulHunter.Gameplay.VFX.VFXPoolManager.Instance.PlayChestOpenVFX(transform.position);
            }

            // Chest UI aur Backend Logic Trigger karo
            string rewardMessage = "";
            var logic = FindFirstObjectByType<SoulHunter.Gameplay.Core.ChestLogicController>();
            if (logic != null)
            {
                rewardMessage = logic.ProcessChest(); // UI bypass / Backend processing
            }
            else
            {
                Debug.LogWarning("[ChestPickup] ChestLogicController nahi mila!");
            }

            // UI call
            var chestUI = FindFirstObjectByType<SoulHunter.Gameplay.UI.ChestUI>();
            if (chestUI != null)
            {
                chestUI.OpenChest(rewardMessage);
            }

            gameObject.SetActive(false);
        }

        public void ResetPickup()
        {
            _isCollected = false;
        }
    }
}
