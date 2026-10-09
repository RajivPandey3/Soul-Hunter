using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The touch area of the VS "Joystick Type: Default" stick (TouchControlsUI). The wiki names the types (Legacy, Default)
    /// but not what they do, so Soul Hunter chose (balance method, owner rule 2026-10-09): Default is a floating stick. A
    /// thumb landing anywhere in this area moves the whole stick under it and drives it from there; on release the stick goes
    /// back to its resting place, where it waits in plain sight so players see where to touch. Reason: a floating stick is
    /// the usual default on phones (no aiming for a small fixed pad while watching the horde), and Legacy keeps the fixed
    /// stick Soul Hunter had before. The press is handed to the stick's own OnScreenStick, which feeds the virtual gamepad.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FloatingStickZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform _stickBase;
        private OnScreenStick _stick;
        private Vector2 _home;
        private bool _pressed;

        public bool IsPressed => _pressed;

        /// <summary>Connects the area to the stick it moves; the stick's current place is its resting place.</summary>
        public void Init(RectTransform stickBase, OnScreenStick stick)
        {
            _stickBase = stickBase;
            _stick = stick;
            _home = stickBase.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_stickBase == null || _stick == null || eventData == null) return;
            var parent = (RectTransform)_stickBase.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var local))
            {
                // Keep the whole stick on screen when the thumb lands near an edge.
                Vector2 half = _stickBase.rect.size * 0.5f;
                Rect room = parent.rect;
                local.x = Mathf.Clamp(local.x, room.xMin + half.x, Mathf.Max(room.xMin + half.x, room.xMax - half.x));
                local.y = Mathf.Clamp(local.y, room.yMin + half.y, Mathf.Max(room.yMin + half.y, room.yMax - half.y));
                _stickBase.localPosition = new Vector3(local.x, local.y, _stickBase.localPosition.z);
            }
            _pressed = true;
            _stick.OnPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_pressed && _stick != null && eventData != null) _stick.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData) => Release(eventData);

        /// <summary>Lets go of the stick (thumb up, type changed or controls hidden) and sends it home.</summary>
        public void Release(PointerEventData eventData = null)
        {
            if (_pressed && _stick != null) _stick.OnPointerUp(eventData);
            _pressed = false;
            if (_stickBase != null) _stickBase.anchoredPosition = _home;
        }

        private void OnDisable() => Release();
    }
}
