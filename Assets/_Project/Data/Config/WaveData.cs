using UnityEngine;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.Data
{
    [System.Serializable]
    public sealed class WavePhase
    {
        public string PhaseName = "Opening";
        public float StartTimeInSeconds;
        public float SpawnIntervalMultiplier = 1f;
        public int EnemiesPerSpawnBonus;
        [Range(0f, 1f)] public float EliteChanceBonus;
        public GameObject EnemyPrefab;
        public bool IsBossPhase;
        [Tooltip("VS enemy minimum: the swarm is topped up straight away whenever fewer enemies are alive (0 = off)")]
        [Min(0)] public int MinimumEnemies;
        [Tooltip("VS wave enemy types (from the minute table). Empty: the stage's own enemy, with the elite chance.")]
        public List<GameObject> EnemyTypes = new List<GameObject>();
    }

    /// <summary>VS-style scripted formations. Event enemies ignore the player and fly a fixed path.</summary>
    public enum StageEventType
    {
        /// <summary>A tight cluster flies across the player's position from one side.</summary>
        Swarm,
        /// <summary>A line of enemies, side by side, sweeps across the player's position.</summary>
        Wall,
        /// <summary>A circle around the player flies inward, closes, and opens out the other side.</summary>
        ClosingRing
    }

    /// <summary>
    /// VS wave (vampire.survivors.wiki, Enemies): one per minute, with its own enemy minimum and spawn interval. The
    /// minimum rises and falls through a stage (Mad Forest drops from 40 to 10 at 5:00 but sends tougher enemies).
    /// </summary>
    [System.Serializable]
    public sealed class MinuteWave
    {
        public int Minute;
        [Min(0)] public int Minimum;
        [Tooltip("Seconds between spawn ticks during this minute (before the level multiplier and Curse)")]
        [Min(0.02f)] public float Interval = 1f; // VS Cappella Magna 21:00 and 26:00: 0.02 s
        [Tooltip("Extra elite chance this minute: a quiet minute with tougher enemies")]
        [Range(0f, 1f)] public float EliteBonus;
        [Tooltip("VS wiki (Enemies, Waves): the enemy types of this minute's wave. Below the minimum they fill the quota in " +
                 "turn; above it, each spawn tick sends one of each type. Any enemy gameplay prefab (EnemyController + " +
                 "EnemyData) works. Empty: the stage's own enemy, with the elite chance (as before).")]
        public List<GameObject> EnemyTypes = new List<GameObject>();
    }

    /// <summary>
    /// Where a stage's enemies come from (VS: Mad Forest from all four sides, Inlaid Library only from the west and
    /// east ends, Gallo Tower mostly from above and below). On screen, left/right is the world X axis and up/down Z.
    /// </summary>
    public enum SpawnSide { AllSides = 0, LeftRight = 1, TopBottom = 2 }

    [System.Serializable]
    public sealed class StageEventEntry
    {
        [Tooltip("Stage time (seconds) when the event starts. Events run during the boss fight too (VS).")]
        public float TimeSeconds;
        public StageEventType Type;
        [Min(1)] public int Count = 16;
        [Tooltip("Flight speed relative to the default event speed (1 = 1.5x the enemy's speed). VS swarms are fast.")]
        [Min(0.2f)] public float Speed = 1f;
        [Tooltip("VS wave events: how many times the event fires in a row (1 = once).")]
        [Min(1)] public int Repeat = 1;
        [Tooltip("VS wave events: seconds between repeats.")]
        [Min(0.1f)] public float RepeatDelaySeconds = 5f;
        [Tooltip("VS wave events: % chance each time it fires (100 = always); divided by the player's Luck.")]
        [Range(0f, 100f)] public float ChancePercent = 100f;
    }

    /// <summary>
    /// Learning Comment:
    /// Kami #1 Fix: Wave System (Vampire Survivors).
    /// Har wave ka apna ek time hota hai (jaise 60 seconds par naye dushman).
    /// Is Data file se Spawner ko pata chalega ki kis time par kaunsa dushman spawn karna hai.
    /// </summary>
    [CreateAssetMenu(fileName = "New Wave Data", menuName = "Soul Hunter/Data/Wave Data")]
    public class WaveData : ScriptableObject
    {
        public string WaveName = "Wave 1";
        [Tooltip("Game shuru hone ke kitne second baad ye wave aayegi")]
        public float StartTimeInSeconds = 0f;
        
        [Tooltip("Kaunsa dushman aayega (Jaise Bat ya Zombie ka prefab)")]
        public GameObject EnemyPrefab;
        
        [Tooltip("Kitne interval me spawn honge")]
        public float SpawnInterval = 1f;
        
        [Tooltip("Ek baar me kitne spawn honge (Horde size)")]
        public int EnemiesPerSpawn = 1;
        
        [Tooltip("Kya ye ek Boss hai? (Agar haan, toh ye sirf ek baar spawn hoga)")]
        public bool IsBossWave = false;

        [Header("Enemy minimum (automatic phases)")]
        [Tooltip("Enemies kept alive during the first automatic 5-minute phase. Provisional balance value.")]
        [Min(0)] public int MinimumEnemies = 30;
        [Tooltip("Added to the minimum for each later automatic phase. Provisional balance value.")]
        [Min(0)] public int MinimumEnemiesPerPhase = 20;

        [Header("Where enemies come from (built by WaveTableBuilder)")]
        public SpawnSide SpawnSide = SpawnSide.AllSides;
        [Tooltip("For LeftRight/TopBottom: share of spawns from those sides (the rest from anywhere). Provisional.")]
        [Range(0f, 1f)] public float SideBias = 0.8f;

        [Header("VS minute table (built by WaveTableBuilder)")]
        [Tooltip("One entry per minute; used when there are no authored Phases. Empty: the automatic 5-minute phases.")]
        public List<MinuteWave> Minutes = new List<MinuteWave>();
        [System.NonSerialized] private WavePhase[] _minutePhases;

        public bool HasMinuteTable => (Phases == null || Phases.Count == 0) && Minutes != null && Minutes.Count > 0;

        [Header("Optional VS-style timed phases")]
        [Tooltip("Agar empty ho toh 5-minute pressure phases runtime par automatically use hongi.")]
        public List<WavePhase> Phases = new List<WavePhase>();
        [System.NonSerialized] private WavePhase[] _fallbackPhases;

        [Header("Stage events")]
        [Tooltip("Scripted formations. If empty, a default schedule runs at 2:30, 7:30, 12:30, 17:30 and 22:30.")]
        public List<StageEventEntry> Events = new List<StageEventEntry>();
        [System.NonSerialized] private List<StageEventEntry> _fallbackEvents;

        /// <summary>Events in time order; the default schedule when none are authored. Provisional balance.</summary>
        public IReadOnlyList<StageEventEntry> GetEvents()
        {
            if (Events != null && Events.Count > 0)
            {
                var sorted = new List<StageEventEntry>(Events);
                sorted.RemoveAll(e => e == null);
                sorted.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
                return sorted;
            }
            // Between the 5-minute chest elites, cycling through every formation.
            return _fallbackEvents ??= new List<StageEventEntry>
            {
                new StageEventEntry { TimeSeconds = 150f, Type = StageEventType.Swarm, Count = 20 },
                new StageEventEntry { TimeSeconds = 450f, Type = StageEventType.Wall, Count = 16 },
                new StageEventEntry { TimeSeconds = 750f, Type = StageEventType.ClosingRing, Count = 16 },
                new StageEventEntry { TimeSeconds = 1050f, Type = StageEventType.Swarm, Count = 24 },
                new StageEventEntry { TimeSeconds = 1350f, Type = StageEventType.Wall, Count = 20 }
            };
        }

        /// <summary>The current minute's wave as a phase: its minimum, its interval (as a multiplier of SpawnInterval) and elite bonus.</summary>
        private WavePhase MinutePhase(float elapsedSeconds)
        {
            if (_minutePhases == null || _minutePhases.Length != Minutes.Count)
            {
                var sorted = new List<MinuteWave>(Minutes);
                sorted.RemoveAll(m => m == null);
                sorted.Sort((a, b) => a.Minute.CompareTo(b.Minute));
                Minutes = sorted;
                _minutePhases = new WavePhase[sorted.Count];
                for (int i = 0; i < sorted.Count; i++)
                {
                    var m = sorted[i];
                    _minutePhases[i] = new WavePhase
                    {
                        PhaseName = $"Minute {m.Minute}",
                        StartTimeInSeconds = m.Minute * 60f,
                        SpawnIntervalMultiplier = m.Interval / Mathf.Max(0.05f, SpawnInterval),
                        EnemiesPerSpawnBonus = 0,
                        EliteChanceBonus = m.EliteBonus,
                        EnemyPrefab = EnemyPrefab,
                        IsBossPhase = IsBossWave,
                        MinimumEnemies = m.Minimum,
                        EnemyTypes = m.EnemyTypes != null ? m.EnemyTypes.FindAll(t => t != null) : new List<GameObject>()
                    };
                }
            }
            int minute = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) / 60f);
            int index = 0;
            for (int i = 0; i < Minutes.Count; i++) if (Minutes[i].Minute <= minute) index = i;
            return _minutePhases[index];
        }

        public WavePhase GetPhase(float elapsedSeconds)
        {
            WavePhase selected = null;
            if (Phases != null)
            {
                for (int i = 0; i < Phases.Count; i++)
                {
                    var phase = Phases[i];
                    if (phase != null && elapsedSeconds >= phase.StartTimeInSeconds &&
                        (selected == null || phase.StartTimeInSeconds > selected.StartTimeInSeconds))
                        selected = phase;
                }
            }
            if (selected != null) return selected;
            if (HasMinuteTable) return MinutePhase(elapsedSeconds);

            // Canonical fallback phases keep existing WaveData assets playable
            // while matching the escalating 30-minute survival rhythm.
            if (_fallbackPhases == null)
            {
                _fallbackPhases = new WavePhase[5];
                for (int i = 0; i < _fallbackPhases.Length; i++)
                {
                    _fallbackPhases[i] = new WavePhase
                    {
                        PhaseName = $"Auto Phase {i + 1}",
                        StartTimeInSeconds = i * 300f,
                        SpawnIntervalMultiplier = Mathf.Max(0.45f, 1f - i * 0.12f),
                        EnemiesPerSpawnBonus = i,
                        EliteChanceBonus = i * 0.04f,
                        EnemyPrefab = EnemyPrefab,
                        IsBossPhase = IsBossWave,
                        MinimumEnemies = MinimumEnemies + i * MinimumEnemiesPerPhase
                    };
                }
            }
            int phaseIndex = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) / 300f), 0, _fallbackPhases.Length - 1);
            return _fallbackPhases[phaseIndex];
        }
    }
}
