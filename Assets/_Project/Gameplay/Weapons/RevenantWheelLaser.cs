using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// One laser of the Revenant Wheel (RevenantWheelWeapon): a long thin trigger that turns with the wheel. It carries
    /// the laser's VS Hitbox Delay (HitboxDelay, 0.2 s) and its Pierce.
    /// VS wiki (Phieraggi, raw, fetched 2026-10-12): "Phieraggi still only has a Pierce of 7, it cannot attack all the
    /// enemies at the same time even though the lasers looks like an Area-of-Effect." The page does not say how the 7 is
    /// counted on a laser that stays. Balance method: a laser hits at most Pierce enemies in each hitbox-delay window
    /// (0.2 s), the nearest reading of "cannot attack all the enemies at the same time" for a shot that never leaves.
    /// </summary>
    public sealed class RevenantWheelLaser : MonoBehaviour
    {
        /// <summary>Its place on the wheel, in degrees from the wheel's own angle.</summary>
        [System.NonSerialized] public float OffsetDegrees;
        /// <summary>Game time when it retracts.</summary>
        [System.NonSerialized] public float EndsAt;
        /// <summary>Enemies it may hit in one hitbox-delay window.</summary>
        [System.NonSerialized] public int Pierce = 7;

        private HitboxDelay _delay;
        private ProjectileDamage _damage;
        private int _windowHits;
        private float _windowEnds;

        /// <summary>Hits let through in the current window (tests).</summary>
        public int WindowHits => Time.time >= _windowEnds ? 0 : _windowHits;

        /// <summary>Call after the laser is switched on: (re)starts its hitbox delay and puts the pierce gate in front of it.</summary>
        public void Arm(ProjectileDamage damage, HitboxDelay delay, float delaySeconds)
        {
            _damage = damage;
            _delay = delay;
            _windowHits = 0;
            _windowEnds = 0f;
            if (_delay != null)
            {
                _delay.Delay = delaySeconds;
                _delay.enabled = true; // its OnEnable takes the hit gate; ours goes in front of it
            }
            if (_damage != null) _damage.HitGate = Gate;
        }

        private void OnDisable()
        {
            if (_damage != null && _damage.HitGate == Gate) _damage.HitGate = null;
        }

        /// <summary>True when <paramref name="target"/> may be hit now.</summary>
        public bool Gate(Component target)
        {
            if (Time.time >= _windowEnds) _windowHits = 0;
            if (_windowHits >= Mathf.Max(1, Pierce)) return false;
            if (_delay != null && !_delay.Allow(target)) return false;
            if (_windowHits == 0) _windowEnds = Time.time + (_delay != null ? Mathf.Max(0.01f, _delay.Delay) : 0.2f);
            _windowHits++;
            return true;
        }
    }
}
