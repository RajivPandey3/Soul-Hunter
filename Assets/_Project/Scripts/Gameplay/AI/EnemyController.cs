using UnityEngine;
using SoulHunter.Gameplay.Animation;
using SoulHunter.Gameplay.Physics;

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
        public Rigidbody Rigidbody { get; private set; }
        public EntityAnimator Animator { get; private set; }
        public Transform Target { get; set; } // Can be set by an aggro sphere or event

        private EnvironmentScanner _scanner;
        private IEnemyState _currentState;
        
        public float MoveSpeed = 3f;
        public float ChaseDistance = 10f;
        public float AttackDistance = 1.5f;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Animator = GetComponentInChildren<EntityAnimator>();
            _scanner = GetComponent<EnvironmentScanner>();
            
            // For now, let's auto-find the player (in a real game, use Physics.OverlapSphere for aggro)
            var player = FindObjectOfType<SoulHunter.Gameplay.Player.PlayerController>();
            if (player != null)
            {
                Target = player.transform;
            }
        }

        private void Start()
        {
            ChangeState(new EnemyIdleState(this));
        }

        private void Update()
        {
            _currentState?.UpdateLogic();
        }

        private void FixedUpdate()
        {
            if (_currentState != null)
            {
                var envData = _scanner != null ? _scanner.ScanEnvironment() : new EnvironmentData();
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
