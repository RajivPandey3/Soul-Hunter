using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SoulHunter.UI
{
    /// <summary>
    /// Shows a hint while a control is focused (keyboard/gamepad) or under the mouse, e.g. what a run mode toggle does.
    /// </summary>
    public sealed class FocusHint : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public string Hint { get; set; }
        public Action<string> Show { get; set; }

        public void OnSelect(BaseEventData eventData) => Show?.Invoke(Hint);
        public void OnPointerEnter(PointerEventData eventData) => Show?.Invoke(Hint);
    }
}
