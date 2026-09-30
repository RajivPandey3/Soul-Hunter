using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Core.Services;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Player ka In-Game HUD (Heads-Up Display).
    /// Ye screen par top-left mein hathiyaron ki tasveerein (icons), Gold aur Kills dikhata hai.
    /// Sath hi Kael ki Health Bar ko update karta hai.
    /// </summary>
    public class GameHUDController : MonoBehaviour
    {
        [Header("Player Health")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private HealthController _playerHealth;

        [Header("Economy & Stats")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _killText;

        [Header("Inventory (Top Left)")]
        [Tooltip("Empty = WeaponManager.Instance (HUD_Canvas is a prefab and cannot reference the scene's manager).")]
        [SerializeField] private WeaponManager _weaponManager;

        private SoulHunter.Gameplay.Core.RunStatsTracker _stats;

        private void Start()
        {
            // VS pause menu (Resume / Options / Quit) on the GameSystemManager panel.
            PauseMenuUI.Attach(SoulHunter.Gameplay.Core.GameSystemManager.Instance);

            // Readable HUD labels (they were dark blue on the dark ground) and the stage progression label.
            var readability = GetComponent<HudReadability>();
            if (readability == null) readability = gameObject.AddComponent<HudReadability>();
            var timer = FindFirstObjectByType<TimerUI>();
            readability.Setup(_goldText, _killText, timer != null ? timer.TimeText : null);
            if (GetComponent<TouchControlsUI>() == null) gameObject.AddComponent<TouchControlsUI>(); // touch stick (mobile / Visible Joystick)
            // The owner's story: each stage opens with its story line.
            var storyCard = GetComponent<StoryCardUI>();
            if (storyCard == null) storyCard = gameObject.AddComponent<StoryCardUI>();
            storyCard.Setup();

            // Health Bar setup
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
                // VS: a small HP bar under the hero instead of a strip across the bottom of the screen.
                HealthBarFollow.Attach(_healthSlider, _playerHealth.transform);

                // Owner (2026-09-30): no red full-screen tint at low health (the old LowHealthWarning was removed).
            }

            // VS inventory row: 6 weapon and 6 passive slots, top left.
            InventoryHud.Attach(GetComponentInParent<Canvas>(), _weaponManager != null ? _weaponManager : WeaponManager.Instance,
                _goldText != null ? _goldText.rectTransform : null);

            // VS HUD shows gold collected this run (the saved total lives in the menu shop).
            _stats = SoulHunter.Gameplay.Core.RunStatsTracker.Instance;
            if (_stats != null)
            {
                _stats.OnKillsChanged += UpdateKills;
                UpdateKills(_stats.TotalKills);
                _stats.OnGoldChanged += UpdateGoldUI;
                HudGoldHold.Changed += RefreshGold;
                UpdateGoldUI(_stats.TotalGoldCollected);
            }
        }

        private void OnDestroy()
        {
            if (_playerHealth != null) _playerHealth.OnHealthChanged -= UpdateHealthBar;

            if (_stats != null)
            {
                _stats.OnKillsChanged -= UpdateKills;
                _stats.OnGoldChanged -= UpdateGoldUI;
                HudGoldHold.Changed -= RefreshGold;
            }
        }

        private void UpdateHealthBar(int newHealth, int maxHealth)
        {
            if (_healthSlider != null && maxHealth > 0)
            {
                _healthSlider.value = (float)newHealth / maxHealth;
            }
        }

        private int _runGold;

        private void UpdateGoldUI(int gold)
        {
            _runGold = gold;
            if (_goldText != null)
            {
                // Gold still being counted up on the chest screen is held back (HudGoldHold).
                _goldText.text = "Gold: " + System.Math.Max(0, gold - HudGoldHold.Hidden).ToString();
            }
        }

        private void RefreshGold() => UpdateGoldUI(_runGold);

        private void UpdateKills(int kills)
        {
            if (_killText != null) _killText.text = "Total Kills: " + kills.ToString();
        }
    }
}
