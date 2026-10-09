using System.Collections.Generic;
using System.IO;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds the VS wave enemy types of Levels 2-10 (StageEnemyTables, release item P2-02, owner 2026-10-09): one EnemyData
    /// and one gameplay prefab variant each, made from that level's own enemy or elite gameplay prefab (existing models,
    /// recoloured and resized, as Level 1), then rebuilds the wave tables so each level's minutes list them, and lists the
    /// new types in the Bestiary (GameContent.BestiaryEnemies) in level order after Level 1's.
    /// Menu: Soul Hunter/Content/Build Level 2-10 Enemy Types, or in batch mode:
    /// Unity.exe -batchmode -nographics -projectPath &lt;copy&gt; -executeMethod SoulHunter.Editor.Content.StageEnemyTypeBuilder.BuildAll -quit -logFile &lt;log&gt;
    /// </summary>
    public static class StageEnemyTypeBuilder
    {
        public const string EnemyDataFolder = "Assets/_Project/Data/Config/Enemies";
        public const string GameplayPrefabFolder = "Assets/Prefabs/Enemies/Gameplay";
        public const string CatalogPath = "Assets/Resources/GameContent.asset";

        public static string BaseEnemyPrefab(int level) => $"{GameplayPrefabFolder}/SH10_L{level:00}_EnemyGameplay.prefab";
        public static string BaseElitePrefab(int level) => $"{GameplayPrefabFolder}/SH10_L{level:00}_EliteGameplay.prefab";

        /// <summary>The level's own enemy data (L0N_&lt;Stage&gt;_Enemy_Data.asset), the template each type copies.</summary>
        public static string BaseEnemyData(int level)
        {
            string prefix = $"L{level:00}_";
            foreach (var file in Directory.GetFiles(EnemyDataFolder, prefix + "*_Enemy_Data.asset", SearchOption.TopDirectoryOnly))
                return file.Replace('\\', '/');
            throw new FileNotFoundException($"Missing Level {level} enemy data ({prefix}*_Enemy_Data.asset)", EnemyDataFolder);
        }

        [MenuItem("Soul Hunter/Content/Build Level 2-10 Enemy Types")]
        public static void BuildAll()
        {
            int built = 0;
            foreach (var table in StageEnemyTables.All) built += BuildLevel(table);
            AssetDatabase.SaveAssets();
            WaveTableBuilder.BuildAll();
            UpdateBestiary();
            AssetDatabase.SaveAssets();
            Debug.Log($"[StageEnemyTypeBuilder] Built {built} enemy types for Levels 2-10, the wave tables and the Bestiary list.");
        }

        public static int BuildLevel(StageEnemyTable table)
        {
            Directory.CreateDirectory(table.DataFolder);
            Directory.CreateDirectory(table.PrefabFolder);
            AssetDatabase.Refresh();
            string templatePath = BaseEnemyData(table.Level);
            var template = AssetDatabase.LoadAssetAtPath<EnemyData>(templatePath);
            if (template == null) throw new FileNotFoundException($"Missing Level {table.Level} enemy data", templatePath);
            foreach (var type in table.Types)
            {
                var data = WriteData(table, type, template);
                WritePrefab(table, type, data);
            }
            return table.Types.Length;
        }

        private static EnemyData WriteData(StageEnemyTable table, StageEnemyType type, EnemyData template)
        {
            string path = table.DataPath(type.Id);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                data = Object.Instantiate(template);
                AssetDatabase.CreateAsset(data, path);
            }
            data.EnemyName = type.Name;
            data.MaxHealth = type.MaxHealth;
            data.HealthPerPlayerLevel = type.HealthPerLevelWhole;
            data.HealthPerPlayerLevelFraction = type.HealthPerLevelFraction;
            data.DamageToPlayer = type.Damage;
            data.MoveSpeed = type.MoveSpeed;
            data.Knockback = table.KnockbackOf(type);
            data.MaxKnockback = type.MaxKnockback;
            data.ExperienceValue = type.ExperienceValue;
            data.Movement = table.MovementOf(type);
            data.DropsChest = false;
            data.FreezeResistance = type.FreezeResistance;
            data.InstantKillResistance = type.InstantKillResistance;
            data.DebuffResistance = type.DebuffResistance;
            data.DefangResistance = type.DefangResistance;
            data.Lives = Mathf.Max(1, type.Lives);
            data.Abilities.Clear();
            if (type.Shoots)
                data.Abilities.Add(new EnemyAbilitySpec(EnemyAbilityKind.Shoot)
                {
                    IntervalSeconds = type.ShootInterval,
                    Speed = type.ShootVsSpeed * EnemyAbilityDefaults.VsSpeedToUnits,
                    Damage = type.ShootDamage,
                    Count = 1,
                });
            data.VisualTint = type.Tint;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void WritePrefab(StageEnemyTable table, StageEnemyType type, EnemyData data)
        {
            string basePath = table.EliteModelOf(type) ? BaseElitePrefab(table.Level) : BaseEnemyPrefab(table.Level);
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null) throw new FileNotFoundException($"Missing Level {table.Level} gameplay prefab", basePath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                instance.name = table.PrefabName(type.Id);
                instance.transform.localScale = basePrefab.transform.localScale * type.Scale;
                var controller = instance.GetComponent<EnemyController>();
                if (controller == null) throw new System.ArgumentException($"{basePath} has no EnemyController.");
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("_enemyData").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(instance, table.PrefabPath(type.Id));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        /// <summary>
        /// The Bestiary order: Level 1's VS types, then each later level's VS types in level order, then the entries that
        /// were there before which are none of those (each level's enemy, elite and boss), in their old order.
        /// </summary>
        public static void UpdateBestiary()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            if (catalog == null) throw new FileNotFoundException("Missing GameContent", CatalogPath);
            var ordered = new List<EnemyData>();
            foreach (var type in MadForestEnemies.Types)
                Add(ordered, AssetDatabase.LoadAssetAtPath<EnemyData>(MadForestEnemies.DataPath(type.Id)), MadForestEnemies.DataPath(type.Id));
            foreach (var table in StageEnemyTables.All)
                foreach (var type in table.Types)
                    Add(ordered, AssetDatabase.LoadAssetAtPath<EnemyData>(table.DataPath(type.Id)), table.DataPath(type.Id));
            var types = new HashSet<EnemyData>(ordered);
            foreach (var old in catalog.BestiaryEnemies ?? new EnemyData[0])
            {
                if (old == null || types.Contains(old)) continue;
                string path = AssetDatabase.GetAssetPath(old);
                if (path.Contains("/MadForest/") || StageEnemyTables.IsTypeAsset(path)) continue;
                if (!ordered.Contains(old)) ordered.Add(old);
            }
            catalog.BestiaryEnemies = ordered.ToArray();
            EditorUtility.SetDirty(catalog);
        }

        private static void Add(List<EnemyData> list, EnemyData data, string path)
        {
            if (data == null) throw new FileNotFoundException("Missing enemy type data (build it first)", path);
            if (!list.Contains(data)) list.Add(data);
        }
    }
}
