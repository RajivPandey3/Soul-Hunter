using System;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// VS rule: while a run is played, checks every locked character's unlock condition (stage cleared,
    /// time survived in a stage, player level, kills, weapon level). A met condition unlocks the character
    /// in the save for good and shows a short notice; buying it happens on the character select screen.
    /// GameSessionManager adds this to itself, so every run has exactly one.
    /// </summary>
    public sealed class CharacterUnlockTracker : MonoBehaviour
    {
        private const float NoticeSeconds = 3.5f;

        private LevelProgressionManager _progression;
        private SaveService _save;
        private int _highestClearedStage;
        private int _lastCheckedSecond = -1;
        private bool _subscribed;
        private GameObject _notice;
        private float _noticeUntil;

        public event Action<CharacterData> OnCharacterUnlocked;
        public int HighestClearedStage => _highestClearedStage;

        private void Start()
        {
            GameServices.Instance?.TryGet(out _save);
            _progression = LevelProgressionManager.Instance;
            if (_progression != null) _progression.OnStageCleared += HandleStageCleared;
            if (RunStatsTracker.Instance != null) RunStatsTracker.Instance.OnKillsChanged += HandleKills;
            if (WeaponManager.Instance != null) WeaponManager.Instance.OnWeaponAcquiredOrUpgraded += HandleWeapon;
            _subscribed = true;
            Evaluate();
        }

        private void OnDestroy()
        {
            if (!_subscribed) return;
            if (_progression != null) _progression.OnStageCleared -= HandleStageCleared;
            if (RunStatsTracker.Instance != null) RunStatsTracker.Instance.OnKillsChanged -= HandleKills;
            if (WeaponManager.Instance != null) WeaponManager.Instance.OnWeaponAcquiredOrUpgraded -= HandleWeapon;
        }

        private void Update()
        {
            // Time and player level change continuously; checking once per stage second is enough.
            int second = _progression != null ? Mathf.FloorToInt(_progression.StageElapsedTime) : 0;
            if (second != _lastCheckedSecond)
            {
                _lastCheckedSecond = second;
                Evaluate();
            }
            UpdateNotice();
        }

        /// <summary>
        /// Bug MIS B05 (2026-09-30): an unlock during a level-up drew "New character unlocked" on top of "LEVEL UP!". While a
        /// menu has the game paused the notice waits hidden and its time does not run; it shows (with its sound) once
        /// play resumes, as the story card does.
        /// </summary>
        private void UpdateNotice()
        {
            if (_notice == null) return;
            bool paused = SoulHunter.Core.Services.GameTime.IsPaused;
            if (paused)
            {
                if (_notice.activeSelf) _notice.SetActive(false);
                _noticeUntil += Time.unscaledDeltaTime;
                return;
            }
            if (!_notice.activeSelf || !_noticeSounded)
            {
                _notice.SetActive(true);
                if (!_noticeSounded)
                {
                    _noticeSounded = true;
                    var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                    if (audio != null) audio.PlaySFX(audio.UnlockSound);
                }
            }
            LayoutNotice();
            if (Time.unscaledTime < _noticeUntil) return;
            Destroy(_notice);
            _notice = null;
            // A notice that a newer one replaced before it could be read comes back now.
            if (_replaced.Count > 0) ShowNotice(_replaced.Pop());
        }

        /// <summary>
        /// Notices replaced within their first ReadSeconds (two unlocks in the same second: at 15:00 a Skip rank and an
        /// item). The newest shows at once, as before; the ones it covered follow it, so no unlock goes unseen.
        /// </summary>
        private readonly System.Collections.Generic.Stack<string> _replaced = new System.Collections.Generic.Stack<string>();
        private const float ReadSeconds = 1.5f;
        private const int MaxReplaced = 8;

        /// <summary>Notice size and place, in canvas units (from the safe area's top centre).</summary>
        public const float NoticeWidth = 1000f, NoticeHeight = 97f, NoticeTop = 135f, NoticeMargin = 24f;

        private RectTransform _noticeBox;
        private TMPro.TextMeshProUGUI _noticeText;
        private Vector4 _noticeLaidOut;

        /// <summary>The notice's box (its backing plate) and its text, while a notice exists (tests).</summary>
        public RectTransform NoticeBox => _notice != null ? _noticeBox : null;
        public TMPro.TextMeshProUGUI NoticeText => _notice != null ? _noticeText : null;

        /// <summary>
        /// Fits the notice to the screen it is on: hung from the safe area's top centre; as wide as NoticeWidth or the safe
        /// area minus its margins, whichever is less; taller when a narrow screen wraps it onto more lines; its text never
        /// under the touch floor (12 sp). Runs when it is shown and whenever the canvas or safe area changes (a rotation).
        /// </summary>
        private void LayoutNotice()
        {
            if (_notice == null || _noticeBox == null || _noticeText == null) return;
            var canvasRect = _notice.transform as RectTransform;
            Rect safe = SoulHunter.Gameplay.UI.UiScreen.SafeAreaNormalized;
            float room = canvasRect != null ? canvasRect.rect.width * safe.width : NoticeWidth;
            // On a phone the notice hangs under the HUD strip and the stage name (it was drawn over the stage name).
            float top = Mathf.Max(NoticeTop, SoulHunter.Gameplay.UI.GameHUDController.PhoneClearTop);
            var key = new Vector4(room, safe.xMin + safe.xMax * 7f, top, safe.yMax);
            if (key == _noticeLaidOut) return;
            _noticeLaidOut = key;
            _noticeBox.anchorMin = _noticeBox.anchorMax = new Vector2(safe.center.x, safe.yMax);
            _noticeBox.anchoredPosition = new Vector2(0f, -top);
            float floor = Mathf.Max(18f, SoulHunter.Gameplay.UI.UiScreen.MinTextUnits);
            _noticeText.fontSizeMin = floor;
            if (_noticeText.fontSizeMax < floor) _noticeText.fontSizeMax = floor;
            float width = Mathf.Max(1f, Mathf.Min(NoticeWidth, room - 2f * NoticeMargin));
            // Height: the approved 97 units hold one line; a narrow screen wraps the line, so the box grows to hold it.
            float size = _noticeText.fontSize;
            _noticeText.fontSize = Mathf.Max(floor, 28f);
            float wrapped = _noticeText.GetPreferredValues(_noticeText.text, Mathf.Max(1f, width - 48f), 0f).y + 12f;
            _noticeText.fontSize = size;
            _noticeBox.sizeDelta = new Vector2(width, Mathf.Max(NoticeHeight, Mathf.Ceil(wrapped)));
        }

        private bool _noticeSounded;

        /// <summary>The unlock notice, while one exists (tests).</summary>
        public GameObject Notice => _notice;

        private void HandleStageCleared(int stage)
        {
            _highestClearedStage = Mathf.Max(_highestClearedStage, stage);
            // VS stage completion per character (wiki Characters): OnStageCleared fires only for a level cleared at 30:00
            // or for Level 10 when Shadow Kael dies, so this is the level completed with the character played.
            var data = _save != null ? _save.CurrentData : null;
            if (data != null && CharacterStages.Record(data, GameContentCatalog.CurrentName(data.SelectedCharacterName), stage))
                _save.SaveGame();
            // A stage clears at 30:00: that is the full stage survived, even though the clock moves on to the next one.
            RecordPowerUpUnlocks(stage, 30f * 60f);
            Evaluate();
        }

        private void HandleKills(int _) => Evaluate();
        private void HandleWeapon(UpgradeData _) => Evaluate();

        public RunProgress CurrentProgress()
        {
            var experience = PlayerController.Instance != null ? PlayerController.Instance.GetComponent<PlayerExperience>() : null;
            var weapons = WeaponManager.Instance;
            return new RunProgress(
                _progression != null ? _progression.CurrentStage : 1,
                _progression != null ? _progression.StageElapsedTime : 0f,
                _highestClearedStage,
                experience != null ? experience.CurrentLevel : 1,
                RunStatsTracker.Instance != null ? RunStatsTracker.Instance.TotalKills : 0,
                weapons != null ? (Func<UpgradeData.UpgradeType, int>)weapons.GetWeaponLevel : null);
        }

        /// <summary>Unlocks (and saves) every character whose condition this run has met.</summary>
        public void Evaluate()
        {
            var data = _save != null ? _save.CurrentData : null;
            if (data == null) return;
            var unlocked = UnlockInto(data, CurrentProgress());
            foreach (var character in unlocked)
            {
                Debug.Log($"[CharacterUnlockTracker] {character.CharacterName} unlocked ({CharacterUnlocks.ConditionText(character)})");
                ShowNotice($"New character unlocked: {character.CharacterName}!");
                if (RunStatsTracker.Instance != null) RunStatsTracker.Instance.RecordUnlock($"Character: {character.CharacterName}");
                OnCharacterUnlocked?.Invoke(character);
            }
            bool powerUps = RecordPowerUpUnlocks(_progression != null ? _progression.CurrentStage : 1, _progression != null ? _progression.StageElapsedTime : 0f, save: false);
            if (unlocked.Count > 0 || powerUps) _save.SaveGame();
        }

        /// <summary>
        /// VS locked PowerUps (owner, 2026-10-01): a set character at level 80 unlocks a Reroll rank, and time survived in
        /// Levels 6-10 unlocks Skip ranks (<see cref="PowerUpUnlocks"/>). Returns true when something new was unlocked.
        /// </summary>
        private bool RecordPowerUpUnlocks(int stage, float stageSeconds, bool save = true)
        {
            var data = _save != null ? _save.CurrentData : null;
            if (data == null) return false;
            var experience = PlayerController.Instance != null ? PlayerController.Instance.GetComponent<PlayerExperience>() : null;
            string character = GameContentCatalog.CurrentName(data.SelectedCharacterName);
            var news = PowerUpUnlocks.Record(data, character, experience != null ? experience.CurrentLevel : 1, stage, stageSeconds);
            foreach (var line in news)
            {
                Debug.Log($"[CharacterUnlockTracker] {line}");
                ShowNotice(line);
                if (RunStatsTracker.Instance != null) RunStatsTracker.Instance.RecordUnlock(line);
            }
            if (news.Count > 0 && save) _save.SaveGame();
            return news.Count > 0;
        }

        /// <summary>Marks every catalog character whose condition is met as unlocked in data; returns the new ones.</summary>
        public static System.Collections.Generic.List<CharacterData> UnlockInto(GameData data, in RunProgress progress)
        {
            var unlocked = new System.Collections.Generic.List<CharacterData>();
            var catalog = GameContentCatalog.Load();
            if (data == null || catalog == null || catalog.Characters == null) return unlocked;
            foreach (var character in catalog.Characters)
            {
                if (character == null || CharacterUnlocks.IsUnlocked(character, data)) continue;
                if (CharacterUnlocks.IsMet(character, progress) && CharacterUnlocks.MarkUnlocked(character, data))
                    unlocked.Add(character);
            }
            return unlocked;
        }

        /// <summary>
        /// Shows and lists an unlock earned elsewhere (CollectionTracker: Banish ranks, favourites, the Bestiary), the same way
        /// as a character or PowerUp unlock. The caller saves.
        /// </summary>
        public void AnnounceUnlock(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            Debug.Log($"[CharacterUnlockTracker] {line}");
            ShowNotice(line);
            if (RunStatsTracker.Instance != null) RunStatsTracker.Instance.RecordUnlock(line);
        }

        /// <summary>Shows a line in the notice without listing it as an unlock (a Soul Egg picked up).</summary>
        public void ShowMessage(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            Debug.Log($"[CharacterUnlockTracker] {line}");
            ShowNotice(line);
        }

        private void ShowNotice(string message)
        {
            if (_notice != null)
            {
                bool unread = !_noticeSounded || _noticeUntil - Time.unscaledTime > NoticeSeconds - ReadSeconds;
                if (unread && _noticeText != null && _noticeText.text != message && _replaced.Count < MaxReplaced)
                    _replaced.Push(_noticeText.text);
                Destroy(_notice);
            }
            _notice = new GameObject("CharacterUnlockedNotice");
            var canvas = _notice.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
            // Bug MIS v2 C13 (2026-10-02): yellow text straight over the horde was hard to read; a dark plate sits behind it.
            // Responsive (owner, 2026-10-10: landscape and portrait): the plate is the notice's box. It hangs from the top
            // centre of the safe area, NoticeTop units under it, at most NoticeWidth wide and never wider than the screen
            // minus its margins (LayoutNotice). Before, it was 52% of a 1920-wide layout, fixed by screen shares.
            var plateGo = new GameObject("Backing", typeof(RectTransform));
            plateGo.transform.SetParent(_notice.transform, false);
            _noticeBox = (RectTransform)plateGo.transform;
            _noticeBox.pivot = new Vector2(0.5f, 1f);
            var plateImage = plateGo.AddComponent<UnityEngine.UI.Image>();
            SoulHunter.Gameplay.UI.UITheme.ApplySurface(plateImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.HudBacking);
            plateImage.raycastTarget = false;
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(_noticeBox, false);
            var rect = (RectTransform)textGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.fontSize = 40f; // a size first, so TextMeshPro's later Awake keeps these limits
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 40f;
            text.margin = new Vector4(24f, 6f, 24f, 6f);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(text, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Accent);
            text.text = message;
            text.raycastTarget = false;
            _noticeText = text;
            _noticeLaidOut = new Vector4(-1f, -1f, -1f, -1f);
            LayoutNotice();
            _noticeUntil = Time.unscaledTime + NoticeSeconds;
            _noticeSounded = false;
            UpdateNotice(); // shown (with its sound) now, or held hidden until a menu closes
        }
    }
}
