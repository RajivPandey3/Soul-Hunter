using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// VS wiki (Treasure_Chest skipping rules): the first six chests on a save cannot be skipped; after that 1- and
    /// 3-item chests can be, and 5-item chests once 50 of them (or 500 chests) have been opened. Once 5-item chests can be
    /// skipped, chests holding only coin bags skip their animation by themselves. (The wiki's third unlock, having seen
    /// "the final fireworks", has no Soul Hunter counterpart.)
    /// </summary>
    public static class ChestSkipRules
    {
        public const int UnskippableChests = 6, FiveItemChestsToSkip = 50, ChestsToSkipFiveItems = 500;

        public static bool FiveItemSkipUnlocked(int chestsOpenedBefore, int fiveItemChestsBefore) =>
            fiveItemChestsBefore >= FiveItemChestsToSkip || chestsOpenedBefore >= ChestsToSkipFiveItems;

        public static bool CanSkip(int chestsOpenedBefore, int fiveItemChestsBefore, int itemCount)
        {
            if (chestsOpenedBefore < UnskippableChests) return false;
            return itemCount < 5 || FiveItemSkipUnlocked(chestsOpenedBefore, fiveItemChestsBefore);
        }

        public static bool SkipsItself(int chestsOpenedBefore, int fiveItemChestsBefore, bool onlyCoins) =>
            onlyCoins && FiveItemSkipUnlocked(chestsOpenedBefore, fiveItemChestsBefore);
    }

    /// <summary>
    /// The VS-style chest-opening screen (vampire.survivors.wiki/w/Treasure_Chest: the intro blast, the Open button,
    /// the background lighting up on 3- and 5-item chests, reels, stars behind each item, coin particles and a coin
    /// count-up that scales in and pulses, fireworks on 5-item chests, a Done button that rotates in, and the
    /// "Treasure Found" jingle followed by one jingle per chest size). Soul Hunter's own art: the approved Soul Reliquary
    /// chest and Soul Coins (ChestPreviewStage) and the approved VFX v2 sprites. The game is paused, so everything runs
    /// on unscaled time. Timings are provisional.
    /// </summary>
    public sealed class ChestOpeningScreen : MonoBehaviour
    {
        public enum Phase { Intro, WaitingToOpen, Revealing, Done, Closed }

        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f), Soul = new Color(0.45f, 0.95f, 1f), ArcanaTint = new Color(0.78f, 0.5f, 1f);

        public Phase Current { get; private set; } = Phase.Closed;
        public bool IsOpen => Current != Phase.Closed;
        public bool CanSkip { get; private set; }
        public int ItemCount => _result != null ? _result.ItemCount : 0;
        public int RevealedCount { get; private set; }
        public int ShownGold { get; private set; }
        public bool BackgroundLit => _circle != null && _circle.color.a > 0.05f;
        public int FireworksShown { get; private set; }
        public ChestPreviewStage Preview => _stage;
        public Button OpenButton => _openButton;
        public Button DoneButton => _doneButton;
        public event System.Action Closed;

        private ChestResult _result;
        private ChestPreviewStage _stage;
        private RectTransform _root;
        private Image _backdrop;
        private RawImage _chestImage, _circle, _flash, _ring;
        private TextMeshProUGUI _title, _coinText, _skipHint;
        private Button _openButton, _doneButton;
        private readonly List<RectTransform> _cards = new List<RectTransform>();
        private readonly List<Image> _cardIcons = new List<Image>();
        private readonly List<RawImage> _cardStars = new List<RawImage>();
        private readonly List<Sprite> _reelIcons = new List<Sprite>();
        private readonly List<(RawImage img, Vector2 v, float age)> _sparks = new List<(RawImage, Vector2, float)>();
        private Texture2D _sparkleTexture;
        private Coroutine _sequence;
        private float _coinPulse;

        public static ChestOpeningScreen Create(Canvas canvas)
        {
            var go = new GameObject("ChestOpeningScreen", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas.rootCanvas.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var screen = go.AddComponent<ChestOpeningScreen>();
            screen._root = rect;
            screen.Build();
            go.SetActive(false);
            return screen;
        }

        // ---------- build ----------

        private void Build()
        {
            var content = GameContentCatalog.Load();
            _sparkleTexture = content != null ? content.ChestFxSparkle : null;

            _backdrop = gameObject.AddComponent<Image>();
            _backdrop.color = new Color(0f, 0f, 0f, 0f);
            _backdrop.raycastTarget = true; // the game underneath does not take clicks

            // Centred a little above the middle; the title sits under the HUD's stage name.
            _circle = Raw("Light", content != null ? content.ChestFxCircle : null, new Vector2(0f, 70f), 760f);
            _chestImage = Raw("Chest", null, new Vector2(0f, 70f), 440f);
            _flash = Raw("Flash", content != null ? content.ChestFxFlash : null, new Vector2(0f, 70f), 560f);
            _ring = Raw("Ring", content != null ? content.ChestFxRing : null, new Vector2(0f, 70f), 460f);

            _title = Label("Title", new Vector2(0f, 330f), new Vector2(1200f, 110f), 78f);
            _coinText = Label("Coins", new Vector2(0f, -130f), new Vector2(700f, 90f), 64f);
            _coinText.color = Gold;
            _skipHint = Label("SkipHint", new Vector2(700f, -500f), new Vector2(420f, 50f), 26f);
            _skipHint.text = "Esc / Right click / B: skip";
            _skipHint.color = new Color(1f, 1f, 1f, 0.6f);

            _openButton = MakeButton("OPEN", new Vector2(0f, -430f), PressOpen);
            _doneButton = MakeButton("DONE", new Vector2(0f, -430f), PressDone);

            foreach (var definition in GameContentCatalog.Load() != null ? GameContentCatalog.Load().WeaponDefinitions : new WeaponDefinition[0])
                if (definition != null && definition.Icon != null) _reelIcons.Add(definition.Icon);
        }

        private RawImage Raw(string name, Texture texture, Vector2 position, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_root, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0f);
            return image;
        }

        private TextMeshProUGUI Label(string name, Vector2 position, Vector2 size, float fontSize, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent != null ? parent : _root, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(0, 0, 0, 255);
            return label;
        }

        private Button MakeButton(string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_root, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(340f, 96f);
            var button = go.GetComponent<Button>();
            OptionsMenuUI.ApplyFocusColors(button, go.GetComponent<Image>());
            button.onClick.AddListener(onClick);
            var label = Label("Label", Vector2.zero, rect.sizeDelta, 46f, rect);
            label.text = text;
            go.SetActive(false);
            return button;
        }

        // ---------- flow ----------

        /// <summary>Shows the screen for <paramref name="result"/>. The caller pauses the game.</summary>
        public void Show(ChestResult result, bool canSkip, GameObject chestPrefab, GameObject coinPrefab)
        {
            _result = result ?? new ChestResult { ItemCount = 1 };
            CanSkip = canSkip;
            RevealedCount = 0;
            ShownGold = 0;
            FireworksShown = 0;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ClearCards();
            if (_stage != null) Destroy(_stage.gameObject);
            _stage = ChestPreviewStage.Create(chestPrefab, coinPrefab, _result.IsArcana);
            _chestImage.texture = _stage.Texture;
            _chestImage.color = Color.white;
            _title.text = _result.IsArcana ? "ARCANA CHEST!" : _result.ItemCount >= 5 ? "TREASURE x5!" : _result.ItemCount >= 3 ? "TREASURE x3!" : "TREASURE!";
            _title.color = _result.IsArcana ? ArcanaTint : Gold;
            _coinText.text = "";
            _coinText.transform.localScale = Vector3.zero;
            _skipHint.gameObject.SetActive(canSkip);
            _circle.color = new Color(1f, 1f, 1f, 0f);
            _openButton.gameObject.SetActive(false);
            _doneButton.gameObject.SetActive(false);
            Current = Phase.Intro;
            _sequence = StartCoroutine(Intro());
        }

        private IEnumerator Intro()
        {
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.TreasureFoundSound != null ? audio.TreasureFoundSound : audio.ChestOpenSound);
            // Intro blast: the backdrop dims, the chest drops in with a flash and a ring.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                _backdrop.color = new Color(_result.IsArcana ? 0.08f : 0f, 0f, _result.IsArcana ? 0.12f : 0.02f, 0.88f * t);
                yield return null;
            }
            Blast(0.9f);
            yield return Wait(0.35f);
            Current = Phase.WaitingToOpen;
            yield return RotateIn(_openButton);
        }

        public void PressOpen()
        {
            if (Current != Phase.WaitingToOpen) return;
            if (_sequence != null) StopCoroutine(_sequence);
            _openButton.gameObject.SetActive(false);
            _sequence = StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            Current = Phase.Revealing;
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null && !_result.IsArcana) audio.PlaySFX(audio.TreasureJingle(_result.ItemCount));
            else if (audio != null) audio.PlaySFX(audio.TreasureCSound);
            int coins = _result.ItemCount >= 5 ? 36 : _result.ItemCount >= 3 ? 22 : 12;
            _stage.Open(coins);
            yield return Wait(ChestPreviewStage.LidOpenSeconds * 0.7f);
            Blast(1.2f);
            if (_result.ItemCount >= 3 || _result.IsArcana) _circle.color = new Color(1f, 1f, 1f, 0.01f); // the panel lights up (not on 1-item chests)

            BuildCards();
            var counting = StartCoroutine(CountCoins(1.1f));
            for (int i = 0; i < _cards.Count; i++)
            {
                yield return RevealCard(i, _result.ItemCount > 1 ? 0.55f : 0.2f);
                if (_result.ItemCount >= 5) Fireworks(3);
            }
            if (_result.ItemCount >= 5) { Fireworks(6); yield return Wait(0.4f); Fireworks(6); }
            yield return counting; // DONE only once the gold has finished counting up
            yield return Wait(0.5f);
            Current = Phase.Done;
            yield return RotateIn(_doneButton);
        }

        /// <summary>VS skip: jumps to the end (everything revealed, Done shown). Returns false when this chest cannot be skipped.</summary>
        public bool TrySkip()
        {
            if (!CanSkip || Current == Phase.Done || Current == Phase.Closed) return false;
            if (_sequence != null) StopCoroutine(_sequence);
            StopAllCoroutines();
            _openButton.gameObject.SetActive(false);
            _backdrop.color = new Color(0f, 0f, 0.02f, 0.88f);
            _stage.FinishNow();
            if (_cards.Count == 0) BuildCards();
            for (int i = 0; i < _cards.Count; i++) ShowCard(i);
            RevealedCount = _cards.Count;
            ShownGold = _result.Gold;
            _coinText.text = "+" + _result.Gold;
            _coinText.transform.localScale = Vector3.one;
            if (_result.ItemCount >= 3 || _result.IsArcana) _circle.color = new Color(1f, 1f, 1f, 0.35f);
            Current = Phase.Done;
            _doneButton.gameObject.SetActive(true);
            _doneButton.transform.localRotation = Quaternion.identity;
            _doneButton.transform.localScale = Vector3.one;
            Focus(_doneButton);
            return true;
        }

        public void PressDone()
        {
            if (Current != Phase.Done) return;
            Close();
        }

        private void Close()
        {
            StopAllCoroutines();
            Current = Phase.Closed;
            if (_stage != null) { Destroy(_stage.gameObject); _stage = null; }
            foreach (var spark in _sparks) if (spark.img != null) Destroy(spark.img.gameObject);
            _sparks.Clear();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void OnDestroy()
        {
            if (_stage != null) Destroy(_stage.gameObject);
        }

        // ---------- pieces ----------

        private void BuildCards()
        {
            ClearCards();
            int n = Mathf.Max(1, _result.Rewards.Count);
            float width = 190f, gap = 22f;
            float start = -(n - 1) * (width + gap) * 0.5f;
            for (int i = 0; i < _result.Rewards.Count; i++)
            {
                var reward = _result.Rewards[i];
                var card = new GameObject("Reward" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                card.SetParent(_root, false);
                card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
                card.anchoredPosition = new Vector2(start + i * (width + gap), -270f);
                card.sizeDelta = new Vector2(width, 190f);
                var frame = card.GetComponent<Image>();
                frame.raycastTarget = false;
                frame.color = reward.IsEvolution ? new Color(0.35f, 0.1f, 0.12f, 0.92f) : new Color(0.07f, 0.06f, 0.12f, 0.92f);

                var star = new GameObject("Stars", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                star.rectTransform.SetParent(card, false);
                star.rectTransform.anchoredPosition = new Vector2(0f, 22f);
                star.rectTransform.sizeDelta = new Vector2(170f, 170f);
                star.texture = _sparkleTexture;
                star.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
                star.raycastTarget = false;

                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.rectTransform.SetParent(card, false);
                icon.rectTransform.anchoredPosition = new Vector2(0f, 22f);
                icon.rectTransform.sizeDelta = new Vector2(100f, 100f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = false;

                var name = Label("Name", new Vector2(0f, -52f), new Vector2(width - 8f, 40f), 22f, card);
                name.text = reward.Name;
                name.enableAutoSizing = true; name.fontSizeMin = 12f; name.fontSizeMax = 22f;
                var level = Label("Level", new Vector2(0f, -80f), new Vector2(width - 8f, 28f), 20f, card);
                level.text = reward.IsEvolution ? "EVOLUTION!" : reward.IsCoinBag ? "+25 gold" : reward.Level <= 1 ? "NEW!" : "Level " + reward.Level;
                level.color = reward.IsEvolution ? new Color(1f, 0.4f, 0.35f) : Gold;
                name.gameObject.SetActive(false);
                level.gameObject.SetActive(false);
                card.localScale = Vector3.zero;
                _cards.Add(card);
                _cardIcons.Add(icon);
                _cardStars.Add(star);
            }
        }

        private void ClearCards()
        {
            foreach (var card in _cards) if (card != null) Destroy(card.gameObject);
            _cards.Clear(); _cardIcons.Clear(); _cardStars.Clear();
        }

        private IEnumerator RevealCard(int index, float reelSeconds)
        {
            var card = _cards[index];
            var icon = _cardIcons[index];
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.15f) { card.localScale = Vector3.one * t; yield return null; }
            card.localScale = Vector3.one;
            // Reel: icons spin past before the prize stops.
            icon.enabled = _reelIcons.Count > 0;
            float next = 0f;
            for (float t = 0f; t < reelSeconds; t += Time.unscaledDeltaTime)
            {
                if (t >= next && _reelIcons.Count > 0)
                {
                    icon.sprite = _reelIcons[Random.Range(0, _reelIcons.Count)];
                    next = t + Mathf.Lerp(0.04f, 0.12f, t / reelSeconds);
                }
                yield return null;
            }
            ShowCard(index);
            RevealedCount = Mathf.Max(RevealedCount, index + 1);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UIConfirmSound, 0.7f);
            Burst(card.anchoredPosition + new Vector2(0f, 22f), 8, _result.Rewards[index].IsEvolution ? new Color(1f, 0.45f, 0.35f) : Gold);
        }

        private void ShowCard(int index)
        {
            var reward = _result.Rewards[index];
            var card = _cards[index];
            card.localScale = Vector3.one;
            var icon = _cardIcons[index];
            icon.sprite = reward.Icon;
            icon.enabled = reward.Icon != null;
            _cardStars[index].color = new Color(Gold.r, Gold.g, Gold.b, 0.9f);
            foreach (Transform child in card) child.gameObject.SetActive(true);
            if (!icon.enabled) icon.gameObject.SetActive(false);
        }

        private IEnumerator CountCoins(float seconds)
        {
            // Scales in, counts up, then pulses.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.2f) { _coinText.transform.localScale = Vector3.one * t; yield return null; }
            _coinText.transform.localScale = Vector3.one;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                ShownGold = Mathf.RoundToInt(_result.Gold * Mathf.Clamp01(t / seconds));
                _coinText.text = "+" + ShownGold;
                yield return null;
            }
            ShownGold = _result.Gold;
            _coinText.text = "+" + ShownGold;
            _coinPulse = 1f;
        }

        private void Blast(float strength)
        {
            StartCoroutine(Pulse(_flash, 0.35f, 0.6f, 1.25f * strength, Color.white));
            StartCoroutine(Pulse(_ring, 0.5f, 0.3f, 1.9f * strength, _result != null && _result.IsArcana ? ArcanaTint : Soul));
        }

        private static IEnumerator Pulse(RawImage image, float seconds, float fromScale, float toScale, Color color)
        {
            if (image == null || image.texture == null) yield break;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                image.rectTransform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, 1f - (1f - t) * (1f - t));
                image.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }
            image.color = new Color(color.r, color.g, color.b, 0f);
        }

        private IEnumerator RotateIn(Button button)
        {
            button.gameObject.SetActive(true);
            var t0 = button.transform;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                t0.localRotation = Quaternion.Euler(0f, 0f, (1f - t) * -120f);
                t0.localScale = Vector3.one * t;
                yield return null;
            }
            t0.localRotation = Quaternion.identity;
            t0.localScale = Vector3.one;
            Focus(button);
        }

        private static void Focus(Button button)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private void Fireworks(int count)
        {
            FireworksShown++;
            var colors = new[] { Gold, Soul, new Color(1f, 0.4f, 0.4f), new Color(0.6f, 1f, 0.5f), ArcanaTint };
            for (int i = 0; i < count; i++)
                Burst(new Vector2(Random.Range(-820f, 820f), Random.Range(-120f, 460f)), 10, colors[Random.Range(0, colors.Length)]);
        }

        private void Burst(Vector2 at, int sparks, Color color)
        {
            if (_sparkleTexture == null) return;
            for (int i = 0; i < sparks; i++)
            {
                var img = new GameObject("Spark", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                img.rectTransform.SetParent(_root, false);
                img.rectTransform.anchoredPosition = at;
                img.rectTransform.sizeDelta = Vector2.one * Random.Range(26f, 48f);
                img.texture = _sparkleTexture;
                img.color = color;
                img.raycastTarget = false;
                float angle = Random.value * Mathf.PI * 2f;
                _sparks.Add((img, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(160f, 420f), 0f));
            }
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_circle.color.a > 0f)
            {
                _circle.rectTransform.Rotate(0f, 0f, -25f * dt);
                float a = Mathf.MoveTowards(_circle.color.a, 0.35f, dt * 0.8f);
                var tint = _result != null && _result.IsArcana ? ArcanaTint : Gold;
                _circle.color = new Color(tint.r, tint.g, tint.b, a);
            }
            for (int i = 0; i < _cardStars.Count; i++)
                if (_cardStars[i] != null) _cardStars[i].rectTransform.Rotate(0f, 0f, 40f * dt);
            if (_coinPulse > 0f)
            {
                _coinPulse += dt;
                _coinText.transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(_coinPulse * 6f));
            }
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                var (img, v, age) = _sparks[i];
                age += dt;
                if (img == null || age > 0.9f) { if (img != null) Destroy(img.gameObject); _sparks.RemoveAt(i); continue; }
                v *= 1f - 2.2f * dt;
                img.rectTransform.anchoredPosition += v * dt;
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1f - age / 0.9f);
                _sparks[i] = (img, v, age);
            }

            if (CanSkip && SkipPressed()) TrySkip();
        }

        private static bool SkipPressed()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                   (mouse != null && mouse.rightButton.wasPressedThisFrame) ||
                   (pad != null && pad.buttonEast.wasPressedThisFrame);
        }
    }
}
