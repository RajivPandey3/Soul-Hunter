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

        private float _bodyRadius = 0.5f;
        /// <summary>Flat body radius for crowd separation (collider radius x scale: chest elites and bosses are bigger).</summary>
        public float CrowdRadius => _bodyRadius * Mathf.Abs(transform.lossyScale.x);
        /// <summary>This physics step's push from the crowd, written by EnemyCrowd.Rebuild.</summary>
        public Vector3 CrowdPush { get; set; }
        private int _crowdId;
        /// <summary>Instance id, cached (read for every enemy on every frame by EnemyCrowd).</summary>
        public int CrowdId => _crowdId != 0 ? _crowdId : (_crowdId = GetInstanceID());

        public bool IsSpectral => _isSpectral;
        public SoulHunter.Gameplay.Data.EnemyData Data => _enemyData;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            // VS-style swarm movement: enemy bodies damage the player but do
            // not form a physical wall that blocks backward movement.
            var bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null) bodyCollider.isTrigger = true;
            _bodyRadius = bodyCollider is CapsuleCollider capsule ? capsule.radius
                : bodyCollider is SphereCollider sphere ? sphere.radius
                : bodyCollider is BoxCollider box ? 0.5f * Mathf.Max(box.size.x, box.size.z)
                : 0.5f;
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
                    drop.GemValue = _enemyData.ExperienceValue;
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
            ResetKnockback(); // pooled reuse starts with the data's knockback again (Garlic's increase lasts one life)
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

        /// <param name="speed">Relative to the default event speed (1 = 1.5x the enemy's own speed).</param>
        public void StartFlyingMode(Vector3 direction, float speed = 1f)
        {
            ChangeState(new EnemyFlyState(this, direction, EnemyFlyState.DefaultLifetimeSeconds,
                EnemyFlyState.SpeedMultiplier * Mathf.Max(0.2f, speed)));
        }

        // ---------------------------------------------------------------- VS knockback
        /// <summary>VS: a hit reverses the enemy's movement for 120 ms (vampire.survivors.wiki, Knockback).</summary>
        public const float KnockbackSeconds = 0.12f;
        /// <summary>VS: each Garlic hit makes an enemy take 0.3 more knockback, up to its maximum.</summary>
        public const float GarlicKnockbackStep = 0.3f;
        private float _knockbackTaken = 1f;
        private float _knockbackStrength;
        private float _knockbackUntil = -1f;

        public float KnockbackTaken => _knockbackTaken;
        public float MaxKnockback => _enemyData != null ? _enemyData.MaxKnockback : 3f;
        public bool IsKnockedBack => Time.time < _knockbackUntil;
        /// <summary>This knockback's reversed speed as a multiple of the enemy's move speed.</summary>
        public float KnockbackStrength => _knockbackStrength;

        /// <summary>Reversed speed during a knockback: move speed x weapon knockback x knockback taken.</summary>
        public static float KnockbackSpeed(float moveSpeed, float weaponKnockback, float taken) =>
            Mathf.Max(0f, moveSpeed * weaponKnockback * taken);

        /// <summary>A hit: for 120 ms the enemy moves away from its target instead of toward it. Fliers and frozen enemies stay put.</summary>
        public void ApplyKnockback(float weaponKnockback)
        {
            if (IsFlying || IsFrozen) return;
            float strength = Mathf.Max(0f, weaponKnockback) * _knockbackTaken;
            if (strength <= 0f) return;
            _knockbackStrength = strength;
            _knockbackUntil = Time.time + KnockbackSeconds;
        }

        /// <summary>Garlic: the enemy takes more knockback from now on, up to its maximum.</summary>
        public void AddKnockbackTaken(float amount)
        {
            _knockbackTaken = Mathf.Min(MaxKnockback, _knockbackTaken + Mathf.Max(0f, amount));
        }

        private void ResetKnockback()
        {
            _knockbackTaken = _enemyData != null ? _enemyData.Knockback : 1f;
            _knockbackUntil = -1f;
            _knockbackStrength = 0f;
        }

        /// <summary>This enemy's VS movement skill (Homing when it has no EnemyData).</summary>
        public SoulHunter.Gameplay.Data.EnemyMovement Movement =>
            _enemyData != null ? _enemyData.Movement : SoulHunter.Gameplay.Data.EnemyMovement.Homing;

        /// <summary>Floaty enemies each start their weave at a different point, so a crowd does not sway in step.</summary>
        public float FloatyPhase { get; private set; }

        /// <summary>
        /// Fixed Direction (VS): the enemy keeps the line it was given at its own speed, passes the player and leaves
        /// (returned to the pool without drops once its course is over). Not relocated and not pushed by the crowd.
        /// </summary>
        public void StartFixedCourse(Vector3 direction, float lifetimeSeconds)
        {
            ChangeState(new EnemyFlyState(this, direction, Mathf.Max(1f, lifetimeSeconds), 1f));
        }

        /// <summary>Called by spawners once the enemy is placed, targeted and at its stage speed.</summary>
        public void BeginMovement()
        {
            FloatyPhase = Random.value * Mathf.PI * 2f;
            if (Movement != SoulHunter.Gameplay.Data.EnemyMovement.FixedDirection || Target == null) return;
            Vector3 toTarget = Target.position - transform.position;
            toTarget.y = 0f;
            // Long enough to cross the whole spawn ring and leave on the far side.
            float lifetime = (toTarget.magnitude * 2f + 6f) / Mathf.Max(0.1f, MoveSpeed);
            StartFixedCourse(toTarget, lifetime);
        }

        /// <summary>The multiplier last given to ApplyCampaignSpeed (stage speed x Curse).</summary>
        public float CampaignSpeedMultiplier { get; private set; } = 1f;

        public void ApplyCampaignSpeed(float multiplier)
        {
            CampaignSpeedMultiplier = Mathf.Max(0.1f, multiplier);
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
