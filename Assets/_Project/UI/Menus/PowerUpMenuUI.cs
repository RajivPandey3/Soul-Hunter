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
        }

        private void BuyPowerUp(string type, int price)
        {
            if (_economy == null || _saveService == null) return;

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
            }
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
