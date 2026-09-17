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
            Time.timeScale = 0f;
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

        private void OnGUI()
        {
            if (!_isShopOpen) return;

            var boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 22;
            boxStyle.normal.textColor = Color.yellow;
            boxStyle.alignment = TextAnchor.UpperCenter;

            int width = 460;
            int height = 370;
            Rect rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            
            GUI.Box(rect, "WANDERING MERCHANT", boxStyle);

            var buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 17;

            if (GameServices.Instance != null && GameServices.Instance.TryGet<EconomyService>(out var economy))
            {
                GUI.Label(new Rect(rect.x + 20, rect.y + 40, rect.width - 40, 30), $"Your Souls: {economy.HarvestedSouls}", buttonStyle);

                if (GUI.Button(new Rect(rect.x + 30, rect.y + 75, rect.width - 60, 40), $"Buy Potion (+{_healAmount} HP) for {_cost} Souls", buttonStyle))
                {
                    if (TryPurchase(economy))
                    {
                        if (SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
                        {
                            SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(-_cost, transform.position + Vector3.up * 2f);
                        }
                        Destroy(gameObject);
                    }
                }

                if (GUI.Button(new Rect(rect.x + 30, rect.y + 125, rect.width - 60, 40), $"Buy Might Blessing (+{Mathf.RoundToInt(_mightBonus * 100f)}% Might) for {_mightBlessingCost} Souls", buttonStyle))
                {
                    if (TryPurchaseBlessing(BlessingType.Might, economy))
                    {
                        if (SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
                        {
                            SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(-_mightBlessingCost, transform.position + Vector3.up * 2f);
                        }
                        Destroy(gameObject);
                    }
                }

                if (GUI.Button(new Rect(rect.x + 30, rect.y + 175, rect.width - 60, 40), $"Buy Armor Blessing (+{_armorBonus} Armor) for {_armorBlessingCost} Souls", buttonStyle))
                {
                    if (TryPurchaseBlessing(BlessingType.Armor, economy))
                    {
                        if (SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
                        {
                            SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(-_armorBlessingCost, transform.position + Vector3.up * 2f);
                        }
                        Destroy(gameObject);
                    }
                }

                if (GUI.Button(new Rect(rect.x + 30, rect.y + 225, rect.width - 60, 40), $"Buy Max Health Blessing (+{_maxHealthBonus} Max HP) for {_maxHealthBlessingCost} Souls", buttonStyle))
                {
                    if (TryPurchaseBlessing(BlessingType.MaxHealth, economy))
                    {
                        if (SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
                        {
                            SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(-_maxHealthBlessingCost, transform.position + Vector3.up * 2f);
                        }
                        Destroy(gameObject);
                    }
                }
            }

            if (GUI.Button(new Rect(rect.x + 30, rect.y + 285, rect.width - 60, 40), "Leave", buttonStyle))
            {
                CloseShop();
            }
        }

        // Learning Comment: Closes the merchant shop and safely restores normal gameplay time flow (Time.timeScale = 1f).
        public void CloseShop()
        {
            _isShopOpen = false;
            Time.timeScale = 1f;
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
            if (_isShopOpen)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
