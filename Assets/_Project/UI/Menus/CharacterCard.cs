using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SoulHunter.UI
{
    /// <summary>
    /// One character card on the selection grid. Moving onto it with keyboard or gamepad
    /// selects the character, the same as clicking it.
    /// </summary>
    public sealed class CharacterCard : MonoBehaviour, ISelectHandler
    {
        public int Index { get; set; }
        public Action<int> Selected { get; set; }

        public void OnSelect(BaseEventData eventData) => Selected?.Invoke(Index);
    }
}
