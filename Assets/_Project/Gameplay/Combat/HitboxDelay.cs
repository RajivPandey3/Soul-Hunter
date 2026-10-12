using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// VS "Hitbox Delay" for a shot that stays inside enemies (vampire.survivors.wiki Runetracer and NO_FUTURE, raw pages
    /// fetched 2026-10-12): "the same enemy cannot be hit more often than every half a second by the same rune. However,
    /// because all active delays are reset simultaneously as soon as any one elapses or bounces off a wall, some enemies
    /// may get hit more often." The shot hits an enemy when it first touches it and again while it stays inside, once
    /// per <see cref="Delay"/>; every enemy's delay ends together when the oldest one runs out or the shot bounces.
    /// Without this component a <see cref="ProjectileDamage"/> hits only when it enters an enemy.
    /// </summary>
    [RequireComponent(typeof(ProjectileDamage))]
    public sealed class HitboxDelay : MonoBehaviour
    {
        /// <summary>Seconds before the same enemy can be hit again (game time).</summary>
        [System.NonSerialized] public float Delay = 0.5f;

        private readonly HashSet<int> _waiting = new HashSet<int>();
        private float _resetAt;
        private ProjectileDamage _damage;
        private ScreenBounce _bounce;
        private int _bouncesSeen;

        /// <summary>Enemies whose delay is running (tests).</summary>
        public int Waiting => _waiting.Count;

        private void OnEnable()
        {
            _damage = GetComponent<ProjectileDamage>();
            _bounce = GetComponent<ScreenBounce>();
            _bouncesSeen = 0;
            _waiting.Clear();
            if (_damage != null) _damage.HitGate = Allow;
        }

        // A pooled shot reused by another weapon hits on entry only, until a weapon turns the delay on again.
        private void OnDisable()
        {
            if (_damage != null && _damage.HitGate == Allow) _damage.HitGate = null;
            _waiting.Clear();
            enabled = false;
        }

        private void OnTriggerStay(Collider other)
        {
            if (_damage != null) _damage.TryHit(other);
        }

        /// <summary>True when <paramref name="target"/> may be hit now; it then waits out the delay.</summary>
        public bool Allow(Component target)
        {
            if (target == null) return false;
            if (_bounce != null && _bounce.Bounces != _bouncesSeen)
            {
                _bouncesSeen = _bounce.Bounces;
                _waiting.Clear();
            }
            if (_waiting.Count > 0 && Time.time >= _resetAt) _waiting.Clear();
            if (!_waiting.Add(target.GetInstanceID())) return false;
            if (_waiting.Count == 1) _resetAt = Time.time + Mathf.Max(0.01f, Delay);
            return true;
        }
    }
}
