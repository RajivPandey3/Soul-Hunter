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
    /// VS rule: the whole game is playable with keyboard or controller, and controls can be remapped
    /// (Options). All actions live in one map so remaps save and load as one JSON string.
    /// </summary>
    public class InputService : IGameService, System.IDisposable
    {
        public const string MoveAction = "Move";
        public const string DashAction = "Dash";
        public const string UltimateAction = "Ultimate";
        public const string PauseAction = "Pause";

        private readonly EventBus _eventBus;
        private InputActionMap _map;
        private InputAction _moveAction;
        private InputAction _dashAction;
        private InputAction _ultimateAction;
        private InputAction _pauseAction;

        public InputService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>The player's actions (Move, Dash, Ultimate, Pause); the Options screen remaps these.</summary>
        public InputActionMap Actions => _map;

        public void Initialize()
        {
            if (_map != null) return;
            _map = new InputActionMap("Player");

            _moveAction = _map.AddAction(MoveAction, InputActionType.Value);
            _moveAction.expectedControlType = "Vector2";
            _moveAction.AddBinding("<Gamepad>/leftStick");
            _moveAction.AddBinding("<Gamepad>/dpad");
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

            _dashAction = _map.AddAction(DashAction, InputActionType.Button);
            _dashAction.AddBinding("<Keyboard>/space");
            _dashAction.AddBinding("<Gamepad>/buttonSouth");
            _dashAction.AddBinding("<Keyboard>/shift");

            _ultimateAction = _map.AddAction(UltimateAction, InputActionType.Button);
            _ultimateAction.AddBinding("<Keyboard>/q");
            _ultimateAction.AddBinding("<Gamepad>/buttonNorth");

            _pauseAction = _map.AddAction(PauseAction, InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");
            _pauseAction.AddBinding("<Keyboard>/p");

            _moveAction.performed += OnMovePerformed;
            _moveAction.canceled += OnMovePerformed;
            _dashAction.performed += OnDashPerformed;
            _ultimateAction.performed += OnUltimatePerformed;
            _pauseAction.performed += OnPausePerformed;
            _map.Enable();

            Debug.Log("[InputService] Initialized with New Input System.");
        }

        // Unity's SaveBindingOverridesAsJson keys overrides by binding IDs, which are new random GUIDs every
        // time this service builds its actions (every launch), so saved remaps would never load again.
        // Actions and binding order are fixed here, so remaps are stored by action name + binding index.
        [System.Serializable] private class SavedOverride { public string Action; public int Index; public string Path; }
        [System.Serializable] private class SavedOverrides { public System.Collections.Generic.List<SavedOverride> Items = new System.Collections.Generic.List<SavedOverride>(); }

        /// <summary>Applies saved remaps (from SaveBindingOverrides); empty or invalid keeps the defaults.</summary>
        public void LoadBindingOverrides(string json)
        {
            if (_map == null) return;
            _map.RemoveAllBindingOverrides();
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var saved = JsonUtility.FromJson<SavedOverrides>(json);
                if (saved?.Items == null) return;
                foreach (var item in saved.Items)
                {
                    var action = _map.FindAction(item.Action);
                    if (action != null && item.Index >= 0 && item.Index < action.bindings.Count && !string.IsNullOrEmpty(item.Path))
                        action.ApplyBindingOverride(item.Index, item.Path);
                }
            }
            catch (System.Exception e)
            {
                _map.RemoveAllBindingOverrides();
                Debug.LogWarning($"[InputService] Ignored invalid saved controls: {e.Message}");
            }
        }

        public string SaveBindingOverrides()
        {
            if (_map == null) return "";
            var saved = new SavedOverrides();
            foreach (var action in _map.actions)
                for (int i = 0; i < action.bindings.Count; i++)
                    if (!string.IsNullOrEmpty(action.bindings[i].overridePath))
                        saved.Items.Add(new SavedOverride { Action = action.name, Index = i, Path = action.bindings[i].overridePath });
            return saved.Items.Count == 0 ? "" : JsonUtility.ToJson(saved);
        }

        public void ResetBindings()
        {
            if (_map != null) _map.RemoveAllBindingOverrides();
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
            if (_map == null) return;
            _moveAction.performed -= OnMovePerformed;
            _moveAction.canceled -= OnMovePerformed;
            _dashAction.performed -= OnDashPerformed;
            _ultimateAction.performed -= OnUltimatePerformed;
            _pauseAction.performed -= OnPausePerformed;
            _map.Disable();
            _map.Dispose();
            _map = null;
            _moveAction = _dashAction = _ultimateAction = _pauseAction = null;
        }
    }

    /// <summary>
    /// Who keeps the Pause action off (bug hunt 2026-10-29). Each modal screen used to remember on its own whether it had
    /// switched the one shared action off, so when two overlapped (a chest over a level-up) the first to close put it
    /// back under the other. Now the action is off while anyone holds it, and back as it was once the last one lets go.
    /// </summary>
    public static class PauseKeyHolds
    {
        private static readonly System.Collections.Generic.HashSet<object> Holders = new System.Collections.Generic.HashSet<object>();
        private static readonly System.Collections.Generic.List<object> Gone = new System.Collections.Generic.List<object>();
        private static bool _wasEnabled;
        private static UnityEngine.InputSystem.InputAction _action;

        public static bool IsHeld { get { Prune(); return Holders.Count > 0; } }

        public static void Hold(object owner, UnityEngine.InputSystem.InputAction pause)
        {
            if (owner == null || pause == null) return;
            Prune();
            if (Holders.Count == 0 || _action != pause) { _action = pause; _wasEnabled = pause.enabled; }
            Holders.Add(owner);
            pause.Disable();
        }

        public static void Release(object owner, UnityEngine.InputSystem.InputAction pause)
        {
            if (owner == null || !Holders.Remove(owner)) return;
            Prune();
            if (Holders.Count == 0 && _wasEnabled) (pause ?? _action)?.Enable();
        }

        // A screen destroyed while it held the key (scene change) lets go of it.
        private static void Prune()
        {
            Gone.Clear();
            foreach (var holder in Holders) if (holder is UnityEngine.Object unity && unity == null) Gone.Add(holder);
            foreach (var holder in Gone) Holders.Remove(holder);
        }
    }
}
