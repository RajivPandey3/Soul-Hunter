using UnityEngine;
using SoulHunter.Core.Events;
using SoulHunter.Core.Services;
using System;

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

        public Rigidbody Rigidbody { get; private set; }
        public Vector2 CurrentMoveInput { get; private set; }
        public SoulHunter.Gameplay.Animation.EntityAnimator Animator { get; private set; }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            _scanner = GetComponent<SoulHunter.Gameplay.Physics.EnvironmentScanner>();
            Animator = GetComponentInChildren<SoulHunter.Gameplay.Animation.EntityAnimator>();
        }

        private void Start()
        {
            if (GameServices.Instance == null)
            {
                Debug.LogError("[PlayerController] GameServices.Instance is NULL! The Bootstrapper must run first.");
                return;
            }

            _eventBus = GameServices.Instance.Get<EventBus>();

            if (_eventBus == null)
            {
                Debug.LogError("[PlayerController] EventBus is NULL in GameServices!");
                return;
            }

            _onMoveEventHandler = OnMoveEvent;
            _onAttackEventHandler = OnAttackEvent;
            
            _eventBus.Subscribe(_onMoveEventHandler);
            _eventBus.Subscribe(_onAttackEventHandler);

            ChangeState(new PlayerIdleState(this));
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                if (_onMoveEventHandler != null) _eventBus.Unsubscribe(_onMoveEventHandler);
                if (_onAttackEventHandler != null) _eventBus.Unsubscribe(_onAttackEventHandler);
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

        private void Update()
        {
            _currentState?.UpdateLogic();
        }

        private void FixedUpdate()
        {
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
    }
}
