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
    ///   -sh-autoplay-god                       invincible, for long visual tours
    ///   -sh-autoplay-gold N                    start with N gold (on the throw-away save), e.g. to try the merchant
    ///   -sh-autoplay-boss-after S              after S seconds of play, skip the stage clock to its boss (e.g. Shadow Kael)
    ///   -sh-autoplay-skip-to T                 move the stage clock to T seconds (e.g. 290, just before the first chest elite)
    ///   -sh-autoplay-skip-after S (default 10) seconds of play before -sh-autoplay-skip-to applies
    ///   -sh-autoplay-jump-to N                 jump the run to stage N, as the Debug "Jump To Stage" menu does
    ///   -sh-autoplay-jump-after S (default 20) seconds of play before -sh-autoplay-jump-to applies
    ///   -sh-autoplay-start-level N             start at player level N (the bot picks every level-up), to measure later stages
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

        public static string ArgValue(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        public static bool HasArg(string name) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), name) >= 0;

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
            Directory.CreateDirectory(Path.Combine(Config.OutputFolder, "shots"));
            while (SceneManager.GetActiveScene().name != "MainMenu") yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            var save = GameServices.Instance.Get<SaveService>();
            // A -sh-autoplay launch already uses a throw-away save. Launched any other way (editor, tests) the bot
            // redirects saving to a temp file first: gold pickups save immediately and would change real progress.
            if (!HasArg(Argument)) save.UseFile(Path.Combine(Application.temporaryCachePath, "autoplay_save.json"));
            save.CurrentData.SelectedCharacterName = Config.Character;
            if (Config.Gold >= 0) save.CurrentData.Currency = Config.Gold; // the save is a throw-away one by now
            GameServices.Instance.Get<SceneService>().LoadSceneAsync("Main_Gameplay");
            while (SceneManager.GetActiveScene().name != "Main_Gameplay" || PlayerController.Instance == null ||
                   LevelProgressionManager.Instance == null) yield return null;
            yield return new WaitForSecondsRealtime(1f);

            _player = PlayerController.Instance;
            _health = _player.GetComponent<HealthController>();
            _experience = _player.GetComponent<PlayerExperience>();
            _bus = GameServices.Instance.Get<EventBus>();
            if (Config.God) _health.IsInvincible = true;
            if (Config.Stage > 1) LevelProgressionManager.Instance.StartStage(Config.Stage);

            var levelUp = FindFirstObjectByType<LevelUpManager>();
            if (levelUp != null) levelUp.OnLevelUpUIRound += choices => StartCoroutine(ChooseUpgrade(levelUp, choices));
            LevelProgressionManager.Instance.OnStageStarted += stage => { Event("stage", $"\"stage\": {stage}"); StartCoroutine(ShotSoon($"stage{stage:00}")); };
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.OnGameOver += () => Finish("died");
                GameSessionManager.Instance.OnVictory += () => Finish("victory");
            }
            _health.OnDamaged += OnHurt;

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
            var gem = Nearest(XPGem.ActiveGems.Where(g => g != null && g.isActiveAndEnabled).Select(g => g.transform), me, 12f);
            if (gem != null) pull += Flat(gem.position - me).normalized * (near > 8 ? 0.8f : 1.6f);
            var chest = Nearest(FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Select(c => c.transform), me, 25f);
            if (chest != null) pull += Flat(chest.position - me).normalized * 2f;
            // Walk up to the Level 3 merchant while it is there, as a player would.
            var merchant = Nearest(FindObjectsByType<WanderingMerchant>(FindObjectsSortMode.None).Where(m => !m.HasLeft).Select(m => m.transform), me, 30f);
            if (merchant != null) pull += Flat(merchant.position - me).normalized * 3f;
            if (_health.CurrentHealth < _health.MaxHealth * 0.5f)
            {
                var food = Nearest(FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Select(c => c.transform), me, 25f);
                if (food != null) pull += Flat(food.position - me).normalized * 2.5f;
            }

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
            if (_experience.CurrentLevel % 5 == 0) yield return Shot($"levelup{_experience.CurrentLevel:00}");
            Event("levelup", $"\"level\": {_experience.CurrentLevel}, \"choices\": [{string.Join(", ", choices.Select(c => $"\"{Escape(c.UpgradeName)}\""))}], \"picked\": \"{Escape(pick.UpgradeName)}\"");
            // Pick through the level-up screen like a player, so the panel closes (calling SelectUpgrade directly left it
            // on screen after the last level-up).
            var ui = FindFirstObjectByType<LevelUpUI>();
            var select = typeof(LevelUpUI).GetMethod("OnUpgradeSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ui != null && select != null) select.Invoke(ui, new object[] { pick });
            else levelUp.SelectUpgrade(pick);
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
                if (Config.BossAfterSeconds >= 0f && !_bossSkipped && Elapsed >= Config.BossAfterSeconds)
                {
                    _bossSkipped = true;
                    LevelProgressionManager.Instance.SkipToBoss();
                    Event("boss-skip", $"\"stage\": {LevelProgressionManager.Instance.CurrentStage}");
                }
                var kael = FindFirstObjectByType<SoulHunter.Gameplay.AI.ShadowKaelController>();
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
                foreach (var chest in FindObjectsByType<ChestUI>(FindObjectsSortMode.None))
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
                foreach (var merchant in FindObjectsByType<WanderingMerchant>(FindObjectsSortMode.None))
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
        private IEnumerator Shot(string name)
        {
            string file = Path.Combine(Config.OutputFolder, "shots", $"{_shots.Count:000}_{name}.png");
            _shots.Add(Path.GetFileName(file));
            // No WaitForEndOfFrame: it never resumes in a batchmode editor, which hung the report.
            // CaptureScreenshot already captures at the end of the frame (a player, or the editor's Game view, with UI).
            if (!Application.isBatchMode)
            {
                ScreenCapture.CaptureScreenshot(file);
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

        private int RecentFps()
        {
            if (_frameTimes.Count == 0) return 0;
            float avg = _frameTimes.Average();
            _frameTimes.Clear();
            return Mathf.RoundToInt(1f / Mathf.Max(avg, 0.0001f));
        }

        private void WriteTimeline()
        {
            var progression = LevelProgressionManager.Instance;
            int fps = RecentFps();
            if (fps > 0 && Elapsed > 5f) _minFps = Mathf.Min(_minFps, fps);
            var weapons = WeaponManager.Instance != null ? WeaponManager.Instance.GetActiveWeaponTypes().Select(t => $"\"{t}:{WeaponManager.Instance.GetWeaponLevel(t)}\"") : Enumerable.Empty<string>();
            var stats = RunStatsTracker.Instance;
            _timeline.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{{\"t\": {0:0.0}, \"stage\": {1}, \"stageTime\": {2:0}, \"level\": {3}, \"hp\": {4}, \"maxHp\": {5}, \"kills\": {6}, \"gold\": {7}, \"enemies\": {8}, \"fps\": {9}, \"paused\": {10}, \"weapons\": [{11}], \"gems\": {12}}}",
                Elapsed, progression != null ? progression.CurrentStage : 0, progression != null ? progression.StageElapsedTime : 0f,
                _experience != null ? _experience.CurrentLevel : 0, _health != null ? _health.CurrentHealth : 0, _health != null ? _health.MaxHealth : 0,
                stats != null ? stats.TotalKills : 0, stats != null ? stats.TotalGoldCollected : 0, EnemyController.ActiveEnemies.Count, fps,
                GameTime.IsPaused ? "true" : "false", string.Join(", ", weapons), XPGem.ActiveGems.Count));
            File.WriteAllText(Path.Combine(Config.OutputFolder, "timeline.jsonl"), _timeline.ToString());
        }

        private void Event(string kind, string fields)
        {
            _events.AppendLine(string.Format(CultureInfo.InvariantCulture, "{{\"t\": {0:0.0}, \"event\": \"{1}\", {2}}}", _startedAt > 0f ? Elapsed : 0f, kind, fields));
            if (Config != null && !string.IsNullOrEmpty(Config.OutputFolder))
                File.WriteAllText(Path.Combine(Config.OutputFolder, "events.jsonl"), _events.ToString());
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
            WriteTimeline();
            var stats = RunStatsTracker.Instance;
            var progression = LevelProgressionManager.Instance;
            string summary = string.Format(CultureInfo.InvariantCulture,
                "{{\n  \"result\": \"{0}\",\n  \"survivedSeconds\": {1:0.0},\n  \"stage\": {2},\n  \"level\": {3},\n  \"kills\": {4},\n  \"gold\": {5},\n  \"maxEnemies\": {6},\n  \"minFps\": {7},\n  \"errors\": {8},\n  \"character\": \"{9}\",\n  \"screenshots\": {10}\n}}\n",
                reason, Elapsed, progression != null ? progression.CurrentStage : 0, _experience != null ? _experience.CurrentLevel : 0,
                stats != null ? stats.TotalKills : 0, stats != null ? stats.TotalGoldCollected : 0, _maxEnemies,
                _minFps == int.MaxValue ? 0 : _minFps, _errors, Escape(Config.Character), _shots.Count);
            File.WriteAllText(Path.Combine(Config.OutputFolder, "summary.json"), summary);
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
