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

        public void OpenChest(string message, ChestResult result)
        {
            if (_rewardText != null) _rewardText.text = message;
            var data = SaveData();
            int before = data != null ? data.ChestsOpened : 0;
            int fiveBefore = data != null ? data.FiveItemChestsOpened : 0;
            int items = result != null ? result.ItemCount : 1;
            if (data != null)
            {
                data.ChestsOpened++;
                if (items >= 5) data.FiveItemChestsOpened++;
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
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null) return;
                _screen = ChestOpeningScreen.Create(canvas);
                _screen.Closed += CloseChest;
            }

            if (!_paused) { SoulHunter.Core.Services.GameTime.Pause(this); _paused = true; }
            DisablePauseAction();
            var pool = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
            _screen.Show(result, ChestSkipRules.CanSkip(before, fiveBefore, items),
                pool != null ? pool.ChestPrefab : null, pool != null ? pool.GoldCoinPrefab : null);
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
            if (_chestPanel != null) _chestPanel.SetActive(false);
            RestorePauseAction();
            if (_paused) { SoulHunter.Core.Services.GameTime.Resume(this); _paused = false; }
            SaveService()?.SaveGame();
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
