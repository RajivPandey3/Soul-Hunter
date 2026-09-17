using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Core
{
    public sealed class CampaignHazard : MonoBehaviour
    {
        public enum HazardType { Fire, Trap, Poison }
        [SerializeField] private int _damage = 8;
        [SerializeField] private float _tickInterval = 0.5f;
        private float _timer;

        public void Configure(HazardType type, int damage) => _damage = Mathf.Max(1, damage);

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _tickInterval;
            var health = other.GetComponentInParent<HealthController>();
            if (health != null) health.TakeDamage(new DamagePacket(_damage, other.ClosestPoint(transform.position), Vector3.zero));
        }
    }
}
