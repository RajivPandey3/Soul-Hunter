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

        [Header("Optional VS-style timed phases")]
        [Tooltip("Agar empty ho toh 5-minute pressure phases runtime par automatically use hongi.")]
        public List<WavePhase> Phases = new List<WavePhase>();
        [System.NonSerialized] private WavePhase[] _fallbackPhases;

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
                        IsBossPhase = IsBossWave
                    };
                }
            }
            int phaseIndex = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) / 300f), 0, _fallbackPhases.Length - 1);
            return _fallbackPhases[phaseIndex];
        }
    }
}
