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
    public class InputService : IGameService
    {
        private readonly EventBus _eventBus;
        private InputAction _moveAction;

        public InputService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Initialize()
        {
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
            
            Debug.Log("[InputService] Initialized with New Input System.");
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            _eventBus.Raise(new PlayerMoveEvent(moveInput));
        }

        // Cleanup to prevent memory leaks if Service is destroyed
        public void Cleanup()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMovePerformed;
                _moveAction.Disable();
                _moveAction.Dispose();
            }
        }
    }
}
