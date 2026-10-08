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
            StyleBar();
        }

        /// <summary>The bar in the theme: a groove behind, the theme's XP colour in front (the HUD prefab is not edited).</summary>
        private void StyleBar()
        {
            if (_xpSlider == null) return;
            var back = _xpSlider.transform.Find("Background");
            if (back != null && back.TryGetComponent<Image>(out var backImage))
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(backImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Groove);
            if (_xpSlider.fillRect != null && _xpSlider.fillRect.TryGetComponent<Image>(out var fillImage))
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(fillImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.XpFill);
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
        public const float LabelHeight = 34f;

        /// <summary>The level label (tests).</summary>
        public TMPro.TextMeshProUGUI LevelLabel => _levelLabel;

        private SoulHunter.Gameplay.Core.LevelUpManager _levelUps;

        /// <summary>
        /// Bug MIS v2 C19 (2026-10-02, owner: "as a cto tum recommend karo"): through a chain of level-ups every screen
        /// showed the final level ("LV 50" on all 30). While a level-up screen is open the label shows the level that
        /// screen is for (21, 22, ...), so the player can see how many are left; the wiki does not describe it.
        /// </summary>
        private void LateUpdate()
        {
            if (_levelLabel == null || _playerExperience == null) return;
            if (_levelUps == null) _levelUps = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
            int level = _levelUps != null && _levelUps.IsLevelUpActive ? _levelUps.LevelOnScreen : _playerExperience.CurrentLevel;
            string text = FormatLevel(level);
            if (_levelLabel.text != text) _levelLabel.text = text;
        }

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

            // Bug MIS v2 C20 (2026-10-02): drawn on the thin bar, the label covered its right end. It now hangs just
            // under the bar's right end.
            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(150f, LabelHeight);
            rect.anchoredPosition = new Vector2(-10f, -4f);

            _levelLabel = labelObject.AddComponent<TMPro.TextMeshProUGUI>();
            _levelLabel.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            _levelLabel.enableAutoSizing = true;
            _levelLabel.fontSizeMin = 12f;
            _levelLabel.fontSizeMax = 30f;
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_levelLabel, SoulHunter.Gameplay.UI.ThemedElement.TextRole.HudLabel);
            _levelLabel.raycastTarget = false;
        }
    }
}
