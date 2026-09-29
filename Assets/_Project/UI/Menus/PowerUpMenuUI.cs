using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SoulHunter.Core.Services;

namespace SoulHunter.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Power-Up Menu. 
    /// Yeh Main Menu ki wo screen hai jahan player apne kamaye hue Gold se hamesha (permanent) rehne wale stats kharidta hai.
    /// In upgrades se player agle har run mein shuru se hi taqatwar hota hai.
    /// </summary>
    public class PowerUpMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _goldText;
        
        [Header("Power-Up Buttons")]
        [SerializeField] private Button _buyMightButton;
        [SerializeField] private Button _buyArmorButton;
        [SerializeField] private Button _buyGreedButton;
        [SerializeField] private Button _buyRevivalButton;

        [Header("Prices")]
        [SerializeField] private int _mightPrice = 500;
        [SerializeField] private int _armorPrice = 300;
        [SerializeField] private int _greedPrice = 400;
        [SerializeField] private int _revivalPrice = 1000;

        // VS healing PowerUps (heal balance pass, 2026-09-29): built at runtime from a copy of an existing button, so the
        // menu scene is not edited. Max Health 3 ranks, Recovery 5 ranks (MetaPowerUps).
        private Button _buyMaxHealthButton;
        private Button _buyRecoveryButton;

        /// <summary>The Max Health and Recovery buttons (tests).</summary>
        public Button MaxHealthButton => _buyMaxHealthButton;
        public Button RecoveryButton => _buyRecoveryButton;

        private EconomyService _economy;
        private SoulHunter.Core.Persistence.SaveService _saveService;
        private bool _listenersBound;

        private void Start()
        {
            if (GameServices.Instance != null)
            {
                GameServices.Instance.TryGet(out _economy);
                GameServices.Instance.TryGet(out _saveService);
            }

            // Button Listeners
            if (_buyMightButton) _buyMightButton.onClick.AddListener(() => BuyPowerUp("Might", _mightPrice));
            if (_buyArmorButton) _buyArmorButton.onClick.AddListener(() => BuyPowerUp("Armor", _armorPrice));
            if (_buyGreedButton) _buyGreedButton.onClick.AddListener(() => BuyPowerUp("Greed", _greedPrice));
            if (_buyRevivalButton) _buyRevivalButton.onClick.AddListener(() => BuyPowerUp("Revival", _revivalPrice));
            BuildHealingButtons();
            if (_buyMaxHealthButton) _buyMaxHealthButton.onClick.AddListener(() => BuyPowerUp("MaxHealth", SoulHunter.Core.Persistence.MetaPowerUps.MaxHealthPrice));
            if (_buyRecoveryButton) _buyRecoveryButton.onClick.AddListener(() => BuyPowerUp("Recovery", SoulHunter.Core.Persistence.MetaPowerUps.RecoveryPrice));

            if (_economy != null)
            {
                _economy.OnGoldChanged += HandleGoldChanged;
                _listenersBound = true;
            }

            UpdateUI();
        }

        private void HandleGoldChanged(int _) { UpdateUI(); }

        private void OnDestroy()
        {
            if (_listenersBound && _economy != null) _economy.OnGoldChanged -= HandleGoldChanged;
            if (_buyMightButton) _buyMightButton.onClick.RemoveAllListeners();
            if (_buyArmorButton) _buyArmorButton.onClick.RemoveAllListeners();
            if (_buyGreedButton) _buyGreedButton.onClick.RemoveAllListeners();
            if (_buyRevivalButton) _buyRevivalButton.onClick.RemoveAllListeners();
            if (_buyMaxHealthButton) _buyMaxHealthButton.onClick.RemoveAllListeners();
            if (_buyRecoveryButton) _buyRecoveryButton.onClick.RemoveAllListeners();
        }

        private void BuildHealingButtons()
        {
            Button template = null;
            foreach (var candidate in new[] { _buyRevivalButton, _buyGreedButton, _buyArmorButton, _buyMightButton })
                if (candidate != null) { template = candidate; break; }
            if (template == null || _buyMaxHealthButton != null) return;
            var existing = new[] { _buyMightButton, _buyArmorButton, _buyGreedButton, _buyRevivalButton };
            var layout = template.transform.parent != null ? template.transform.parent.GetComponent<LayoutGroup>() : null;
            var templateRect = (RectTransform)template.transform;
            float lowest = float.MaxValue;
            foreach (var b in existing)
                if (b != null) lowest = Mathf.Min(lowest, ((RectTransform)b.transform).anchoredPosition.y);
            float step = templateRect.rect.height + 12f;
            _buyMaxHealthButton = CopyButton(template, "Buy_MaxHealth", layout, lowest - step);
            _buyRecoveryButton = CopyButton(template, "Buy_Recovery", layout, lowest - step * 2f);
        }

        private static Button CopyButton(Button template, string name, LayoutGroup layout, float y)
        {
            var copy = Instantiate(template, template.transform.parent);
            copy.name = name;
            copy.onClick.RemoveAllListeners();
            if (layout == null)
            {
                var rect = (RectTransform)copy.transform;
                rect.anchoredPosition = new Vector2(((RectTransform)template.transform).anchoredPosition.x, y);
            }
            return copy;
        }

        private void BuyPowerUp(string type, int price)
        {
            if (_economy == null || _saveService == null) return;
            // VS rank limits for the healing PowerUps.
            if (type == "MaxHealth" && _saveService.CurrentData.MetaMaxHealthLevel >= SoulHunter.Core.Persistence.MetaPowerUps.MaxHealthRanks) return;
            if (type == "Recovery" && _saveService.CurrentData.MetaRecoveryLevel >= SoulHunter.Core.Persistence.MetaPowerUps.RecoveryRanks) return;

            // Kya player ke paas itne paise hain?
            if (_economy.SpendGold(price))
            {
                // Paise the, kharid liya! Ab data update karo
                switch (type)
                {
                    case "Might": _saveService.CurrentData.MetaMightLevel++; break;
                    case "Armor": _saveService.CurrentData.MetaArmorLevel++; break;
                    case "Greed": _saveService.CurrentData.MetaGreedLevel++; break;
                    case "Revival": _saveService.CurrentData.MetaRevivalLevel++; break;
                    case "MaxHealth": _saveService.CurrentData.MetaMaxHealthLevel++; break;
                    case "Recovery": _saveService.CurrentData.MetaRecoveryLevel++; break;
                }
                
                _saveService.SaveGame();
                UpdateUI();
                
                if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
                {
                    SoulHunter.Gameplay.Audio.AudioManager.Instance.PlaySFX(SoulHunter.Gameplay.Audio.AudioManager.Instance.LevelUpSound);
                }
                Debug.Log($"[PowerUpMenu] Successfully bought {type}! New Gold: {_economy.CurrentGold}");
            }
            else
            {
                Debug.Log("[PowerUpMenu] Not enough gold!");
                // Yahan error sound baja sakte hain
            }
        }

        private void UpdateUI()
        {
            if (_economy != null && _goldText != null)
            {
                _goldText.text = $"Gold: {_economy.CurrentGold}";
            }

            // VS Style: Jab level barh jaye toh price mehngi ho jati hai (optional complexity)
            // Abhi ke liye buttons par Levels dikhate hain
            if (_saveService != null)
            {
                UpdateButtonText(_buyMightButton, "Might", _mightPrice, _saveService.CurrentData.MetaMightLevel);
                UpdateButtonText(_buyArmorButton, "Armor", _armorPrice, _saveService.CurrentData.MetaArmorLevel);
                UpdateButtonText(_buyGreedButton, "Greed", _greedPrice, _saveService.CurrentData.MetaGreedLevel);
                UpdateButtonText(_buyRevivalButton, "Revival", _revivalPrice, _saveService.CurrentData.MetaRevivalLevel);
                UpdateRankedButton(_buyMaxHealthButton, "Max Health +10%", SoulHunter.Core.Persistence.MetaPowerUps.MaxHealthPrice,
                    _saveService.CurrentData.MetaMaxHealthLevel, SoulHunter.Core.Persistence.MetaPowerUps.MaxHealthRanks);
                UpdateRankedButton(_buyRecoveryButton, "Recovery +0.1/s", SoulHunter.Core.Persistence.MetaPowerUps.RecoveryPrice,
                    _saveService.CurrentData.MetaRecoveryLevel, SoulHunter.Core.Persistence.MetaPowerUps.RecoveryRanks);
            }
        }

        private void UpdateRankedButton(Button btn, string name, int price, int level, int maxLevel)
        {
            if (btn == null) return;
            bool maxed = level >= maxLevel;
            btn.interactable = !maxed;
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = maxed ? $"{name} Lvl {level}/{maxLevel}\nMAX" : $"{name} Lvl {level}/{maxLevel}\nCost: {price}";
        }

        private void UpdateButtonText(Button btn, string name, int price, int level)
        {
            if (btn == null) return;
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = $"{name} Lvl {level}\nCost: {price}";
            }
        }
    }
}
