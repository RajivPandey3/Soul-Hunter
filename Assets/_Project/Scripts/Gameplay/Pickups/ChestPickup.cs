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

        private void Update()
        {
            if (_isCollected) return;

            Collider[] hits = UnityEngine.Physics.OverlapSphere(transform.position, _pickupRadius, _playerLayer);
            if (hits.Length > 0)
            {
                Collect(hits[0].transform);
            }
        }

        private void Collect(Transform playerTransform)
        {
            _isCollected = true;
            
            if (SoulHunter.Gameplay.VFX.VFXPoolManager.Instance != null)
            {
                SoulHunter.Gameplay.VFX.VFXPoolManager.Instance.PlayChestOpenVFX(transform.position);
            }
            
            // Chest UI Trigger karo
            var chestUI = FindFirstObjectByType<SoulHunter.Gameplay.UI.ChestUI>();
            if (chestUI != null)
            {
                chestUI.OpenChest();
            }
            else
            {
                Debug.LogWarning("ChestUI nahi mili scene mein!");
            }
            
            gameObject.SetActive(false);
        }

        public void ResetPickup()
        {
            _isCollected = false;
        }
    }
}
