using UnityEngine.InputSystem;
using SoulHunter.Core.Input;
using SoulHunter.Core.Services;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Bug MIS UI-06: a screen over the pause menu (Evolutions, Map) goes back on Esc or gamepad B, as Options does,
    /// instead of Esc resuming the whole game. While it is open the Pause action is off; it is put back as it was on
    /// every way the screen leaves (Back, Esc/B, the pause menu closing it, the screen being destroyed).
    /// </summary>
    public sealed class PauseKeyGate
    {
        private InputAction _pause;
        private bool _holding;

        /// <summary>True while this gate keeps the Pause action off.</summary>
        public bool Holding => _holding;

        public void Hold()
        {
            if (_holding) return;
            if (_pause == null && GameServices.Instance != null && GameServices.Instance.TryGet(out InputService input))
                _pause = input?.Actions?.FindAction(InputService.PauseAction);
            if (_pause == null) return;
            PauseKeyHolds.Hold(this, _pause);
            _holding = true;
        }

        public void Release()
        {
            if (!_holding) return;
            _holding = false;
            PauseKeyHolds.Release(this, _pause);
        }

        /// <summary>Esc on a keyboard or B (east) on a gamepad, this frame.</summary>
        public static bool BackPressed() =>
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
    }
}
