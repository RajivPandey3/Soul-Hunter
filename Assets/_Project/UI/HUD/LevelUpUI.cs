using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Level up par game pause hota hai aur 3 random cards aate hain.
    /// Ye script buttons par random upgrades set karti hai aur click hone par WeaponManager ko bhejti hai.
    /// Reroll / Skip / Banish buttons: agar Inspector mein assign na hon toh pehle upgrade button ko
    /// clone karke cards ke neeche ek row mein bana diye jaate hain.
    /// </summary>
    public class LevelUpUI : MonoBehaviour
    {
        [SerializeField] private SoulHunter.Gameplay.Core.LevelUpManager _levelUpManager;
        [SerializeField] private GameObject _levelUpPanel;
        [SerializeField] private Button[] _upgradeButtons;
        [SerializeField] private Button _rerollButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _banishButton;

        private const string ActionBarName = "LevelUp_ActionBar";
        private List<UpgradeData> _currentChoices = new List<UpgradeData>();
        private bool _banishMode;

        private void Awake()
        {
            // Learning Comment:
            // "Read First, Match Later":
            // Agar Inspector mein _levelUpManager assign nahi hai toh dynamic find karo.
            if (_levelUpManager == null)
            {
                _levelUpManager = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
            }

            if (_levelUpPanel == null)
            {
                _levelUpPanel = gameObject;
            }

            // Game shuru hote waqt LevelUp Panel ko hide rakhein (Awake mein ek martaba)
            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_levelUpManager == null)
            {
                _levelUpManager = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
            }

            if (_levelUpManager != null)
            {
                _levelUpManager.OnLevelUpUIRound += ShowLevelUpScreen;
            }
            
            // CRITICAL FIX: Yahan se _levelUpPanel.SetActive(false) hata diya gaya hai!
            // Agar yeh script panel par hi lagi ho, to ShowLevelUpScreen ke SetActive(true) call
            // karte hi OnEnable chalega aur panel usi frame mein band ho jata tha.
        }

        private void OnDisable()
        {
            if (_levelUpManager != null)
            {
                _levelUpManager.OnLevelUpUIRound -= ShowLevelUpScreen;
            }
        }

        private void ShowLevelUpScreen(List<UpgradeData> choices)
{
    if (choices == null)
    {
        Debug.LogError("[LevelUpUI] choices == NULL");
        return;
    }

    _currentChoices = choices;
    _banishMode = false;
    EnsureActionButtons();
    EnsureCardCount(choices.Count);
    StyleHeading();
    // VS: the player's stats are shown when leveling up (wiki Player_stats); a 4-column grid between the heading and the cards.
    if (_levelUpPanel != null)
    {
        var stats = StatsPanel.Attach(_levelUpPanel.transform, new Vector2(0.23f, 0.56f), new Vector2(0.83f, 0.78f), 4, 22f);
        // From 23% of the width: the HUD's item slots end at about 21% (Bug MIS B09: the panel covered the 6th column).
        if (stats != null) stats.FitBetween(HeadingRect(), _upgradeButtons, 0.23f, 0.83f);
    }

    if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
    {
        SoulHunter.Gameplay.Audio.AudioManager.Instance.PlaySFX(
            SoulHunter.Gameplay.Audio.AudioManager.Instance.LevelUpSound
        );
    }

    for (int i = 0; i < _upgradeButtons.Length; i++)
    {
        if (_upgradeButtons[i] == null)
        {
            Debug.LogError($"[LevelUpUI] Button {i} is NULL");
            continue;
        }

        if (i < choices.Count)
        {
            _upgradeButtons[i].gameObject.SetActive(true);

            UpgradeData chosenUpgrade = choices[i];

            StyleCard(_upgradeButtons[i], chosenUpgrade);

            _upgradeButtons[i].onClick.RemoveAllListeners();

            UpgradeData finalUpgrade = chosenUpgrade;

            _upgradeButtons[i].onClick.AddListener(
                () => OnCardClicked(finalUpgrade)
            );
        }
        else
        {
            _upgradeButtons[i].gameObject.SetActive(false);
        }
    }

    UpdateSpacers(Mathf.Min(choices.Count, _upgradeButtons.Length));

    BindActionButtons();
    RefreshActionButtons();

    if (_levelUpPanel != null)
    {
        _levelUpPanel.SetActive(true);
    }
    else
    {
        Debug.LogError("[LevelUpUI] _levelUpPanel is NULL!");
    }

    // Keyboard/controller: focus the first card so a choice can be made without a mouse.
    if (UnityEngine.EventSystems.EventSystem.current != null)
        foreach (var card in _upgradeButtons)
            if (card != null && card.gameObject.activeInHierarchy && card.interactable)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(card.gameObject);
                break;
            }
}

        private static int OwnedLevel(UpgradeData upgrade)
        {
            var weapons = WeaponManager.Instance;
            return weapons != null && upgrade != null ? weapons.GetWeaponLevel(upgrade.Type) : 0;
        }

        // ---------------------------------------------------------------- VS-style cards
        // Owner request 2026-09-25: the cards were plain white buttons with internal names ("SongOfMana Lv.2 (Lv 2)").
        // Each card now shows the upgrade's icon, a readable name, a NEW!/Level tag and what it does.

        public const string IconName = "Card_Icon", TitleName = "Card_Title", TagName = "Card_Tag", DescriptionName = "Card_Description";
        private static readonly Color TagGold = new Color(1f, 0.8f, 0.3f);
        private static readonly Color BanishRed = new Color(1f, 0.35f, 0.3f);

        private void StyleCard(Button card, UpgradeData upgrade)
        {
            if (card == null || upgrade == null) return;
            var background = card.GetComponent<Image>();
            if (background != null) OptionsMenuUI.ApplyFocusColors(card, background);
            TMP_FontAsset font = null;
            foreach (var label in card.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (!label.name.StartsWith("Card_"))
                {
                    font = label.font;
                    label.gameObject.SetActive(false); // the old one-line label
                }
            // The scene's cards also carry old legacy Text labels ("Magic Wand (Level Up)") that showed through.
            foreach (var legacy in card.GetComponentsInChildren<Text>(true))
                legacy.gameObject.SetActive(false);
            if (!card.gameObject.activeSelf) card.gameObject.SetActive(true);

            var icon = CardPart<Image>(card.transform, IconName, new Vector2(0.012f, 0.1f), new Vector2(0.075f, 0.9f));
            icon.sprite = upgrade.Icon;
            icon.preserveAspect = true;
            icon.enabled = upgrade.Icon != null;
            icon.raycastTarget = false;

            int owned = OwnedLevel(upgrade);
            CardText(card.transform, TitleName, font, new Vector2(0.09f, 0.5f), new Vector2(0.8f, 0.95f), UpgradeText.DisplayName(upgrade),
                44f, Color.white, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            CardText(card.transform, TagName, font, new Vector2(0.8f, 0.5f), new Vector2(0.985f, 0.95f),
                _banishMode ? "BANISH" : UpgradeText.Tag(upgrade, owned), 34f, _banishMode ? BanishRed : TagGold,
                TextAlignmentOptions.BottomRight, FontStyles.Bold);
            CardText(card.transform, DescriptionName, font, new Vector2(0.09f, 0.06f), new Vector2(0.985f, 0.5f),
                UpgradeText.Describe(upgrade, owned), 30f, new Color(0.82f, 0.85f, 0.95f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
        }

        private static T CardPart<T>(Transform card, string name, Vector2 min, Vector2 max) where T : Component
        {
            var existing = card.Find(name);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.layer = card.gameObject.layer;
                var rect = (RectTransform)go.transform;
                rect.SetParent(card, false);
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private static void CardText(Transform card, string name, TMP_FontAsset font, Vector2 min, Vector2 max, string value,
            float maxSize, Color color, TextAlignmentOptions alignment, FontStyles style)
        {
            var text = CardPart<TextMeshProUGUI>(card, name, min, max);
            if (font != null) text.font = font;
            text.text = value;
            // Bug MIS B12 (2026-09-30): the first level-up used larger fonts. The cards are built while the panel is still
            // inactive, so TMP's Awake ran later and replaced the limits with its defaults (auto-size up to 72). A set size
            // keeps them (the same fix as the character select screen).
            text.fontSize = maxSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = maxSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;
            foreach (var tmpText in button.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (!tmpText.name.StartsWith("Card_")) { tmpText.text = label; return; }
            var legacyText = button.GetComponentInChildren<Text>(true);
            if (legacyText != null) legacyText.text = label;
        }

        private void OnCardClicked(UpgradeData upgrade)
        {
            if (_banishMode)
            {
                _banishMode = false;
                if (_levelUpManager != null) _levelUpManager.Banish(upgrade);
                HidePanelIfLevelUpFinished();
                return;
            }
            OnUpgradeSelected(upgrade);
        }

        private void OnRerollClicked()
        {
            _banishMode = false;
            if (_levelUpManager != null) _levelUpManager.Reroll();
            HidePanelIfLevelUpFinished();
        }

        private void OnSkipClicked()
        {
            if (_levelUpManager == null || _levelUpManager.SkipsLeft <= 0) return;
            // Close first so a queued level-up can reopen the panel cleanly.
            if (_levelUpPanel != null) _levelUpPanel.SetActive(false);
            _levelUpManager.Skip();
        }

        private void OnBanishClicked()
        {
            if (_levelUpManager == null) return;
            if (!_banishMode && _levelUpManager.BanishesLeft <= 0) return;
            // Banish is a two-step action: arm it, then click the card to remove.
            _banishMode = !_banishMode;
            for (int i = 0; i < _upgradeButtons.Length && i < _currentChoices.Count; i++)
                StyleCard(_upgradeButtons[i], _currentChoices[i]);
            RefreshActionButtons();
        }

        private void HidePanelIfLevelUpFinished()
        {
            if (_levelUpPanel != null && _levelUpManager != null && !_levelUpManager.IsLevelUpActive)
                _levelUpPanel.SetActive(false);
        }

        private void BindActionButtons()
        {
            if (_rerollButton != null) { _rerollButton.onClick.RemoveAllListeners(); _rerollButton.onClick.AddListener(OnRerollClicked); }
            if (_skipButton != null) { _skipButton.onClick.RemoveAllListeners(); _skipButton.onClick.AddListener(OnSkipClicked); }
            if (_banishButton != null) { _banishButton.onClick.RemoveAllListeners(); _banishButton.onClick.AddListener(OnBanishClicked); }
        }

        private void RefreshActionButtons()
        {
            if (_levelUpManager == null) return;
            foreach (var action in new[] { _rerollButton, _skipButton, _banishButton })
                WhitenLabels(action); // readable on the dark action buttons (was red)
            if (_rerollButton != null)
            {
                SetButtonLabel(_rerollButton, $"Reroll ({_levelUpManager.RerollsLeft})");
                _rerollButton.interactable = _levelUpManager.RerollsLeft > 0;
            }
            if (_skipButton != null)
            {
                SetButtonLabel(_skipButton, $"Skip ({_levelUpManager.SkipsLeft})");
                _skipButton.interactable = _levelUpManager.SkipsLeft > 0;
            }
            if (_banishButton != null)
            {
                SetButtonLabel(_banishButton, _banishMode ? "Cancel Banish" : $"Banish ({_levelUpManager.BanishesLeft})");
                _banishButton.interactable = _banishMode || _levelUpManager.BanishesLeft > 0;
            }
        }

        /// <summary>
        /// Builds the Reroll / Skip / Banish row under the upgrade cards by cloning the first card,
        /// unless the buttons are assigned in the Inspector. The row is created once and shared,
        /// so a second LevelUpUI on the same panel reuses it.
        /// </summary>
        private void EnsureActionButtons()
        {
            if (_rerollButton != null && _skipButton != null && _banishButton != null) return;

            Button template = null;
            foreach (var button in _upgradeButtons)
                if (button != null) { template = button; break; }
            if (template == null) return;

            Transform container = template.transform.parent;
            Transform bar = container.Find(ActionBarName);
            if (bar == null)
            {
                var templateRect = (RectTransform)template.transform;
                var barObject = new GameObject(ActionBarName, typeof(RectTransform));
                barObject.layer = template.gameObject.layer;
                bar = barObject.transform;
                bar.SetParent(container, false);
                bar.SetAsLastSibling();
                ((RectTransform)bar).sizeDelta = templateRect.sizeDelta;

                var layout = barObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 24f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
                barObject.AddComponent<LayoutElement>().preferredHeight = templateRect.sizeDelta.y;

                CreateActionButton(template, bar, "Reroll_Button");
                CreateActionButton(template, bar, "Skip_Button");
                CreateActionButton(template, bar, "Banish_Button");
            }

            // Smaller, dark action buttons under the cards (VS style), with the same focus colours.
            var barLayout = bar.GetComponent<LayoutElement>();
            if (barLayout != null) barLayout.preferredHeight = ((RectTransform)template.transform).sizeDelta.y * 0.55f;
            foreach (var action in bar.GetComponentsInChildren<Button>(true))
            {
                var image = action.GetComponent<Image>();
                if (image != null) OptionsMenuUI.ApplyFocusColors(action, image);
                WhitenLabels(action);
                foreach (Transform part in action.transform)
                    if (part.name.StartsWith("Card_")) part.gameObject.SetActive(false); // clones of a styled card
            }

            if (_rerollButton == null) _rerollButton = FindActionButton(bar, "Reroll_Button");
            if (_skipButton == null) _skipButton = FindActionButton(bar, "Skip_Button");
            if (_banishButton == null) _banishButton = FindActionButton(bar, "Banish_Button");
        }

        /// <summary>
        /// Luck can deal a 4th choice, but the panel only ships 3 cards. Clone the last card for any
        /// missing ones (reusing an existing clone by name, so a second LevelUpUI shares it).
        /// Extra cards are hidden on rounds with fewer choices by the normal card loop.
        /// </summary>
        private void EnsureCardCount(int count)
        {
            if (_upgradeButtons == null || _upgradeButtons.Length >= count) return;

            Button template = null;
            for (int i = _upgradeButtons.Length - 1; i >= 0 && template == null; i--)
                template = _upgradeButtons[i];
            if (template == null) return;

            Transform container = template.transform.parent;
            var cards = new List<Button>(_upgradeButtons);
            for (int i = cards.Count; i < count; i++)
            {
                string cardName = $"Upgrade_Button_{i}";
                Transform existing = container.Find(cardName);
                Button card = existing != null ? existing.GetComponent<Button>() : null;
                if (card == null)
                {
                    card = Instantiate(template, container, false);
                    card.name = cardName;
                    card.onClick = new Button.ButtonClickedEvent();
                    // Keep the new card with the others, above the action row.
                    card.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
                }
                cards.Add(card);
                template = card;
            }
            _upgradeButtons = cards.ToArray();
        }

        /// <summary>
        /// Bug MIS B15 (2026-09-30): the cards sit in a layout group that shares the height among them, so with one choice
        /// left the card stretched and the heading and stats panel jumped. Invisible spacers take the places of the missing
        /// cards (up to the panel's 3), so every round keeps the 3-card layout. They are not buttons: focus skips them.
        /// </summary>
        public const int BaseCardCount = 3;

        private void UpdateSpacers(int shown)
        {
            Button last = null;
            for (int i = 0; i < _upgradeButtons.Length && i < Mathf.Max(1, shown); i++)
                if (_upgradeButtons[i] != null) last = _upgradeButtons[i];
            if (last == null) return;
            Transform container = last.transform.parent;
            int need = Mathf.Max(0, BaseCardCount - shown);
            for (int i = 0; i < BaseCardCount; i++)
            {
                string name = "Card_Spacer_" + i;
                Transform spacer = container.Find(name);
                if (spacer == null && i < need)
                {
                    var go = new GameObject(name, typeof(RectTransform));
                    go.layer = container.gameObject.layer;
                    spacer = go.transform;
                    spacer.SetParent(container, false);
                }
                if (spacer == null) continue;
                // The same layout size as a card, so the group shares the height exactly as with three cards.
                var cardRect = (RectTransform)last.transform;
                var size = spacer.GetComponent<LayoutElement>() ?? spacer.gameObject.AddComponent<LayoutElement>();
                size.minHeight = LayoutUtility.GetMinHeight(cardRect);
                size.preferredHeight = LayoutUtility.GetPreferredHeight(cardRect);
                size.flexibleHeight = LayoutUtility.GetFlexibleHeight(cardRect);
                bool on = i < need;
                spacer.gameObject.SetActive(on);
                if (on) spacer.SetSiblingIndex(last.transform.GetSiblingIndex() + 1 + i);
            }
        }

        public const string HeadingText = "LEVEL UP!";

        /// <summary>
        /// The panel's "LEVEL UP" heading (a text beside the cards in ButtonContainer) was drawn at the top of the screen,
        /// over the HUD stage name. It now sits at the bottom of its box, just above the cards, in gold (VS style).
        /// </summary>
        /// <summary>The "LEVEL UP!" heading above the cards (null when the panel has none).</summary>
        private RectTransform HeadingRect()
        {
            foreach (var button in _upgradeButtons)
            {
                if (button == null) continue;
                foreach (Transform child in button.transform.parent)
                    if (child.GetComponent<Button>() == null && child.TryGetComponent<TextMeshProUGUI>(out var heading)) return heading.rectTransform;
                break;
            }
            return null;
        }

        private void StyleHeading()
        {
            Button first = null;
            foreach (var button in _upgradeButtons)
                if (button != null) { first = button; break; }
            if (first == null) return;
            foreach (Transform child in first.transform.parent)
            {
                if (child.GetComponent<Button>() != null || !child.TryGetComponent<TextMeshProUGUI>(out var heading)) continue;
                heading.text = HeadingText;
                heading.alignment = TextAlignmentOptions.Bottom;
                heading.enableAutoSizing = false;
                heading.fontSize = 64f;
                heading.fontStyle = FontStyles.Bold;
                heading.color = TagGold;
                heading.raycastTarget = false;
            }
        }

        /// <summary>
        /// The action buttons are clones of a card, whose label in HUD_Canvas is a red legacy Text; both TMP and legacy
        /// labels are turned white (the hidden Card_ parts keep their own colours).
        /// </summary>
        private static void WhitenLabels(Button action)
        {
            if (action == null) return;
            foreach (var label in action.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (!label.name.StartsWith("Card_")) label.color = Color.white;
            foreach (var label in action.GetComponentsInChildren<Text>(true))
                label.color = Color.white;
        }

        private static void CreateActionButton(Button template, Transform parent, string name)
        {
            Button clone = Instantiate(template, parent, false);
            clone.name = name;
            clone.gameObject.SetActive(true);
            clone.onClick = new Button.ButtonClickedEvent(); // drop any listeners copied from the card
        }

        private static Button FindActionButton(Transform bar, string name)
        {
            Transform child = bar.Find(name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private void OnUpgradeSelected(UpgradeData selectedUpgrade)
        {
            // Panel ko pehle band karein taake agar agla level up queue mein ho toh wo cleanly open ho sake
            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }

            if (_levelUpManager != null)
            {
                _levelUpManager.SelectUpgrade(selectedUpgrade);
            }
        }
    }
}
