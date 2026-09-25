using UnityEngine;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Core
{
    [RequireComponent(typeof(Collider))]
    public class WanderingMerchant : MonoBehaviour
    {
        // Learning Comment:
        // Wandering Merchant se khareedi jane wali permanent stat blessings ki types define karta hai.
        public enum BlessingType
        {
            Might,
            Armor,
            MaxHealth
        }

        [Header("Potion Settings")]
        [SerializeField] private int _healAmount = 50;
        [SerializeField] private int _cost = 50;

        [Header("Blessing Costs")]
        [SerializeField] private int _mightBlessingCost = 100;
        [SerializeField] private int _armorBlessingCost = 100;
        [SerializeField] private int _maxHealthBlessingCost = 100;

        [Header("Blessing Bonuses")]
        [SerializeField] private float _mightBonus = 0.1f;
        [SerializeField] private int _armorBonus = 1;
        [SerializeField] private int _maxHealthBonus = 20;
        
        private bool _isShopOpen = false;
        private HealthController _playerHealth;
        private PlayerStats _playerStats;

        // Learning Comment: Exposes whether the merchant shop is open for deterministic lifecycle control and unit testing.
        public bool IsShopOpen => _isShopOpen;

        // Learning Comment: Exposes potion cost in harvested souls for UI display and deterministic verification.
        public int Cost => _cost;

        // Learning Comment: Exposes potion heal amount for UI display and deterministic verification.
        public int HealAmount => _healAmount;

        // Learning Comment: Exposes blessing costs in harvested souls for UI display and deterministic verification.
        public int MightBlessingCost => _mightBlessingCost;
        public int ArmorBlessingCost => _armorBlessingCost;
        public int MaxHealthBlessingCost => _maxHealthBlessingCost;

        // Learning Comment: Exposes blessing bonus amounts for UI display and deterministic verification.
        public float MightBonus => _mightBonus;
        public int ArmorBonus => _armorBonus;
        public int MaxHealthBonus => _maxHealthBonus;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !_isShopOpen)
            {
                _playerHealth = other.GetComponentInParent<HealthController>() ?? other.GetComponent<HealthController>();
                _playerStats = other.GetComponentInParent<PlayerStats>() ?? other.GetComponent<PlayerStats>();
                OpenShop();
            }
        }
        
        // Learning Comment: Opens the merchant shop and pauses gameplay by freezing Time.timeScale.
        public void OpenShop()
        {
            _isShopOpen = true;
            SoulHunter.Core.Services.GameTime.Pause(this);
            BuildShopUI();
        }

        // Learning Comment: Attempts to purchase a healing potion using harvested souls from EconomyService.
        // Returns false without healing or closing the shop if economy is null or has insufficient souls.
        // On success, deducts soul cost, heals targetHealth (or _playerHealth) by HealAmount, closes the shop, and returns true.
        public bool TryPurchase(EconomyService economy = null, HealthController targetHealth = null)
        {
            if (economy == null)
            {
                return false;
            }

            if (!economy.SpendSouls(_cost))
            {
                return false;
            }

            var health = targetHealth != null ? targetHealth : _playerHealth;
            if (health != null)
            {
                health.Heal(_healAmount);
            }

            CloseShop();
            return true;
        }

        // Learning Comment: Attempts to purchase a permanent stat blessing using harvested souls from EconomyService.
        // Returns false without deducting souls or closing the shop if economy is null, souls are insufficient,
        // or target components cannot be resolved.
        // On success, deducts soul cost, applies the corresponding stat increase, closes the shop, and returns true.
        public bool TryPurchaseBlessing(BlessingType blessing, EconomyService economy = null, PlayerStats targetStats = null, HealthController targetHealth = null)
        {
            if (economy == null)
            {
                return false;
            }

            var stats = targetStats != null ? targetStats : _playerStats;
            var health = targetHealth != null ? targetHealth : _playerHealth;

            if (stats == null && health != null)
            {
                stats = health.GetComponent<PlayerStats>() ?? health.GetComponentInParent<PlayerStats>();
            }

            if (health == null && stats != null)
            {
                health = stats.GetComponent<HealthController>() ?? stats.GetComponentInParent<HealthController>();
            }

            bool componentsResolved = blessing switch
            {
                BlessingType.Might => stats != null,
                BlessingType.Armor => stats != null,
                BlessingType.MaxHealth => stats != null && health != null,
                _ => false
            };

            if (!componentsResolved)
            {
                return false;
            }

            int cost = blessing switch
            {
                BlessingType.Might => _mightBlessingCost,
                BlessingType.Armor => _armorBlessingCost,
                BlessingType.MaxHealth => _maxHealthBlessingCost,
                _ => 0
            };

            if (!economy.SpendSouls(cost))
            {
                return false;
            }

            switch (blessing)
            {
                case BlessingType.Might:
                    stats.AddMight(_mightBonus);
                    break;
                case BlessingType.Armor:
                    stats.AddArmor(_armorBonus);
                    break;
                case BlessingType.MaxHealth:
                    stats.AddMaxHealth(_maxHealthBonus);
                    if (health != null && health != stats.GetComponent<HealthController>() && health != stats.GetComponentInParent<HealthController>())
                    {
                        health.IncreaseMaxHealth(_maxHealthBonus);
                    }
                    break;
            }

            CloseShop();
            return true;
        }

        private enum Offer { Potion, Might, Armor, MaxHealth }

        private GameObject _shopUI;
        private TMPro.TextMeshProUGUI _soulsText;
        /// <summary>The shop screen while open (uGUI, so keyboard and controller can use it).</summary>
        public GameObject ShopUI => _shopUI;

        // The shop was IMGUI (OnGUI), which only a mouse can use. It is now a uGUI screen with a focused
        // first button, so keyboard and controller players can buy too.
        private void BuildShopUI()
        {
            if (_shopUI != null || !Application.isPlaying) return;
            _shopUI = new GameObject("MerchantShop", typeof(RectTransform));
            var canvas = _shopUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = _shopUI.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            _shopUI.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var panel = ShopRect("Panel", _shopUI.transform, new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.82f));
            panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.08f, 0.07f, 0.12f, 0.96f);
            ShopText("Title", panel, new Vector2(0f, 0.87f), new Vector2(1f, 0.98f), "WANDERING MERCHANT", 40f).color = Color.yellow;
            _soulsText = ShopText("Souls", panel, new Vector2(0f, 0.78f), new Vector2(1f, 0.87f), "", 28f);

            var first = ShopButton(panel, 0.66f, $"Buy Potion (+{_healAmount} HP) for {_cost} Souls", () => Buy(Offer.Potion));
            ShopButton(panel, 0.54f, $"Buy Might Blessing (+{Mathf.RoundToInt(_mightBonus * 100f)}% Might) for {_mightBlessingCost} Souls", () => Buy(Offer.Might));
            ShopButton(panel, 0.42f, $"Buy Armor Blessing (+{_armorBonus} Armor) for {_armorBlessingCost} Souls", () => Buy(Offer.Armor));
            ShopButton(panel, 0.30f, $"Buy Max Health Blessing (+{_maxHealthBonus} Max HP) for {_maxHealthBlessingCost} Souls", () => Buy(Offer.MaxHealth));
            ShopButton(panel, 0.12f, "Leave", CloseShop);
            RefreshSouls();
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        private void DestroyShopUI()
        {
            if (_shopUI == null) return;
            if (Application.isPlaying) Destroy(_shopUI); else DestroyImmediate(_shopUI);
            _shopUI = null;
        }

        private void RefreshSouls()
        {
            if (_soulsText == null) return;
            int souls = GameServices.Instance != null && GameServices.Instance.TryGet<EconomyService>(out var economy) ? economy.HarvestedSouls : 0;
            _soulsText.text = $"Your Souls: {souls}";
        }

        private void Buy(Offer offer)
        {
            if (GameServices.Instance == null || !GameServices.Instance.TryGet<EconomyService>(out var economy)) return;
            bool bought;
            int cost;
            switch (offer)
            {
                case Offer.Potion: bought = TryPurchase(economy); cost = _cost; break;
                case Offer.Might: bought = TryPurchaseBlessing(BlessingType.Might, economy); cost = _mightBlessingCost; break;
                case Offer.Armor: bought = TryPurchaseBlessing(BlessingType.Armor, economy); cost = _armorBlessingCost; break;
                default: bought = TryPurchaseBlessing(BlessingType.MaxHealth, economy); cost = _maxHealthBlessingCost; break;
            }
            if (!bought) { RefreshSouls(); return; }
            if (SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
                SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(-cost, transform.position + Vector3.up * 2f);
            Destroy(gameObject);
        }

        private UnityEngine.UI.Button ShopButton(RectTransform panel, float top, string label, System.Action onClick)
        {
            var rect = ShopRect(label, panel, new Vector2(0.06f, top - 0.1f), new Vector2(0.94f, top));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            SoulHunter.Gameplay.UI.OptionsMenuUI.ApplyFocusColors(button, image);
            button.onClick.AddListener(() => onClick());
            ShopText("Text", rect, Vector2.zero, Vector2.one, label, 24f);
            return button;
        }

        private static RectTransform ShopRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TMPro.TextMeshProUGUI ShopText(string name, Transform parent, Vector2 min, Vector2 max, string text, float size)
        {
            var label = ShopRect(name, parent, min, max).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = text;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = size;
            label.raycastTarget = false;
            return label;
        }

        // Learning Comment: Closes the merchant shop and safely restores normal gameplay time flow (Time.timeScale = 1f).
        public void CloseShop()
        {
            _isShopOpen = false;
            SoulHunter.Core.Services.GameTime.Resume(this);
            DestroyShopUI();
        }

        // Learning Comment: When the merchant component or GameObject is disabled (e.g. stage transition or object pooling),
        // safely restore Time.timeScale to 1f and close the shop if currently open so gameplay does not remain permanently paused.
        private void OnDisable()
        {
            if (_isShopOpen)
            {
                CloseShop();
            }
        }
        
        // Learning Comment: Ensures Time.timeScale is safely restored to 1f if the merchant is destroyed while the shop is open.
        private void OnDestroy()
        {
            DestroyShopUI();
            if (_isShopOpen)
            {
                SoulHunter.Core.Services.GameTime.Resume(this);
            }
        }
    }
}
