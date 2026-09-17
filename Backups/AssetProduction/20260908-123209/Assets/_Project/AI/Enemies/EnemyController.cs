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
        
        [SerializeField] private SoulHunter.Gameplay.Data.EnemyData _enemyData;

        public float MoveSpeed { get; private set; }
        private float _baseMoveSpeed;
        public float ChaseDistance = 500f; // Vampire Survivors enemies always chase
        public float AttackDistance = 1.5f;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Animator = GetComponentInChildren<EntityAnimator>();
            _scanner = GetComponent<EnvironmentScanner>();
            
            // 100% Data-Driven setup
            if (_enemyData != null)
            {
                MoveSpeed = _enemyData.MoveSpeed;
                
                var health = GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
                if (health != null) health.Initialize(_enemyData.MaxHealth);
                
                var drop = GetComponent<EnemyDrop>();
                if (drop != null) 
                {
                    drop.ChickenDropChance = _enemyData.DropChanceChicken;
                    drop.DropsChest = _enemyData.DropsChest;
                }
            }
            else
            {
                MoveSpeed = 3f; // Default fallback
            }
            _baseMoveSpeed = MoveSpeed;

            // Target Player ko dhundho
            var player = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
            if (player != null)
            {
                Target = player.transform;
            }

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

        private void OnEnable()
        {
            if (!ActiveEnemies.Contains(this)) ActiveEnemies.Add(this);
            if (Target == null)
            {
                var player = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
                if (player != null) Target = player.transform;
            }
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }
            if (_currentState == null) ChangeState(new EnemyIdleState(this));
        }

        private void OnDisable()
        {
            ActiveEnemies.Remove(this);
            _currentState?.Exit();
            _currentState = null;
            Target = null;
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        public void StartFlyingMode(Vector3 direction)
        {
            ChangeState(new EnemyFlyState(this, direction));
        }

        public void ApplyCampaignSpeed(float multiplier)
        {
            MoveSpeed = _baseMoveSpeed * Mathf.Max(0.1f, multiplier);
        }

        private void Update()
        {
            _currentState?.UpdateLogic();
        }

        private void FixedUpdate()
        {
            if (_currentState != null)
            {
                // Agar scanner disable kiya hua hai, toh CPU bachaane ke liye usey mat chalao
                var envData = (_scanner != null && _scanner.isActiveAndEnabled) ? _scanner.ScanEnvironment() : new EnvironmentData();
                _currentState.UpdatePhysics(ref envData);
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
