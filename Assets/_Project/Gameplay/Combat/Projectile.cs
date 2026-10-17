using UnityEngine;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors ke 'Magic Wand' jaisi projectile script.
    /// Ye sidha aage move karti hai aur kisi enemy se takrane par use damage deti hai.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        /// <summary>The knockback of the weapon that spawned this (VS infobox "knockback"); negative = the default 1.</summary>
        [System.NonSerialized] public float Knockback = -1f;
        [SerializeField] private float _speed = 10f;
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private DamageType _damageType = DamageType.Normal;

        private Rigidbody _rb;
        private float _currentLifetime;
        private int _bounceCount;
        private int _maxBounces;
        private string _sourceWeaponName;
        /// <summary>The weapon that fired this (set by AutoAttackWeapon.AimAtHostiles), credited on the Results screen.</summary>
        [System.NonSerialized] public AutoAttackWeapon SourceWeapon;
        public event System.Action<Projectile> Deactivated;
        /// <summary>Fired by a boss's mirrored weapon: hits only the player. Reset on every reuse from the pool.</summary>
        public bool TargetsPlayer
        {
            get => _targetsPlayer;
            set
            {
                _targetsPlayer = value;
                // 60 FPS pass (2026-10-15): a shot that hits only the player flies on the EnemyShot layer, which meets
                // only the Player layer. On Default it made a trigger pair with every enemy it crossed (Level 5's Lost
                // Heads keep about 390 bullets in the air through the horde) and each pair ran OnTriggerEnter for nothing.
                if (EnemyShotLayer >= 0) gameObject.layer = value ? EnemyShotLayer : _homeLayer;
            }
        }
        private bool _targetsPlayer;
        private int _homeLayer;
        /// <summary>The layer of shots that hit only the player (-1 when the project has no such layer).</summary>
        public static int EnemyShotLayer => _enemyShotLayer ??= LayerMask.NameToLayer("EnemyShot");
        private static int? _enemyShotLayer;

        /// <summary>Every enabled shot; ProjectileRunner moves them all from one Update (60 FPS pass, 2026-10-15).</summary>
        public static readonly System.Collections.Generic.List<Projectile> ActiveShots = new System.Collections.Generic.List<Projectile>();
        private int _activeIndex = -1;
        internal int ActiveIndex { set => _activeIndex = value; }
        private Transform _transform;

        private void OnDisable()
        {
            int i = _activeIndex;
            _activeIndex = -1;
            if (i < 0 || i >= ActiveShots.Count || ActiveShots[i] != this) ActiveShots.Remove(this);
            else
            {
                int last = ActiveShots.Count - 1;
                ActiveShots[i] = ActiveShots[last];
                if (ActiveShots[i] != null) ActiveShots[i]._activeIndex = i;
                ActiveShots.RemoveAt(last);
            }
            _recentHits.Clear();
            Deactivated?.Invoke(this);
            Deactivated = null;
        }
        private int _remainingHits;
        private bool _blockedByWalls;
        private readonly System.Collections.Generic.Dictionary<IDamageable, float> _recentHits = new();

        /// <summary>Enemy bounces this shot has in total (tests and tools).</summary>
        public int MaxBounces => _maxBounces;
        /// <summary>Screen-edge bounces this shot has left (VS Arcanas: Hellfire, Cross, Death Spiral; tests and tools).</summary>
        public int ScreenBouncesLeft => _screenBouncesLeft;
        private int _screenBouncesLeft;

        /// <summary>
        /// VS Waltz of Pearls (XI) / Iron Blue Will (VII): a shot that "already has infinite pierce" bounces off the edges of
        /// the screen instead of off enemies (Hellfire and Death Spiral once, Cross and Heaven Sword twice). Reset on reuse.
        /// </summary>
        public void AddScreenBounces(int extra)
        {
            if (extra > 0) _screenBouncesLeft += extra;
        }

        /// <summary>Adds bounces on top of the current configuration (a character's extra bounces).</summary>
        public void AddBounces(int extra)
        {
            if (extra <= 0) return;
            _maxBounces += extra;
            _remainingHits += extra;
        }

        public void ConfigureContacts(int pierce, int bounces, bool blockedByWalls)
        {
            _maxBounces = Mathf.Max(0, bounces);
            _bounceCount = 0;
            _remainingHits = Mathf.Max(1, pierce) + _maxBounces;
            _blockedByWalls = blockedByWalls;
            _recentHits.Clear();
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _transform = transform;
            if (gameObject.layer != EnemyShotLayer) _homeLayer = gameObject.layer;
            // Only the shot's own running glow and trail (looping, playing on enable); a one-off burst such as an
            // explosion at the end of a flight is played by its weapon and left alone.
            _fx = System.Array.FindAll(GetComponentsInChildren<ParticleSystem>(true), fx => fx.main.loop && fx.main.playOnAwake);
            _rb.useGravity = false;
            _rb.isKinematic = true; // Use trigger collisions
            
            var col = GetComponent<Collider>();
            col.isTrigger = true; // Important for trigger detection
        }

        private void OnEnable()
        {
            _sourceWeaponName = null;
            SourceWeapon = null;
            TargetsPlayer = false;
            _currentLifetime = _lifetime;
            _bounceCount = 0;
            // VS Arcana bounces apply only to each card's listed weapons, which add them when they fire
            // (AutoAttackWeapon.ArcanaBounces). This used to read one global count, so every pooled projectile, Shadow
            // Kael's mirrored shots included, bounced.
            _maxBounces = 0;
            _screenBouncesLeft = 0;
            ConfigureContacts(1, _maxBounces, false);
            // The stored index is only trusted when the list agrees: a pooled copy can carry its template's index.
            if (!(_activeIndex >= 0 && _activeIndex < ActiveShots.Count && ActiveShots[_activeIndex] == this))
            {
                _activeIndex = ActiveShots.Count;
                ActiveShots.Add(this);
            }
            ProjectileRunner.Ensure();
            if (_fxStopped) { _fxStopped = false; foreach (var fx in _fx) if (fx != null) fx.Play(false); }
        }

        // 60 FPS pass (2026-10-15): a shot's glow and trail particles are switched off while the shot is well off the
        // screen (Level 5 with Knife, Fire Wand and Song of Mana kept 850 systems running, 2.5 ms a frame) and switched
        // back on when it returns. Only the look changes; flight, hits and lifetime do not.
        private ParticleSystem[] _fx = System.Array.Empty<ParticleSystem>();
        private bool _fxStopped;
        private const float OffScreenMargin = 0.25f; // of the view: its trail (a few units behind it) is off screen too

        internal void UpdateFx(Camera cam)
        {
            if (_fx.Length == 0 || cam == null) return;
            Vector3 v = cam.WorldToViewportPoint(_transform.position);
            bool off = v.x < -OffScreenMargin || v.x > 1f + OffScreenMargin || v.y < -OffScreenMargin || v.y > 1f + OffScreenMargin;
            if (off == _fxStopped) return;
            _fxStopped = off;
            foreach (var fx in _fx)
            {
                if (fx == null) continue;
                if (off) fx.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                else fx.Play(false);
            }
        }

        /// <summary>True while this shot's particles are off because it is far off the screen (tests).</summary>
        public bool FxStopped => _fxStopped;

        /// <summary>One frame of flight, called by ProjectileRunner (it was this shot's own Update).</summary>
        internal void Tick(float dt)
        {
            if (_transform == null) _transform = transform;
            _transform.position += _transform.forward * (_speed * dt);
            if (_screenBouncesLeft > 0) BounceOffScreenEdge();

            _currentLifetime -= dt;
            if (_currentLifetime <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>Turns the shot back when it is leaving the camera's view, using up one screen bounce.</summary>
        private void BounceOffScreenEdge()
        {
            Vector3 forward = transform.forward;
            if (!ScreenEdge.Reflect(transform.position, ref forward)) return;
            transform.forward = forward;
            _screenBouncesLeft--;
        }

        /// <param name="pierce">Enemies this projectile can hit before it disappears (VS pierce). Default 1.</param>
        public void Initialize(Vector3 direction, float speed, float damage, float lifetime, DamageType damageType = DamageType.Normal, string sourceWeaponName = null, int pierce = 1)
        {
            _sourceWeaponName = sourceWeaponName;
            _speed = speed;
            _damage = Mathf.RoundToInt(damage);
            _lifetime = lifetime;
            _damageType = damageType;
            _currentLifetime = lifetime;
            _bounceCount = 0;
            _maxBounces = 0;
            _screenBouncesLeft = 0;
            ConfigureContacts(pierce, _maxBounces, false);
            transform.forward = direction;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActiveAndEnabled) return;
            // Apni goli se khud ko damage na ho: the player's shots skip the player; a boss's mirrored shots hit only the player.
            bool isPlayer = other.CompareTag("Player") ||
                (!other.CompareTag("Enemy") && other.GetComponentInParent<SoulHunter.Gameplay.Player.PlayerController>() != null); // an enemy body is never the player: no parent search per hit
            if (isPlayer != TargetsPlayer) return;

            var target = DamageableLookup.Of(other);
            if (target != null)
            {
                if (_recentHits.TryGetValue(target, out float lastHit) && Time.time - lastHit < 0.1f) return;
                _recentHits[target] = Time.time;
                Vector3 knockback = transform.forward;
                using (DamageSource.From(SourceWeapon, _sourceWeaponName))
                    target.TakeDamage(new DamagePacket(_damage, transform.position, knockback, _damageType).WithKnockback(Knockback));
                _remainingHits--;
                if (_remainingHits <= 0) { gameObject.SetActive(false); return; }
                if (_bounceCount < _maxBounces)
                {
                    _bounceCount++;
                    Vector3 normal = other.transform.position - transform.position;
                    normal.y = 0f;
                    Vector3 reflected = Vector3.Reflect(transform.forward, normal.normalized);
                    reflected.y = 0f;
                    if (reflected.sqrMagnitude < 0.001f) reflected = -transform.forward;
                    transform.forward = reflected.normalized;
                }
            }
            else if (_blockedByWalls && !other.isTrigger) gameObject.SetActive(false);
        }
    }
}
