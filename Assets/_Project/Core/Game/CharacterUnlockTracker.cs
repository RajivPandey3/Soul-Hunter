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
            if (Time.unscaledTime >= _noticeUntil) Destroy(_notice);
        }

        private bool _noticeSounded;

        /// <summary>The unlock notice, while one exists (tests).</summary>
        public GameObject Notice => _notice;

        private void HandleStageCleared(int stage)
        {
            _highestClearedStage = Mathf.Max(_highestClearedStage, stage);
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
                ShowNotice(character);
                OnCharacterUnlocked?.Invoke(character);
            }
            if (unlocked.Count > 0) _save.SaveGame();
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

        private void ShowNotice(CharacterData character)
        {
            if (_notice != null) Destroy(_notice);
            _notice = new GameObject("CharacterUnlockedNotice");
            var canvas = _notice.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = _notice.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(_notice.transform, false);
            var rect = (RectTransform)textGo.transform;
            rect.anchorMin = new Vector2(.2f, .78f);
            rect.anchorMax = new Vector2(.8f, .88f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 44f;
            text.color = new Color(1f, .85f, .25f);
            text.text = $"New character unlocked: {character.CharacterName}!";
            text.raycastTarget = false;
            _noticeUntil = Time.unscaledTime + NoticeSeconds;
            _noticeSounded = false;
            UpdateNotice(); // shown (with its sound) now, or held hidden until a menu closes
        }
    }
}
