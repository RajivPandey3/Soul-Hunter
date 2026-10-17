using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using SoulHunter.Core.Events;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Scenes;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment: AutoplayBot plays Soul Hunter on its own so a developer (or an AI agent without hands) can
    /// watch a real run: it walks away from the horde, collects gems, chests and (when hurt) food, avoids obstacles,
    /// picks level-up upgrades, closes chests and the merchant, and records what happened:
    ///   <c>&lt;out&gt;/shots/*.png</c>  screenshots every few seconds and at events (level-up, chest, stage, death),
    ///   <c>&lt;out&gt;/timeline.jsonl</c>  one line every 5 s (time, stage, level, HP, kills, gold, enemies, FPS, weapons),
    ///   <c>&lt;out&gt;/events.jsonl</c>  level-up choices, chests, stage changes, damage, errors,
    ///   <c>&lt;out&gt;/summary.json</c>  the result (survived time, cause, level, kills, FPS, error count).
    /// Only runs when the game is launched with <c>-sh-autoplay &lt;out folder&gt;</c> (never in a normal launch) and
    /// then plays on a throw-away save (SaveService), so the player's progress is never touched. Options:
    ///   -sh-autoplay-minutes N   (default 5)   run length before it writes the summary and quits
    ///   -sh-autoplay-character Name            hero to play (default Kael)
    ///   -sh-autoplay-stage N                   start at stage N
    ///   -sh-autoplay-shots S     (default 10)  seconds between timed screenshots
    ///   -sh-autoplay-no-shots    no screenshots at all (frame-time measurements)
    ///   -sh-autoplay-god                       invincible, for long visual tours
    ///   -sh-autoplay-gold N                    start with N gold (on the throw-away save), e.g. to try the merchant
    ///   -sh-autoplay-boss-after S              after S seconds of play, skip the stage clock to its boss (e.g. Shadow Kael)
    ///   -sh-autoplay-skip-to T                 move the stage clock to T seconds (e.g. 290, just before the first chest elite)
    ///   -sh-autoplay-skip-after S (default 10) seconds of play before -sh-autoplay-skip-to applies
    ///   -sh-autoplay-jump-to N                 jump the run to stage N, as the Debug "Jump To Stage" menu does
    ///   -sh-autoplay-jump-after S (default 20) seconds of play before -sh-autoplay-jump-to applies
    ///   -sh-autoplay-start-level N             start at player level N (the bot picks every level-up), to measure later stages
    ///   -sh-autoplay-no-blood                  play with Disable Blood on (A/B checks of the blood spray's cost)
    ///   -sh-autoplay-heal-powerups             play with the VS Max Health and Recovery PowerUps bought (heal balance)
    ///   -sh-autoplay-revival                   play with the VS Revival PowerUp bought (one Revive, to see the Revive screen)
    ///   -sh-autoplay-inverse                   play in Inverse mode (e.g. the merchant's Inverse wares on stage 3)
    ///   -sh-autoplay-arcana                    play in Arcana mode; every Arcana choice screen (start pick, Arcana chest,
    ///                                          Draw a Fate) is screenshot ("arcana-choice") before the shown card is taken
    ///   -sh-autoplay-discover "Evo_A,Evo_B"    mark those evolutions discovered on the throw-away save (level-up cards then
    ///                                          show the evolution hint; add -sh-autoplay-levelup-shots to capture it)
    ///   -sh-autoplay-evolve "Evo_A,Evo_B"      start the run with those evolutions already made
    /// Screenshot options (owner review of the UI; all on the throw-away save):
    ///   -sh-autoplay-shop-shot                 before the run: the PowerUp shop after buying a few ranks
    ///   -sh-autoplay-powerup-off-shot          in the shop flow: one PowerUp maxed and switched off, its cell selected
    ///   -sh-autoplay-select-shot               before the run: the main menu and the character select screen
    ///   -sh-autoplay-sort-shot                 in the select flow: the character select sorted A-Z, descending
    ///   -sh-autoplay-menu-sections-shot        before the run: the main menu, then Collection, Bestiary, Unlocks and Credits
    ///                                          (a few entries found and kills recorded, Bestiary unlocked, menu reloaded)
    ///   -sh-autoplay-style-sample              before the run: the UI style sample screen, also at 2x
    ///   -sh-autoplay-early-shots               every 0.2 s for the first 3 s of the run
    ///   -sh-autoplay-touch-sizes               with every screenshot, a "touch-sizes" event: each shown control's size on
    ///                                          screen in pixels and dp (Android 48 dp touch target check, T-01)
    ///   -sh-autoplay-pause-shot                20 s in: the pause menu with its stats panel
    ///   -sh-autoplay-options-shot              with -pause-shot: every Options section over the pause menu
    ///   -sh-autoplay-evolutions-shot           ~22 s in: pause, press Evolutions, screenshot (and the next page), resume
    ///   -sh-autoplay-levelup-shots             every level-up screen (otherwise every fifth level)
    ///   -sh-autoplay-unlock-shot               every unlock notice while it is shown
    ///   -sh-autoplay-results-shot              at the time limit: quit through the pause menu and screenshot the Results
    /// Tools/Autoplay/run-autoplay.ps1 builds and runs it.
    /// </summary>
    public sealed class AutoplayBot : MonoBehaviour
    {
        public const string Argument = "-sh-autoplay";

        public sealed class Settings
        {
            public string OutputFolder;
            public float Minutes = 5f;
            public string Character = "Kael";
            public int Stage = 1;
            public float ShotInterval = 10f;
            public bool God;
            /// <summary>Gold to start with; -1 keeps the save's gold.</summary>
            public int Gold = -1;
            /// <summary>Seconds of play before the stage clock skips to the boss; -1 never.</summary>
            public float BossAfterSeconds = -1f;
            /// <summary>Stage clock time to skip to (chest elites come every 5 minutes); -1 never.</summary>
            public float SkipToStageSeconds = -1f;
            public float SkipAfterSeconds = 10f;
            /// <summary>Stage to jump to mid-run (LevelProgressionManager.StartStage, like the Debug menu); 0 never.</summary>
            public int JumpToStage;
            public float JumpAfterSeconds = 20f;
            /// <summary>Player level to start at (1 = normal); the level-ups are chosen by the bot as usual.</summary>
            public int StartLevel = 1;
            /// <summary>Plays with the Disable Blood option on (A/B checks of the blood spray's cost).</summary>
            public bool NoBlood;
            /// <summary>Plays with the VS Max Health (rank 3) and Recovery (rank 5) PowerUps bought (heal balance checks).</summary>
            public bool HealPowerUps;
            /// <summary>Plays with the VS Revival PowerUp bought (one Revive).</summary>
            public bool Revival;
            /// <summary>Plays in Inverse mode.</summary>
            public bool Inverse;
            public bool QuitWhenDone = true;
        }

        public Settings Config { get; private set; }
        public bool Finished { get; private set; }
        public string Result { get; private set; } = "running";

        private readonly StringBuilder _events = new StringBuilder();
        private readonly StringBuilder _timeline = new StringBuilder();
        private readonly List<string> _shots = new List<string>();
        private readonly List<float> _frameTimes = new List<float>();
        private int _errors;
        private float _startedAt;
        private float _nextShot, _nextTimeline, _nextSteer;
        private int _minFps = int.MaxValue;
        private int _maxEnemies;
        private Vector3 _wander = Vector3.forward;
        private bool _merchantSeen;
        private Vector2 _lastInput;
        private int _lastHealth = -1;
        private EventBus _bus;
        private PlayerController _player;
        private HealthController _health;
        private PlayerExperience _experience;

        // ------------------------------------------------------------------ start-up

        // The command line, read once: GetCommandLineArgs builds a new array on every call, and the bot asked about ten
        // times a frame (about 1 ms a frame of the bot's own cost in the 2026-10-14 profile).
        private static string[] _args;
        private static string[] Args => _args ?? (_args = System.Environment.GetCommandLineArgs());

        public static string ArgValue(string name)
        {
            var args = Args;
            int i = System.Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        public static bool HasArg(string name) => System.Array.IndexOf(Args, name) >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            string output = ArgValue(Argument);
            var inv = CultureInfo.InvariantCulture;
#if UNITY_EDITOR
            // Editor menu (Soul Hunter/Autoplay): the request survives the Play Mode domain reload in SessionState.
            string request = UnityEditor.SessionState.GetString("SH_Autoplay_Request", "");
            if (string.IsNullOrEmpty(output) && !string.IsNullOrEmpty(request))
            {
                UnityEditor.SessionState.EraseString("SH_Autoplay_Request");
                var parts = request.Split('|');
                Launch(new Settings
                {
                    OutputFolder = parts[0],
                    Minutes = parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, inv, out var minutes) ? minutes : 3f,
                    Character = parts.Length > 2 ? parts[2] : "Kael",
                });
                return;
            }
#endif
            if (string.IsNullOrEmpty(output)) return;
            var settings = new Settings
            {
                OutputFolder = output,
                Minutes = float.TryParse(ArgValue("-sh-autoplay-minutes"), NumberStyles.Float, inv, out var m) ? m : 5f,
                Character = ArgValue("-sh-autoplay-character") ?? "Kael",
                Stage = int.TryParse(ArgValue("-sh-autoplay-stage"), out var s) ? Mathf.Clamp(s, 1, 10) : 1,
                ShotInterval = float.TryParse(ArgValue("-sh-autoplay-shots"), NumberStyles.Float, inv, out var shots) ? shots : 10f,
                God = HasArg("-sh-autoplay-god"),
                Gold = int.TryParse(ArgValue("-sh-autoplay-gold"), out var gold) ? Mathf.Max(0, gold) : -1,
                BossAfterSeconds = float.TryParse(ArgValue("-sh-autoplay-boss-after"), NumberStyles.Float, inv, out var boss) ? Mathf.Max(0f, boss) : -1f,
                SkipToStageSeconds = float.TryParse(ArgValue("-sh-autoplay-skip-to"), NumberStyles.Float, inv, out var skipTo) ? Mathf.Max(0f, skipTo) : -1f,
                SkipAfterSeconds = float.TryParse(ArgValue("-sh-autoplay-skip-after"), NumberStyles.Float, inv, out var skipAfter) ? Mathf.Max(0f, skipAfter) : 10f,
                JumpToStage = int.TryParse(ArgValue("-sh-autoplay-jump-to"), out var jumpTo) ? Mathf.Clamp(jumpTo, 1, 10) : 0,
                JumpAfterSeconds = float.TryParse(ArgValue("-sh-autoplay-jump-after"), NumberStyles.Float, inv, out var jumpAfter) ? Mathf.Max(0f, jumpAfter) : 20f,
                StartLevel = int.TryParse(ArgValue("-sh-autoplay-start-level"), out var startLevel) ? Mathf.Clamp(startLevel, 1, 400) : 1,
                NoBlood = HasArg("-sh-autoplay-no-blood"),
                HealPowerUps = HasArg("-sh-autoplay-heal-powerups"),
                Revival = HasArg("-sh-autoplay-revival"),
                Inverse = HasArg("-sh-autoplay-inverse"),
            };
            Launch(settings);
        }

        /// <summary>Starts a bot that boots into a run (also used by PlayMode tests with QuitWhenDone = false).</summary>
        public static AutoplayBot Launch(Settings settings)
        {
            var go = new GameObject("AutoplayBot");
            DontDestroyOnLoad(go);
            var bot = go.AddComponent<AutoplayBot>();
            bot.Config = settings;
            return bot;
        }

        private readonly List<System.Action> _unsubscribe = new List<System.Action>();

        private void OnDestroy()
        {
            foreach (var undo in _unsubscribe) undo();
            _unsubscribe.Clear();
            // A player build stores its quality level between launches; a measuring run must not leave its own behind.
            if (_qualityBefore >= 0) QualitySettings.SetQualityLevel(_qualityBefore, true);
            RestoreRenderScale();
            _drawCalls.Dispose();
            _setPass.Dispose();
            _batchesCount.Dispose();
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            _errors++;
            if (_errors <= 50) Event("error", $"\"message\": \"{Escape(condition)}\"");
        }

        private IEnumerator Start()
        {
            // -sh-autoplay-orientation portrait|landscape: a phone run in that orientation, without touching the phone's
            // own rotation setting (owner, 2026-10-10: the same screens in landscape and portrait).
            string orientation = ArgValue("-sh-autoplay-orientation");
            if (orientation == "portrait") Screen.orientation = ScreenOrientation.Portrait;
            else if (orientation == "landscape") Screen.orientation = ScreenOrientation.LandscapeLeft;
            Directory.CreateDirectory(Path.Combine(Config.OutputFolder, "shots"));
            // A run froze for 8 minutes after a chest (2026-10-16); the watchdog writes where the main thread is stuck.
            SoulHunter.Core.Services.SlowSpan.StartWatchdog(Path.Combine(Config.OutputFolder, "stall.txt"));
            while (SceneManager.GetActiveScene().name != "MainMenu") yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            var save = GameServices.Instance.Get<SaveService>();
            // A -sh-autoplay launch already uses a throw-away save. Launched any other way (editor, tests) the bot
            // redirects saving to a temp file first: gold pickups save immediately and would change real progress.
            if (!HasArg(Argument)) save.UseFile(Path.Combine(Application.temporaryCachePath, "autoplay_save.json"));
            save.CurrentData.SelectedCharacterName = Config.Character;
            // A bot run must not stop when its window loses focus (a whole profile run sat paused, 2026-10-15); the benchmark
            // does the same. This is the throw-away save.
            save.CurrentData.PauseOnFocusLoss = false;
            // VS item unlocks (2026-10-12): bot runs measure the whole item pool, so the throw-away save has every item
            // unlocked; -sh-autoplay-locked-items plays a new save's locked pool instead.
            if (HasArg("-sh-autoplay-locked-items")) SoulHunter.Core.Persistence.ItemUnlocks.LockEverything(save.CurrentData);
            else SoulHunter.Core.Persistence.ItemUnlocks.UnlockEverything(save.CurrentData);
            if (Config.Gold >= 0) save.CurrentData.Currency = Config.Gold; // the save is a throw-away one by now
            save.CurrentData.DisableBlood = Config.NoBlood;
            // VS PowerUps on the throw-away save: none, or Max Health and Recovery at their VS max with -sh-autoplay-heal-powerups.
            save.CurrentData.PowerUpRanks.Clear();
            if (Config.HealPowerUps)
            {
                SoulHunter.Core.Persistence.PowerUps.SetRank(save.CurrentData, SoulHunter.Core.Persistence.PowerUpId.MaxHealth, 3);
                SoulHunter.Core.Persistence.PowerUps.SetRank(save.CurrentData, SoulHunter.Core.Persistence.PowerUpId.Recovery, 5);
            }
            if (Config.Revival) SoulHunter.Core.Persistence.PowerUps.SetRank(save.CurrentData, SoulHunter.Core.Persistence.PowerUpId.Revival, 1);
            if (Config.Inverse) save.CurrentData.ModeInverse = true;
            // -sh-autoplay-arcana: Arcana mode on (the start pick, Arcana chests and the choice screen; Housekeeping shoots it).
            if (HasArg("-sh-autoplay-arcana")) save.CurrentData.ModeArcana = true;
            // -sh-autoplay-discover "Evo_A,Evo_B": those evolutions count as found on this save (level-up evolution hints,
            // the Evolutions screen and the Collection show them).
            string discover = ArgValue("-sh-autoplay-discover");
            if (!string.IsNullOrEmpty(discover))
            {
                var marked = new List<string>();
                foreach (var id in discover.Split(','))
                    if (!string.IsNullOrWhiteSpace(id) && SoulHunter.Core.Persistence.EvolutionDiscovery.MarkDiscovered(save.CurrentData, id.Trim()))
                        marked.Add($"\"{Escape(id.Trim())}\"");
                Event("discover", $"\"evolutions\": [{string.Join(", ", marked)}]");
            }
            // -sh-autoplay-menu-sections-shot: the main menu, then Collection, Bestiary, Unlocks and Credits in turn.
            if (HasArg("-sh-autoplay-menu-sections-shot")) yield return MenuSectionShots(save.CurrentData);
            // -sh-autoplay-powerup-off-shot runs inside the shop flow.
            if (HasArg("-sh-autoplay-shop-shot") || HasArg("-sh-autoplay-powerup-off-shot")) yield return ShopShots(save.CurrentData);
            // -sh-autoplay-select-shot: screenshot the main menu, then the character select screen (what Start opens);
            // -sh-autoplay-sort-shot runs inside it.
            if (HasArg("-sh-autoplay-select-shot") || HasArg("-sh-autoplay-sort-shot")) yield return SelectShots();
            // -sh-autoplay-style-sample: the UI style sample screen (the look waiting for approval), captured at 2x.
            if (HasArg("-sh-autoplay-style-sample")) yield return StyleSampleShot();
            GameServices.Instance.Get<SceneService>().LoadSceneAsync("Main_Gameplay");
            while (SceneManager.GetActiveScene().name != "Main_Gameplay" || PlayerController.Instance == null ||
                   LevelProgressionManager.Instance == null) yield return null;
            // -sh-autoplay-early-shots: every 0.2 s for the first 3 s of the run (how the stage first appears).
            if (HasArg("-sh-autoplay-early-shots")) StartCoroutine(EarlyShots());
            // -sh-autoplay-pause-shot: 20 s in, pause, screenshot the pause menu (stats panel), resume.
            if (HasArg("-sh-autoplay-pause-shot")) StartCoroutine(PauseShot());
            // -sh-autoplay-evolutions-shot: ~22 s in, pause, press Evolutions, screenshot (and the next page), resume.
            if (HasArg("-sh-autoplay-evolutions-shot")) StartCoroutine(EvolutionsShot());
            // -sh-autoplay-unlock-shot: screenshot every unlock notice while it is on screen (not hidden behind a menu).
            if (HasArg("-sh-autoplay-unlock-shot")) StartCoroutine(UnlockNoticeShots());
            // -sh-autoplay-render-census "60,120": at those run seconds, list what the camera draws, grouped by renderer kind
            // and material and by source object (render-census.txt), to find where the draw calls come from (2026-10-15).
            string census = ArgValue("-sh-autoplay-render-census");
            if (!string.IsNullOrEmpty(census)) StartCoroutine(RenderCensus(census));
            yield return new WaitForSecondsRealtime(1f);

            _player = PlayerController.Instance;
            _health = _player.GetComponent<HealthController>();
            _experience = _player.GetComponent<PlayerExperience>();
            _bus = GameServices.Instance.Get<EventBus>();
            if (Config.God) _health.IsInvincible = true;
            if (Config.Stage > 1) LevelProgressionManager.Instance.StartStage(Config.Stage);
            // -sh-autoplay-evolve "Evo_A,Evo_B": start with those evolutions (their base weapon at 8 and passive, then
            // evolved), to watch new evolutions in play.
            string evolve = ArgValue("-sh-autoplay-evolve");
            if (!string.IsNullOrEmpty(evolve)) yield return GiveEvolutions(evolve.Split(','));

            // Every subscription is undone in OnDestroy: a level-up after the bot is gone (a gem collected while a test
            // tears down) used to start a coroutine on the destroyed bot (MissingReferenceException, found 2026-09-29).
            var levelUp = FindFirstObjectByType<LevelUpManager>();
            if (levelUp != null)
            {
                System.Action<List<UpgradeData>> choose = choices => StartCoroutine(ChooseUpgrade(levelUp, choices));
                levelUp.OnLevelUpUIRound += choose;
                _unsubscribe.Add(() => { if (levelUp != null) levelUp.OnLevelUpUIRound -= choose; });
            }
            var progression = LevelProgressionManager.Instance;
            System.Action<int> stageStarted = stage => { Event("stage", $"\"stage\": {stage}"); StartCoroutine(ShotSoon($"stage{stage:00}")); };
            progression.OnStageStarted += stageStarted;
            _unsubscribe.Add(() => { if (progression != null) progression.OnStageStarted -= stageStarted; });
            var session = GameSessionManager.Instance;
            if (session != null)
            {
                System.Action died = () => Finish("died"), won = () => Finish("victory");
                session.OnGameOver += died;
                session.OnVictory += won;
                _unsubscribe.Add(() => { if (session != null) { session.OnGameOver -= died; session.OnVictory -= won; } });
            }
            _health.OnDamaged += OnHurt;
            var health = _health;
            _unsubscribe.Add(() => { if (health != null) health.OnDamaged -= OnHurt; });
            // Lantern income (balance pass, 2026-09-29): every broken Soul Lantern and what it dropped.
            System.Action<SoulHunter.Gameplay.Pickups.LightSource, SoulHunter.Gameplay.Pickups.LightSourceDrops.Drop> lantern =
                (source, drop) => Event("lantern", $"\"drop\": \"{drop}\"");
            SoulHunter.Gameplay.Pickups.LightSource.AnyBroken += lantern;
            _unsubscribe.Add(() => SoulHunter.Gameplay.Pickups.LightSource.AnyBroken -= lantern);

            // A give list due in the first second goes in before the start-level XP below: those level-ups used to fill the
            // six weapon slots first, so the list gave nothing (the frame-time suite's "heavykill" ran without it).
            if (!string.IsNullOrEmpty(ArgValue("-sh-autoplay-give")) && GiveAfter <= 1f) GiveListedItems();
            // Start at a later player level: grant the XP, and the level-up screens that follow are answered by the bot.
            while (_experience != null && _experience.CurrentLevel < Config.StartLevel)
                _experience.AddXP(_experience.XPToNextLevel - _experience.CurrentXP);

            _startedAt = Time.realtimeSinceStartup;
            _nextShot = _startedAt + 2f;
            _nextTimeline = _startedAt;
            Event("start", $"\"character\": \"{Escape(Config.Character)}\", \"stage\": {Config.Stage}, \"god\": {(Config.God ? "true" : "false")}");
            StartCoroutine(Housekeeping());
        }

        // ------------------------------------------------------------------ playing

        private void Update()
        {
            if (_player == null || Finished) return;
            _frameTimes.Add(Time.unscaledDeltaTime);
            float now = Time.realtimeSinceStartup;
            WatchForHangs(now);
            WatchLevelUpFrames();
            if (now >= _nextSteer)
            {
                _nextSteer = now + 0.1f;
                Steer();
            }
            // Timed shots show play, not a level-up or chest screen (level-ups came every ~15 s and hid every shot).
            if (now >= _nextShot && !GameTime.IsPaused)
            {
                _nextShot = now + Config.ShotInterval;
                StartCoroutine(Shot($"t{Elapsed:0000}"));
            }
            if (now >= _nextTimeline)
            {
                _nextTimeline = now + 5f;
                WriteTimeline();
            }
            if (Elapsed >= Config.Minutes * 60f) Finish("time limit");
        }

        private float Elapsed => Time.realtimeSinceStartup - _startedAt;
        private bool _bossSkipped;
        private bool _clockSkipped;
        private bool _jumped;
        private bool _shadowKaelSeen;

        /// <summary>
        /// Steering: flee enemies (closer = much stronger), go for gems and chests, go for food when hurt, slide
        /// around obstacles, and circle slowly when nothing is near.
        /// </summary>
        private void Steer()
        {
            Vector3 me = _player.transform.position;
            Vector3 push = Vector3.zero;
            int near = 0;
            foreach (var enemy in EnemyController.ActiveEnemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                Vector3 away = me - enemy.transform.position;
                away.y = 0f;
                float d = away.magnitude;
                if (d > 4.5f || d < 0.01f) continue;
                near++;
                // Like a VS player: let the horde come into weapon range, only step away when it gets close.
                push += away / (d * d) * (d < 2.2f ? 8f : 2f);
            }
            _maxEnemies = Mathf.Max(_maxEnemies, EnemyController.ActiveEnemies.Count);

            Vector3 pull = Vector3.zero;
            // The whole-scene searches (gems, chests, merchant, lanterns, items, food) run four times a second and their
            // answers are kept in between: at ten times a second they were most of the bot's own frame cost.
            // 60 FPS pass (2026-10-15): one search a frame in turn, so the bot's own cost no longer lands in one frame and
            // shows up as a stutter in the frame-time runs.
            if (_lookStep > 0 || Time.realtimeSinceStartup >= _nextLook)
            {
                if (_lookStep == 0) _nextLook = Time.realtimeSinceStartup + 0.25f;
                switch (_lookStep)
                {
                    case 0: _gemGoal = Nearest(XPGem.ActiveGems.Where(g => g != null && g.isActiveAndEnabled).Select(g => g.transform), me, 12f); break;
                    case 1: _chestGoal = Nearest(FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Select(c => c.transform), me, 25f); break;
                    // Walk up to the Level 3 merchant while it is there, as a player would.
                    case 2: _merchantGoal = Nearest(FindObjectsByType<WanderingMerchant>(FindObjectsSortMode.None).Where(m => !m.HasLeft).Select(m => m.transform), me, 30f); break;
                    // Like a VS player: break Soul Lanterns in view and pick up what they drop (coins, bags, items). Since the
                    // 2026-09-29 VS drop rule these are where coins and chickens come from; the bot used to ignore them.
                    case 3: _lanternGoal = near < 6 ? Nearest(FindObjectsByType<LightSource>(FindObjectsSortMode.None).Where(l => l.isActiveAndEnabled).Select(l => l.transform), me, 18f) : null; break;
                    case 4: _itemGoal = near < 6 ? Nearest(FindObjectsByType<Pickup>(FindObjectsSortMode.None).Where(p => p.isActiveAndEnabled).Select(p => p.transform), me, 14f) : null; break;
                    default:
                        _foodGoal = _health.CurrentHealth < _health.MaxHealth * 0.5f
                            ? Nearest(FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Select(c => c.transform), me, 25f) : null;
                        break;
                }
                _lookStep = (_lookStep + 1) % 6;
            }
            if (Reachable(_gemGoal)) pull += Flat(_gemGoal.position - me).normalized * (near > 8 ? 0.8f : 1.6f);
            if (Reachable(_chestGoal)) pull += Flat(_chestGoal.position - me).normalized * 2f;
            if (Reachable(_merchantGoal)) pull += Flat(_merchantGoal.position - me).normalized * 3f;
            if (near < 6)
            {
                if (Reachable(_lanternGoal)) pull += Flat(_lanternGoal.position - me).normalized * 1.8f;
                if (Reachable(_itemGoal)) pull += Flat(_itemGoal.position - me).normalized * 1.8f;
            }
            if (Reachable(_foodGoal)) pull += Flat(_foodGoal.position - me).normalized * 2.5f;

            if (push.sqrMagnitude < 0.01f && pull.sqrMagnitude < 0.01f)
            {
                _wander = Quaternion.Euler(0f, 2f, 0f) * _wander; // a slow circle (~18 s a turn) keeps new tiles and spawns coming
                pull = _wander;
            }
            Vector3 move = Flat(push + pull);
            if (move.sqrMagnitude < 0.0001f) move = _wander;
            move.Normalize();

            // Obstacles: if one is right ahead, turn along it.
            int obstacleMask = 1 << LayerMask.NameToLayer("Obstacle");
            if (UnityEngine.Physics.SphereCast(me + Vector3.up * 0.5f, 0.4f, move, out var hit, 1.6f, obstacleMask))
            {
                var along = Vector3.Cross(Vector3.up, Flat(hit.normal)).normalized;
                if (Vector3.Dot(along, move) < 0f) along = -along;
                move = (along + Flat(hit.normal) * 0.3f).normalized;
            }

            var right = _player.GetCameraRelativeMovement(Vector2.right);
            var forward = _player.GetCameraRelativeMovement(Vector2.up);
            var input = new Vector2(Vector3.Dot(move, right), Vector3.Dot(move, forward)).normalized;
            if ((input - _lastInput).sqrMagnitude > 0.0025f)
            {
                _lastInput = input;
                _bus.Raise(new PlayerMoveEvent(input));
            }
        }

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        private float _nextLook;
        private Transform _gemGoal, _chestGoal, _merchantGoal, _lanternGoal, _itemGoal, _foodGoal;
        /// <summary>A goal kept from the last look is still there (not collected, broken or gone).</summary>
        private static bool Reachable(Transform goal) => goal != null && goal.gameObject.activeInHierarchy;

        private static Transform Nearest(IEnumerable<Transform> items, Vector3 from, float range)
        {
            Transform best = null;
            float bestSqr = range * range;
            foreach (var t in items)
            {
                float sqr = Flat(t.position - from).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = t; }
            }
            return best;
        }

        /// <summary>Upgrades owned weapons first (towards evolutions), then new weapons, then passives.</summary>
        private IEnumerator ChooseUpgrade(LevelUpManager levelUp, List<UpgradeData> choices)
        {
            yield return null; // let the level-up screen open (and be captured)
            if (!levelUp.IsLevelUpActive || choices == null || choices.Count == 0) yield break;
            var weapons = WeaponManager.Instance;
            UpgradeData pick = choices.OrderByDescending(c => Score(weapons, c)).First();
            // -sh-autoplay-levelup-hold S: keep each level-up screen open S seconds, as a player reading the cards would.
            if (float.TryParse(ArgValue("-sh-autoplay-levelup-hold"), NumberStyles.Float, CultureInfo.InvariantCulture, out var hold) && hold > 0f)
                yield return new WaitForSecondsRealtime(hold);
            // -sh-autoplay-levelup-ui: look at the screen as a player does, once its layout has settled.
            if (HasArg("-sh-autoplay-levelup-ui")) yield return new WaitForSecondsRealtime(0.5f);
            // -sh-autoplay-levelup-shots: every level-up screen (the level-up stats panel); otherwise every fifth.
            if (_experience.CurrentLevel % 5 == 0 || HasArg("-sh-autoplay-levelup-shots")) yield return Shot($"levelup{_experience.CurrentLevel:00}");
            if (HasArg("-sh-autoplay-levelup-ui")) Event("levelup-ui", LevelUpUiState());
            // -sh-autoplay-ui-graphics: every visible UI graphic during the first level-up (what dims or covers the HUD).
            if (HasArg("-sh-autoplay-ui-graphics") && !_graphicsDumped) { _graphicsDumped = true; Event("ui-graphics", UiGraphicsDump()); }
            Event("levelup", $"\"level\": {_experience.CurrentLevel}, \"choices\": [{string.Join(", ", choices.Select(c => $"\"{Escape(c.UpgradeName)}\""))}], \"picked\": \"{Escape(pick.UpgradeName)}\"");
            // Pick through the level-up screen like a player, so the panel closes (calling SelectUpgrade directly left it
            // on screen after the last level-up).
            var ui = FindFirstObjectByType<LevelUpUI>();
            var select = typeof(LevelUpUI).GetMethod("OnUpgradeSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ui != null && select != null) select.Invoke(ui, new object[] { pick });
            else levelUp.SelectUpgrade(pick);
        }

        private bool _graphicsDumped;

        private static string UiGraphicsDump()
        {
            var sb = new System.Text.StringBuilder("\"graphics\": [");
            bool first = true;
            foreach (var g in FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
            {
                if (g == null || !g.isActiveAndEnabled || g.color.a < 0.05f || g.canvas == null) continue;
                var r = g.rectTransform;
                var corners = new Vector3[4];
                r.GetWorldCorners(corners);
                Camera cam = g.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : g.canvas.worldCamera;
                Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]), b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                if (b.x - a.x < 200f && b.y - a.y < 200f) continue; // only big graphics can dim or cover
                if (!first) sb.Append(", ");
                first = false;
                string sprite = g is UnityEngine.UI.Image img && img.sprite != null ? img.sprite.name : "";
                sb.Append($"{{\"name\": \"{Escape(g.name)}\", \"parent\": \"{Escape(g.transform.parent != null ? g.transform.parent.name : "")}\", \"type\": \"{g.GetType().Name}\", \"canvas\": \"{Escape(g.canvas.rootCanvas.name)}\", \"sort\": {g.canvas.rootCanvas.sortingOrder}, \"color\": \"{g.color}\", \"sprite\": \"{Escape(sprite)}\", \"rect\": \"{a.x:0},{a.y:0}-{b.x:0},{b.y:0}\"}}");
            }
            return sb.Append("]").ToString();
        }

        /// <summary>Where the level-up panel and its cards are on screen, and anything hiding them.</summary>
        private static string LevelUpUiState()
        {
            var ui = FindFirstObjectByType<LevelUpUI>(FindObjectsInactive.Include);
            if (ui == null) return "\"ui\": null";
            var panel = typeof(LevelUpUI).GetField("_levelUpPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(ui) as GameObject;
            var sb = new StringBuilder();
            sb.Append($"\"ui\": \"{Escape(PathOf(ui.transform))}\", \"panel\": \"{(panel != null ? Escape(PathOf(panel.transform)) : "null")}\"");
            if (panel == null) return sb.ToString();
            sb.Append($", \"panelActive\": {(panel.activeInHierarchy ? "true" : "false")}, \"panelRect\": \"{ScreenRectOf(panel.transform)}\"");
            var canvas = panel.GetComponentInParent<Canvas>(true);
            if (canvas != null) sb.Append($", \"canvas\": \"{Escape(canvas.name)}\", \"canvasEnabled\": {(canvas.isActiveAndEnabled ? "true" : "false")}, \"sort\": {canvas.sortingOrder}, \"mode\": \"{canvas.renderMode}\"");
            foreach (var group in panel.GetComponentsInParent<CanvasGroup>(true))
                sb.Append($", \"group_{Escape(group.name)}\": {group.alpha.ToString(CultureInfo.InvariantCulture)}");
            var buttons = panel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            sb.Append(", \"buttons\": [");
            sb.Append(string.Join(", ", buttons.Select(b => $"\"{Escape(b.name)} {(b.gameObject.activeInHierarchy ? "on" : "off")} {ScreenRectOf(b.transform)}\"")));
            sb.Append($"], \"screen\": \"{Screen.width}x{Screen.height}\"");
            var stats = panel.GetComponentInChildren<StatsPanel>(true);
            if (stats != null)
            {
                var sr = (RectTransform)stats.transform;
                var pr = (RectTransform)sr.parent;
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    ", \"stats\": \"{0}\", \"statsFont\": {1:0.0}, \"statsRows\": {2}, \"parentScale\": \"{3:0.000},{4:0.000}\", \"parentRect\": \"{5:0}x{6:0}\", \"statsAnchors\": \"{7:0.000}-{8:0.000}\"",
                    ScreenRectOf(sr), stats.FontSize, stats.Rows != null ? stats.Rows.Length : 0, pr.lossyScale.x, pr.lossyScale.y,
                    pr.rect.width, pr.rect.height, sr.anchorMin.y, sr.anchorMax.y));
            }
            return sb.ToString();
        }

        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

        private static string ScreenRectOf(Transform t)
        {
            if (!(t is RectTransform rect)) return "-";
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]), b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            return string.Format(CultureInfo.InvariantCulture, "({0:0},{1:0})-({2:0},{3:0})", a.x, a.y, b.x, b.y);
        }

        private IEnumerator GiveEvolutions(string[] names)
        {
            var weapons = WeaponManager.Instance;
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            if (weapons == null || catalog == null || catalog.Evolutions == null) yield break;
            foreach (var name in names)
            {
                var evolution = System.Array.Find(catalog.Evolutions, e => e != null && e.EvolvedName == name.Trim());
                if (evolution == null) { Event("evolve-missing", $"\"name\": \"{Escape(name)}\""); continue; }
                // Each recipe's own levels (Sundered Hour: Clock Lancet 7 and both rings at 9).
                weapons.GiveWeapon(evolution.BaseWeapon, Mathf.Max(1, evolution.BaseWeaponMaxLevel));
                weapons.GiveWeapon(evolution.RequiredPassive, Mathf.Max(1, evolution.RequiredPassiveMaxLevel));
                if (evolution.AdditionalPassives != null)
                    foreach (var extra in evolution.AdditionalPassives) weapons.GiveWeapon(extra.Type, Mathf.Max(1, extra.MinimumLevel));
                yield return null;
                bool ok = weapons.TryEvolveWeapon(out string evolved);
                Event("evolve", $"\"name\": \"{Escape(name)}\", \"ok\": {(ok ? "true" : "false")}, \"got\": \"{Escape(evolved)}\"");
                yield return null;
            }
        }

        private static int Score(WeaponManager weapons, UpgradeData upgrade)
        {
            if (weapons == null) return 0;
            int level = weapons.GetWeaponLevel(upgrade.Type);
            bool weapon = weapons.GetWeaponObject(upgrade.Type) != null;
            if (weapon && level > 0) return 30 + Random.Range(0, 5);
            if (weapon) return 20 + Random.Range(0, 5);
            if (level > 0) return 10 + Random.Range(0, 5);
            return Random.Range(0, 5);
        }

        /// <summary>Chests and the merchant pause the game until closed; a player would close them.</summary>
        private IEnumerator Housekeeping()
        {
            while (!Finished)
            {
                yield return new WaitForSecondsRealtime(0.4f);
                if (Config.SkipToStageSeconds >= 0f && !_clockSkipped && Elapsed >= Config.SkipAfterSeconds)
                {
                    _clockSkipped = true;
                    LevelProgressionManager.Instance.SkipTo(Config.SkipToStageSeconds);
                    Event("clock-skip", string.Format(CultureInfo.InvariantCulture, "\"stage\": {0}, \"stageTime\": {1:0}", LevelProgressionManager.Instance.CurrentStage, Config.SkipToStageSeconds));
                }
                if (Config.JumpToStage > 0 && !_jumped && Elapsed >= Config.JumpAfterSeconds)
                {
                    _jumped = true;
                    LevelProgressionManager.Instance.StartStage(Config.JumpToStage); // what Soul Hunter/Debug/Jump To Stage does
                    Event("stage-jump", $"\"stage\": {Config.JumpToStage}");
                }
                WatchStrongEnemies();
                var progressionNow = LevelProgressionManager.Instance;
                if (Config.BossAfterSeconds >= 0f && !_bossSkipped && Elapsed >= Config.BossAfterSeconds)
                {
                    _bossSkipped = true;
                    LevelProgressionManager.Instance.SkipToBoss();
                    Event("boss-skip", $"\"stage\": {LevelProgressionManager.Instance.CurrentStage}");
                }
                // Scene searches only where they can find something: a search of the whole scene every 0.4 s made a bot-only
                // stutter that the frame-time runs counted as the game's (60 FPS pass, 2026-10-15).
                var kael = !_shadowKaelSeen && progressionNow != null && progressionNow.CurrentStage == 10
                    ? FindFirstObjectByType<SoulHunter.Gameplay.AI.ShadowKaelController>() : null;
                if (kael != null && !_shadowKaelSeen)
                {
                    _shadowKaelSeen = true;
                    var mirrored = kael.GetComponentsInChildren<SoulHunter.Gameplay.Combat.AutoAttackWeapon>(true)
                        .Select(w => $"\"{w.gameObject.name.Replace("(Clone)", "")}:{w.CurrentLevel}\"");
                    Event("shadow-kael", $"\"might\": {kael.MirroredMight.ToString("0.00", CultureInfo.InvariantCulture)}, \"revivals\": {kael.MirroredRevivals}, \"weapons\": [{string.Join(", ", mirrored)}]");
                    // A level-up or chest screen can open as the boss arrives: shoot the fight, not the menu.
                    for (int i = 0; i < 3; i++)
                    {
                        while (GameTime.IsPaused && !Finished) yield return new WaitForSecondsRealtime(0.2f);
                        yield return new WaitForSecondsRealtime(i == 0 ? 0.3f : 1.5f);
                        if (kael == null || !kael.isActiveAndEnabled) break;
                        yield return Shot($"shadow-kael-{i + 1}");
                    }
                }
                var revive = ReviveScreen.Current;
                if (revive != null && revive.IsOpen)
                {
                    // VS Revival: a death with a Revival left shows the Revive button; a player presses it.
                    yield return Shot("revive");
                    var reviveStats = PlayerController.Instance != null ? PlayerController.Instance.Stats : null;
                    int left = reviveStats != null ? reviveStats.Revivals : 0;
                    revive.PressRevive();
                    Event("revive", string.Format(CultureInfo.InvariantCulture, "\"revivalsBefore\": {0}, \"revivalsLeft\": {1}, \"hp\": {2}, \"max\": {3}",
                        left, reviveStats != null ? reviveStats.Revivals : 0, _health != null ? _health.CurrentHealth : 0, _health != null ? _health.MaxHealth : 0));
                }
                var arcanaChoice = ArcanaChoiceScreen.Current;
                if (arcanaChoice != null && arcanaChoice.IsOpen)
                {
                    // VS Arcana choice (P2-01): a player takes the card shown first. Screenshot it first, once it has settled.
                    yield return new WaitForSecondsRealtime(0.4f);
                    if (!arcanaChoice.IsOpen) continue;
                    yield return Shot("arcana-choice");
                    Event("arcana-choice", string.Format(CultureInfo.InvariantCulture, "\"title\": \"{0}\", \"cards\": {1}, \"taken\": \"{2}\"",
                        Escape(arcanaChoice.TitleText != null ? arcanaChoice.TitleText.text : ""), arcanaChoice.Cards.Count,
                        arcanaChoice.Selected != null ? Escape(arcanaChoice.Selected.CardName) : ""));
                    arcanaChoice.PressTake();
                }
                // A chest screen and the merchant's shop pause the game; the merchant stands only on Level 3.
                bool pausedNow = GameTime.IsPaused;
                bool merchantStage = progressionNow != null && progressionNow.CurrentStage == 3;
                foreach (var chest in pausedNow ? FindObjectsByType<ChestUI>(FindObjectsSortMode.None) : System.Array.Empty<ChestUI>())
                    if (chest != null && chest.IsOpen)
                    {
                        // VS chest screen: wait for OPEN, press it, shoot the reveal, press DONE (like a player).
                        var screen = chest.Screen;
                        float until = Time.unscaledTime + 5f;
                        while (screen.Current == ChestOpeningScreen.Phase.Intro && Time.unscaledTime < until) yield return null;
                        yield return Shot("chest");
                        screen.PressOpen();
                        until = Time.unscaledTime + 10f;
                        while (screen.Current != ChestOpeningScreen.Phase.Done && screen.IsOpen && Time.unscaledTime < until) yield return null;
                        yield return Shot("chest-open");
                        Event("chest", string.Format(CultureInfo.InvariantCulture, "\"items\": {0}, \"gold\": {1}, \"canSkip\": {2}",
                            screen.ItemCount, screen.ShownGold, screen.CanSkip ? "true" : "false"));
                        yield return chest.OpenAndClose();
                    }
                foreach (var merchant in pausedNow || (merchantStage && !_merchantSeen)
                             ? FindObjectsByType<WanderingMerchant>(FindObjectsSortMode.None) : System.Array.Empty<WanderingMerchant>())
                {
                    if (!_merchantSeen)
                    {
                        _merchantSeen = true;
                        yield return Shot("merchant-seen"); // the merchant in the world, before the shop opens
                    }
                    if (merchant.IsShopOpen)
                    {
                        yield return Shot("merchant");
                        int bought = 0;
                        foreach (var ware in merchant.WeaponWares)
                            if (merchant.BuyWeapon(ware)) bought++;
                        if (merchant.BuySoulEgg()) bought++;
                        Event("merchant", $"\"bought\": {bought}");
                        yield return Shot("merchant-bought");
                        merchant.Leave();
                    }
                }
            }
        }

        private void OnHurt()
        {
            if (_health == null) return;
            int hp = _health.CurrentHealth;
            if (_lastHealth >= 0 && hp < _lastHealth) Event("hurt", $"\"hp\": {hp}, \"max\": {_health.MaxHealth}");
            _lastHealth = hp;
        }

        // ------------------------------------------------------------------ elites and bosses

        private readonly Dictionary<HealthController, (string kind, float spawnedAt, int maxHp, System.Action onDied)> _strong =
            new Dictionary<HealthController, (string, float, int, System.Action)>();
        private EnemySpawner _spawner;

        /// <summary>
        /// Records every chest elite and stage boss: when it appears (health, size, distance), when it dies and how
        /// long it lived, with screenshots of its arrival and of the chest it leaves.
        /// </summary>
        private void WatchStrongEnemies()
        {
            if (_spawner == null) _spawner = FindFirstObjectByType<EnemySpawner>();
            var progression = LevelProgressionManager.Instance;
            // One that left without dying (a stage change clears the arena) is forgotten, so its pooled body can be seen again.
            foreach (var gone in _strong.Keys.Where(h => h == null || !h.gameObject.activeInHierarchy).ToList())
            {
                if (gone != null) gone.OnDied -= _strong[gone].onDied;
                Event(_strong[gone].kind + "-despawned", string.Format(CultureInfo.InvariantCulture, "\"aliveSeconds\": {0:0.0}", Elapsed - _strong[gone].spawnedAt));
                _strong.Remove(gone);
            }
            foreach (var enemy in EnemyController.ActiveEnemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                bool boss = _spawner != null && _spawner.IsBoss(enemy);
                var drop = enemy.GetComponent<EnemyDrop>();
                if (!boss && (drop == null || !drop.WillDropChest)) continue;
                var health = enemy.GetComponent<HealthController>();
                if (health == null || health.CurrentHealth <= 0 || _strong.ContainsKey(health)) continue;

                string kind = boss ? "boss" : "elite";
                float distance = _player != null ? Flat(enemy.transform.position - _player.transform.position).magnitude : -1f;
                Event(kind + "-spawn", string.Format(CultureInfo.InvariantCulture,
                    "\"stage\": {0}, \"stageTime\": {1:0}, \"name\": \"{2}\", \"hp\": {3}, \"scale\": {4:0.00}, \"distance\": {5:0.0}, \"playerLevel\": {6}, \"dropsChest\": {7}",
                    progression.CurrentStage, progression.StageElapsedTime, Escape(enemy.name.Replace("(Clone)", "")), health.MaxHealth,
                    enemy.transform.localScale.x, distance, _experience != null ? _experience.CurrentLevel : 0,
                    drop != null && drop.WillDropChest ? "true" : "false"));
                StartCoroutine(ShotSoon($"{kind}-spawn"));

                void OnDied()
                {
                    health.OnDied -= OnDied;
                    var seen = _strong[health];
                    _strong.Remove(health);
                    StartCoroutine(CheckSoulGem(seen.kind, health.transform.position, drop != null ? drop.GemValue : 0));
                    Event(seen.kind + "-killed", string.Format(CultureInfo.InvariantCulture,
                        "\"stage\": {0}, \"stageTime\": {1:0}, \"hp\": {2}, \"aliveSeconds\": {3:0.0}, \"playerLevel\": {4}",
                        progression.CurrentStage, progression.StageElapsedTime, seen.maxHp, Elapsed - seen.spawnedAt,
                        _experience != null ? _experience.CurrentLevel : 0));
                    StartCoroutine(ShotAfter($"{seen.kind}-killed", 0.3f));
                }
                _strong[health] = (kind, Elapsed, health.MaxHealth, OnDied);
                health.OnDied += OnDied;
            }
        }

        /// <summary>Owner's story: a boss or chest elite leaves its soul gem by its chest. Records whether one is there.</summary>
        private IEnumerator CheckSoulGem(string kind, Vector3 where, int value)
        {
            yield return null;
            int gems = XPGem.ActiveGems.Count(g => g != null && g.isActiveAndEnabled && g.XPValue == value &&
                                                  Flat(g.transform.position - where).magnitude < 3f);
            int chests = FindObjectsByType<ChestPickup>(FindObjectsSortMode.None)
                .Count(c => c.gameObject.activeInHierarchy && Flat(c.transform.position - where).magnitude < 3f);
            Event(kind + "-drops", string.Format(CultureInfo.InvariantCulture, "\"soulGems\": {0}, \"gemValue\": {1}, \"chests\": {2}", gems, value, chests));
        }

        private IEnumerator ShotAfter(string name, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            while (GameTime.IsPaused && !Finished) yield return new WaitForSecondsRealtime(0.2f);
            yield return Shot(name);
        }

        // ------------------------------------------------------------------ recording

        private IEnumerator ShotSoon(string name)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot(name);
        }

        /// <summary>A player build captures the full screen with the UI; the editor renders the game camera.</summary>
        /// <summary>
        /// -sh-autoplay-shop-shot: before the run, opens the VS PowerUps screen on the throw-away save (with gold), buys a
        /// few ranks like a player and screenshots it. The menu lives in the UI assembly, so it is reached by name.
        /// </summary>
        /// <summary>
        /// Opens the UI style sample (UI assembly, reached by name), waits for it to settle and captures it at twice the
        /// window's resolution, so text and icons are judged sharp.
        /// </summary>
        private IEnumerator UnlockNoticeShots()
        {
            GameObject seen = null;
            while (this != null)
            {
                var notice = GameObject.Find("CharacterUnlockedNotice");
                var canvas = notice != null ? notice.GetComponent<Canvas>() : null;
                if (notice != null && notice != seen && canvas != null && canvas.enabled && !SoulHunter.Core.Services.GameTime.IsPaused)
                {
                    seen = notice;
                    yield return new WaitForSecondsRealtime(0.3f);
                    var text = notice.GetComponentInChildren<TMPro.TMP_Text>();
                    Event("unlock-notice", $"\"text\": \"{Escape(text != null ? text.text : "")}\"");
                    yield return Shot("unlock-notice");
                }
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        private IEnumerator StyleSampleShot()
        {
            var type = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("SoulHunter.UI.UiStyleSampleScreen")).FirstOrDefault(t => t != null);
            if (type == null) { Event("style-sample", "\"error\": \"UiStyleSampleScreen not found\""); yield break; }
            type.GetMethod("Show")?.Invoke(null, null);
            yield return new WaitForSecondsRealtime(1.5f);
            string file = Path.Combine(Config.OutputFolder, "shots", "style_sample_2x.png");
            ScreenCapture.CaptureScreenshot(file, 2);
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("style-sample");
            Event("style-sample", string.Format(CultureInfo.InvariantCulture, "\"file\": \"{0}\", \"screen\": \"{1}x{2}\"", Escape(file), Screen.width, Screen.height));
            type.GetMethod("Hide")?.Invoke(null, null);
            yield return null;
        }

        private IEnumerator SelectShots()
        {
            MonoBehaviour Find(string type) => FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(b => b.GetType().Name == type);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("mainmenu");
            var navigation = Find("MainMenuNavigation");
            navigation?.GetType().GetMethod("ShowCharacters")?.Invoke(navigation, null);
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("character-select");
            var texts = FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled &&
                (t.text.StartsWith("Gold") || t.text.StartsWith("FILTER") || t.name == "StartingWeapon_Text" || t.text == "Character Selection"));
            var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c => c.isRootCanvas);
            // Every shown button's own size and its size on screen, in the 1920x1080 layout's units (the theme's sizes).
            var buttons = FindObjectsByType<UnityEngine.UI.Selectable>(FindObjectsSortMode.None)
                .Where(s => s.isActiveAndEnabled && !s.name.StartsWith("Card_") && !s.name.StartsWith("Stage_")).Select(s =>
                {
                    var r = (RectTransform)s.transform;
                    var corners = new Vector3[4];
                    r.GetWorldCorners(corners);
                    float k = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
                    return string.Format(CultureInfo.InvariantCulture, "{0} {1:0}x{2:0} shown {3:0}x{4:0}", s.name, r.rect.width, r.rect.height,
                        (corners[2].x - corners[0].x) / k, (corners[2].y - corners[0].y) / k);
                });
            Event("select-layout", string.Format(CultureInfo.InvariantCulture, "\"screen\": \"{0}x{1}\", \"scale\": {2:0.00}, \"texts\": \"{3}\", \"buttons\": \"{4}\"",
                Screen.width, Screen.height, canvas != null ? canvas.scaleFactor : 0f,
                Escape(string.Join("; ", texts.Select(t => $"{t.name}='{t.text}' size {t.fontSize:0.0} auto {t.enableAutoSizing} max {t.fontSizeMax:0}"))),
                Escape(string.Join("; ", buttons))));
            // -sh-autoplay-sort-shot: the VS sort, A-Z descending (CharacterSelectionUI.SetSort / SetSortDescending; the
            // screen lives in the UI assembly, so it is reached by name). The sort is not saved; it goes back to default.
            if (HasArg("-sh-autoplay-sort-shot"))
            {
                var select = Find("CharacterSelectionUI");
                var type = select != null ? select.GetType() : null;
                var sortType = type?.GetNestedType("CharacterSort");
                var setSort = type?.GetMethod("SetSort");
                var setDescending = type?.GetMethod("SetSortDescending");
                if (sortType == null || setSort == null || setDescending == null)
                    Event("sort-shot", "\"error\": \"CharacterSelectionUI sort API not found\"");
                else
                {
                    setSort.Invoke(select, new[] { System.Enum.Parse(sortType, "Alphabetical") });
                    setDescending.Invoke(select, new object[] { true });
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Shot("character-select-sort-za-desc");
                    var visible = type.GetMethod("VisibleNames")?.Invoke(select, null) as List<string>;
                    Event("sort-shot", string.Format(CultureInfo.InvariantCulture, "\"sort\": \"{0}\", \"order\": \"{1}\", \"visible\": \"{2}\"",
                        Escape(type.GetProperty("SortText")?.GetValue(select) as string ?? ""),
                        Escape(type.GetProperty("OrderText")?.GetValue(select) as string ?? ""),
                        Escape(visible != null ? string.Join(", ", visible) : "")));
                    setSort.Invoke(select, new[] { System.Enum.Parse(sortType, "Default") });
                    setDescending.Invoke(select, new object[] { false });
                    yield return new WaitForSecondsRealtime(0.2f);
                }
            }
            navigation?.GetType().GetMethod("ShowHome")?.Invoke(navigation, null);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        /// <summary>
        /// -sh-autoplay-menu-sections-shot: the VS main menu sections (Collection, Bestiary, Unlocks, Credits). The throw-away
        /// save gets a few Collection entries found, a few Bestiary kills and the Bestiary unlocked, and the main menu is
        /// loaded again so its Bestiary button is built (MainMenuNavigation builds the sections in Start). Each section is
        /// opened with its own button like a player, screenshot, and closed with its Back button.
        /// </summary>
        private IEnumerator MenuSectionShots(SoulHunter.Core.Persistence.GameData data)
        {
            MonoBehaviour Find(string type) => FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(b => b.GetType().Name == type);
            // A few Collection entries of each kind (evolutions come from EvolutionDiscovery, -sh-autoplay-discover).
            int found = 0;
            foreach (var group in SoulHunter.Gameplay.Data.CollectionCatalog.All()
                         .Where(e => e != null && !string.IsNullOrEmpty(e.Id) && !e.Id.StartsWith(SoulHunter.Core.Persistence.CollectionBook.EvolutionPrefix))
                         .GroupBy(e => e.Kind))
                foreach (var entry in group.Take(3))
                {
                    SoulHunter.Core.Persistence.CollectionBook.MarkFound(data, entry.Id, out bool isNew);
                    if (isNew) found++;
                }
            // A few Bestiary entries known, with different kill counts.
            var killCounts = new[] { 250, 40, 7, 1 };
            var enemies = SoulHunter.Gameplay.Data.BestiaryCatalog.All();
            int known = 0;
            for (int i = 0; i < enemies.Count && i < killCounts.Length; i++)
            {
                string id = SoulHunter.Gameplay.Data.BestiaryCatalog.IdOf(enemies[i]);
                for (int k = 0; k < killCounts[i]; k++) SoulHunter.Core.Persistence.Bestiary.RecordKill(data, id);
                known++;
            }
            SoulHunter.Core.Persistence.Bestiary.Unlock(data);
            Event("menu-sections-data", $"\"collectionFound\": {found}, \"bestiaryKnown\": {known}, \"bestiaryUnlocked\": {(SoulHunter.Core.Persistence.Bestiary.IsUnlocked(data) ? "true" : "false")}");

            // Load the main menu again so the Bestiary button exists (the save is in memory; nothing is written).
            var old = Find("MainMenuNavigation");
            GameServices.Instance.Get<SceneService>().LoadSceneAsync("MainMenu");
            float until = Time.realtimeSinceStartup + 20f;
            MonoBehaviour navigation = null;
            while (Time.realtimeSinceStartup < until)
            {
                navigation = Find("MainMenuNavigation");
                if (navigation != null && navigation != old && SceneManager.GetActiveScene().name == "MainMenu") break;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.8f);
            if (navigation == null) { Event("menu-sections", "\"error\": \"MainMenuNavigation not found\""); yield break; }
            yield return Shot("mainmenu-sections");

            var navType = navigation.GetType();
            var results = new List<string>();
            foreach (var (label, buttonName, screenName) in new[]
                     {
                         ("collection", "CollectionButton", "Collection"),
                         ("bestiary", "BestiaryButton", "BestiaryScreen"),
                         ("unlocks", "UnlocksButton", "Unlocks"),
                         ("credits", "CreditsButton", "Credits"),
                     })
            {
                var button = navType.GetProperty(buttonName)?.GetValue(navigation) as UnityEngine.UI.Button;
                var screen = navType.GetProperty(screenName)?.GetValue(navigation) as MonoBehaviour;
                if (button == null || screen == null)
                {
                    results.Add($"\"{label}\": \"{(button == null ? "no button" : "no screen")}\"");
                    continue;
                }
                button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Shot("menu-" + label);
                var count = screen.GetType().GetProperty("CountText")?.GetValue(screen) as TMPro.TMP_Text;
                results.Add($"\"{label}\": \"{(screen.gameObject.activeInHierarchy ? "shown" : "hidden")}; {Escape(count != null ? count.text : "")}\"");
                var back = screen.GetType().GetProperty("BackButton")?.GetValue(screen) as UnityEngine.UI.Button;
                if (back != null) back.onClick.Invoke();
                else navType.GetMethod("ShowHome")?.Invoke(navigation, null);
                yield return new WaitForSecondsRealtime(0.3f);
            }
            Event("menu-sections", string.Join(", ", results));
        }

        /// <summary>
        /// -sh-autoplay-evolutions-shot: about 22 s into the run (after -sh-autoplay-pause-shot, not over a level-up or chest),
        /// pause, press the pause menu's Evolutions button (EvolutionRecipesScreen), screenshot, page Next once when there is
        /// a second page and screenshot again, then Back and resume.
        /// </summary>
        private IEnumerator EvolutionsShot()
        {
            yield return new WaitForSecondsRealtime(22f);
            var system = GameSystemManager.Instance;
            if (system == null) yield break;
            while (SoulHunter.Core.Services.GameTime.IsPaused) yield return null;
            system.TogglePause();
            yield return new WaitForSecondsRealtime(0.5f);
            var pause = FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
            if (pause == null || pause.EvolutionsButton == null)
            {
                Event("evolutions", "\"error\": \"pause menu or its Evolutions button not found\"");
                if (system.IsPaused) system.Resume();
                yield break;
            }
            pause.EvolutionsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            var screen = pause.RecipesScreen;
            yield return Shot("evolutions");
            if (screen != null)
            {
                int pages = screen.PageCount;
                if (pages > 1 && screen.NextButton != null)
                {
                    screen.NextButton.onClick.Invoke();
                    yield return new WaitForSecondsRealtime(0.4f);
                    yield return Shot("evolutions-page2");
                }
                Event("evolutions", string.Format(CultureInfo.InvariantCulture, "\"open\": {0}, \"recipes\": {1}, \"pages\": {2}, \"page\": {3}",
                    screen.IsOpen ? "true" : "false", screen.Recipes.Count, pages, screen.Page));
                if (screen.BackButton != null) screen.BackButton.onClick.Invoke();
                else screen.Close();
                yield return new WaitForSecondsRealtime(0.3f);
            }
            else Event("evolutions", "\"error\": \"EvolutionRecipesScreen not created\"");
            if (system.IsPaused) system.Resume();
        }

        private IEnumerator ShopShots(SoulHunter.Core.Persistence.GameData data)
        {
            data.Currency = Mathf.Max(data.Currency, 20000);
            MonoBehaviour Find(string type) => FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(b => b.GetType().Name == type);
            var navigation = Find("MainMenuNavigation");
            navigation?.GetType().GetMethod("ShowPowerUps")?.Invoke(navigation, null);
            yield return new WaitForSecondsRealtime(0.5f);
            var shop = Find("PowerUpMenuUI");
            if (shop != null)
            {
                var select = shop.GetType().GetMethod("Select");
                var buy = shop.GetType().GetMethod("BuySelected");
                foreach (var (id, ranks) in new[] { (SoulHunter.Core.Persistence.PowerUpId.Might, 2), (SoulHunter.Core.Persistence.PowerUpId.Recovery, 5), (SoulHunter.Core.Persistence.PowerUpId.Magnet, 1) })
                {
                    select?.Invoke(shop, new object[] { id });
                    for (int i = 0; i < ranks; i++) buy?.Invoke(shop, null);
                }
                select?.Invoke(shop, new object[] { SoulHunter.Core.Persistence.PowerUpId.Greed });
            }
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("powerups");
            Event("powerups", string.Format(CultureInfo.InvariantCulture, "\"bought\": {0}, \"gold\": {1}", SoulHunter.Core.Persistence.PowerUps.TotalBought(data), data.Currency));
            // -sh-autoplay-powerup-off-shot: VS PowerUp on/off. Area is maxed on the throw-away save, selected, and switched
            // off with the shop's own Turn Off (ToggleSelected); the shot shows its OFF cell selected. Switched back on after.
            if (HasArg("-sh-autoplay-powerup-off-shot") && shop != null)
            {
                var area = SoulHunter.Core.Persistence.PowerUpId.Area;
                SoulHunter.Core.Persistence.PowerUps.SetRank(data, area, SoulHunter.Core.Persistence.PowerUps.Info(area).MaxRank);
                var select = shop.GetType().GetMethod("Select");
                var toggle = shop.GetType().GetMethod("ToggleSelected");
                select?.Invoke(shop, new object[] { area });
                if (SoulHunter.Core.Persistence.PowerUps.IsEnabled(data, area))
                {
                    if (toggle != null) toggle.Invoke(shop, null);
                    else { SoulHunter.Core.Persistence.PowerUps.SetEnabled(data, area, false); select?.Invoke(shop, new object[] { area }); }
                }
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shot("powerups-off");
                Event("powerup-off", string.Format(CultureInfo.InvariantCulture, "\"powerup\": \"{0}\", \"maxed\": {1}, \"enabled\": {2}, \"info\": \"{3}\"",
                    area, SoulHunter.Core.Persistence.PowerUps.IsMaxed(data, area) ? "true" : "false",
                    SoulHunter.Core.Persistence.PowerUps.IsEnabled(data, area) ? "true" : "false",
                    Escape(shop.GetType().GetProperty("InfoText")?.GetValue(shop) as string ?? "")));
                if (!SoulHunter.Core.Persistence.PowerUps.IsEnabled(data, area))
                {
                    if (toggle != null) toggle.Invoke(shop, null);
                    else SoulHunter.Core.Persistence.PowerUps.SetEnabled(data, area, true);
                }
                yield return new WaitForSecondsRealtime(0.2f);
            }
            navigation?.GetType().GetMethod("ShowHome")?.Invoke(navigation, null);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        private IEnumerator PauseShot()
        {
            yield return new WaitForSecondsRealtime(20f);
            var system = GameSystemManager.Instance;
            if (system == null) yield break;
            while (SoulHunter.Core.Services.GameTime.IsPaused) yield return null; // not over a level-up or chest
            system.TogglePause();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("pause-stats");
            // -sh-autoplay-options-shot: the Options screen over the pause menu too.
            if (HasArg("-sh-autoplay-options-shot"))
            {
                // Through the pause screen's own button, as a player does (the pause screen steps back).
                var pauseUi = system.PauseMenuPanel != null ? system.PauseMenuPanel.GetComponent<SoulHunter.Gameplay.UI.PauseMenuUI>() : null;
                if (pauseUi != null) pauseUi.OpenOptions();
                var options = pauseUi != null ? FindFirstObjectByType<SoulHunter.Gameplay.UI.OptionsMenuUI>() : SoulHunter.Gameplay.UI.OptionsMenuUI.Open();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shot("options");
                // Every other section too (Favorites opens first; Display and Gameplay hold the ON/OFF switches, Controls the key bindings).
                if (options != null)
                    foreach (var section in new[] { "Display", "Sound", "Gameplay", "Controls" })
                    {
                        var tab = options.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                            .FirstOrDefault(b => b.GetComponentInChildren<TMPro.TMP_Text>(true) is TMPro.TMP_Text t && t.text.Trim() == section);
                        if (tab == null) continue;
                        tab.onClick.Invoke();
                        yield return new WaitForSecondsRealtime(0.4f);
                        yield return Shot("options-" + section.ToLowerInvariant());
                    }
                if (options != null) options.Close();
                yield return new WaitForSecondsRealtime(0.3f);
            }
            if (system.IsPaused) system.Resume();
        }

        private IEnumerator ResultsShot(string reason)
        {
            var session = GameSessionManager.Instance;
            var system = GameSystemManager.Instance;
            if (reason == "time limit" && session != null && !session.IsRunFinished && system != null)
            {
                float until = Time.realtimeSinceStartup + 20f;
                while (SoulHunter.Core.Services.GameTime.IsPaused && Time.realtimeSinceStartup < until) yield return null; // not over a level-up
                if (!system.IsPaused) system.TogglePause();
                yield return new WaitForSecondsRealtime(0.3f);
                system.QuitToMainMenu();
            }
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("results");
            var stats = RunStatsTracker.Instance;
            if (stats == null) yield break;
            var rows = new List<string>();
            foreach (var w in stats.Weapons)
                rows.Add(string.Format(CultureInfo.InvariantCulture, "{{\"name\": \"{0}\", \"level\": {1}, \"damage\": {2:0}, \"seconds\": {3:0.0}, \"dps\": {4:0.0}}}",
                    Escape(w.Name), w.Weapon != null ? w.Weapon.CurrentLevel : w.Level, w.Damage, w.SecondsUsed, w.Dps));
            var pickups = new List<string>();
            foreach (var p in stats.Pickups) pickups.Add($"\"{Escape(p.Key)}\": {p.Value}");
            Event("results", $"\"weapons\": [{string.Join(", ", rows)}], \"items\": {stats.Items.Count}, \"pickups\": {{{string.Join(", ", pickups)}}}, \"unlocks\": {stats.Unlocks.Count}");
        }

        private IEnumerator EarlyShots()
        {
            float start = Time.realtimeSinceStartup;
            for (int i = 0; i <= 15; i++)
            {
                while (Time.realtimeSinceStartup < start + i * 0.2f) yield return null;
                yield return Shot($"early{i * 200:0000}ms");
            }
        }

        /// <summary>
        /// Owner report 2026-09-30 ("kabhi bhi hang ho ja raha hai"): logs every frame longer than 0.25 s (not the
        /// screenshot frames) and any pause held for more than 20 s, with who holds it.
        /// </summary>
        private int _lastGc0, _lastGc1, _lastGc2;

        private void WatchForHangs(float now)
        {
            float dt = Time.unscaledDeltaTime;
            // Garbage collections since the last frame (a full collection of a large heap is a usual cause of a long frame).
            int gc0 = System.GC.CollectionCount(0), gc1 = System.GC.CollectionCount(1), gc2 = System.GC.CollectionCount(2);
            // A Pentagram wipe: the length of its own frame and of the frame after it, with the wipe's own measured parts.
            int sinceWipe = Time.frameCount - PentagramWeapon.LastWipeFrame;
            if (PentagramWeapon.LastWipeFrame >= 0 && (sinceWipe == 1 || sinceWipe == 2))
                Event("wipe", string.Format(CultureInfo.InvariantCulture,
                    "\"frame\": \"{0}\", \"ms\": {1:0}, \"erased\": {2}, \"enemyPassMs\": {3:0.0}, \"itemPassMs\": {4:0.0}",
                    sinceWipe == 1 ? "wipe" : "after", dt * 1000f, PentagramWeapon.LastWipeCount, PentagramWeapon.LastEnemyWipeMs, PentagramWeapon.LastItemWipeMs));
            if (dt > 0.25f && Time.frameCount > _shotFrame + 3 && Elapsed > 3f)
            {
                var progression = LevelProgressionManager.Instance;
                Event("hitch", string.Format(CultureInfo.InvariantCulture,
                    "\"ms\": {0:0}, \"stageTime\": {1:0.0}, \"paused\": {2}, \"pausers\": \"{3}\", \"enemies\": {4}, \"gems\": {5}, \"level\": {6}, \"gc\": [{7}, {8}, {9}], \"heapMb\": {10:0.0}, \"objects\": {11}",
                    dt * 1000f, progression != null ? progression.StageElapsedTime : 0f, GameTime.IsPaused ? "true" : "false",
                    GameTime.PauserNames(), EnemyController.ActiveEnemies.Count, XPGem.ActiveGems.Count,
                    _experience != null ? _experience.CurrentLevel : 0,
                    gc0 - _lastGc0, gc1 - _lastGc1, gc2 - _lastGc2, System.GC.GetTotalMemory(false) / 1048576f,
                    FindObjectsByType<Transform>(FindObjectsSortMode.None).Length) + $", \"spans\": \"{Escape(SoulHunter.Core.Services.SlowSpan.TakeRecent())}\"");
            }
            else if (dt > 0.025f && Time.frameCount > _shotFrame + 3 && Elapsed > 3f && _joltEvents < 400)
            {
                // A jolt (a frame over 25 ms, which a 60 FPS player sees): what was slow in it, for the 60 FPS work (2026-10-17).
                _joltEvents++;
                Event("jolt", string.Format(CultureInfo.InvariantCulture,
                    "\"ms\": {0:0.0}, \"paused\": {1}, \"enemies\": {2}, \"gc\": [{3}, {4}, {5}]",
                    dt * 1000f, GameTime.IsPaused ? "true" : "false", EnemyController.ActiveEnemies.Count,
                    gc0 - _lastGc0, gc1 - _lastGc1, gc2 - _lastGc2) + $", \"spans\": \"{Escape(SoulHunter.Core.Services.SlowSpan.TakeRecent())}\"" +
                    $", \"triggers\": \"{Escape(_lastFrameTriggers)}\"" +
                    $", \"hits\": \"{Escape(_lastFrameHits)}\"");
            }
            else SoulHunter.Core.Services.SlowSpan.Prune(); // a long frame reports the timed sections of its last two frames
            // -sh-autoplay-hit-timing: the release-build time of weapon hits, summed per frame (a jolt lists the previous frame).
            if (HasArg("-sh-autoplay-hit-timing"))
            {
                if (!HitTiming.Enabled) { HitTiming.Enabled = true; HitTiming.Clear(); }
                else if (_hitTimingFrame != Time.frameCount) { _hitTimingFrame = Time.frameCount; _lastFrameHits = HitTiming.Describe(); HitTiming.Clear(); }
            }
            // -sh-autoplay-count-triggers: ProjectileDamage trigger enters, counted per frame (a jolt lists the top ones).
            if (HasArg("-sh-autoplay-count-triggers"))
            {
                if (ProjectileDamage.EnterStats == null) ProjectileDamage.EnterStats = new Dictionary<string, int>();
                else if (_triggerFrame != Time.frameCount) { _triggerFrame = Time.frameCount; _lastFrameTriggers = TopTriggerEnters(); ProjectileDamage.EnterStats.Clear(); }
            }
            _lastGc0 = gc0; _lastGc1 = gc1; _lastGc2 = gc2;
            if (!GameTime.IsPaused) { _pausedSince = -1f; _pauseReported = false; return; }
            if (_pausedSince < 0f) _pausedSince = now;
            if (!_pauseReported && now - _pausedSince > 20f)
            {
                _pauseReported = true;
                Event("stuck-pause", $"\"pausers\": \"{GameTime.PauserNames()}\"");
                StartCoroutine(Shot("stuck-pause"));
            }
        }

        /// <summary>
        /// Bug MIS O2 (2026-09-30: 8-10 FPS while level-up screens were open, measured with a PNG saved on every
        /// level-up): frames while a level-up screen is open, without screenshot frames. With
        /// -sh-autoplay-xp-burst-after S (and -sh-autoplay-xp-burst-levels N, default 30) a chain of N level-ups starts at S.
        /// </summary>
        private void WatchLevelUpFrames()
        {
            if (_levelUpManager == null) _levelUpManager = FindFirstObjectByType<LevelUpManager>();
            if (_levelUpManager != null && _levelUpManager.IsLevelUpActive && Time.frameCount > _shotFrame + 3)
            {
                float dt = Time.unscaledDeltaTime;
                _levelUpFrames++;
                _levelUpSeconds += dt;
                _levelUpWorst = Mathf.Max(_levelUpWorst, dt);
            }
            float burstAt = float.TryParse(ArgValue("-sh-autoplay-xp-burst-after"), NumberStyles.Float, CultureInfo.InvariantCulture, out var b) ? b : -1f;
            if (burstAt >= 0f && !_xpBurstDone && Elapsed >= burstAt && _experience != null)
            {
                _xpBurstDone = true;
                int levels = int.TryParse(ArgValue("-sh-autoplay-xp-burst-levels"), out var n) ? Mathf.Clamp(n, 1, 200) : 30;
                Event("xp-burst", $"\"levels\": {levels}, \"fromLevel\": {_experience.CurrentLevel}");
                for (int i = 0; i < levels; i++) _experience.AddXP(Mathf.Max(1, _experience.XPToNextLevel - _experience.CurrentXP));
            }
            // -sh-autoplay-give "Pentagram:8,Garlic:8" (and -sh-autoplay-give-after S, default 12): gives those items at
            // those levels once, so a build run can measure one weapon (the Pentagram's screen wipe, 2026-10-13).
            if (!_giveDone && Elapsed >= GiveAfter) GiveListedItems();
            // -sh-autoplay-chest-after S: drops one chest on the hero once (the chest screen's cost in a profile).
            if (!_chestDropped && float.TryParse(ArgValue("-sh-autoplay-chest-after"), NumberStyles.Float, CultureInfo.InvariantCulture, out var chestAt)
                && Elapsed >= chestAt && PickupPoolManager.Instance != null && _player != null)
            {
                _chestDropped = true;
                PickupPoolManager.Instance.SpawnChest(_player.transform.position);
                Event("chest-dropped", "\"by\": \"bot\"");
            }
            // -sh-autoplay-quality N sets the quality level once. -sh-autoplay-quality-ab "A,B" switches between two levels
            // every 30 s with V-Sync and the frame cap off, so one run compares them on the same hero (the timeline's
            // "quality" field says which was on).
            if (!_qualitySet && int.TryParse(ArgValue("-sh-autoplay-quality"), out int fixedQuality))
            {
                _qualitySet = true;
                if (_qualityBefore < 0) _qualityBefore = QualitySettings.GetQualityLevel();
                QualitySettings.SetQualityLevel(fixedQuality, true);
                DisplaySettingsApplier.Apply(); // a quality level carries its own V-Sync count; the game's rule comes back
            }
            // -sh-autoplay-profile <file> (development build; -sh-autoplay-profile-at S, default 60; -sh-autoplay-profile-seconds N,
            // default 6): writes the Unity profiler to that file for a short window only, so the capture stays small.
            string profileFile = ArgValue("-sh-autoplay-profile");
            if (profileFile != null && _profileState < 2)
            {
                float at = float.TryParse(ArgValue("-sh-autoplay-profile-at"), out float a) ? a : 60f;
                float seconds = float.TryParse(ArgValue("-sh-autoplay-profile-seconds"), out float n) ? n : 6f;
                // -sh-autoplay-profile-below FPS: wait (after the start time) until two seconds in a row ran slower than that.
                bool slowEnough = true;
                if (float.TryParse(ArgValue("-sh-autoplay-profile-below"), out float below))
                {
                    _profileFrames++;
                    if (Time.realtimeSinceStartup - _profileSecondStart >= 1f)
                    {
                        float fps = _profileFrames / (Time.realtimeSinceStartup - _profileSecondStart);
                        _profileSlowSeconds = fps < below && Time.timeScale > 0f ? _profileSlowSeconds + 1 : 0;
                        _profileFrames = 0;
                        _profileSecondStart = Time.realtimeSinceStartup;
                    }
                    slowEnough = _profileSlowSeconds >= 2;
                }
                // -sh-autoplay-profile-paused: also start while a menu holds the game (level-up, chest, pause screens).
                bool running = Time.timeScale > 0f || HasArg("-sh-autoplay-profile-paused");
                if (_profileState == 0 && Elapsed >= at && running && slowEnough)
                {
                    UnityEngine.Profiling.Profiler.logFile = profileFile;
                    UnityEngine.Profiling.Profiler.enableBinaryLog = true;
                    UnityEngine.Profiling.Profiler.enabled = true;
                    _profileUntil = Time.realtimeSinceStartup + seconds;
                    _profileNextPart = Time.realtimeSinceStartup + ProfilePartSeconds;
                    _profilePart = 1;
                    _profileState = 1;
                    Event("profile", "\"state\": \"start\", \"enemies\": " + EnemyController.ActiveEnemies.Count);
                }
                else if (_profileState == 1 && Time.realtimeSinceStartup < _profileUntil && Time.realtimeSinceStartup >= _profileNextPart)
                {
                    // A new file every few seconds: the editor keeps only the last 300 frames of a loaded capture, so a long
                    // capture lost all but its last ~4 s (2026-10-17). Parts: l3.raw, l3-part2.raw, l3-part3.raw ...
                    _profilePart++;
                    _profileNextPart = Time.realtimeSinceStartup + ProfilePartSeconds;
                    UnityEngine.Profiling.Profiler.enableBinaryLog = false;
                    UnityEngine.Profiling.Profiler.logFile = System.IO.Path.ChangeExtension(profileFile, null) + "-part" + _profilePart + System.IO.Path.GetExtension(profileFile);
                    UnityEngine.Profiling.Profiler.enableBinaryLog = true;
                }
                else if (_profileState == 1 && Time.realtimeSinceStartup >= _profileUntil)
                {
                    UnityEngine.Profiling.Profiler.enabled = false;
                    UnityEngine.Profiling.Profiler.enableBinaryLog = false;
                    UnityEngine.Profiling.Profiler.logFile = "";
                    _profileState = 2;
                }
            }
            // -sh-autoplay-1080p: the contract baseline is 1920x1080; on a smaller display the 3D view renders at the URP render
            // scale that gives 1080p's pixel count (PerformanceBenchmark's rule). Put back when the bot goes.
            if (_renderScaleBefore < 0f && HasArg("-sh-autoplay-1080p")) Apply1080pRenderScale();
            // -sh-autoplay-span-ms N: record timed sections from N ms (default 5), to break a jolt down further.
            if (!_spanMsSet) { _spanMsSet = true; if (double.TryParse(ArgValue("-sh-autoplay-span-ms"), NumberStyles.Float, CultureInfo.InvariantCulture, out double spanMs)) SoulHunter.Core.Services.SlowSpan.RecordMs = spanMs; }
            string qualityAb = ArgValue("-sh-autoplay-quality-ab");
            if (qualityAb != null && Elapsed >= _nextQualitySwitch)
            {
                var levels = qualityAb.Split(',');
                if (levels.Length == 2 && int.TryParse(levels[_qualityAbIndex % 2], out int abQuality))
                {
                    if (_qualityBefore < 0) _qualityBefore = QualitySettings.GetQualityLevel();
                    QualitySettings.SetQualityLevel(abQuality, true);
                    DisplaySettingsApplier.Uncapped = true;
                    DisplaySettingsApplier.Apply();
                }
                _qualityAbIndex++;
                _nextQualitySwitch = Elapsed + 30f;
            }
            // -sh-autoplay-vsync-ab: V-Sync on for 30 s, then off for 30 s, and so on, with the game's 60 FPS cap (the throw-away
            // save's option), so one run compares what the player feels (the timeline's "vsync" and "jolts" fields).
            if (HasArg("-sh-autoplay-vsync-ab") && Elapsed >= _nextVSyncSwitch && GameSettings.Data != null)
            {
                GameSettings.Data.VSync = _vSyncAbIndex % 2 == 0;
                DisplaySettingsApplier.Apply();
                _vSyncAbIndex++;
                _nextVSyncSwitch = Elapsed + 30f;
            }
            // -sh-autoplay-vat-ab: the horde is drawn on the GPU (HordeVatDrawer) for 30 s, then by its own skinned renderers for
            // 30 s, and so on, so one run compares both on the same hero (the timeline's "vat" field says which was on).
            if (HasArg("-sh-autoplay-vat-ab") && Elapsed >= _nextVatSwitch)
            {
                HordeVatDrawer.Enabled = _vatAbIndex % 2 == 0;
                _vatAbIndex++;
                _nextVatSwitch = Elapsed + 30f;
            }
            // -sh-autoplay-bullet-ab: enemy bullets are drawn instanced (EnemyBulletDrawer) for 30 s, then by their own
            // renderers and halo particles for 30 s, and so on (the timeline's "bulletDraw" field says which was on).
            if (HasArg("-sh-autoplay-bullet-ab") && Elapsed >= _nextBulletSwitch)
            {
                SoulHunter.Gameplay.Hazards.EnemyBulletDrawer.Enabled = _bulletAbIndex % 2 == 0;
                _bulletAbIndex++;
                _nextBulletSwitch = Elapsed + 30f;
            }
            // -sh-autoplay-omen <OmenEvent name> (and -sh-autoplay-omen-after S, default 20): fires that Omen once, then takes
            // screenshots 1, 4 and 10 s later, so a build run can show an Omen without waiting for the wheel.
            if (!_omenDone && Elapsed >= (float.TryParse(ArgValue("-sh-autoplay-omen-after"), NumberStyles.Float, CultureInfo.InvariantCulture, out var omenAt) ? omenAt : 20f)
                && System.Enum.TryParse<OmenEvent>(ArgValue("-sh-autoplay-omen") ?? "", true, out var omen) && RandomEventDirector.Instance != null)
            {
                _omenDone = true;
                bool fired = RandomEventDirector.Instance.Fire(omen);
                Event("omen", $"\"name\": \"{OmenTable.Name(omen)}\", \"fired\": {(fired ? "true" : "false")}");
                StartCoroutine(OmenShots(omen.ToString()));
            }
        }

        private float GiveAfter => float.TryParse(ArgValue("-sh-autoplay-give-after"), NumberStyles.Float, CultureInfo.InvariantCulture, out var giveAt) ? giveAt : 12f;

        private void GiveListedItems()
        {
            _giveDone = true;
            var weapons = WeaponManager.Instance;
            foreach (string part in (ArgValue("-sh-autoplay-give") ?? "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                var bits = part.Split(':');
                if (weapons == null || !System.Enum.TryParse<UpgradeData.UpgradeType>(bits[0].Trim(), true, out var type)) continue;
                int level = bits.Length > 1 && int.TryParse(bits[1], out var l) ? l : 1;
                // A passive goes up one level at a time through ApplyUpgrade, as a level-up does, so its stat applies.
                // GiveWeapon only sets the level: until 2026-10-17 every given passive (Duplicator, Spinach, Skull
                // O'Maniac, Cursed Coffer ...) changed no stat in measurement runs.
                var definition = SoulHunter.Gameplay.Data.WeaponDefinitionLookup.Find(type);
                if (definition != null && definition.IsPassive)
                {
                    for (int next = weapons.GetWeaponLevel(type) + 1; next <= level; next++)
                    {
                        var upgrade = definition.GetLevel(next);
                        if (upgrade == null) break;
                        weapons.ApplyUpgrade(upgrade);
                    }
                }
                else weapons.GiveWeapon(type, level);
                Event("give", $"\"item\": \"{type}\", \"level\": {weapons.GetWeaponLevel(type)}");
            }
        }

        private bool _omenDone, _giveDone, _chestDropped;
        private EnemySpawner _spawnerForLog; // pooled enemy bodies in the timeline (memory checks)

        private IEnumerator OmenShots(string name)
        {
            float last = 0f;
            foreach (float at in new[] { 1f, 4f, 10f })
            {
                yield return new WaitForSeconds(at - last);
                last = at;
                yield return Shot($"omen-{name}-{at:0}s");
            }
        }

        private LevelUpManager _levelUpManager;
        private int _levelUpFrames;
        private float _levelUpSeconds, _levelUpWorst;
        private bool _xpBurstDone;

        private int _lookStep;
        private int _shotFrame = -10;
        private float _pausedSince = -1f;
        private bool _pauseReported;

        private IEnumerator Shot(string name)
        {
            // -sh-autoplay-no-shots: no screenshots at all, for frame-time runs (a capture stalls its frame by 80-100 ms).
            if (HasArg("-sh-autoplay-no-shots")) yield break;
            if (HasArg("-sh-autoplay-touch-sizes")) LogTouchSizes(name);
            _shotFrame = Time.frameCount;
            string file = Path.Combine(Config.OutputFolder, "shots", $"{_shots.Count:000}_{name}.png");
            _shots.Add(Path.GetFileName(file));
            // No WaitForEndOfFrame: it never resumes in a batchmode editor, which hung the report.
            // CaptureScreenshot already captures at the end of the frame (a player, or the editor's Game view, with UI).
            if (!Application.isBatchMode)
            {
                // On a phone CaptureScreenshot takes a path relative to persistentDataPath (an absolute one saved nothing).
                string capture = file;
                string data = Application.persistentDataPath.Replace('\\', '/').TrimEnd('/') + "/";
                if (Application.isMobilePlatform && file.Replace('\\', '/').StartsWith(data)) capture = file.Replace('\\', '/').Substring(data.Length);
                ScreenCapture.CaptureScreenshot(capture);
                yield break;
            }
            var cam = Camera.main;
            if (cam == null) yield break;
            var rt = new RenderTexture(1280, 720, 24);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
        }

        // 60 FPS in every window (2026-10-14): an average hides short stutters, so each timeline line also counts the frames
        // slower than 60 FPS allows (16.7 ms plus 0.5 ms of timer jitter) and keeps the slowest frame.
        private const float SlowFrameSeconds = 0.0172f;
        private int _windowSlowFrames;
        private float _windowMaxFrame;
        // A jolt the eye sees: over 25 ms (with V-Sync on, a frame just past 16.7 ms waits for the next refresh: 33 ms).
        private const float JoltFrameSeconds = 0.025f;
        private int _windowJolts;

        private int RecentFps()
        {
            _windowSlowFrames = 0;
            _windowMaxFrame = 0f;
            _windowJolts = 0;
            if (_frameTimes.Count == 0) return 0;
            foreach (float dt in _frameTimes)
            {
                if (dt > SlowFrameSeconds) _windowSlowFrames++;
                if (dt > JoltFrameSeconds) _windowJolts++;
                if (dt > _windowMaxFrame) _windowMaxFrame = dt;
            }
            float avg = _frameTimes.Average();
            _frameTimes.Clear();
            return Mathf.RoundToInt(1f / Mathf.Max(avg, 0.0001f));
        }

        private int _profileState, _profileFrames, _profileSlowSeconds, _profilePart;
        private float _profileNextPart;
        private const float ProfilePartSeconds = 4f;
        private float _profileSecondStart;
        private float _profileUntil;
        private bool _qualitySet;
        private int _qualityBefore = -1;
        private int _qualityAbIndex;
        private float _nextQualitySwitch;
        private int _vSyncAbIndex;
        private int _vatAbIndex;
        private int _bulletAbIndex;
        private float _nextBulletSwitch;

        // Draw calls (2026-10-15): the render counters of the last frame, in the timeline. Unity fills them in a development
        // build; a release build gives -1 (the recorder is not valid there).
        private Unity.Profiling.ProfilerRecorder _drawCalls, _setPass, _batchesCount;

        private static long RenderCounter(ref Unity.Profiling.ProfilerRecorder recorder, string name)
        {
            if (!recorder.Valid) recorder = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, name);
            return recorder.Valid ? recorder.LastValue : -1;
        }
        private float _nextVatSwitch;
        private float _nextVSyncSwitch;
        private float _renderScaleBefore = -1f;
        private int _joltEvents, _triggerFrame = -1;
        // A jolt is the previous frame: its trigger enters and hit timing, kept before the clear.
        private string _lastFrameTriggers = "", _lastFrameHits = "";
        private int _hitTimingFrame = -1;

        /// <summary>The most frequent ProjectileDamage trigger enters since the last clear, "name > layer hit/miss xN".</summary>
        private static string TopTriggerEnters()
        {
            var stats = ProjectileDamage.EnterStats;
            if (stats == null || stats.Count == 0) return "";
            return string.Join("; ", stats.OrderByDescending(kv => kv.Value).Take(6).Select(kv => kv.Key + " x" + kv.Value));
        }
        private bool _spanMsSet;

        private static System.Reflection.PropertyInfo RenderScaleProperty(out Object pipeline)
        {
            pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            return pipeline != null ? pipeline.GetType().GetProperty("renderScale") : null;
        }

        private void Apply1080pRenderScale()
        {
            var property = RenderScaleProperty(out var pipeline);
            if (property == null) { _renderScaleBefore = 0f; return; }
            _renderScaleBefore = (float)property.GetValue(pipeline);
            float scale = PerformanceBenchmark.BaselineRenderScale();
            property.SetValue(pipeline, scale);
            Event("render-1080p", string.Format(CultureInfo.InvariantCulture, "\"window\": \"{0}x{1}\", \"renderScale\": {2:0.000}, \"rendered\": \"{3}x{4}\"",
                Screen.width, Screen.height, scale, Mathf.RoundToInt(Screen.width * scale), Mathf.RoundToInt(Screen.height * scale)));
        }

        private void RestoreRenderScale()
        {
            if (_renderScaleBefore <= 0f) return;
            var property = RenderScaleProperty(out var pipeline);
            if (property != null) property.SetValue(pipeline, _renderScaleBefore);
            _renderScaleBefore = 0f;
        }

        private void WriteTimeline()
        {
            var progression = LevelProgressionManager.Instance;
            int fps = RecentFps();
            if (fps > 0 && Elapsed > 5f) _minFps = Mathf.Min(_minFps, fps);
            var weapons = WeaponManager.Instance != null ? WeaponManager.Instance.GetActiveWeaponTypes().Select(t => $"\"{t}:{WeaponManager.Instance.GetWeaponLevel(t)}\"") : Enumerable.Empty<string>();
            var stats = RunStatsTracker.Instance;
            if (_spawnerForLog == null) _spawnerForLog = FindFirstObjectByType<EnemySpawner>();
            _timeline.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{{\"t\": {0:0.0}, \"stage\": {1}, \"stageTime\": {2:0}, \"level\": {3}, \"hp\": {4}, \"maxHp\": {5}, \"kills\": {6}, \"gold\": {7}, \"enemies\": {8}, \"fps\": {9}, \"paused\": {10}, \"weapons\": [{11}], \"gems\": {12}, \"heapMb\": {13:0.0}, \"gc\": [{14}, {15}, {16}], \"pooled\": {17}, \"unityMb\": {18:0}, \"quality\": {19}, \"slow\": {20}, \"maxMs\": {21:0.0}, \"shots\": {22}, \"jolts\": {23}, \"vsync\": {24}, \"vat\": {25}, \"vatBodies\": {26}, \"drawCalls\": {27}, \"setPass\": {28}, \"batches\": {29}, \"bulletDraw\": {30}, \"bullets\": {31}, \"bulletsAlive\": {32}, \"writeMs\": {33:0}}}",
                Elapsed, progression != null ? progression.CurrentStage : 0, progression != null ? progression.StageElapsedTime : 0f,
                _experience != null ? _experience.CurrentLevel : 0, _health != null ? _health.CurrentHealth : 0, _health != null ? _health.MaxHealth : 0,
                stats != null ? stats.TotalKills : 0, stats != null ? stats.TotalGoldCollected : 0, EnemyController.ActiveEnemies.Count, fps,
                GameTime.IsPaused ? "true" : "false", string.Join(", ", weapons), XPGem.ActiveGems.Count,
                System.GC.GetTotalMemory(false) / 1048576f, System.GC.CollectionCount(0), System.GC.CollectionCount(1), System.GC.CollectionCount(2),
                _spawnerForLog != null ? _spawnerForLog.PooledEnemies : 0, UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f, QualitySettings.GetQualityLevel(),
                _windowSlowFrames, _windowMaxFrame * 1000f, SoulHunter.Gameplay.Combat.Projectile.ActiveShots.Count,
                _windowJolts, QualitySettings.vSyncCount,
                HordeVatDrawer.Exists && HordeVatDrawer.Enabled ? "true" : "false", HordeVatDrawer.LastBodies,
                RenderCounter(ref _drawCalls, "Draw Calls Count"), RenderCounter(ref _setPass, "SetPass Calls Count"), RenderCounter(ref _batchesCount, "Batches Count"),
                SoulHunter.Gameplay.Hazards.EnemyBulletDrawer.Exists && SoulHunter.Gameplay.Hazards.EnemyBulletDrawer.Enabled ? "true" : "false",
                SoulHunter.Gameplay.Hazards.EnemyBulletDrawer.LastBullets, SoulHunter.Gameplay.Hazards.EnemyBulletLook.ActiveCount,
                ReportWriter.TakeSlowestWriteMs()));
            // -sh-autoplay-count-fx: who owns the playing particle systems (a scene search, so only for diagnosis runs).
            if (HasArg("-sh-autoplay-count-fx"))
            {
                var owners = new Dictionary<string, int>();
                int playing = 0;
                foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                {
                    if (!ps.isPlaying) continue;
                    playing++;
                    var root = ps.transform.root.name.Replace("(Clone)", "");
                    owners[root] = owners.TryGetValue(root, out var n) ? n + 1 : 1;
                }
                var shots = SoulHunter.Gameplay.Combat.Projectile.ActiveShots;
                Event("fx-count", string.Format(CultureInfo.InvariantCulture, "\"playing\": {0}, \"shots\": {1}, \"shotsFxOff\": {2}, \"owners\": {{{3}}}",
                    playing, shots.Count, shots.Count(x => x != null && x.FxStopped),
                    string.Join(", ", owners.OrderByDescending(o => o.Value).Take(12).Select(o => $"\"{Escape(o.Key)}\": {o.Value}"))));
            }
            ReportWriter.Write(Path.Combine(Config.OutputFolder, "timeline.jsonl"), _timeline.ToString());
        }

        /// <summary>
        /// T-01 (2026-10-10): every shown control's size on screen, in pixels and dp (dp = px x 160 / dpi), so a device
        /// run can check the 48 dp touch-target guideline. Controls are buttons, toggles, sliders and the on-screen stick
        /// and buttons (their pads). Overlay canvases draw in screen pixels, so world corners are pixels.
        /// </summary>
        private void LogTouchSizes(string screen)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            // The bot can also be told the density (desktop runs of the phone rule pass -sh-ui-dpi to the game).
            float uiDpi = SoulHunter.Gameplay.UI.UiScreen.Dpi;
            if (uiDpi > 0f) dpi = uiDpi;
            bool touch = SoulHunter.Gameplay.UI.UiScreen.IsTouch;
            Rect safe = Screen.safeArea;
            var rows = new List<string>();
            var boxes = new List<(string Name, Rect Box)>();
            var corners = new Vector3[4];
            void Add(RectTransform r, string label)
            {
                r.GetWorldCorners(corners);
                float w = corners[2].x - corners[0].x, h = corners[2].y - corners[0].y;
                if (w < 1f || h < 1f) return;
                // A screen that stepped back (a CanvasGroup at alpha 0 or taking no taps) is not on screen.
                foreach (var g in r.GetComponentsInParent<CanvasGroup>())
                    if (g.alpha <= 0.01f || !g.blocksRaycasts) return;
                // A row scrolled out of a list's view is hidden by the list's mask: not on screen, not judged.
                // Every list it sits in counts (a grid inside a scrolling page has two masks).
                var view = new Vector3[4];
                for (var t = r.parent; t != null; t = t.parent)
                {
                    if (t.GetComponent<UnityEngine.UI.RectMask2D>() == null && t.GetComponent<UnityEngine.UI.Mask>() == null) continue;
                    ((RectTransform)t).GetWorldCorners(view);
                    if (corners[0].x < view[0].x - 1f || corners[0].y < view[0].y - 1f || corners[2].x > view[2].x + 1f || corners[2].y > view[2].y + 1f) return;
                }
                rows.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1:0}x{2:0}px {3:0}x{4:0}dp", label, w, h, w * 160f / dpi, h * 160f / dpi));
                boxes.Add((label, new Rect(corners[0].x, corners[0].y, w, h)));
            }
            foreach (var s in FindObjectsByType<UnityEngine.UI.Selectable>(FindObjectsSortMode.None))
                if (s.isActiveAndEnabled && s.interactable && s.transform is RectTransform r && !(s is UnityEngine.UI.Scrollbar) && s.name != "XP_Bar_Slider" && s.name != "Player_HealthBar_Slider") Add(r, s.name); // the two bars are read, not touched
            foreach (var b in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (!b.isActiveAndEnabled) continue;
                string type = b.GetType().Name;
                if (type == "OnScreenButton" && b.transform is RectTransform rb) Add(rb, b.name);
                else if (type == "OnScreenStick" && b.transform.parent is RectTransform pad) Add(pad, pad.name);
            }
            // Layout checks (owner, 2026-10-10: the same screens in landscape and portrait): outside the safe area, two
            // controls overlapping, and (touch) a control under 48 dp. Only the top-most screen's controls are compared:
            // controls of a screen hidden under a veil are left in by FindObjectsByType, so overlaps are reported, not judged.
            var off = boxes.Where(b => b.Box.xMin < safe.xMin - 1f || b.Box.yMin < safe.yMin - 1f || b.Box.xMax > safe.xMax + 1f || b.Box.yMax > safe.yMax + 1f).Select(b => b.Name);
            var small = touch ? boxes.Where(b => Mathf.Min(b.Box.width, b.Box.height) * 160f / dpi < 47.5f).Select(b => b.Name) : Enumerable.Empty<string>();
            var overlaps = new List<string>();
            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    Rect a = boxes[i].Box, c = boxes[j].Box;
                    float ox = Mathf.Min(a.xMax, c.xMax) - Mathf.Max(a.xMin, c.xMin), oy = Mathf.Min(a.yMax, c.yMax) - Mathf.Max(a.yMin, c.yMin);
                    if (ox > 2f && oy > 2f) overlaps.Add(boxes[i].Name + "/" + boxes[j].Name);
                }
            Event("touch-sizes", string.Format(CultureInfo.InvariantCulture,
                "\"screen\": \"{0}\", \"px\": \"{1}x{2}\", \"dpi\": {3:0}, \"touch\": {4}, \"offScreen\": \"{5}\", \"under48dp\": \"{6}\", \"overlaps\": \"{7}\", \"textOut\": \"{8}\", \"controls\": \"{9}\"",
                Escape(screen), Screen.width, Screen.height, dpi, touch ? "true" : "false", Escape(string.Join("; ", off)), Escape(string.Join("; ", small)),
                Escape(string.Join("; ", overlaps)), Escape(string.Join("; ", TextOutsideItsBox())), Escape(string.Join("; ", rows))));
        }

        /// <summary>
        /// Every shown text whose drawn letters leave their box (owner, 2026-10-11: "tab se text bahar ja raha hai ... sabhi
        /// panel check karo"): a label wider or taller than the button, tab or card it sits on, a text cut short, and a
        /// plain text drawn wider than its own rectangle. Each entry names the control, the text and by how many pixels.
        /// </summary>
        private static List<string> TextOutsideItsBox()
        {
            var found = new List<string>();
            var box = new Vector3[4];
            var view = new Vector3[4];
            var drawnOnControls = new List<(UnityEngine.UI.Selectable holder, Vector3 lo, Vector3 hi, string words)>();
            foreach (var text in FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            {
                if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text) || text.color.a <= 0.01f || !(text.transform is RectTransform)) continue;
                if (text.canvas == null || text.canvas.renderMode == RenderMode.WorldSpace) continue; // damage numbers
                bool hidden = false;
                foreach (var g in text.GetComponentsInParent<CanvasGroup>()) if (g.alpha <= 0.01f) hidden = true;
                if (hidden) continue;
                text.ForceMeshUpdate();
                Bounds drawn = text.textBounds;
                if (drawn.size.x <= 0f) continue;
                Vector3 lo = text.transform.TransformPoint(drawn.min), hi = text.transform.TransformPoint(drawn.max);
                // Scrolled out of a list's view: not on screen, not judged.
                bool masked = false;
                for (var t = text.transform.parent; t != null && !masked; t = t.parent)
                {
                    if (t.GetComponent<UnityEngine.UI.RectMask2D>() == null && t.GetComponent<UnityEngine.UI.Mask>() == null) continue;
                    ((RectTransform)t).GetWorldCorners(view);
                    masked = hi.x < view[0].x || lo.x > view[2].x || hi.y < view[0].y || lo.y > view[2].y;
                }
                if (masked) continue;
                var holder = text.GetComponentInParent<UnityEngine.UI.Selectable>();
                bool control = holder != null && !(holder is UnityEngine.UI.Slider) && !(holder is UnityEngine.UI.Scrollbar);
                ((RectTransform)(control ? holder.transform : text.transform)).GetWorldCorners(box);
                // A control's label must stay inside the control; a plain text is only judged across (it may hang low by design).
                // Two texts of one control drawn over each other (a card's name on its description).
                if (control)
                {
                    foreach (var other in drawnOnControls)
                    {
                        if (other.holder != holder) continue;
                        float ox = Mathf.Min(hi.x, other.hi.x) - Mathf.Max(lo.x, other.lo.x), oy = Mathf.Min(hi.y, other.hi.y) - Mathf.Max(lo.y, other.lo.y);
                        if (ox > 3f && oy > 3f)
                            found.Add(string.Format(CultureInfo.InvariantCulture, "{0} '{1}' over '{2}'", holder.name,
                                text.text.Length > 24 ? text.text.Substring(0, 24) : text.text, other.words));
                    }
                    drawnOnControls.Add((holder, lo, hi, text.text.Length > 24 ? text.text.Substring(0, 24) : text.text));
                }
                float over = Mathf.Max(box[0].x - lo.x, hi.x - box[2].x);
                if (control) over = Mathf.Max(over, Mathf.Max(box[0].y - lo.y, hi.y - box[2].y));
                bool cut = text.isTextTruncated;
                if (over <= 1.5f && !cut) continue;
                string words = text.text.Length > 24 ? text.text.Substring(0, 24) : text.text;
                found.Add(string.Format(CultureInfo.InvariantCulture, "{0} '{1}' {2}{3:0}px", control ? holder.name : text.name,
                    words.Replace('\n', ' '), cut ? "cut " : "", Mathf.Max(0f, over)));
            }
            return found;
        }

        private IEnumerator RenderCensus(string times)
        {
            var at = times.Split(',').Select(t => float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : -1f)
                .Where(v => v >= 0f).OrderBy(v => v).ToList();
            foreach (float t in at)
            {
                while (Elapsed < t || Time.timeScale <= 0f) yield return null;
                yield return new WaitForEndOfFrame(); // isVisible holds the frame just drawn
                var byMaterial = new Dictionary<string, int>();
                var bySource = new Dictionary<string, int>();
                var materials = new HashSet<Material>();
                var shaders = new HashSet<Shader>();
                int visible = 0, particles = 0;
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!r.enabled || !r.isVisible || r.forceRenderingOff) continue;
                    if (r is ParticleSystemRenderer)
                    {
                        var ps = r.GetComponent<ParticleSystem>();
                        if (ps == null || ps.particleCount == 0) continue;
                        particles += ps.particleCount;
                    }
                    visible++;
                    var mat = r.sharedMaterial;
                    if (mat != null) { materials.Add(mat); if (mat.shader != null) shaders.Add(mat.shader); }
                    string kind = r.GetType().Name.Replace("Renderer", "");
                    string key = $"{kind} | {(mat != null ? mat.name : "-")} | {(mat != null && mat.shader != null ? mat.shader.name : "-")}";
                    byMaterial[key] = byMaterial.TryGetValue(key, out int c) ? c + 1 : 1;
                    string root = r.transform.root.name.Replace("(Clone)", "").Trim();
                    root = System.Text.RegularExpressions.Regex.Replace(root, @"\s*\(\d+\)$", "");
                    string src = $"{root} / {r.gameObject.name.Replace("(Clone)", "").Trim()}";
                    bySource[src] = bySource.TryGetValue(src, out int d) ? d + 1 : 1;
                }
                var sb = new StringBuilder();
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "== t {0:0} s: {1} visible renderers, {2} materials, {3} shaders, {4} particles; enemies {5}; GPU horde {6} bodies in {7} draws; enemy bullets {8} in {9} draws",
                    Elapsed, visible, materials.Count, shaders.Count, particles, EnemyController.ActiveEnemies.Count,
                    HordeVatDrawer.LastBodies, HordeVatDrawer.LastDraws,
                    SoulHunter.Gameplay.Hazards.EnemyBulletDrawer.LastBullets, SoulHunter.Gameplay.Hazards.EnemyBulletDrawer.LastDraws));
                sb.AppendLine("-- by renderer | material | shader");
                foreach (var kv in byMaterial.OrderByDescending(k => k.Value).Take(45)) sb.AppendLine($"  {kv.Value,5}  {kv.Key}");
                sb.AppendLine("-- by source (root / object)");
                foreach (var kv in bySource.OrderByDescending(k => k.Value).Take(45)) sb.AppendLine($"  {kv.Value,5}  {kv.Key}");
                if (Config != null && !string.IsNullOrEmpty(Config.OutputFolder))
                    File.AppendAllText(Path.Combine(Config.OutputFolder, "render-census.txt"), sb.ToString());
                Event("render-census", $"\"visible\": {visible}, \"materials\": {materials.Count}");
            }
        }

        private void Event(string kind, string fields)
        {
            _events.AppendLine(string.Format(CultureInfo.InvariantCulture, "{{\"t\": {0:0.0}, \"event\": \"{1}\", {2}}}", _startedAt > 0f ? Elapsed : 0f, kind, fields));
            if (Config != null && !string.IsNullOrEmpty(Config.OutputFolder))
                ReportWriter.Write(Path.Combine(Config.OutputFolder, "events.jsonl"), _events.ToString());
        }

        private void Finish(string reason)
        {
            if (Finished) return;
            Finished = true;
            Result = reason;
            StartCoroutine(FinishRoutine(reason));
        }

        private IEnumerator FinishRoutine(string reason)
        {
            _bus?.Raise(new PlayerMoveEvent(Vector2.zero));
            yield return Shot(reason == "died" ? "death" : "end");
            // -sh-autoplay-results-shot: show the VS Results screen (quitting from the pause menu at the time limit, as a
            // player would) and screenshot it, with its weapon table in the events.
            if (HasArg("-sh-autoplay-results-shot")) yield return ResultsShot(reason);
            WriteTimeline();
            var stats = RunStatsTracker.Instance;
            var progression = LevelProgressionManager.Instance;
            string summary = string.Format(CultureInfo.InvariantCulture,
                "{{\n  \"result\": \"{0}\",\n  \"survivedSeconds\": {1:0.0},\n  \"stage\": {2},\n  \"level\": {3},\n  \"kills\": {4},\n  \"gold\": {5},\n  \"maxEnemies\": {6},\n  \"minFps\": {7},\n  \"errors\": {8},\n  \"character\": \"{9}\",\n  \"screenshots\": {10}\n}}\n",
                reason, Elapsed, progression != null ? progression.CurrentStage : 0, _experience != null ? _experience.CurrentLevel : 0,
                stats != null ? stats.TotalKills : 0, stats != null ? stats.TotalGoldCollected : 0, _maxEnemies,
                _minFps == int.MaxValue ? 0 : _minFps, _errors, Escape(Config.Character), _shots.Count);
            if (_levelUpFrames > 0)
                Event("levelup-frames", string.Format(CultureInfo.InvariantCulture, "\"frames\": {0}, \"avgFps\": {1:0.0}, \"worstMs\": {2:0}",
                    _levelUpFrames, _levelUpFrames / Mathf.Max(0.0001f, _levelUpSeconds), _levelUpWorst * 1000f));
            ReportWriter.Write(Path.Combine(Config.OutputFolder, "summary.json"), summary);
            ReportWriter.Flush();
            Debug.Log($"[AutoplayBot] Finished ({reason}). Report in {Config.OutputFolder}");
            yield return new WaitForSecondsRealtime(0.5f);
            if (!Config.QuitWhenDone) yield break;
#if UNITY_EDITOR
            if (HasArg("-sh-autoplay-exit")) UnityEditor.EditorApplication.Exit(0);
            else UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }
}
