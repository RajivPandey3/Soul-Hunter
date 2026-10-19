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
        /// <summary>This enemy's transform, cached (read for every enemy on every step).</summary>
        public Transform CachedTransform { get; private set; }
        /// <summary>True when it has an EntityAnimator (looked up once).</summary>
        public bool HasAnimator { get; private set; }
        private bool _flatBody;       // no gravity and height frozen: its vertical velocity is always 0
        private bool _rotationLocked; // every rotation axis frozen: nothing can spin it
        private bool _scans;          // its EnvironmentScanner is switched on (off on every horde prefab)

        /// <summary>The body's vertical velocity (0 without an engine call for a body that cannot move vertically).</summary>
        public float VerticalVelocity => _flatBody || Rigidbody == null ? 0f : Rigidbody.linearVelocity.y;
        /// <summary>True when the body cannot rotate, so its spin never needs clearing.</summary>
        public bool RotationLocked => _rotationLocked;

        // ---------------------------------------------------------------- one loop for the whole horde
        // 60 FPS pass (2026-10-14): about 300 enemies each had their own Update and FixedUpdate, and each read the
        // clock and the player's position from the engine several times a step. EnemyRunner now steps them all from
        // one Update and one FixedUpdate; during a step the clock and the target's position are read once.
        private static bool _inStep;
        private static float _stepTime;
        private static bool _stepTargetKnown, _stepTargetAlive;
        private static Transform _stepTarget;
        private static Vector3 _stepTargetPosition;

        /// <summary>True while EnemyRunner is stepping the horde.</summary>
        public static bool InStep => _inStep;
        /// <summary>Game time: read once per step while the horde is stepped, else Time.time.</summary>
        public static float Now => _inStep ? _stepTime : Time.time;

        /// <summary>The target's position (read once per step for the whole horde); false when there is no target.</summary>
        public bool TryGetTargetPosition(out Vector3 position)
        {
            var target = Target;
            if (!_inStep)
            {
                bool alive = target != null;
                position = alive ? target.position : default;
                return alive;
            }
            if (!_stepTargetKnown || !ReferenceEquals(target, _stepTarget))
            {
                _stepTarget = target;
                _stepTargetKnown = true;
                _stepTargetAlive = target != null;
                _stepTargetPosition = _stepTargetAlive ? target.position : default;
            }
            position = _stepTargetPosition;
            return _stepTargetAlive;
        }

        /// <summary>Steps every live enemy: its frame logic (physics false) or its movement (physics true).</summary>
        internal static void StepAll(bool physics)
        {
            if (ActiveEnemies.Count == 0) return;
            _stepTime = Time.time;
            _stepTargetKnown = false;
            _stepTarget = null;
            _inStep = true;
            try
            {
                if (physics) EnemyCrowd.EnsureBuilt();
                // Backwards: an enemy that leaves during its own step (a flier's lifetime) removes itself from the list.
                for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
                {
                    if (i >= ActiveEnemies.Count) continue;
                    var enemy = ActiveEnemies[i];
                    if (ReferenceEquals(enemy, null)) continue;
                    try
                    {
                        if (physics) enemy.PhysicsStep(); else enemy.FrameStep();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e, enemy); // one enemy's error never stops the rest of the horde
                    }
                }
            }
            finally
            {
                _inStep = false;
                _stepTarget = null;
            }
        }
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
        private float _crowdRadius;
        private bool _crowdRadiusKnown;
        /// <summary>CrowdRadius as last read; EnemyCrowd reads it afresh for a new spawn and every few rebuilds.</summary>
        internal float CrowdRadiusCached(bool refresh)
        {
            if (refresh || !_crowdRadiusKnown) { _crowdRadius = CrowdRadius; _crowdRadiusKnown = true; }
            return _crowdRadius;
        }
        /// <summary>This physics step's push from the crowd, written by EnemyCrowd.Rebuild.</summary>
        public Vector3 CrowdPush { get; set; }
        private int _crowdId;
        /// <summary>Instance id, cached (read for every enemy on every frame by EnemyCrowd).</summary>
        public int CrowdId => _crowdId != 0 ? _crowdId : (_crowdId = GetInstanceID());

        public bool IsSpectral => _isSpectral;

        /// <summary>
        /// VS Defang (Defang page): this life, the enemy cannot deal damage to the player, and it looks green unless the
        /// "Show Defanged Enemies" option is off. Set by EnemySpawner for ordinary wave spawns; cleared on every spawn.
        /// </summary>
        public bool IsDefanged { get; private set; }

        /// <summary>Soul Hunter's shade for a defanged enemy (VS: "a green appearance").</summary>
        public static readonly Color DefangTint = new Color(0.55f, 1f, 0.55f, 1f);

        /// <summary>The enemy type's own colour (EnemyData.VisualTint), or null for the model's colours.</summary>
        public Color? TypeTint => _enemyData != null && _enemyData.VisualTint.a > 0f ? _enemyData.VisualTint : (Color?)null;

        public void SetDefanged(bool defanged)
        {
            IsDefanged = defanged;
            var flash = GetComponentInChildren<SoulHunter.Gameplay.VFX.DamageFlash>();
            if (flash != null) flash.SetTint(defanged && SoulHunter.Core.Persistence.GameSettings.ShowDefangedEnemies ? DefangTint : TypeTint);
        }
        /// <summary>A stage boss (set by EnemySpawner.SpawnBoss; bosses have their own pools). VS: immune to Pentagram.</summary>
        public bool IsStageBoss { get; set; }
        public SoulHunter.Gameplay.Data.EnemyData Data => _enemyData;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            CachedTransform = transform;
            if (Rigidbody != null)
            {
                _flatBody = !Rigidbody.useGravity && (Rigidbody.constraints & RigidbodyConstraints.FreezePositionY) != 0;
                _rotationLocked = (Rigidbody.constraints & RigidbodyConstraints.FreezeRotation) == RigidbodyConstraints.FreezeRotation;
            }
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
            HasAnimator = Animator != null;
            _visualRoot = transform.Find("SH10_Visual");
            _scanner = GetComponent<EnvironmentScanner>();
            
            // 100% Data-Driven setup
            if (_enemyData != null)
            {
                MoveSpeed = _enemyData.MoveSpeed;
                
                var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
                if (health != null) health.Initialize(_enemyData.MaxHealth);

                _touchDamage = GetComponent<SoulHunter.Gameplay.Combat.TouchDamage>();
                ApplyContactDamage();

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

        private SoulHunter.Gameplay.Combat.TouchDamage _touchDamage;

        /// <summary>
        /// Contact damage from the data, times the Endless cycle's enemy damage (AGENTS.md run modes: +25% per cycle).
        /// Applied on every spawn (OnEnable), not only in Awake: pooled bodies, stage bosses among them, are reused in a
        /// later Endless cycle and kept cycle 0's damage (Bug MIS AI-05).
        /// </summary>
        private void ApplyContactDamage()
        {
            if (_touchDamage != null && _enemyData != null)
                _touchDamage.DamageAmount = Mathf.RoundToInt(_enemyData.DamageToPlayer * SoulHunter.Gameplay.Data.RunModes.EnemyDamageMultiplier);
        }

        private void Start()
        {
            // The type's tint after every Awake (DamageFlash collects its renderers in Awake).
            if (!IsDefanged && TypeTint.HasValue) SetDefanged(false);
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
            EnemyRunner.Ensure();
            // A flat body (no gravity, height frozen) never uses the ground scan: every stage floor is flat, so the chase
            // never tilts its path, and its vertical speed is 0 grounded or not. Skipping it saves two physics queries per
            // enemy 20 times a second (12,000 a second for 300 enemies; 60 FPS pass, 2026-10-14).
            _scans = _scanner != null && _scanner.enabled && !_flatBody;
            _crowdRadiusKnown = false; // spawners resize pooled bodies (chest elites, guardians)
            CrowdPush = Vector3.zero;  // the last life's push is not this life's
            ResetKnockback(); // pooled reuse starts with the data's knockback again (Garlic's increase lasts one life)
            ClearResistanceOverrides(); // Garlic's freeze-resistance reduction, and any override, last one life too
            IgnoresPentagram = false;   // the Tide Caller's Pentagram immunity lasts one life
            RestartAbilities();    // VS skills start again on every spawn (pooling-safe)
            SetDefanged(false); // Defang lasts one life; this also restores the type's own tint
            LingeringSlow = 1f;                 // Requiem of Souls' slow lasts one life
            ApplyContactDamage();  // this cycle's Endless damage on every spawn, pooled reuse included (AI-05)
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
            if (_abilities != null) _abilities.Stop();
            _currentState?.Exit();
            _currentState = null;
            // Spawners refresh Target on every spawn, including pooled reuse.
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>The state the enemy is in (chase, fly, frozen, hold, lunge ...).</summary>
        public IEnemyState CurrentState => _currentState;

        // ---------------------------------------------------------------- VS resistances and skills
        // vampire.survivors.wiki/w/Enemies, "Resistances": freeze, instant kill and debuff (EnemyResistance has the rules).
        private float _freezeResistance;
        private EnemyAbilities _abilities;

        /// <summary>Current freeze resistance: the data's value, lowered by Garlic hits during this life.</summary>
        public float FreezeResistance => _freezeResistance;
        /// <summary>VS instant kill resistance (Pentagram, Soul Toll). Stage bosses are always immune.</summary>
        public bool ResistsInstantKill => EnemyResistance.ResistsInstantKill(IsStageBoss,
            _instantKillResistanceOverride ?? (_enemyData != null && _enemyData.InstantKillResistance));
        /// <summary>
        /// This life is "unaffected by Pentagram" (VS The Drowner; the Tide Caller sets it). Cleared on every spawn.
        /// </summary>
        public bool IgnoresPentagram { get; set; }
        /// <summary>VS debuff resistance: Garlic's knockback and freeze-resistance reductions do not land.</summary>
        public bool ResistsDebuffs => _debuffResistanceOverride ?? (_enemyData != null && _enemyData.DebuffResistance);

        // This life's resistances when they are not the data's (Death's Shadow takes the VS Stalker statbox on a pooled
        // boss model). Cleared on every spawn (OnEnable). A stage boss stays immune to instant kills whatever the override.
        private bool? _instantKillResistanceOverride;
        private bool? _debuffResistanceOverride;

        /// <summary>Gives this life its own VS resistances (freeze, instant kill, debuff) instead of its data's.</summary>
        public void OverrideResistances(float freezeResistance, bool instantKillResistance, bool debuffResistance)
        {
            _freezeResistance = Mathf.Max(0f, freezeResistance);
            _instantKillResistanceOverride = instantKillResistance;
            _debuffResistanceOverride = debuffResistance;
        }

        /// <summary>Back to the data's resistances.</summary>
        public void ClearResistanceOverrides()
        {
            _instantKillResistanceOverride = null;
            _debuffResistanceOverride = null;
            ResetFreezeResistance();
        }
        /// <summary>This enemy's skill runner (null when its data has no abilities and none was ever started).</summary>
        public EnemyAbilities Abilities => _abilities;

        private void ResetFreezeResistance()
        {
            _freezeResistance = _enemyData != null ? Mathf.Max(0f, _enemyData.FreezeResistance) : 0f;
        }

        /// <summary>Starts this enemy's VS skills from its data (none: the runner, if any, is stopped).</summary>
        public void RestartAbilities()
        {
            var specs = _enemyData != null ? _enemyData.Abilities : null;
            if (_abilities == null) _abilities = GetComponent<EnemyAbilities>();
            if (specs == null || specs.Count == 0)
            {
                if (_abilities != null) _abilities.Stop();
                return;
            }
            if (_abilities == null) _abilities = gameObject.AddComponent<EnemyAbilities>();
            _abilities.Begin(this, specs);
        }

        /// <summary>
        /// Swaps this enemy's data at runtime (tests use a ScriptableObject.Instantiate copy so assets never change) and
        /// restarts its knockback, freeze resistance and skills. Health, speed, contact damage and drops keep what Awake set.
        /// </summary>
        public void UseData(SoulHunter.Gameplay.Data.EnemyData data)
        {
            _enemyData = data;
            ResetKnockback();
            ResetFreezeResistance();
            if (isActiveAndEnabled) RestartAbilities();
        }

        /// <summary>
        /// A weapon's freeze (Clock Lancet): lands unless the freeze resistance is higher than the weapon's freeze chance
        /// (VS). Returns true when the enemy was frozen. Orologion uses Freeze directly and ignores resistance.
        /// </summary>
        public bool TryWeaponFreeze(float weaponFreezeChance, float duration)
        {
            if (!EnemyResistance.WeaponFreezes(_freezeResistance, weaponFreezeChance)) return false;
            Freeze(duration);
            return true;
        }

        /// <summary>
        /// One Garlic hit's debuffs (VS Garlic, vampire.survivors.wiki/w/Garlic): +0.3 knockback taken up to the maximum
        /// and -0.1 freeze resistance down to 0, unless the enemy resists debuffs. Returns true when they landed.
        /// </summary>
        public bool ApplyGarlicDebuff()
        {
            if (!EnemyResistance.GarlicDebuffApplies(ResistsDebuffs)) return false;
            AddKnockbackTaken(GarlicKnockbackStep);
            _freezeResistance = EnemyResistance.AfterGarlicHit(_freezeResistance);
            return true;
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
            // VS Out of Bounds (XII): "Freezing enemies generates explosions."
            SoulHunter.Gameplay.Core.ArcanaManager.OnEnemyFrozen(this);
            var resume = _currentState is EnemyFrozenState frozen ? frozen.ResumeState
                : _currentState is EnemyFlyState ? _currentState : null;
            ChangeState(new EnemyFrozenState(this, duration, resume));
        }

        /// <param name="speed">Relative to the default event speed (1 = 1.5x the enemy's own speed).</param>
        /// <param name="weave">Above 0, the flier weaves along its line (EnemyFlyState).</param>
        public void StartFlyingMode(Vector3 direction, float speed = 1f, float weave = 0f)
        {
            ChangeState(new EnemyFlyState(this, direction, EnemyFlyState.DefaultLifetimeSeconds,
                EnemyFlyState.SpeedMultiplier * Mathf.Max(0.2f, speed), weave));
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
        public bool IsKnockedBack => Now < _knockbackUntil;
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
            bool fixedCourse = Movement == SoulHunter.Gameplay.Data.EnemyMovement.FixedDirection ||
                               Movement == SoulHunter.Gameplay.Data.EnemyMovement.FloatyFixedDirection;
            if (!fixedCourse || Target == null) return;
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
            MoveSpeed = _baseMoveSpeed * CampaignSpeedMultiplier * LingeringSlow;
        }

        /// <summary>
        /// Requiem of Souls' lasting slow (VS Mannajja, vampire.survivors.wiki/w/Mannajja): "each hit reduces the Movement
        /// Speed of the affected enemy by an additional 5%, until it reaches a minimum of 20% of their original speed after
        /// 16 hits", unless the enemy resists debuffs. Lasts one life.
        /// </summary>
        public float LingeringSlow { get; private set; } = 1f;

        public bool ApplyLingeringSlow(float step, float floor)
        {
            if (ResistsDebuffs) return false;
            LingeringSlow = Mathf.Max(floor, LingeringSlow - step);
            MoveSpeed = _baseMoveSpeed * CampaignSpeedMultiplier * LingeringSlow;
            return true;
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

        // Called by EnemyRunner once a frame (it was this enemy's own Update).
        private void FrameStep()
        {
            FaceTarget();
            _currentState?.UpdateLogic();
        }

        private void FaceTarget()
        {
            // Fliers keep facing their path, set once by FaceDirection.
            if (_visualRoot == null || IsFlying || !TryGetTargetPosition(out Vector3 target)) return;
            FaceDirection(target - CachedTransform.position);
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

        // Called by EnemyRunner once a physics step (it was this enemy's own FixedUpdate).
        private void PhysicsStep()
        {
            if (_currentState != null)
            {
                // Agar scanner disable kiya hua hai, toh CPU bachaane ke liye usey mat chalao
                if (_scans)
                {
                    // Ground data does not need to be recalculated at every physics
                    // tick for a moving swarm. Refresh at 20 Hz and reuse the last
                    // result between scans.
                    if (Now >= _nextEnvironmentScanTime)
                    {
                        _cachedEnvironmentData = _scanner.ScanEnvironment();
                        _nextEnvironmentScanTime = Now + 0.05f;
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
