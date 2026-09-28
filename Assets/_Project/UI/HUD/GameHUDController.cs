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

                // VS-style low-HP feedback: pulsing red tint at 30% HP or below.
                var lowHealthWarning = GetComponent<LowHealthWarning>();
                if (lowHealthWarning == null) lowHealthWarning = gameObject.AddComponent<LowHealthWarning>();
                lowHealthWarning.Bind(_playerHealth);
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
            }
        }

        private void UpdateHealthBar(int newHealth, int maxHealth)
        {
            if (_healthSlider != null && maxHealth > 0)
            {
                _healthSlider.value = (float)newHealth / maxHealth;
            }
        }

        private void UpdateGoldUI(int gold)
        {
            if (_goldText != null)
            {
                _goldText.text = "Gold: " + gold.ToString();
            }
        }

        private void UpdateKills(int kills)
        {
            if (_killText != null) _killText.text = "Total Kills: " + kills.ToString();
        }
    }
}
