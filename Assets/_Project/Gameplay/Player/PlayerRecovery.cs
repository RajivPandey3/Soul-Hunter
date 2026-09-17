using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Player
{
    [RequireComponent(typeof(PlayerStats), typeof(HealthController))]
    public sealed class PlayerRecovery : MonoBehaviour
    {
        private PlayerStats _stats;
        private HealthController _health;
        private float _fraction;
        private void Awake() { _stats = GetComponent<PlayerStats>(); _health = GetComponent<HealthController>(); }
        private void OnDisable() { _fraction = 0f; }
        private void Update() { AdvanceRecovery(Time.deltaTime); }
        private void AdvanceRecovery(float delta)
        {
            if (delta <= 0f || _health == null || _stats == null) return;
            if (_health.CurrentHealth <= 0 || _health.CurrentHealth >= _health.MaxHealth) { _fraction = 0f; return; }
            _fraction += Mathf.Max(0f, _stats.Regen) * delta;
            int healing = Mathf.FloorToInt(_fraction);
            if (healing == 0) return;
            _fraction -= healing;
            _health.Heal(healing);
        }
    }
}
