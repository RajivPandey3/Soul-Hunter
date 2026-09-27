using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// VS dairy cart (vampire.survivors.wiki, Dairy Plant: "once they have taken enough damage, they launch at high speed
    /// toward the opposite end of the rail, dealing heavy damage to any enemies they collide with"). Like the Soul
    /// Lantern it is tagged and layered "Enemy" so every weapon can hit it, but it is not an EnemyController: nothing aims
    /// at it, it is never a kill and it never hurts or blocks the player. When its health runs out it launches instead of
    /// breaking, deals 200 to every enemy on its path (once each per run), stops at the far end and can be launched back.
    /// Health, speed and damage are provisional.
    /// </summary>
    [RequireComponent(typeof(HealthController))]
    public class RailCart : MonoBehaviour
    {
        public const int MaxHealth = 30;
        public const float Speed = 16f;
        public const int EnemyDamage = 200;
        public const float HitRadius = 1.1f;
        public const string SourceName = "Rail Cart";

        public bool IsMoving { get; private set; }
        public int Launches { get; private set; }
        public int EnemiesHit { get; private set; }
        public HealthController Health => _health;

        private HealthController _health;
        private float _position;   // local X along the rail
        private int _direction = 1;
        private bool _placed;
        private readonly HashSet<int> _hitThisRun = new HashSet<int>();

        private void Awake()
        {
            _health = GetComponent<HealthController>();
            _health.KnockbackResistance = 1f;
            _health.TryPreventDeath = Launch; // it launches instead of breaking: no OnDied, so no drop and no kill
        }

        private void OnEnable()
        {
            _health.Initialize(MaxHealth);
            _health.IsInvincible = false;
            if (!_placed)
            {
                _placed = true;
                _position = -RailCartTrack.HalfTravel;
                _direction = 1;
                ApplyPosition();
            }
        }

        /// <summary>Launches toward the other end (weapons do this by emptying its health). Always keeps it alive.</summary>
        public bool Launch()
        {
            _health.ResetHealth();
            if (IsMoving) return true;
            IsMoving = true;
            Launches++;
            _hitThisRun.Clear();
            _health.IsInvincible = true;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.HitSpark, transform.position + Vector3.up);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFXThrottled(audio.ExplosionSound, 0.1f, 0.5f);
            return true;
        }

        private void Update()
        {
            if (!IsMoving) return;
            float dt = Time.deltaTime; // game time: stands still while paused
            if (dt <= 0f) return;
            _health.IsInvincible = true;

            Vector3 from = transform.position;
            _position += _direction * Speed * dt;
            bool arrived = Mathf.Abs(_position) >= RailCartTrack.HalfTravel;
            _position = Mathf.Clamp(_position, -RailCartTrack.HalfTravel, RailCartTrack.HalfTravel);
            ApplyPosition();
            HitEnemies(from, transform.position);

            if (arrived)
            {
                IsMoving = false;
                _direction = -_direction;
                _health.ResetHealth();
                _health.IsInvincible = false;
                SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.HitSpark, transform.position + Vector3.up);
            }
        }

        private void ApplyPosition()
        {
            transform.localPosition = new Vector3(_position, transform.localPosition.y, 0f);
        }

        private void HitEnemies(Vector3 from, Vector3 to)
        {
            Vector3 push = (to - from).normalized;
            var enemies = EnemyController.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue; // a kill removes enemies from the list
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                if (StageHazardTable.SegmentDistanceXZ(from, to, enemy.transform.position) > HitRadius + enemy.CrowdRadius) continue;
                if (!_hitThisRun.Add(enemy.GetInstanceID())) continue;
                var health = enemy.GetComponent<HealthController>();
                if (health == null) continue;
                int before = health.CurrentHealth;
                health.TakeDamage(new DamagePacket(EnemyDamage, to, push) { WeaponKnockback = 3f });
                EnemiesHit++;
                int applied = Mathf.Max(0, before - health.CurrentHealth);
                if (applied > 0 && SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
                    SoulHunter.Gameplay.Core.RunStatsTracker.Instance.RecordDamage(SourceName, applied);
            }
        }
    }
}
