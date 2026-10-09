using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// Base class for all ground pickups (Gold, Magnet, TimeFreeze, etc).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class Pickup : MonoBehaviour, ITickedPickup
    {
        [SerializeField] protected float _pickupRadius = 1.5f;
        [Tooltip("Unused since the heavy-kill pass (PlayerReach checks the Player layer); kept so prefabs keep their data.")]
        [SerializeField] protected LayerMask _playerLayer;

        protected bool _isCollected = false;

        /// <summary>Speed a pulled pickup starts flying at, and how fast it speeds up (units/s, units/s^2; Soul Hunter's numbers, wiki silent).</summary>
        public const float PullStartSpeed = 12f, PullAcceleration = 40f;

        private Transform _pullTarget;
        private float _pullSpeed;

        /// <summary>True while this pickup is flying to the player.</summary>
        public bool IsPulled => _pullTarget != null;

        /// <summary>
        /// Flies the pickup to <paramref name="target"/>, where the normal pickup check collects it. VS wiki (Magnet):
        /// "Experience Gems and other pickups will be collected" inside the Magnet radius (PlayerExperience's sweep).
        /// </summary>
        public void Magnetize(Transform target)
        {
            if (_isCollected || target == null || _pullTarget != null) return;
            _pullTarget = target;
            _pullSpeed = PullStartSpeed;
        }

        public int TickerIndex { get; set; } = -1;

        // Heavy-kill pass (2026-09-29): PickupTicker calls Tick for every pickup in one loop instead of an Update each.
        protected virtual void OnEnable() => PickupTicker.Add(this);
        protected virtual void OnDisable() => PickupTicker.Remove(this);

        public virtual void Tick()
        {
            if (_isCollected) return;

            if (_pullTarget != null)
            {
                if (!_pullTarget.gameObject.activeInHierarchy) _pullTarget = null;
                else
                {
                    _pullSpeed += PullAcceleration * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, _pullTarget.position, _pullSpeed * Time.deltaTime);
                }
            }

            // Same test as an overlap sphere on the Player layer, without a physics query per pickup per frame.
            var player = PlayerReach.Within(transform.position, _pickupRadius);
            if (player != null) Collect(player);
        }

        private void Collect(Collider player)
        {
            _isCollected = true;
            OnPickedUp(player);
            // VS Results: "all the pickups and the amount of them".
            if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null) SoulHunter.Gameplay.Core.RunStatsTracker.Instance.RecordPickup(ResultsName);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.GoldPickup, transform.position + Vector3.up * 0.5f);

            // Auto pool or destroy
            gameObject.SetActive(false);
        }

        protected abstract void OnPickedUp(Collider player);

        /// <summary>This pickup's name in the Results screen's pickup list (Soul Hunter's own names), or null.</summary>
        public virtual string ResultsName => null;

        public virtual void ResetPickup()
        {
            _isCollected = false;
            _pullTarget = null;
        }
    }
}
