using UnityEngine;
using SoulHunter.Gameplay.Animation;
using SoulHunter.Gameplay.Physics;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// Decoupled AI Controller. Uses the exact same Animator and Scanner components as the Player.
    /// Passes logic execution to the active IEnemyState.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyController : MonoBehaviour
    {
        public static readonly List<EnemyController> ActiveEnemies = new List<EnemyController>();
        public Rigidbody Rigidbody { get; private set; }
        public EntityAnimator Animator { get; private set; }
        public Transform Target { get; set; } // Can be set by an aggro sphere or event

        private EnvironmentScanner _scanner;
        private IEnemyState _currentState;
        private Transform _visualRoot;
        private EnvironmentData _cachedEnvironmentData;
        private float _nextEnvironmentScanTime;
        private Vector3 _lastFacingDirection;
        private SoulHunter.Gameplay.Combat.DamageType _vulnerabilityType = SoulHunter.Gameplay.Combat.DamageType.Normal;
        private float _vulnerabilityMultiplier = 1f;
        private bool _isSpectral;
        
        [SerializeField] private SoulHunter.Gameplay.Data.EnemyData _enemyData;

        public float MoveSpeed { get; private set; }
        private float _baseMoveSpeed;
        public float ChaseDistance = 500f; // Vampire Survivors enemies always chase
        public float AttackDistance = 1.5f;

        public bool IsSpectral => _isSpectral;
        public SoulHunter.Gameplay.Data.EnemyData Data => _enemyData;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            // VS-style swarm movement: enemy bodies damage the player but do
            // not form a physical wall that blocks backward movement.
            var bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null) bodyCollider.isTrigger = true;
            // Hundreds of skinned enemies casting shadows halved the frame rate on the low-end reference laptop
            // (UHD 620); the hero, bosses and stage obstacles still cast them.
            SetCastsShadows(false);
            // Off-screen members of the horde do not need to animate (they only chase); up to ~300 skinned
            // animators updating every frame were the largest CPU cost on the reference laptop.
            foreach (var animator in GetComponentsInChildren<Animator>(true))
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
            Animator = GetComponentInChildren<EntityAnimator>();
            _visualRoot = transform.Find("SH10_Visual");
            _scanner = GetComponent<EnvironmentScanner>();
            
            // 100% Data-Driven setup
            if (_enemyData != null)
            {
                MoveSpeed = _enemyData.MoveSpeed;
                
                var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
                if (health != null) health.Initialize(_enemyData.MaxHealth);

                var touchDamage = GetComponent<SoulHunter.Gameplay.Combat.TouchDamage>();
                if (touchDamage != null) touchDamage.DamageAmount = _enemyData.DamageToPlayer;

                var drop = GetComponent<EnemyDrop>();
                if (drop != null) 
                {
                    drop.ChickenDropChance = _enemyData.DropChanceChicken;
                    drop.GoldDropChance = _enemyData.DropChanceGold;
                    drop.GoldValue = _enemyData.GoldValue;
                    drop.DropsChest = _enemyData.DropsChest;
                }
            }
            else
            {
                MoveSpeed = 3f; // Default fallback
            }
            _baseMoveSpeed = MoveSpeed;

            // Force override inspector serialization value to ensure they always chase
            ChaseDistance = 500f;
        }

        private void Start()
        {
            // Default behaviour
            if (_currentState == null)
            {
                ChangeState(new EnemyIdleState(this));
            }
        }

        private SoulHunter.Gameplay.Combat.HealthController _hitSoundHealth;

        // VS: every hit on an enemy is heard (AudioManager throttles it for hordes).
        private void PlayHitSound()
        {
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFXThrottled(audio.EnemyHitSound, SoulHunter.Gameplay.Audio.AudioManager.EnemyHitInterval, 0.6f);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.HitSpark, transform.position + Vector3.up * 1f);
        }

        private void PlayDeathSound()
        {
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFXThrottled(audio.EnemyDeathSound, SoulHunter.Gameplay.Audio.AudioManager.EnemyDeathInterval, 0.5f);
        }

        private void OnEnable()
        {
            if (_hitSoundHealth == null) _hitSoundHealth = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (_hitSoundHealth != null) { _hitSoundHealth.OnDamaged += PlayHitSound; _hitSoundHealth.OnDied += PlayDeathSound; }
            if (!ActiveEnemies.Contains(this)) ActiveEnemies.Add(this);
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }
            if (_currentState == null) ChangeState(new EnemyIdleState(this));
        }

        private void OnDisable()
        {
            if (_hitSoundHealth != null) { _hitSoundHealth.OnDamaged -= PlayHitSound; _hitSoundHealth.OnDied -= PlayDeathSound; }
            ActiveEnemies.Remove(this);
            _currentState?.Exit();
            _currentState = null;
            // Spawners refresh Target on every spawn, including pooled reuse.
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>True while a Time Freeze or Clock Lancet holds this enemy.</summary>
        public bool IsFrozen => _currentState is EnemyFrozenState;

        /// <summary>Only bosses cast shadows (EnemySpawner turns them on); the horde does not, for performance.</summary>
        public void SetCastsShadows(bool cast)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = cast ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>True while a stage event flies this enemy along a fixed path (also while frozen mid-flight).</summary>
        public bool IsFlying => _currentState is EnemyFlyState ||
                                (_currentState is EnemyFrozenState frozen && frozen.ResumeState is EnemyFlyState);

        /// <summary>
        /// Time Freeze / Clock Lancet: stop in place, then carry on with what the enemy was doing. A stage-event
        /// flier resumes its path (and its lifetime); everyone else chases again.
        /// </summary>
        public void Freeze(float duration)
        {
            var resume = _currentState is EnemyFrozenState frozen ? frozen.ResumeState
                : _currentState is EnemyFlyState ? _currentState : null;
            ChangeState(new EnemyFrozenState(this, duration, resume));
        }

        public void StartFlyingMode(Vector3 direction)
        {
            ChangeState(new EnemyFlyState(this, direction));
        }

        public void ApplyCampaignSpeed(float multiplier)
        {
            MoveSpeed = _baseMoveSpeed * Mathf.Max(0.1f, multiplier);
        }

        public void ConfigureDamageVulnerability(SoulHunter.Gameplay.Combat.DamageType type, float multiplier, bool isSpectral = false)
        {
            _vulnerabilityType = type;
            _vulnerabilityMultiplier = Mathf.Max(0.1f, multiplier);
            _isSpectral = isSpectral;
        }

        public float GetDamageMultiplier(SoulHunter.Gameplay.Combat.DamageType type)
        {
            return type == _vulnerabilityType ? _vulnerabilityMultiplier : 1f;
        }

        private void Update()
        {
            FaceTarget();
            _currentState?.UpdateLogic();
        }

        private void FaceTarget()
        {
            // Fliers keep facing their path, set once by FaceDirection.
            if (_visualRoot == null || Target == null || IsFlying) return;
            FaceDirection(Target.position - transform.position);
        }

        public void FaceDirection(Vector3 direction)
        {
            if (_visualRoot == null) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            direction.Normalize();

            // Avoid rewriting the transform when the facing direction has not
            // meaningfully changed. This matters when hundreds of enemies are alive.
            if (Vector3.Dot(direction, _lastFacingDirection) > 0.9999f) return;
            _lastFacingDirection = direction;

            // SH10 models use the opposite authored forward axis; keep the
            // gameplay root untouched and rotate only the visual child.
            _visualRoot.rotation = Quaternion.LookRotation(direction, Vector3.up)
                                   * Quaternion.Euler(0f, 180f, 0f);
        }

        private void FixedUpdate()
        {
            if (_currentState != null)
            {
                // Agar scanner disable kiya hua hai, toh CPU bachaane ke liye usey mat chalao
                if (_scanner != null && _scanner.isActiveAndEnabled)
                {
                    // Ground data does not need to be recalculated at every physics
                    // tick for a moving swarm. Refresh at 20 Hz and reuse the last
                    // result between scans.
                    if (Time.time >= _nextEnvironmentScanTime)
                    {
                        _cachedEnvironmentData = _scanner.ScanEnvironment();
                        _nextEnvironmentScanTime = Time.time + 0.05f;
                    }
                }
                else
                {
                    _cachedEnvironmentData = new EnvironmentData();
                }

                _currentState.UpdatePhysics(ref _cachedEnvironmentData);
            }
        }

        public void ChangeState(IEnemyState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }
    }
}
