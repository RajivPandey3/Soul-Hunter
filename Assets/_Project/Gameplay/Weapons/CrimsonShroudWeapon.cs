using System;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.Combat
{
    public sealed class CrimsonShroudWeapon : LaurelWeapon
    {
        [SerializeField, Min(0.1f)] private float _retaliationRadius = 3f;
        private readonly int[] _queuedDamage = new int[50];
        private int _head, _count;
        private float _pulseTimer;
        private Func<int, int> _previousModifier;
        public override int MaxLevel => 1;
        public override int MaxCharges => 3;
        public override float BaseRechargeCooldown => 8f;
        public override float BlockGraceSeconds => 1f;
        public int PendingPulses => _count;
        public float RetaliationRadius => _retaliationRadius * (_stats != null ? Mathf.Max(0f, _stats.Area) : 1f);
        public event Action<int> OnRetaliation;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_playerHealth != null && _playerHealth.DamageModifier != ModifyDamage)
            {
                _previousModifier = _playerHealth.DamageModifier;
                _playerHealth.DamageModifier = ModifyDamage;
            }
            OnBlocked -= QueueRetaliation;
            OnBlocked += QueueRetaliation;
        }

        protected override void OnDisable()
        {
            OnBlocked -= QueueRetaliation;
            if (_playerHealth != null && _playerHealth.DamageModifier == ModifyDamage)
                _playerHealth.DamageModifier = _previousModifier;
            _previousModifier = null;
            _head = _count = 0;
            _pulseTimer = 0f;
            base.OnDisable();
        }

        private int ModifyDamage(int damage)
        {
            int capped = Mathf.Min(10, damage);
            return _previousModifier != null ? _previousModifier(capped) : capped;
        }

        public int CalculateRetaliationDamage(int receivedDamage)
        {
            float multiplier = _stats != null ? _stats.Might * _stats.Curse * (1f + Mathf.Clamp(_stats.Armor, 0, 50) * 0.1f) : 1f;
            return Mathf.Max(0, Mathf.RoundToInt(Mathf.Clamp(receivedDamage, 0, 100) * multiplier));
        }

        private void QueueRetaliation(DamagePacket packet)
        {
            int damage = CalculateRetaliationDamage(packet.Amount);
            int pulses = _stats != null ? 1 + Mathf.Clamp(_stats.Amount, 0, 49) : 1;
            for (int i = 0; i < pulses && _count < _queuedDamage.Length; i++)
            {
                _queuedDamage[(_head + _count) % _queuedDamage.Length] = damage;
                _count++;
            }
        }

        protected override void Update()
        {
            base.Update();
            AdvanceRetaliation(Time.deltaTime);
        }

        private void AdvanceRetaliation(float delta)
        {
            if (delta <= 0f || _count == 0) return;
            _pulseTimer -= delta;
            // Bound catch-up work after a long frame; retain the remaining queue.
            for (int i = 0; i < 4 && _count > 0 && _pulseTimer <= 0f; i++)
            {
                int damage = _queuedDamage[_head];
                _head = (_head + 1) % _queuedDamage.Length;
                _count--;
                _pulseTimer += 0.1f;
                Pulse(damage);
            }
            if (_count == 0) _pulseTimer = 0f;
        }

        private void Pulse(int damage)
        {
            float radiusSquared = RetaliationRadius * RetaliationRadius;
            // Iterate backwards: lethal damage removes the current registry entry.
            for (int i = EnemyController.ActiveEnemies.Count - 1; i >= 0; i--)
            {
                if (i >= EnemyController.ActiveEnemies.Count) continue;
                var enemy = EnemyController.ActiveEnemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                Vector3 offset = enemy.transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSquared) continue;
                var health = enemy.GetComponent<HealthController>();
                if (health == null) continue;
                int before = health.CurrentHealth;
                health.TakeDamage(new DamagePacket(damage, enemy.transform.position, Vector3.zero));
                int applied = Mathf.Max(0, before - health.CurrentHealth);
                if (applied > 0 && RunStatsTracker.Instance != null)
                    RunStatsTracker.Instance.RecordDamage("Crimson Shroud", applied);
            }
            OnRetaliation?.Invoke(damage);
        }

        protected override Color ChargeColor(int charges) => charges >= 3 ? Color.red : charges == 2 ? new Color(1f, 0.5f, 0f) : Color.green;
    }
}
