using UnityEngine;
using SoulHunter.Core.Services;
using SoulHunter.Core.Events;

namespace SoulHunter.Core.Input
{
    /// <summary>
    /// Learning Comment:
    /// InputService Unity ke naye Input System ko sune ga aur decoupled Events fire karega.
    /// Ye EventBus ko use karta hai taaki Player Controller is par directly depend na kare.
    /// </summary>
    public class InputService : IGameService
    {
        private readonly EventBus _eventBus;

        public InputService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Initialize()
        {
            // Future Implementation:
            // Yahan Unity Input Action asset ko initialize karenge aur events bind karenge.
            // Example: _playerInputActions.Player.Move.performed += OnMovePerformed;
            
            Debug.Log("[InputService] Initialized.");
        }

        // Example callback (To be connected when Input Action Asset is generated)
        /*
        private void OnMovePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _eventBus.Raise(new PlayerMoveEvent(context.ReadValue<Vector2>()));
        }
        */
    }
}
