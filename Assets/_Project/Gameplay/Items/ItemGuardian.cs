using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// One life of a stage item's guardian (ItemGuardians). VS wiki (City_Atlantean and the other three): "When killed,
    /// City Atlantean drops a Golden Egg instead of experience". So this life leaves no soul gem, and its death leaves a
    /// Soul Egg where it fell. The body comes from the enemy pool; the component goes quiet when the body is pooled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemGuardian : MonoBehaviour
    {
        /// <summary>Eggs dropped by guardians since the game started (tests).</summary>
        public static int EggsDropped { get; private set; }

        private HealthController _health;
        private bool _armed;

        public static ItemGuardian Begin(GameObject body)
        {
            if (body == null) return null;
            var guardian = body.GetComponent<ItemGuardian>();
            if (guardian == null) guardian = body.AddComponent<ItemGuardian>();
            guardian.Arm();
            return guardian;
        }

        private void Arm()
        {
            _health = GetComponent<HealthController>();
            var drop = GetComponent<EnemyDrop>();
            if (drop != null) drop.EraseDropsThisLife(); // "instead of experience": no gem; it still counts as a kill
            if (_health != null)
            {
                _health.OnDied -= HandleDied;
                _health.OnDied += HandleDied;
            }
            _armed = true;
        }

        private void HandleDied()
        {
            if (!_armed) return;
            Disarm();
            EggsDropped++;
            SoulEggPickup.Create(transform.position);
        }

        private void OnDisable() => Disarm();

        private void Disarm()
        {
            _armed = false;
            if (_health != null) _health.OnDied -= HandleDied;
        }
    }
}
