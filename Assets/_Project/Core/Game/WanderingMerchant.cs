using System;
using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Level 3 signature (The Forgotten Village), following the VS Merchant rule
    /// (vampire.survivors.wiki/w/Merchant, checked 2026-09-26):
    /// - stands near where the stage starts; touching it opens the shop (the game pauses);
    /// - sells weapons (1,000 gold each, never past the 6 weapon slots) and repeatable Soul Eggs
    ///   (10,000 gold, VS Golden Egg), paid with the player's gold;
    /// - several things can be bought in one visit, but the shop can be visited once:
    ///   leaving it makes the merchant go away for the rest of the stage.
    /// Prices are the VS numbers and provisional Soul Hunter balance.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WanderingMerchant : MonoBehaviour
    {
        public enum WareState { Available, Owned, SlotsFull, TooExpensive }

        [Header("Wares (VS merchant: Bone and Cherry Bomb are the weapons Soul Hunter shares with its list)")]
        [SerializeField] private UpgradeData.UpgradeType[] _weaponWares = { UpgradeData.UpgradeType.Bone, UpgradeData.UpgradeType.CherryBomb };
        [SerializeField] private int _weaponPrice = 1000;
        [SerializeField] private int _soulEggPrice = 10000;

        private bool _isShopOpen;
        private bool _hasLeft;

        public bool IsShopOpen => _isShopOpen;
        /// <summary>True once the player has closed the shop; the merchant never opens again.</summary>
        public bool HasLeft => _hasLeft;
        public IReadOnlyList<UpgradeData.UpgradeType> WeaponWares => _weaponWares;
        public int WeaponPrice => _weaponPrice;
        public int SoulEggPrice => _soulEggPrice;
        /// <summary>What the last purchase did, shown in the shop (e.g. "Soul Egg: +1% Might").</summary>
        public string LastMessage { get; private set; } = "";

        private void OnTriggerEnter(Collider other)
        {
            if (_isShopOpen || _hasLeft || !other.CompareTag("Player")) return;
            OpenShop();
        }

        public void OpenShop()
        {
            if (_hasLeft) return;
            _isShopOpen = true;
            GameTime.Pause(this);
            BuildShopUI();
        }

        /// <summary>Closes the screen and resumes the game (tests and scene teardown). Players leave with <see cref="Leave"/>.</summary>
        public void CloseShop()
        {
            _isShopOpen = false;
            GameTime.Resume(this);
            DestroyShopUI();
        }

        /// <summary>VS rule: once the player exits the shop, the merchant disappears from the map.</summary>
        public void Leave()
        {
            CloseShop();
            _hasLeft = true;
            if (Application.isPlaying) Destroy(gameObject);
        }

        // ---------- Rules (no services, so EditMode tests can drive them) ----------

        public WareState WeaponState(UpgradeData.UpgradeType type, WeaponManager weapons, int gold)
        {
            if (weapons != null && weapons.GetWeaponLevel(type) > 0) return WareState.Owned;
            if (weapons == null || !weapons.HasSlotFor(type)) return WareState.SlotsFull;
            return gold < _weaponPrice ? WareState.TooExpensive : WareState.Available;
        }

        /// <summary>
        /// Buys one of the merchant's weapons at level 1. Nothing is charged when the weapon is not sold here,
        /// is already owned, or no weapon slot is free.
        /// </summary>
        public bool TryBuyWeapon(UpgradeData.UpgradeType type, Func<int, bool> spendGold, WeaponManager weapons)
        {
            if (spendGold == null || weapons == null || Array.IndexOf(_weaponWares, type) < 0) return false;
            if (weapons.GetWeaponLevel(type) > 0 || !weapons.HasSlotFor(type)) return false;
            if (!spendGold(_weaponPrice)) return false;
            var levelOne = LevelOne(type);
            if (levelOne != null) weapons.ApplyUpgrade(levelOne); else weapons.GiveWeapon(type, 1);
            LastMessage = $"Bought {Name(type)}";
            return weapons.GetWeaponLevel(type) > 0;
        }

        /// <summary>Buys a Soul Egg for <paramref name="character"/>: one random stat, permanent, from the next run.</summary>
        public bool TryBuySoulEgg(GameData data, string character, Func<int, bool> spendGold, System.Random rng, out SoulEggs.Stat stat)
        {
            stat = default;
            if (data == null || string.IsNullOrEmpty(character) || spendGold == null) return false;
            if (!spendGold(_soulEggPrice)) return false;
            stat = SoulEggs.Add(data, character, rng);
            LastMessage = $"Soul Egg: {SoulEggs.Describe(stat)} for {character} (from the next run)";
            return true;
        }

        // ---------- Purchases through the game's services (shop buttons, AutoplayBot) ----------

        public bool BuyWeapon(UpgradeData.UpgradeType type)
        {
            if (!TryGetEconomy(out var economy)) return false;
            bool bought = TryBuyWeapon(type, economy.SpendGold, WeaponManager.Instance);
            if (bought) ShowCost(_weaponPrice);
            RefreshShopUI();
            return bought;
        }

        public bool BuySoulEgg()
        {
            if (!TryGetEconomy(out var economy) || !GameServices.Instance.TryGet<SaveService>(out var save) || save.CurrentData == null)
                return false;
            bool bought = TryBuySoulEgg(save.CurrentData, save.CurrentData.SelectedCharacterName, economy.SpendGold, new System.Random(), out _);
            if (bought) { save.SaveGame(); ShowCost(_soulEggPrice); }
            RefreshShopUI();
            return bought;
        }

        private static bool TryGetEconomy(out EconomyService economy)
        {
            economy = null;
            return GameServices.Instance != null && GameServices.Instance.TryGet(out economy);
        }

        private static int CurrentGold() => TryGetEconomy(out var economy) ? economy.CurrentGold : 0;

        private void ShowCost(int cost)
        {
            if (SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
                SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(-cost, transform.position + Vector3.up * 2f);
        }

        private static UpgradeData LevelOne(UpgradeData.UpgradeType type)
        {
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            if (catalog == null || catalog.Upgrades == null) return null;
            foreach (var upgrade in catalog.Upgrades)
                if (upgrade != null && upgrade.Type == type && upgrade.Level == 1) return upgrade;
            return null;
        }

        private static string Name(UpgradeData.UpgradeType type)
        {
            var levelOne = LevelOne(type);
            return levelOne != null ? UpgradeText.DisplayName(levelOne) : type.ToString();
        }

        // ---------- Shop screen (uGUI, so keyboard and controller can use it) ----------

        private GameObject _shopUI;
        /// <summary>The shop screen while open.</summary>
        public GameObject ShopUI => _shopUI;

        private void RefreshShopUI()
        {
            if (!_isShopOpen) return;
            DestroyShopUI();
            BuildShopUI();
        }

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

            // Owner (2026-10-04): dark, full contrast (UITheme), on a dark veil over the paused game.
            SoulHunter.Gameplay.UI.UITheme.AddVeil(_shopUI.transform);
            var panel = ShopRect("Panel", _shopUI.transform, new Vector2(0.27f, 0.14f), new Vector2(0.73f, 0.86f));
            SoulHunter.Gameplay.UI.UITheme.ApplyPanel(panel.gameObject.AddComponent<UnityEngine.UI.Image>());
            SoulHunter.Gameplay.UI.UITheme.ApplyText(ShopText("Title", panel, new Vector2(0f, 0.89f), new Vector2(1f, 0.98f), "WANDERING MERCHANT", 40f), SoulHunter.Gameplay.UI.ThemedElement.TextRole.Title);
            int gold = CurrentGold();
            GoldRow(panel, gold);

            UnityEngine.UI.Button focus = null;
            float top = 0.79f;
            foreach (var type in _weaponWares)
            {
                var state = WeaponState(type, WeaponManager.Instance, gold);
                string suffix = state switch
                {
                    WareState.Owned => "OWNED",
                    WareState.SlotsFull => "NO FREE SLOT",
                    _ => $"{_weaponPrice} G"
                };
                var levelOne = LevelOne(type);
                string description = levelOne != null ? UpgradeText.Describe(levelOne, 0) : "";
                var captured = type;
                var icon = WeaponDefinitionLookup.Find(type)?.Icon;
                var button = WareCard(panel, top, icon, Name(type), description, suffix, () => BuyWeapon(captured));
                SetAvailable(button, state == WareState.Available);
                if (focus == null && button.interactable) focus = button;
                top -= 0.14f;
            }

            string character = GameServices.Instance != null && GameServices.Instance.TryGet<SaveService>(out var save) && save.CurrentData != null
                ? save.CurrentData.SelectedCharacterName : "your hunter";
            var egg = WareCard(panel, top, GameContentCatalog.Load()?.SoulEggIcon, "Soul Egg",
                $"Permanently raises a random stat of {character}, from the next run.", $"{_soulEggPrice} G", () => BuySoulEgg());
            SetAvailable(egg, gold >= _soulEggPrice);
            if (focus == null && egg.interactable) focus = egg;

            ShopText("Message", panel, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.34f), LastMessage, 24f).color = SoulHunter.Gameplay.UI.UITheme.Rewards.Soul;
            var leave = ShopButton(panel, 0.18f, "Leave (the merchant moves on)", Leave);
            if (focus == null) focus = leave;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }

        // Sold-out or unaffordable wares stay readable: the theme's unusable plate with its locked text colour
        // (ThemedElement.ShowUsable restyles them when the button's state changes).
        private static void SetAvailable(UnityEngine.UI.Button button, bool available)
        {
            button.interactable = available;
            if (!available) SoulHunter.Gameplay.UI.ThemedElement.For(button.gameObject).ShowUsable(false);
        }

        /// <summary>The player's gold with the theme's coin, like the Power Up screen.</summary>
        private static void GoldRow(RectTransform panel, int gold)
        {
            var row = ShopRect("GoldRow", panel, new Vector2(0.3f, 0.81f), new Vector2(0.7f, 0.89f));
            var coin = ShopRect("Coin", row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            coin.sizeDelta = new Vector2(44f, 44f);
            coin.anchoredPosition = new Vector2(-70f, 0f);
            var coinImage = coin.gameObject.AddComponent<UnityEngine.UI.Image>();
            coinImage.sprite = SoulHunter.Gameplay.UI.UITheme.IconFor(SoulHunter.Gameplay.UI.UITheme.IconSlot.Coin);
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;
            var text = ShopText("Gold", row, new Vector2(0.5f, 0f), new Vector2(1f, 1f), gold.ToString("N0"), 34f);
            text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            ((RectTransform)text.transform).offsetMin = new Vector2(-40f, 0f);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(text, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Gold);
        }

        /// <summary>
        /// A ware as a card like the Power Up and level-up cards (owner, 2026-10-08: the Pro look everywhere): the item's
        /// icon, its name and what it does, and its price with a coin (or OWNED / NO FREE SLOT). The state text is the
        /// card's first text.
        /// </summary>
        private UnityEngine.UI.Button WareCard(RectTransform panel, float top, Sprite icon, string name, string description,
            string state, Action onClick)
        {
            var rect = ShopRect("Ware", panel, new Vector2(0.05f, top - 0.12f), new Vector2(0.95f, top));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            bool price = state.EndsWith(" G");
            // A price shows its coin instead of the "G" (like the Power Up screen); OWNED / NO FREE SLOT stay words.
            string shown = price && int.TryParse(state.Substring(0, state.Length - 2), out var amount) ? amount.ToString("N0") : state;
            var stateText = ShopText("Price", rect, new Vector2(0.74f, 0.1f), new Vector2(0.97f, 0.9f), shown, 30f);
            stateText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            if (price)
            {
                // The coin sits just left of the number.
                stateText.ForceMeshUpdate();
                float width = stateText.GetPreferredValues(shown).x;
                var coin = ShopRect("Coin", stateText.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                coin.pivot = new Vector2(1f, 0.5f);
                coin.sizeDelta = new Vector2(34f, 34f);
                coin.anchoredPosition = new Vector2(-(width + 10f), 0f);
                var coinImage = coin.gameObject.AddComponent<UnityEngine.UI.Image>();
                coinImage.sprite = SoulHunter.Gameplay.UI.UITheme.IconFor(SoulHunter.Gameplay.UI.UITheme.IconSlot.Coin);
                coinImage.preserveAspect = true;
                coinImage.raycastTarget = false;
            }
            var iconRect = ShopRect("Icon", rect, new Vector2(0.015f, 0.1f), new Vector2(0.015f, 0.9f));
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(64f, 0f);
            var iconImage = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var nameText = ShopText("Name", rect, new Vector2(0.14f, 0.5f), new Vector2(0.72f, 0.95f), name, 32f);
            nameText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
            var descText = ShopText("Description", rect, new Vector2(0.14f, 0.06f), new Vector2(0.72f, 0.5f), description, 22f);
            descText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Card, SoulHunter.Gameplay.UI.UITheme.IconSlot.None, labels: false,
                size: SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Free);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(nameText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(descText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.CardSoft);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(stateText, price ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card : SoulHunter.Gameplay.UI.ThemedElement.TextRole.Tag);
            return button;
        }

        private void DestroyShopUI()
        {
            if (_shopUI == null) return;
            if (Application.isPlaying) Destroy(_shopUI); else DestroyImmediate(_shopUI);
            _shopUI = null;
        }

        private UnityEngine.UI.Button ShopButton(RectTransform panel, float top, string label, Action onClick)
        {
            var rect = ShopRect("Ware", panel, new Vector2(0.05f, top - 0.12f), new Vector2(0.95f, top));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            ShopText("Text", rect, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), label, 26f);
            bool leave = label.StartsWith("Leave");
            // Wares keep their two-line, left-aligned text and their row size; Leave is a standard action button.
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, leave ? SoulHunter.Gameplay.UI.UITheme.ButtonKind.Danger : SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal,
                SoulHunter.Gameplay.UI.UITheme.IconSlot.None, labels: leave,
                size: leave ? SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Standard : SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Free);
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
            SoulHunter.Gameplay.UI.UITheme.ApplyText(label, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body); // the theme's font, never the scene default
            return label;
        }

        // The shop must never leave the game paused (stage change, scene unload).
        private void OnDisable()
        {
            if (_isShopOpen) CloseShop();
        }

        private void OnDestroy()
        {
            DestroyShopUI();
            if (_isShopOpen) GameTime.Resume(this);
        }
    }
}
