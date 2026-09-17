using UnityEngine;
using UnityEngine.InputSystem;
using SoulHunter.Core.Services;
using SoulHunter.Core.Events;

namespace SoulHunter.Core.Input
{
    /// <summary>
    /// Learning Comment:
    /// InputService Unity ke naye Input System ko sune ga aur decoupled Events fire karega.
    /// Ye EventBus ko use karta hai taaki Player Controller is par directly depend na kare.
    /// Yahan hum manually InputAction create kar rahe hain taaki Asset dependency na rahe.
    /// </summary>
    public class InputService : IGameService, System.IDisposable
    {
        private readonly EventBus _eventBus;
        private InputAction _moveAction;
        private InputAction _dashAction;
        private InputAction _ultimateAction;
        private InputAction _pauseAction;

        public InputService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Initialize()
        {
            if (_moveAction != null) return;
            // Setup movement action manually
            _moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("2DVector")
                .With("up", "<Keyboard>/w")
                .With("down", "<Keyboard>/s")
                .With("left", "<Keyboard>/a")
                .With("right", "<Keyboard>/d");

            _moveAction.AddCompositeBinding("2DVector")
                .With("up", "<Keyboard>/upArrow")
                .With("down", "<Keyboard>/downArrow")
                .With("left", "<Keyboard>/leftArrow")
                .With("right", "<Keyboard>/rightArrow");

            _moveAction.performed += OnMovePerformed;
            _moveAction.canceled += OnMovePerformed;
            _moveAction.Enable();

            _dashAction = new InputAction("Dash", binding: "<Keyboard>/space");
            _dashAction.AddBinding("<Keyboard>/shift");
            _dashAction.performed += OnDashPerformed;
            _dashAction.Enable();

            _ultimateAction = new InputAction("Ultimate", binding: "<Keyboard>/q");
            _ultimateAction.performed += OnUltimatePerformed;
            _ultimateAction.Enable();

            _pauseAction = new InputAction("Pause", binding: "<Keyboard>/escape");
            _pauseAction.AddBinding("<Keyboard>/p");
            _pauseAction.performed += OnPausePerformed;
            _pauseAction.Enable();

            Debug.Log("[InputService] Initialized with New Input System.");
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            _eventBus.Raise(new PlayerMoveEvent(moveInput));
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            _eventBus.Raise(new PlayerDashEvent());
        }

        private void OnUltimatePerformed(InputAction.CallbackContext context)
        {
            _eventBus.Raise(new PlayerUltimateEvent());
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            _eventBus.Raise(new PauseToggleEvent());
        }

        public void Dispose() { Cleanup(); }

        // Cleanup to prevent memory leaks if Service is destroyed
        public void Cleanup()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMovePerformed;
                _moveAction.Disable();
                _moveAction.Dispose();
                _moveAction = null;
            }

            if (_dashAction != null)
            {
                _dashAction.performed -= OnDashPerformed;
                _dashAction.Disable();
                _dashAction.Dispose();
                _dashAction = null;
            }

            if (_ultimateAction != null)
            {
                _ultimateAction.performed -= OnUltimatePerformed;
                _ultimateAction.Disable();
                _ultimateAction.Dispose();
                _ultimateAction = null;
            }

            if (_pauseAction != null)
            {
                _pauseAction.performed -= OnPausePerformed;
                _pauseAction.Disable();
                _pauseAction.Dispose();
                _pauseAction = null;
            }
        }
    }
}
