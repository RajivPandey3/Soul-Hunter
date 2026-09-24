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
        private readonly Collider[] _dashHits = new Collider[50];

        public float MoveSpeed
        {
            get
            {
                if (_stats == null) _stats = GetComponent<PlayerStats>();
                return _moveSpeed * (_stats != null ? _stats.MoveSpeedMultiplier : 1f) * _campaignMovementMultiplier;
            }
        }
        public static PlayerController Instance { get; private set; }
        public PlayerStats Stats => _stats != null ? _stats : (_stats = GetComponent<PlayerStats>());

        public Rigidbody Rigidbody { get; private set; }
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
            Rigidbody = GetComponent<Rigidbody>();
            // Physics movement ko render frames ke beech smooth rakho.
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
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
                Time.timeScale = 1f;
            }

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
            }

            ChangeState(new PlayerIdleState(this));
        }

        private void HandlePlayerDeath()
        {
            var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            var stats = GetComponent<PlayerStats>();
            if (health != null && stats != null && stats.UseRevival())
            {
                health.ReviveFromDeath();
                Debug.Log($"[PlayerController] Revival consumed. Remaining revivals: {stats.Revivals}");
                return;
            }

            if (SoulHunter.Gameplay.Core.GameSessionManager.Instance != null)
            {
                SoulHunter.Gameplay.Core.GameSessionManager.Instance.GameOver();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

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

        private void OnDashEvent(PlayerDashEvent dashEvent)
        {
            if (!IsDashing)
            {
                StartCoroutine(DashCoroutine());
            }
        }

        private IEnumerator DashCoroutine()
        {
            IsDashing = true;

            var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
            if (health != null) health.IsInvincible = true;

            // 1. Phasing: Kinematic on taake dushmano ki bheed (horde) dash ko rok na sake
            Rigidbody.isKinematic = true;

            Vector3 dashDirection = GetCameraRelativeMovement(CurrentMoveInput).normalized;

            if (dashDirection == Vector3.zero)
            {
                dashDirection = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
            }

            // 2. Damage Buffer for Soul Reap Dash
            Collider[] dashHits = _dashHits;
            int enemyLayer = LayerMask.GetMask("Enemy");

            float elapsed = 0;
            while(elapsed < _dashDuration)
            {
                // Smooth glide through enemies
                transform.position += dashDirection * (_dashSpeed * Time.deltaTime);

                // 3. Deal damage to anyone we pass through (Soul Reap)
                int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, 1.5f, dashHits, enemyLayer);
                for (int i = 0; i < hitCount; i++)
                {
                    if (dashHits[i] == null) continue;
                    var damageable = dashHits[i].GetComponentInParent<SoulHunter.Gameplay.Combat.IDamageable>();
                    if (damageable != null)
                    {
                        // Dash deals fixed 50 damage
                        damageable.TakeDamage(new SoulHunter.Gameplay.Combat.DamagePacket(50, transform.position, dashDirection));
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Revert Phasing
            Rigidbody.isKinematic = false;
            if (health != null) health.IsInvincible = false;

            // Cooldown
            yield return new WaitForSeconds(_dashCooldown);
            IsDashing = false;
        }

        private void Update()
        {
            _currentState?.UpdateLogic();

            // Face the direction of movement
            if (CurrentMoveInput.x != 0)
            {
                // Flips the whole player object (best for 2D sprites, works for 3D too)
                float sign = Mathf.Sign(CurrentMoveInput.x);
                transform.localScale = new Vector3(sign, 1, 1);
            }
        }

        private void FixedUpdate()
        {
            if (IsDashing) return;

            if (_currentState != null)
            {
                var envData = _scanner != null ? _scanner.ScanEnvironment() : new SoulHunter.Gameplay.Physics.EnvironmentData();
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

            // A level-specific 3D visual may already be attached to the gameplay root.
            // Keep the authored visual and do not spawn the legacy character visual again.
            if (GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            {
                Animator = GetComponentInChildren<SoulHunter.Gameplay.Animation.EntityAnimator>();
                return;
            }

            // 2. Read selected hero name from SaveService
            var saveSvc = GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
            string heroName = "Hero_1_0"; // Default Fallback agar save delete ho chuki ho

            if (saveSvc != null && !string.IsNullOrEmpty(saveSvc.CurrentData.SelectedCharacterName))
            {
                heroName = saveSvc.CurrentData.SelectedCharacterName;
            }

            // 3. Load the corresponding CharacterData from Resources
            var charData = SoulHunter.Gameplay.Data.GameContentCatalog.FindCharacter(heroName) ?? Resources.Load<SoulHunter.Gameplay.Data.CharacterData>($"Characters/{heroName}") ??
                           Resources.Load<SoulHunter.Gameplay.Data.CharacterData>($"Characters/Char_{heroName}");

            if (charData != null && charData.CharacterModelPrefab != null)
                {
                    // 4. Spawn the beautiful 2D Prefab inside the player root
                    var visualModel = Instantiate(charData.CharacterModelPrefab, transform);
                    visualModel.transform.localPosition = Vector3.zero;

                    // 5. Connect the new Animator to our Controller
                    Animator = visualModel.GetComponent<SoulHunter.Gameplay.Animation.EntityAnimator>()
                               ?? GetComponentInChildren<SoulHunter.Gameplay.Animation.EntityAnimator>();
                }
        }
    }
}
