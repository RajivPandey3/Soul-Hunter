using System.Collections.Generic;
using System.IO;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds the guardians of the hidden stage items (ItemGuardians): one EnemyData each (the four share the VS
    /// statbox) and one gameplay prefab variant per level and guardian, made from that level's elite gameplay prefab
    /// (existing models, recoloured and resized, as the wave types). Lists the prefabs in GameContent.ItemGuards and the
    /// four types at the end of the Bestiary.
    /// Menu: Soul Hunter/Content/Build Item Guardians, or in batch mode:
    /// Unity.exe -batchmode -nographics -projectPath &lt;copy&gt; -executeMethod SoulHunter.Editor.Content.ItemGuardianBuilder.BuildAll -quit -logFile &lt;log&gt;
    /// </summary>
    public static class ItemGuardianBuilder
    {
        [MenuItem("Soul Hunter/Content/Build Item Guardians")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(ItemGuardians.DataFolder);
            Directory.CreateDirectory(ItemGuardians.PrefabFolder);
            AssetDatabase.Refresh();
            var template = AssetDatabase.LoadAssetAtPath<EnemyData>(StageEnemyTypeBuilder.BaseEnemyData(1));
            if (template == null) throw new FileNotFoundException("Missing Level 1 enemy data (the template)");

            var prefabs = new List<GameObject>();
            var datas = new List<EnemyData>();
            foreach (var guard in ItemGuardians.All)
            {
                var data = WriteData(guard, template);
                datas.Add(data);
                for (int level = 1; level <= 10; level++) prefabs.Add(WritePrefab(guard, level, data));
            }
            AssetDatabase.SaveAssets();

            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(StageEnemyTypeBuilder.CatalogPath);
            if (catalog == null) throw new FileNotFoundException("Missing GameContent", StageEnemyTypeBuilder.CatalogPath);
            catalog.ItemGuards = prefabs.ToArray();
            var bestiary = new List<EnemyData>(catalog.BestiaryEnemies ?? new EnemyData[0]);
            foreach (var data in datas) if (!bestiary.Contains(data)) bestiary.Add(data);
            catalog.BestiaryEnemies = bestiary.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ItemGuardianBuilder] Built {datas.Count} guardians and {prefabs.Count} prefabs, GameContent.ItemGuards and the Bestiary list.");
        }

        private static EnemyData WriteData(ItemGuardians.Guard guard, EnemyData template)
        {
            string path = ItemGuardians.DataPath(guard.Id);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                data = Object.Instantiate(template);
                AssetDatabase.CreateAsset(data, path);
            }
            data.EnemyName = guard.Name;
            data.MaxHealth = ItemGuardians.Health;
            data.HealthPerPlayerLevel = 0;
            data.HealthPerPlayerLevelFraction = 0f;
            data.DamageToPlayer = ItemGuardians.Damage;
            data.MoveSpeed = ItemGuardians.VsMoveSpeed * EnemyAbilityDefaults.VsSpeedToUnits;
            data.Knockback = ItemGuardians.Knockback;
            data.MaxKnockback = ItemGuardians.MaxKnockback;
            data.ExperienceValue = ItemGuardians.Experience;
            data.Movement = EnemyMovement.Homing;
            data.DropsChest = false;
            data.FreezeResistance = ItemGuardians.FreezeResistance;
            data.InstantKillResistance = ItemGuardians.InstantKillResistance;
            data.DebuffResistance = ItemGuardians.DebuffResistance;
            data.DefangResistance = true; // never an ordinary wave spawn
            data.Lives = 1;
            data.Abilities.Clear();
            data.VisualTint = guard.Tint;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject WritePrefab(ItemGuardians.Guard guard, int level, EnemyData data)
        {
            string basePath = StageEnemyTypeBuilder.BaseElitePrefab(level);
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null) throw new FileNotFoundException($"Missing Level {level} elite gameplay prefab", basePath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                instance.name = ItemGuardians.PrefabName(level, guard.Id);
                instance.transform.localScale = basePrefab.transform.localScale * ItemGuardians.Scale;
                var controller = instance.GetComponent<EnemyController>();
                if (controller == null) throw new System.ArgumentException($"{basePath} has no EnemyController.");
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("_enemyData").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(instance, ItemGuardians.PrefabPath(level, guard.Id));
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
