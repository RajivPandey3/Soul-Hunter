using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Hides a HUD label (kills, gold, time, stage) while a menu has the game paused on a touch screen. The item slots stay:
    /// VS shows the weapons on these screens (wiki Level_up, Patch 0.2.8: "current stats and weapon levels will be displayed
    /// when leveling up and on the pause screen"). On a phone the
    /// bigger HUD sits where the pause, level-up and chest screens put their buttons and cards, so its text showed through
    /// behind them (phone run, 2026-10-11). The touch pads follow the same rule (<see cref="TouchControlsUI"/>). A PC keeps
    /// its HUD under the veil, as approved.
    /// </summary>
    public sealed class HudMenuHide : MonoBehaviour
    {
        private CanvasGroup _group;

        /// <summary>True while HUD pieces are hidden: a touch screen with the game paused by a menu.</summary>
        public static bool Hidden => UiScreen.IsTouch && SoulHunter.Core.Services.GameTime.IsPaused;

        public static void Attach(GameObject piece)
        {
            if (piece != null && piece.GetComponent<HudMenuHide>() == null) piece.AddComponent<HudMenuHide>();
        }

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        }

        private void LateUpdate()
        {
            float alpha = Hidden ? 0f : 1f;
            if (_group.alpha != alpha) _group.alpha = alpha;
        }
    }
}
