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
                // VS wave enemy types (WaveTables.EnemyTypesByStage), resolved from prefab paths.
                foreach (var minute in wave.Minutes)
                    foreach (var path in WaveTables.EnemyTypePaths(i + 1, minute.Minute))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null) throw new System.IO.FileNotFoundException("Missing wave enemy type", path);
                        if (prefab.GetComponent<SoulHunter.Gameplay.AI.EnemyController>() == null)
                            throw new System.ArgumentException($"{path} is not an enemy (no EnemyController).");
                        minute.EnemyTypes.Add(prefab);
                    }
                // Types that come only as wave bosses (StageEnemyTable.BossOnly): the spawner finds them here.
                wave.BossTypes = new System.Collections.Generic.List<GameObject>();
                var table = StageEnemyTables.For(i + 1);
                if (table != null)
                    foreach (var id in table.BossOnly)
                    {
                        string path = table.PrefabPath(id);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null) throw new System.IO.FileNotFoundException("Missing wave boss type", path);
                        wave.BossTypes.Add(prefab);
                    }
                wave.SpawnSide = WaveTables.SideFor(i + 1);
                wave.SideBias = WaveTables.SideBiasFor(i + 1);
                wave.Events = WaveTables.Events(i + 1);
                EditorUtility.SetDirty(wave);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[WaveTableBuilder] Wrote the minute tables, spawn sides and events of all 10 stages.");
        }
    }
}
