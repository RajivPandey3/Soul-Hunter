using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// Chest milne par VS jaisi opening screen (ChestOpeningScreen) chalti hai: intro blast, OPEN button, lid khulta hai,
    /// coins, reels, fireworks aur DONE (vampire.survivors.wiki/w/Treasure_Chest). The old text panel (_chestPanel with
    /// _rewardText and _claimButton) stays hidden; the reward text is still written to it.
    /// </summary>
    public class ChestUI : MonoBehaviour
    {
        [SerializeField] private GameObject _chestPanel;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private Button _claimButton;
        [SerializeField] private WeaponManager _weaponManager;
        [SerializeField] private System.Collections.Generic.List<UpgradeData> _availableUpgrades;

        private ChestOpeningScreen _screen;
        private bool _pauseActionWasEnabled, _paused;

        /// <summary>A chest screen is open (the game is paused under it).</summary>
        public bool IsOpen => _screen != null && _screen.IsOpen;
        public ChestOpeningScreen Screen => _screen;
        /// <summary>Chests opened without a screen: coin-only chests once skipping allows it (VS).</summary>
        public int QuickChests { get; private set; }

        private void Start()
        {
            if (_chestPanel != null) _chestPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            RestorePauseAction();
            // Modal UI can be destroyed during a scene transition while the run is paused.
            if (_paused) SoulHunter.Core.Services.GameTime.Resume(this);
        }

        public void OpenChest(string message) => OpenChest(message, null);

        /// <summary>
        /// Counts a chest opened on the save (VS skip rules and the first-six sequence) without a chest screen: an Arcana
        /// chest opens the Arcana choice instead (release item P2-01). Returns the counts before this chest.
        /// </summary>
        public (int before, int fiveBefore) RecordOpened(int items)
        {
            var data = SaveData();
            int before = data != null ? data.ChestsOpened : 0;
            int fiveBefore = data != null ? data.FiveItemChestsOpened : 0;
            if (data != null)
            {
                data.ChestsOpened++;
                if (items >= 5) data.FiveItemChestsOpened++;
            }
            return (before, fiveBefore);
        }

        public void OpenChest(string message, ChestResult result) => OpenChest(message, result, countAsChest: true);

        /// <param name="countAsChest">False for a chest already counted (Game Killer's extra items after an Arcana chest).</param>
        public void OpenChest(string message, ChestResult result, bool countAsChest)
        {
            using (SoulHunter.Core.Services.SlowSpan.Measure("ChestUI.OpenChest"))
            {
                if (_rewardText != null) _rewardText.text = message;
                int items = result != null ? result.ItemCount : 1;
                int before, fiveBefore;
                if (countAsChest) (before, fiveBefore) = RecordOpened(items);
                else
                {
                    var data = SaveData();
                    before = data != null ? data.ChestsOpened : 0;
                    fiveBefore = data != null ? data.FiveItemChestsOpened : 0;
                }

                // VS: once 5-item chests can be skipped, coin-only chests just play a quick in-game animation.
                if (result != null && ChestSkipRules.SkipsItself(before, fiveBefore, result.OnlyCoins))
                {
                    QuickChests++;
                    var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                    if (audio != null) audio.PlaySFX(audio.ChestOpenSound);
                    var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
                    if (player != null) SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.GoldPickup, player.transform.position);
                    return;
                }

                if (_screen == null)
                {
                    _screen = ChestOpeningScreen.Create(ScreenCanvas());
                    _screen.Closed += CloseChest;
                }

                if (!_paused) { SoulHunter.Core.Services.GameTime.Pause(this); _paused = true; }
                DisablePauseAction();
                var pool = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
                _screen.Show(result, ChestSkipRules.CanSkip(before, fiveBefore, items),
                    pool != null ? pool.ChestPrefab : null, pool != null ? pool.GoldCoinPrefab : null);
            }
        }

        public const int ScreenSortingOrder = 400;
        private Canvas _canvas;

        /// <summary>
        /// The chest screen's own overlay canvas, owned by this ChestUI (1920x1080 reference like the HUD, above it, below
        /// the unlock notice). Found 2026-09-29 by the benchmark's stall watchdog: the scene's ChestUI (UI_Bindings) has no
        /// canvas above it and took FindFirstObjectByType&lt;Canvas&gt;(), which could be a temporary canvas (the character
        /// unlock notice or the merchant's shop). When that canvas was switched off or destroyed, the screen stopped
        /// half-way or vanished with it, and the chest's pause was never released: the game stayed frozen.
        /// </summary>
        public Canvas ScreenCanvas()
        {
            if (_canvas != null) return _canvas;
            var go = new GameObject("ChestScreenCanvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = ScreenSortingOrder;
            SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(_canvas); // the game's one UI scale (UiScreen), landscape and portrait
            go.AddComponent<GraphicRaycaster>();
            return _canvas;
        }

        // Safety net: a screen destroyed while open (scene teardown, anything unexpected) must not leave the game paused.
        private void Update()
        {
            if (_paused && (_screen == null || !_screen.IsOpen)) CloseChest();
        }

        /// <summary>Plays the screen to its end and closes it, as a player pressing Open, then Done would (bot, benchmark).</summary>
        public System.Collections.IEnumerator OpenAndClose(float timeoutSeconds = 12f)
        {
            float end = Time.unscaledTime + timeoutSeconds;
            while (IsOpen && Time.unscaledTime < end)
            {
                if (_screen.Current == ChestOpeningScreen.Phase.WaitingToOpen) _screen.PressOpen();
                else if (_screen.Current == ChestOpeningScreen.Phase.Done) _screen.PressDone();
                yield return null;
            }
        }

        private void CloseChest()
        {
            using (SoulHunter.Core.Services.SlowSpan.Measure("ChestUI.CloseChest"))
            {
                if (_chestPanel != null) _chestPanel.SetActive(false);
                RestorePauseAction();
                if (_paused) { SoulHunter.Core.Services.GameTime.Resume(this); _paused = false; }
                SaveService()?.SaveInBackground(); // the disk write froze the game 62-363 ms on the main thread
            }
        }

        // Esc skips a chest (VS), so it must not open the pause menu at the same time (as OptionsMenuUI does).
        private void DisablePauseAction()
        {
            var pause = PauseAction();
            if (pause != null && pause.enabled) { _pauseActionWasEnabled = true; pause.Disable(); }
        }

        private void RestorePauseAction()
        {
            if (!_pauseActionWasEnabled) return;
            _pauseActionWasEnabled = false;
            PauseAction()?.Enable();
        }

        private static UnityEngine.InputSystem.InputAction PauseAction()
        {
            if (SoulHunter.Core.Services.GameServices.Instance == null) return null;
            SoulHunter.Core.Services.GameServices.Instance.TryGet(out SoulHunter.Core.Input.InputService input);
            return input?.Actions?.FindAction(SoulHunter.Core.Input.InputService.PauseAction);
        }

        private static SoulHunter.Core.Persistence.SaveService SaveService() =>
            SoulHunter.Core.Services.GameServices.Instance != null
                ? SoulHunter.Core.Services.GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>()
                : null;

        private static SoulHunter.Core.Persistence.GameData SaveData() => SaveService()?.CurrentData;
    }
}
