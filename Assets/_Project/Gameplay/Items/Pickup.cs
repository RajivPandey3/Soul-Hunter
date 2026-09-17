using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// Base class for all ground pickups (Gold, Magnet, TimeFreeze, etc).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class Pickup : MonoBehaviour
    {
        [SerializeField] protected float _pickupRadius = 1.5f;
        [SerializeField] protected LayerMask _playerLayer;

        protected bool _isCollected = false;

        private readonly Collider[] _playerHits = new Collider[1];

        protected virtual void Update()
        {
            if (_isCollected) return;

            if (_playerLayer.value == 0)
                _playerLayer = LayerMask.GetMask("Player");

            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, _pickupRadius, _playerHits, _playerLayer);
            if (hitCount > 0)
            {
                Collect(_playerHits[0]);
            }
        }

        private void Collect(Collider player)
        {
            _isCollected = true;
            OnPickedUp(player);

            // Auto pool or destroy
            gameObject.SetActive(false);
        }

        protected abstract void OnPickedUp(Collider player);

        public virtual void ResetPickup()
        {
            _isCollected = false;
        }
    }
}
