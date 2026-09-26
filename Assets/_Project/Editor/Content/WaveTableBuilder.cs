using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Writes each stage's VS minute table (WaveTables.Build) into its WaveData asset, in the order the gameplay
    /// scene's EnemySpawner lists them. Menu: Soul Hunter/Content/Build Wave Tables, or
    /// -executeMethod SoulHunter.Editor.Content.WaveTableBuilder.BuildAll -quit
    /// </summary>
    public static class WaveTableBuilder
    {
        public static string[] StageWaves => WaveTables.StageWaveAssets;

        [MenuItem("Soul Hunter/Content/Build Wave Tables")]
        public static void BuildAll()
        {
            for (int i = 0; i < StageWaves.Length; i++)
            {
                var wave = AssetDatabase.LoadAssetAtPath<WaveData>(StageWaves[i]);
                if (wave == null) throw new System.IO.FileNotFoundException("Missing stage wave", StageWaves[i]);
                wave.Minutes = WaveTables.Build(i + 1, wave.SpawnInterval);
                wave.SpawnSide = WaveTables.SideFor(i + 1);
                wave.Events = WaveTables.Events(i + 1);
                EditorUtility.SetDirty(wave);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[WaveTableBuilder] Wrote the minute tables, spawn sides and events of all 10 stages.");
        }
    }
}
