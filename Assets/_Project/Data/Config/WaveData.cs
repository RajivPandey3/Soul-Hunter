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

    [System.Serializable]
    public sealed class StageEventEntry
    {
        [Tooltip("Stage time (seconds) when the event starts. Events at or after the boss time never run.")]
        public float TimeSeconds;
        public StageEventType Type;
        [Min(1)] public int Count = 16;
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
