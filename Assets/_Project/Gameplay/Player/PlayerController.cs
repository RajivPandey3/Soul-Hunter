using UnityEngine;
using SoulHunter.Core.Events;
using SoulHunter.Core.Services;
using System;
using System.Collections;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// Learning Comment:
    /// Hybrid System compliance: Ye class direct Input nahi leti, balki EventBus se event sunti hai.
    /// Isme movement logic direct nahi likhi gayi hai, balki IPlayerState ko pass ki gayi hai (Single Responsibility).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        private EventBus _eventBus;
        private IPlayerState _currentState;
        private SoulHunter.Gameplay.Physics.EnvironmentScanner _scanner;

        // Cache the delegate to avoid allocation during Unsubscribe
        private Action<PlayerMoveEvent> _onMoveEventHandler;
        private Action<PlayerAttackEvent> _onAttackEventHandler;
        private Action<PlayerDashEvent> _onDashEventHandler;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _dashSpeed = 15f;
        [SerializeField] private float _dashDuration = 0.2f;
        [SerializeField] private float _dashCooldown = 1.5f;
        private float _campaignMovementMultiplier = 1f;

        private PlayerStats _stats;
        private Transform _movementCamera;

        public float MoveSpeed
        {
            get
            {
                if (_stats == null) _stats = GetComponent<PlayerStats>();
                float speed = _moveSpeed * (_stats != null ? _stats.MoveSpeedMultiplier : 1f) * _campaignMovementMultiplier
                              * SoulHunter.Gameplay.Data.CampaignLevelCatalog.CurrentPlayerSpeed; // VS stage bonus
                if (IsSlowed) speed *= _slowFactor;
                if (_stats != null && _stats.LowHealthSpeedBonus > 0f)
                {
                    if (_health == null) _health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
                    if (_health != null) speed *= _stats.LowHealthSpeedFactor(_health.CurrentHealth, _health.MaxHealth);
                }
                return speed;
            }
        }
        private SoulHunter.Gameplay.Combat.HealthController _health;
        public static PlayerController Instance { get; private set; }
        public PlayerStats Stats => _stats != null ? _stats : (_stats = GetComponent<PlayerStats>());

        public Rigidbody Rigidbody { get; private set; }
        /// <summary>The hero's body radius and the layers that block it (ObstacleSlide).</summary>
        public float BodyRadius { get; private set; } = 0.4f;
        public int ObstacleMask { get; private set; }
        private Vector2 _eventBusInput;

        /// <summary>
        /// Learning Comment:
        /// Robust Movement Input:
        /// 1. Primary: EventBus se aane wala input (Clean Architecture).
        /// 2. Fallback: Hardware polling (WASD/Arrows) agar game start par Game View focus na ho ya event drop ho jaye.
        /// Is tarah player pehle frame se hi bina kisi rukawat ke move kar sakega.
        /// </summary>
        public Vector2 CurrentMoveInput
        {
            get
            {
                if (_eventBusInput.sqrMagnitude > 0.001f)
                    return _eventBusInput;

                return PollDirectInput();
            }
            private set => _eventBusInput = value;
        }
        public SoulHunter.Gameplay.Animation.EntityAnimator Animator { get; private set; }
        public bool IsDashing { get; private set; }
        private const float DashProbeRadius = 0.4f; // the player's capsule radius
        private const float DashSkin = 0.05f;

        // Owner decision (2026-09-30): Shadow Kael's mirrored Clock Lancet slows the player to half speed for the
        // weapon's freeze time. VS has no player freeze; this is the owner's exception. Game time, so pauses hold it.
        public const float ClockLancetSlowFactor = 0.5f;
        private float _slowUntil = -1f;
        private float _slowFactor = 1f;
        public bool IsSlowed => Time.time < _slowUntil;

        /// <summary>Moves at <paramref name="factor"/> x speed for <paramref name="seconds"/>; a running slow keeps the longer end and the stronger factor.</summary>
        public void Slow(float factor, float seconds)
        {
            if (seconds <= 0f) return;
            _slowFactor = IsSlowed ? Mathf.Min(_slowFactor, factor) : factor;
            _slowUntil = Mathf.Max(_slowUntil, Time.time + seconds);
        }

        public void SetCampaignMovementMultiplier(float multiplier)
        {
            _campaignMovementMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
        }

        public Vector3 GetCameraRelativeMovement(Vector2 input)
        {
            Vector2 clamped = Vector2.ClampMagnitude(input, 1f);
            if (_movementCamera == null && Camera.main != null) _movementCamera = Camera.main.transform;
            if (_movementCamera == null) return new Vector3(clamped.x, 0f, clamped.y);

            // Build a flat, orthonormal basis from camera yaw only. Using the
            // camera's pitched Up vector makes forward/back movement depend on
            // the camera angle and can make that axis feel slower in Game View.
            Vector3 forward = Vector3.ProjectOnPlane(_movementCamera.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            else
                forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < 0.001f || forward.sqrMagnitude < 0.001f)
                return new Vector3(clamped.x, 0f, clamped.y);
            return (right * clamped.x + forward * clamped.y).normalized * clamped.magnitude;
        }

        /// <summary>
        /// Level-up/chest pause ke baad movement state ko stale attack state mein
        /// atakne se bachata hai. Input ko preserve karke player turant normal
        /// Idle/Run state mein resume hota hai.
        /// </summary>
        public void ResumeMovementAfterMenu()
        {
            if (Rigidbody != null && !IsDashing)
            {
                Rigidbody.linearVelocity = new Vector3(0f, Rigidbody.linearVelocity.y, 0f);
            }

            ChangeState(CurrentMoveInput.sqrMagnitude > 0.01f
                ? new PlayerRunState(this)
                : new PlayerIdleState(this));
        }

        private void Awake()
        {
            Instance = this;
            var ownHealth = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (ownHealth != null) ownHealth.PostHitInvulnerability = SoulHunter.Gameplay.Combat.HealthController.VsInvulnerabilityTime; // VS 240 ms
            Rigidbody = GetComponent<Rigidbody>();
            // Physics movement ko render frames ke beech smooth rakho.
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            // Slippery obstacles (ObstacleSlide): no friction on the hero's body, and its radius for the slide's probes.
            ObstacleMask = LayerMask.GetMask("Obstacle");
            foreach (var body in GetComponents<Collider>())
            {
                if (body.isTrigger) continue;
                body.sharedMaterial = ObstacleSlide.Slippery;
                if (body is CapsuleCollider capsule) BodyRadius = capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            }
            if (Camera.main != null) _movementCamera = Camera.main.transform;
            _scanner = GetComponent<SoulHunter.Gameplay.Physics.EnvironmentScanner>();
            Animator = GetComponentInChildren<SoulHunter.Gameplay.Animation.EntityAnimator>();
        }

        private void Start()
        {
            // Learning Comment:
            // 1. Unpause: Agar pichle session/menu se timeScale 0 reh gaya ho,
            // to physics aur movement ko unfreeze karne ke liye timeScale reset karein.
            if (Time.timeScale <= 0f)
            {
                SoulHunter.Core.Services.GameTime.ResetAll();
            }

            ApplySelectedCharacterStats();
            ApplyHealingPowerUps();
            var hurtHealth = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (hurtHealth != null) hurtHealth.OnDamaged += PlayHurtSound;

            var infiniteMap = FindFirstObjectByType<SoulHunter.Gameplay.Environment.InfiniteMap>();
            if (infiniteMap != null)
            {
                infiniteMap.SetPlayer(transform);
            }

            // Learning Comment:
            // 2. Standalone Editor Fallback:
            // Agar developer ne Bootstrap scene ke bajaye seedha Gameplay scene play kiya ho,
            // to services auto-bootstrap ho jayein taaki controller crash na kare.
            if (GameServices.Instance == null)
            {
                Debug.LogWarning("[PlayerController] GameServices.Instance was NULL! Auto-bootstrapping fallback services for standalone play.");
                var services = new GameServices();
                var installer = new SoulHunter.Core.Bootstrap.BootstrapInstaller(services);
                installer.Install();
            }

            LoadVisualModel();

            _eventBus = GameServices.Instance.Get<EventBus>();

            if (_eventBus == null)
            {
                Debug.LogError("[PlayerController] EventBus is NULL in GameServices!");
                ChangeState(new PlayerIdleState(this));
                return;
            }

            _onMoveEventHandler = OnMoveEvent;
            _onAttackEventHandler = OnAttackEvent;
            _onDashEventHandler = OnDashEvent;

            _eventBus.Subscribe(_onMoveEventHandler);
            _eventBus.Subscribe(_onAttackEventHandler);
            _eventBus.Subscribe(_onDashEventHandler);

            // Kami #4 Fix: Player ke marne par Game Over
            var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                health.OnDied += HandlePlayerDeath;
                // VS Revival: a lethal hit with a Revival left never fires OnDied; it is held for the Revive button.
                health.TryPreventDeath = HoldDeathForRevive;
            }

            ChangeState(new PlayerIdleState(this));
        }

        /// <summary>
        /// Opens the Revive screen (set by the HUD's ReviveScreen). Returns false when there is no screen; the player is
        /// then revived at once. Gameplay does not reference the UI: the screen registers itself here.
        /// </summary>
        public Func<PlayerController, bool> ShowReviveScreen { get; set; }
        /// <summary>Fires after every revival (VS Revival "On Revival" effects can hook here), with the health restored.</summary>
        public event Action<int> OnRevived;
        /// <summary>The player died with a Revival left and waits for the Revive button.</summary>
        public bool IsAwaitingRevive
        {
            get
            {
                var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
                return health != null && health.IsAwaitingRevive;
            }
        }

        /// <summary>
        /// HealthController.TryPreventDeath: with a Revival left, the death is held at 0 health (no OnDied, no Game Over)
        /// and the Revive screen opens. With none left, the player dies (OnDied, then Game Over).
        /// </summary>
        private bool HoldDeathForRevive()
        {
            var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            var stats = Stats;
            if (health == null || stats == null || stats.Revivals <= 0) return false;
            if (health.IsAwaitingRevive) return true; // already held: one death, one Revival (VS v1.6.0 "revives triggering twice")
            health.HoldForRevive();
            if (ShowReviveScreen == null || !ShowReviveScreen(this)) ConfirmRevive(); // no screen (tests without a HUD)
            return true;
        }

        /// <summary>
        /// The Revive button: spends exactly one Revival, revives with 50% of max health (VS Revival), releases a spent
        /// Tirajisu (wiki Tirajisú: "only be removed from the inventory when the last Revive is used up") and plays the
        /// revive effect. Does nothing unless a death is held, so a second press can never spend a second Revival.
        /// </summary>
        public bool ConfirmRevive()
        {
            var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            var stats = Stats;
            if (health == null || !health.IsAwaitingRevive) return false;
            if (stats == null || !stats.UseRevival())
            {
                health.FinishDeath(); // nothing left to spend: the normal death and Game Over
                return false;
            }
            int before = health.CurrentHealth;
            health.ReviveFromDeath();
            Debug.Log($"[PlayerController] Revival consumed. Remaining revivals: {stats.Revivals}");
            var weapons = GetComponentInChildren<SoulHunter.Gameplay.Combat.WeaponManager>();
            if (weapons == null) weapons = SoulHunter.Gameplay.Combat.WeaponManager.Instance;
            if (weapons != null) weapons.ReleaseSpentTirajisu(stats.Revivals);
            // Placeholder look and sound (existing level-up circle, heal effect and heal sound); the wiki shows an angel
            // animation, and Soul Hunter's own revive art and audio wait for the owner.
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, transform.position);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Heal, transform.position);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.HealSound);
            OnRevived?.Invoke(health.CurrentHealth - before);
            return true;
        }

        private void HandlePlayerDeath()
        {
            if (SoulHunter.Gameplay.Core.GameSessionManager.Instance != null)
            {
                SoulHunter.Gameplay.Core.GameSessionManager.Instance.GameOver();
            }
        }

        // VS: the player hears every hit taken (throttled so a crowd does not drone).
        private void PlayHurtSound()
        {
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFXThrottled(audio.PlayerHurtSound, SoulHunter.Gameplay.Audio.AudioManager.PlayerHurtInterval, 0.8f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var hurtHealth = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (hurtHealth != null) hurtHealth.OnDamaged -= PlayHurtSound;

            if (_eventBus != null)
            {
                if (_onMoveEventHandler != null) _eventBus.Unsubscribe(_onMoveEventHandler);
                if (_onAttackEventHandler != null) _eventBus.Unsubscribe(_onAttackEventHandler);
                if (_onDashEventHandler != null) _eventBus.Unsubscribe(_onDashEventHandler);
            }

            var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null)
            {
                health.OnDied -= HandlePlayerDeath;
            }
        }

        private void OnMoveEvent(PlayerMoveEvent moveEvent)
        {
            CurrentMoveInput = moveEvent.Direction;
        }

        private void OnAttackEvent(PlayerAttackEvent attackEvent)
        {
            // Don't switch to attack if we are already attacking
            if (_currentState is not PlayerAttackState)
            {
                ChangeState(new PlayerAttackState(this));
            }
        }

        /// <summary>
        /// VS rule: the chosen character's starting max health and stat modifiers apply to the run.
        /// Exact match only, so an unknown or missing selection keeps the default stats.
        /// </summary>
        private void ApplySelectedCharacterStats()
        {
            if (GameServices.Instance == null) return;
            var saveSvc = GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
            if (saveSvc == null || saveSvc.CurrentData == null) return;

            var character = SoulHunter.Gameplay.Data.GameContentCatalog.FindCharacterExact(saveSvc.CurrentData.SelectedCharacterName);
            if (character == null) return;

            var stats = Stats;
            if (stats != null) stats.ApplyCharacter(character);
            var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null && character.BaseMaxHealth > 0) health.Initialize(character.BaseMaxHealth);
            // VS Golden Egg rule: the character's Soul Eggs raise its starting stats (after its max health is set).
            SoulEggs.Apply(SoulEggs.For(saveSvc.CurrentData, character.CharacterName), stats);
            if (health != null && stats != null && stats.StartingShieldSeconds > 0f)
            {
                health.ShieldedUntil = Time.time + stats.StartingShieldSeconds;
                StartingShieldVisual.Attach(health, character.ShieldVisualPrefab);
            }
        }

        /// <summary>
        /// VS Max Health PowerUp: x1.1 per rank on top of the character's, the run starting at full health. The other
        /// PowerUps are applied by PlayerStats.ApplyPowerUps.
        /// </summary>
        private void ApplyHealingPowerUps()
        {
            if (GameServices.Instance == null) return;
            var saveSvc = GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
            var data = saveSvc != null ? saveSvc.CurrentData : null;
            if (data == null) return;
            var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            float multiplier = SoulHunter.Core.Persistence.PowerUps.MaxHealthMultiplier(SoulHunter.Core.Persistence.PowerUps.ActiveRank(data, SoulHunter.Core.Persistence.PowerUpId.MaxHealth)); // 0 while switched off
            if (health != null && multiplier > 1f) health.MultiplyMaxHealth(multiplier, healByIncrease: true);
        }

        private void OnDashEvent(PlayerDashEvent dashEvent)
        {
            // A menu that pauses the game (level-up, chest, pause, shop) takes Space as its Submit key (VS: Space works like
            // Enter in the UI); before, the press also started a dash that ran the moment the game resumed (2026-10-18).
            if (SoulHunter.Core.Services.GameTime.IsPaused) return;
            if (!IsDashing)
            {
                StartCoroutine(DashCoroutine());
            }
        }

        private IEnumerator DashCoroutine()
        {
            IsDashing = true;

            var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            // Its own count, not IsInvincible (Bug MIS CMB-10, 2026-10-19): ending the dash used to clear Soul Burst's
            // 2 s immunity and the bot's god mode with the same bool.
            if (health != null) health.BeginAbilityImmunity();

            // 1. Phasing: Kinematic on taake dushmano ki bheed (horde) dash ko rok na sake
            Rigidbody.isKinematic = true;

            Vector3 dashDirection = GetCameraRelativeMovement(CurrentMoveInput).normalized;

            if (dashDirection == Vector3.zero)
            {
                dashDirection = FacingDirection;
            }

            // Owner decision (2026-09-30): the dash only moves and avoids damage, as the GDD says ("dash through
            // enemies and avoid damage"). It used to deal a flat 50 damage to every enemy it passed; VS has no dash.
            // The body is moved, not the transform: the player's Rigidbody interpolates, which overwrote transform
            // writes every frame, so the dash covered about 0.7 of its 3 units (found 2026-09-30, DashPlayModeTests).
            // Enemies are passed through; stage obstacles and corridor walls still stop the dash.
            int obstacles = LayerMask.GetMask("Obstacle");
            float elapsed = 0;
            while(elapsed < _dashDuration)
            {
                // A pause (a level-up from XP gained mid-dash, a chest, the pause menu) holds the dash: the step is 0 then,
                // and the wall check below used to read that as a wall and end the dash for good (Bug MIS CORE-07, 2026-10-19).
                if (SoulHunter.Core.Services.GameTime.IsPaused) { yield return null; continue; }
                float step = _dashSpeed * Time.deltaTime;
                Vector3 from = Rigidbody.position;
                if (UnityEngine.Physics.SphereCast(from, DashProbeRadius, dashDirection, out RaycastHit wall, step + DashSkin, obstacles, QueryTriggerInteraction.Ignore))
                    step = Mathf.Max(0f, wall.distance - DashSkin);
                Vector3 to = from + dashDirection * step;
                Rigidbody.position = to;
                transform.position = to;
                if (step <= 0f) break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Revert Phasing
            Rigidbody.isKinematic = false;
            if (health != null) health.EndAbilityImmunity();

            // Cooldown
            yield return new WaitForSeconds(_dashCooldown);
            IsDashing = false;
        }

        private void Update()
        {
            _currentState?.UpdateLogic();
            UpdateFacing();
        }

        // --- Facing (3D model turns smoothly; the old code mirrored the model with localScale.x = +-1) ---
        public const float AttackFacingSeconds = 0.45f;
        public const float AttackPoseInterval = 0.6f; // matches the ~0.6 s upper-body pose (PlayerAnimatorSetup.AttackSpeed)
        private const float TurnSharpness = 18f;
        private Transform _visual;
        private Quaternion _visualBaseRotation = Quaternion.identity;
        private float _attackFacingUntil = -1f;
        private float _nextAttackPose;
        private Vector3 _attackDirection = Vector3.forward;

        /// <summary>Flat direction the player's model faces (starts facing up the screen).</summary>
        public Vector3 FacingDirection { get; private set; } = Vector3.forward;

        /// <summary>
        /// A weapon just fired: face the enemy it aimed at for a moment (zero = no enemy, keep facing) and play
        /// the attack pose on the upper body, at most every AttackPoseInterval so several weapons do not spam it.
        /// </summary>
        public void ShowWeaponAttack(Vector3 towardEnemy)
        {
            towardEnemy.y = 0f;
            if (towardEnemy.sqrMagnitude > 0.0001f)
            {
                _attackDirection = towardEnemy.normalized;
                _attackFacingUntil = Time.time + AttackFacingSeconds;
            }
            if (Animator != null && Time.time >= _nextAttackPose)
            {
                Animator.TriggerAttack();
                _nextAttackPose = Time.time + AttackPoseInterval;
            }
        }

        private void UpdateFacing()
        {
            Vector3 desired = FacingDirection;
            if (Time.time < _attackFacingUntil) desired = _attackDirection;
            else if (CurrentMoveInput.sqrMagnitude > 0.01f)
            {
                var move = GetCameraRelativeMovement(CurrentMoveInput);
                move.y = 0f;
                if (move.sqrMagnitude > 0.0001f) desired = move.normalized;
            }
            FacingDirection = desired;
            if (_visual == null) return;
            var target = Quaternion.LookRotation(desired, Vector3.up) * _visualBaseRotation;
            _visual.localRotation = Quaternion.Slerp(_visual.localRotation, target, 1f - Mathf.Exp(-TurnSharpness * Time.deltaTime));
        }

        /// <summary>
        /// Remembers the rotation that makes the model face +Z (up the screen); turning is applied on top.
        /// Models differ (Kael's authored visual faces +Z, the hero prefabs face -Z), so the model's front is
        /// worked out from its legs (the attack pose moves the arms): front = (right thigh - left thigh) x up.
        /// </summary>
        private void BindVisual(Transform visual)
        {
            _visual = visual;
            transform.localScale = Vector3.one;
            if (visual == null) return;
            _visualBaseRotation = visual.localRotation;
            Transform left = null, right = null;
            foreach (var bone in visual.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "Thigh.L") left = bone;
                else if (bone.name == "Thigh.R") right = bone;
            }
            if (left == null || right == null) return;
            var front = Vector3.Cross(transform.InverseTransformVector(right.position - left.position), Vector3.up);
            front.y = 0f;
            if (front.sqrMagnitude < 0.0001f) return;
            _visualBaseRotation = Quaternion.Inverse(Quaternion.LookRotation(front.normalized, Vector3.up)) * visual.localRotation;
            visual.localRotation = _visualBaseRotation;
        }

        private void FixedUpdate()
        {
            if (IsDashing) return;

            if (_currentState != null)
            {
                // Timed sections (SlowSpan, free under 5 ms): a 98 ms frame was profiled inside this method (2026-10-28).
                SoulHunter.Gameplay.Physics.EnvironmentData envData;
                using (SoulHunter.Core.Services.SlowSpan.Measure("Player.Scan"))
                    envData = _scanner != null ? _scanner.ScanEnvironment() : new SoulHunter.Gameplay.Physics.EnvironmentData();
                using (SoulHunter.Core.Services.SlowSpan.Measure("Player.Move"))
                    _currentState.UpdatePhysics(ref envData);
            }
        }

        public void ChangeState(IPlayerState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        /// <summary>
        /// Learning Comment:
        /// Hardware Direct Polling Fallback:
        /// Game start hone par agar Game View focused na ho ya New Input System event drop ho jaye,
        /// ye method seedha hardware keyboard/joystick se direct input le kar responsive movement deta hai.
        /// </summary>
        private Vector2 PollDirectInput()
        {
            Vector2 direct = Vector2.zero;

            // Direct keyboard keys check (WASD & Arrows)
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) direct.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) direct.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) direct.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) direct.x += 1f;

            // Joysticks / Axes fallback
            if (direct.sqrMagnitude < 0.001f)
            {
                float h = UnityEngine.Input.GetAxisRaw("Horizontal");
                float v = UnityEngine.Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(h) > 0.05f || Mathf.Abs(v) > 0.05f)
                {
                    direct = new Vector2(h, v);
                }
            }

            return direct.sqrMagnitude > 1f ? direct.normalized : direct;
        }

        private void LoadVisualModel()
        {
            // 1. Hide the ugly white capsule
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.enabled = false;

            // 2. Read selected hero name from SaveService
            var saveSvc = GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
            string heroName = "Kael"; // Default Fallback agar save delete ho chuki ho

            if (saveSvc != null && !string.IsNullOrEmpty(saveSvc.CurrentData.SelectedCharacterName))
            {
                heroName = saveSvc.CurrentData.SelectedCharacterName;
            }

            // 3. Load the corresponding CharacterData from Resources
            var charData = SoulHunter.Gameplay.Data.GameContentCatalog.FindCharacter(heroName) ?? Resources.Load<SoulHunter.Gameplay.Data.CharacterData>($"Characters/{heroName}") ??
                           Resources.Load<SoulHunter.Gameplay.Data.CharacterData>($"Characters/Char_{heroName}");

            // A 3D visual may already be attached to the gameplay root (Player.prefab carries Kael).
            // A character with its own 3D hero model replaces it; otherwise the authored visual stays.
            var authored = AuthoredVisual();
            if (authored != null)
            {
                var heroModel = charData != null ? charData.CharacterModelPrefab : null;
                if (heroModel != null && heroModel.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                {
                    var hero = Instantiate(heroModel, transform);
                    hero.name = heroModel.name;
                    hero.transform.SetLocalPositionAndRotation(authored.localPosition, authored.localRotation);
                    hero.transform.localScale = authored.localScale;
                    authored.gameObject.SetActive(false);
                    Destroy(authored.gameObject);
                    var flash = GetComponent<SoulHunter.Gameplay.VFX.DamageFlash>();
                    if (flash != null) flash.RefreshRenderers();
                    Animator = hero.GetComponentInChildren<SoulHunter.Gameplay.Animation.EntityAnimator>();
                    BindVisual(hero.transform);
                    return;
                }
                Animator = GetComponentInChildren<SoulHunter.Gameplay.Animation.EntityAnimator>();
                BindVisual(authored);
                return;
            }

            if (charData != null && charData.CharacterModelPrefab != null)
                {
                    // 4. Spawn the beautiful 2D Prefab inside the player root
                    var visualModel = Instantiate(charData.CharacterModelPrefab, transform);
                    visualModel.name = charData.CharacterModelPrefab.name;
                    visualModel.transform.localPosition = Vector3.zero;

                    // 5. Connect the new Animator to our Controller
                    Animator = visualModel.GetComponent<SoulHunter.Gameplay.Animation.EntityAnimator>()
                               ?? GetComponentInChildren<SoulHunter.Gameplay.Animation.EntityAnimator>();
                    BindVisual(visualModel.transform);
                }
        }

        /// <summary>The direct child that holds the authored 3D model (a SkinnedMeshRenderer), or null.</summary>
        private Transform AuthoredVisual()
        {
            var skinned = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinned == null) return null;
            var node = skinned.transform;
            while (node.parent != null && node.parent != transform) node = node.parent;
            return node.parent == transform ? node : null;
        }
    }
}
