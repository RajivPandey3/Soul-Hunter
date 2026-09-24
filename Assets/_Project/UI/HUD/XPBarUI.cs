using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors mein screen ke sabse upar ek neeli patti (blue bar) hoti hai.
    /// Ye script PlayerExperience se judti hai, aur jab bhi XP badhta hai, ye us bar (Slider) ko aage badhati hai.
    /// </summary>
    public class XPBarUI : MonoBehaviour
    {
        [SerializeField] private PlayerExperience _playerExperience;
        
        [Tooltip("Ye Unity ka Slider component hai jisme hum Fill ko use karenge")]
        [SerializeField] private Slider _xpSlider;

        private void Awake()
        {
            // Learning Comment:
            // "Read First, Match Later":
            // Agar Inspector mein references unassigned hon toh scene se dynamically dhoond kar bind karein.
            if (_playerExperience == null)
            {
                _playerExperience = FindFirstObjectByType<PlayerExperience>();
            }
            if (_xpSlider == null)
            {
                _xpSlider = GetComponent<Slider>();
            }
        }

        private void OnEnable()
        {
            if (_playerExperience == null)
            {
                _playerExperience = FindFirstObjectByType<PlayerExperience>();
            }

            if (_playerExperience != null)
            {
                // Jaise hi script chalu ho, XP event se jud jao
                _playerExperience.OnXPChanged += UpdateXPBar;
                
                // Shuruwaati setup (0 se start karne ke liye)
                UpdateXPBar(_playerExperience.CurrentXP, _playerExperience.XPToNextLevel);
            }
        }

        private void OnDisable()
        {
            if (_playerExperience != null)
            {
                _playerExperience.OnXPChanged -= UpdateXPBar;
            }
        }

        private void UpdateXPBar(int currentXP, int maxXP)
        {
            if (_xpSlider != null)
            {
                // Slider ki value 0 se 1 ke beech hoti hai (percentage)
                _xpSlider.value = (float)currentXP / maxXP;
            }

            // OnXPChanged fires after any level-ups, so this also keeps the level label current.
            EnsureLevelLabel();
            if (_levelLabel != null && _playerExperience != null)
                _levelLabel.text = FormatLevel(_playerExperience.CurrentLevel);
        }

        public const string LevelLabelName = "XPBar_LevelLabel";
        private TMPro.TextMeshProUGUI _levelLabel;

        public static string FormatLevel(int level) => $"LV {level}";

        /// <summary>
        /// VS shows the player level at the right end of the XP bar. The HUD prefab has no label for it,
        /// so one is created on the bar at runtime (found by name if it already exists).
        /// </summary>
        private void EnsureLevelLabel()
        {
            if (_levelLabel != null || _xpSlider == null) return;

            Transform bar = _xpSlider.transform;
            Transform existing = bar.Find(LevelLabelName);
            if (existing != null)
            {
                _levelLabel = existing.GetComponent<TMPro.TextMeshProUGUI>();
                return;
            }

            var labelObject = new GameObject(LevelLabelName, typeof(RectTransform));
            labelObject.layer = bar.gameObject.layer;
            labelObject.transform.SetParent(bar, false);
            labelObject.transform.SetAsLastSibling(); // draw over the bar fill

            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(140f, 0f);
            rect.anchoredPosition = new Vector2(-8f, 0f);

            _levelLabel = labelObject.AddComponent<TMPro.TextMeshProUGUI>();
            _levelLabel.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            _levelLabel.enableAutoSizing = true;
            _levelLabel.fontSizeMin = 12f;
            _levelLabel.fontSizeMax = 32f;
            _levelLabel.color = Color.white;
            _levelLabel.raycastTarget = false;
        }
    }
}
