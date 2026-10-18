using UnityEngine;
using System;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// GDD Feature: 10-Level Progression System.
    /// Ye manager stages (1 se 10) ko handle karta hai. Har stage mein time chalta hai, boss aata hai,
    /// aur boss ke marne par next stage shuru hota hai.
    /// </summary>
    public class LevelProgressionManager : MonoBehaviour
    {
        private static LevelProgressionManager _instance;
        public static LevelProgressionManager Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LevelProgressionManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("LevelProgressionManager (Auto)");
                        _instance = go.AddComponent<LevelProgressionManager>();
                    }
                }
                return _instance;
            }
        }
        
        public int CurrentStage { get; private set; } = 1;
        public const int MaxStages = 10;
        public CampaignLevelDefinition CurrentLevel => CampaignLevelCatalog.Get(CurrentStage);
        public float StageElapsedTime { get; private set; }
        public bool IsBossWindow => StageElapsedTime >= CurrentLevel.BossStartSeconds;
        /// <summary>True between a stage being cleared and the next one starting.</summary>
        public bool IsStageTransitioning => _stageTransitionQueued;

        public event Action<int> OnStageStarted;
        public event Action<int> OnStageCleared;
        public event Action<float> OnStageTimeChanged;
        public event Action<int> OnRunDurationReached;
        public event Action OnGameWon;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else if (_instance != this) { Destroy(gameObject); return; }
            BeginRunModes();
        }

        /// <summary>
        /// VS run modes: the gameplay scene's progression manager starts the run, so it copies the modes saved on the
        /// character select screen into RunModes (after clearing anything an earlier run left) before any Start runs.
        /// Limit Break bonuses of an earlier run are cleared too. Restart and back-to-menu destroy this object, which
        /// resets RunModes (OnDestroy).
        /// </summary>
        private static void BeginRunModes()
        {
            SoulHunter.Core.Persistence.SaveService save = null;
            SoulHunter.Core.Services.GameServices.Instance?.TryGet(out save);
            RunModeOptions.BeginRun(save != null ? save.CurrentData : null);
            // VS Music Selection (P3-05): the Soundtrack choice plays all run; without the lock it lasts this run only.
            SoulHunter.Gameplay.Audio.MusicSelection.BeginRun(save != null ? save.CurrentData : null);
            LimitBreak.Reset();
            if (RunModes.Hurry || RunModes.Hyper || RunModes.Inverse || RunModes.Endless || RunModes.Arcana || RunModes.LimitBreak
                || RunModes.RandomEvents || RunModes.RandomLevelUp)
                Debug.Log($"[LevelProgressionManager] Run modes: Hurry={RunModes.Hurry} Hyper={RunModes.Hyper} Inverse={RunModes.Inverse} " +
                          $"Endless={RunModes.Endless} Arcana={RunModes.Arcana} LimitBreak={RunModes.LimitBreak} " +
                          $"Omens={RunModes.RandomEvents} BlindFate={RunModes.RandomLevelUp}");
        }

        private void Start()
        {
            // Game start hone par Stage 1 shuru karo
            StartStage(1);
        }

        private bool _gameWon;
        private int _lastStageSecond = -1;
        private bool _runDurationReached;
        private bool _stageTransitionQueued;
        private bool _bossDefeated;

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            CampaignLevelCatalog.CurrentStage = 0; // no stage bonuses outside a run
            RunModes.Reset(); // nor run modes: the next run copies them from the save again
            SoulHunter.Gameplay.Audio.MusicSelection.Reset();
            LimitBreak.Reset();
            _instance = null;
        }

        private void Update()
        {
            if (_gameWon || Time.timeScale <= 0f) return;
            AdvanceClock(Time.deltaTime);
        }

        /// <summary>
        /// Moves the stage clock by <paramref name="deltaTime"/> of game time. VS Hurry mode runs the clock twice as fast
        /// (RunModes.ClockSpeed), so everything scheduled on it (waves, events, chest elites, the boss, 30:00) comes sooner.
        /// </summary>
        public void AdvanceClock(float deltaTime)
        {
            if (_gameWon || deltaTime <= 0f) return;
            StageElapsedTime += deltaTime * RunModes.ClockSpeed;
            OnStageTimeChanged?.Invoke(StageElapsedTime);

            int second = Mathf.FloorToInt(StageElapsedTime);
            if (second == _lastStageSecond) return;
            _lastStageSecond = second;

            // The guardian rule is told before the guardian comes, so the player can get ready (CTO MIS X3, 2026-10-13).
            if (!_guardianWarned && StageElapsedTime >= CurrentLevel.BossStartSeconds - GuardianWarningLeadSeconds)
            {
                _guardianWarned = true;
                if (CurrentStage < MaxStages && StageElapsedTime < CurrentLevel.BossStartSeconds)
                {
                    var notices = FindFirstObjectByType<CharacterUnlockTracker>();
                    if (notices != null) notices.ShowMessage(GuardianWarning(CurrentStage, CurrentLevel.BossStartSeconds));
                }
            }

            if (!_runDurationReached && StageElapsedTime >= CurrentLevel.RunDurationSeconds)
            {
                _runDurationReached = true;
                Debug.Log($"[LevelProgressionManager] Stage {CurrentStage} reached its 30-minute survival limit.");
                OnRunDurationReached?.Invoke(CurrentStage);
                CompleteStageAfterSurvival();
            }
        }

        public void StartStage(int stageNumber)
        {
            if (_gameWon) return;
            CurrentStage = Mathf.Clamp(stageNumber, 1, MaxStages);
            CampaignLevelCatalog.CurrentStage = CurrentStage; // stage bonuses (player speed, gold)
            StageElapsedTime = 0f;
            _lastStageSecond = -1;
            _runDurationReached = false;
            _stageTransitionQueued = false;
            _bossDefeated = false;
            _guardianWarned = false;
            StageFailed = false;
            // Stage transition must not carry the previous stage's slow motion forward,
            // but a level-up, chest or merchant screen still open keeps its pause.
            SoulHunter.Core.Services.GameTime.SetSlowMotion(1f);
            var player = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
            if (player != null) player.ResumeMovementAfterMenu();
            ApplyHyperLuck(player != null ? player.Stats : null);
            var level = CurrentLevel;
            Debug.Log($"[LevelProgressionManager] Stage {CurrentStage}: {level.Name} Started! Modifier: {level.Modifier}");
            OnStageStarted?.Invoke(CurrentStage);
        }

        private SoulHunter.Gameplay.Player.PlayerStats _hyperLuckStats;
        private float _hyperLuckApplied;

        /// <summary>The Hyper Luck this run's player holds from the current stage (0 without Hyper or on Level 1).</summary>
        public float HyperLuckApplied => _hyperLuckApplied;

        /// <summary>
        /// VS Hyper Luck follows the stage (+10% on the stages mapped to Inlaid Library, Dairy Plant, Gallo Tower and
        /// Cappella Magna, none on Mad Forest; each stage infobox's luck2). On each stage start the previous stage's Hyper
        /// Luck is taken back and the new one given, so it never stacks. Inverse's +20% is given once by PlayerStats.
        /// </summary>
        private void ApplyHyperLuck(SoulHunter.Gameplay.Player.PlayerStats stats)
        {
            if (stats == null) return;
            if (stats != _hyperLuckStats) { _hyperLuckStats = stats; _hyperLuckApplied = 0f; }
            float wanted = RunModes.HyperLuckBonus(CurrentStage);
            float delta = wanted - _hyperLuckApplied;
            if (Mathf.Approximately(delta, 0f)) return;
            stats.AddLuck(delta);
            _hyperLuckApplied = wanted;
        }

        /// <summary>
        /// Debug/QA: moves the stage clock to the boss's arrival (e.g. to meet Shadow Kael without playing 25 minutes).
        /// The stage timer, boss window and later survival limit then run from there as usual.
        /// </summary>
        public void SkipToBoss()
        {
            if (_gameWon || StageElapsedTime >= CurrentLevel.BossStartSeconds) return;
            SkipTo(CurrentLevel.BossStartSeconds);
        }

        /// <summary>
        /// Debug/QA: moves the stage clock forward to <paramref name="seconds"/> (e.g. just before a 5-minute chest elite).
        /// Never moves it back. What the spawner schedules by the clock (chest elites, events, the boss) follows.
        /// </summary>
        public void SkipTo(float seconds)
        {
            if (_gameWon || seconds <= StageElapsedTime) return;
            StageElapsedTime = seconds;
            Debug.Log($"[LevelProgressionManager] Skipped Stage {CurrentStage} clock to {StageElapsedTime:0}s.");
        }

        /// <summary>True once this stage's boss has died (the stage itself runs on to 30:00).</summary>
        public bool IsBossDefeated => _bossDefeated;

        public void ReportBossDefeated()
        {
            if (_bossDefeated || _gameWon) return;
            _bossDefeated = true;
            Debug.Log($"[LevelProgressionManager] Boss of Stage {CurrentStage} Defeated!");
            // Owner (2026-09-27): every stage lasts the full 30 minutes, as in VS; killing the 25:00 boss no longer
            // ends it. 30:00 clears the screen and starts the next stage. The Level 10 boss (Shadow Kael) is the end
            // of the story, so killing him still wins the run (in Endless mode it starts the next cycle instead).
            if (CurrentStage >= MaxStages) CompleteStage("its final boss was defeated");
        }

        /// <summary>
        /// Owner decision (2026-10-13: "lv 1 mein kuch achive kare tabhi lv 2 open ho sake athwa nahi"; chosen: defeat the
        /// stage boss; otherwise the run ends; for this run only). The next level opens only when this level's guardian
        /// (the 25:00 boss) has been defeated by 30:00. If it still stands at 30:00 the run ends there with the Results
        /// screen. Nothing is kept on the save: every run starts at Level 1 and earns each level again.
        /// VS unlocks its stages by hero level in the stage before (wiki Stages: Inlaid Library "reaching level 20 in the
        /// Mad Forest", then 40, 60, 80); in Soul Hunter's one run the hero's level carries on, so the owner chose the boss.
        /// Level 10 is unchanged: it has no time limit and ends only with Shadow Kael.
        /// </summary>
        public bool StageFailed { get; private set; }
        /// <summary>Raised at 30:00 of a level whose guardian still stands, just before the run ends.</summary>
        public event Action<int> OnStageFailed;
        /// <summary>The line shown when the level's boss arrives (and under GAME OVER when it was not defeated in time).</summary>
        public static string GuardianGoal(int stage) => $"Defeat the guardian before 30:00 to open Level {stage + 1}.";
        /// <summary>Stage seconds before the guardian comes at which its rule is told (20:00 for a 25:00 guardian).</summary>
        public const float GuardianWarningLeadSeconds = 300f;
        private bool _guardianWarned;
        public static string GuardianWarning(int stage, float bossStartSeconds) =>
            $"The guardian of Level {stage} comes at {Mathf.FloorToInt(bossStartSeconds / 60f)}:{Mathf.FloorToInt(bossStartSeconds % 60f):00}. Defeat it before 30:00 to open Level {stage + 1}.";
        public static string GuardianFailed(int stage) => $"The guardian of Level {stage} still stood at 30:00. Level {stage + 1} stays closed.";

        private void CompleteStageAfterSurvival()
        {
            // The owner's story (Docs/Game_Design_Document.md, Level 10 and the twist): "To free his daughter's soul,
            // he must destroy himself." Owner, 2026-09-27: the final stage is not survived; it ends only when Shadow
            // Kael is destroyed (ReportBossDefeated). The Soul Core has no time limit.
            if (CurrentStage >= MaxStages)
            {
                Debug.Log("[LevelProgressionManager] The Soul Core has no time limit: Kael must destroy his shadow.");
                return;
            }
            if (!_bossDefeated)
            {
                StageFailed = true;
                Debug.Log($"[LevelProgressionManager] Stage {CurrentStage}: its guardian still stands at 30:00. The run ends.");
                OnStageFailed?.Invoke(CurrentStage);
                var session = GameSessionManager.Instance;
                if (session != null) session.GameOver();
                return;
            }
            CompleteStage("its guardian was defeated and the 30 minutes were survived");
        }

        private void CompleteStage(string reason)
        {
            if (_stageTransitionQueued || _gameWon) return;
            _stageTransitionQueued = true;
            Debug.Log($"[LevelProgressionManager] Stage {CurrentStage} cleared because {reason}.");
            GrantStageClearGold();
            WipeArena();
            OnStageCleared?.Invoke(CurrentStage);

            int next = StageAfter(CurrentStage);
            if (next <= 0) { GameWon(); return; }
            if (next <= CurrentStage)
            {
                // VS Endless: the run goes on as a new cycle, with a stronger horde (RunModes multipliers).
                RunModes.EndlessCycle++;
                Debug.Log($"[LevelProgressionManager] Endless: cycle {RunModes.EndlessCycle} starts again at Level {next}.");
            }
            _nextStage = next;
            StartCoroutine(NextStageRoutine());
        }

        private int _nextStage;

        /// <summary>The stage the one after this one is cleared, or 0 when the run is won.</summary>
        public int NextStage => _stageTransitionQueued ? _nextStage : 0;

        /// <summary>
        /// The stage that follows <paramref name="stage"/>: the next one, or after Level 10 either 0 (the run is won) or,
        /// in VS Endless mode, Level 1 again.
        /// </summary>
        public static int StageAfter(int stage) => stage < MaxStages ? stage + 1 : RunModes.Endless ? 1 : 0;

        /// <summary>
        /// VS wiki (Stages, The_Reaper, Revival): surviving a stage to its end pays 500 gold. Each unused revival adds a
        /// stacking bonus: +100, +200, +300, +400, then +100 for every one after that ("3 revives is 500 + 100 + 200 +
        /// 300 = 1,100"; "caps at 400 and any additional unused Revivals will only give 100"). Owner chose the stacking
        /// pages over the Stages page's flat "100 for each" (2026-09-30). Paid through GoldRewards, so Greed and the
        /// stage's gold bonus apply.
        /// </summary>
        public static int StageClearGold(int unusedRevivals)
        {
            int gold = 500;
            for (int i = 1; i <= unusedRevivals; i++) gold += i <= 4 ? 100 * i : 100;
            return gold;
        }

        /// <summary>The last stage-clear payment (gold actually given), for the HUD and tests.</summary>
        public int LastStageClearGold { get; private set; }

        private void GrantStageClearGold()
        {
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            var stats = player != null ? player.Stats : null;
            LastStageClearGold = SoulHunter.Gameplay.Pickups.GoldRewards.Grant(StageClearGold(stats != null ? stats.Revivals : 0), stats);
            Debug.Log($"[LevelProgressionManager] Stage {CurrentStage} clear reward: {LastStageClearGold} gold.");
        }

        /// <summary>
        /// The stage clear empties the arena. vampire.survivors.wiki/w/Mad_Forest (raw, 2026-10-19), 30:00 row: "All enemies
        /// on the screen are cleared." The wiki does not say how; ordinary enemies die as before (kill, gem). An enemy that
        /// resists instant kills (item guardians, Death's Shadow, res-kill wave bosses; VS Enemies "Resistances") is only
        /// taken off the field: it goes back to its pool without OnDied, so it leaves no Soul Egg, chest, gem or kill
        /// (Bug MIS AI-04: a kited 30,000-health guardian dropped its Soul Egg at 30:00; balance method).
        /// </summary>
        private void WipeArena()
        {
            var enemies = SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;
                if (enemy.ResistsInstantKill)
                {
                    enemy.gameObject.SetActive(false); // OnDisable takes it off ActiveEnemies; the loop runs backwards
                    continue;
                }
                var health = enemy.GetComponent<HealthController>();
                // Only kill them if they are alive (don't double kill)
                if (health != null && health.CurrentHealth > 0)
                {
                    // Instant cleanup without showing a fake 99999 damage popup.
                    health.KillSilently();
                }
            }
        }

        private System.Collections.IEnumerator NextStageRoutine()
        {
            yield return new WaitForSeconds(5f); 
            StartStage(_nextStage);
        }

        private void GameWon()
        {
            if (_gameWon) return;
            _gameWon = true;
            GameSessionManager.Instance?.CompleteCampaign();
            Debug.Log("[LevelProgressionManager] ALL 10 STAGES CLEARED! YOU BEAT THE GAME!");
            OnGameWon?.Invoke();
            SoulHunter.Core.Services.GameTime.Pause(this); // Game finish
        }
    }
}
