using System.IO;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds Level 1's VS Mad Forest enemy types (MadForestEnemies): one EnemyData and one gameplay prefab variant each,
    /// made from the Level 1 enemy or elite gameplay prefab (owner, 2026-10-03: existing models, recoloured and resized),
    /// then rebuilds the wave tables so Level 1's minutes list them. Menu: Soul Hunter/Content/Build Level 1 Enemy Types,
    /// or -executeMethod SoulHunter.Editor.Content.MadForestEnemyBuilder.BuildAll -quit
    /// </summary>
    public static class MadForestEnemyBuilder
    {
        public const string BaseEnemyPrefab = "Assets/Prefabs/Enemies/Gameplay/SH10_L01_EnemyGameplay.prefab";
        public const string BaseElitePrefab = "Assets/Prefabs/Enemies/Gameplay/SH10_L01_EliteGameplay.prefab";
        public const string BaseEnemyData = "Assets/_Project/Data/Config/Enemies/L01_CursedGraveyard_Enemy_Data.asset";

        [MenuItem("Soul Hunter/Content/Build Level 1 Enemy Types")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(MadForestEnemies.DataFolder);
            Directory.CreateDirectory(MadForestEnemies.PrefabFolder);
            AssetDatabase.Refresh();
            var template = AssetDatabase.LoadAssetAtPath<EnemyData>(BaseEnemyData);
            if (template == null) throw new FileNotFoundException("Missing Level 1 enemy data", BaseEnemyData);

            foreach (var type in MadForestEnemies.Types)
            {
                var data = WriteData(type, template);
                WritePrefab(type, data);
            }
            AssetDatabase.SaveAssets();
            WaveTableBuilder.BuildAll();
            Debug.Log($"[MadForestEnemyBuilder] Built {MadForestEnemies.Types.Length} Level 1 enemy types and the wave tables.");
        }

        private static EnemyData WriteData(MadForestEnemies.EnemyType type, EnemyData template)
        {
            string path = MadForestEnemies.DataPath(type.Id);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                data = Object.Instantiate(template);
                AssetDatabase.CreateAsset(data, path);
            }
            data.EnemyName = type.Name;
            data.MaxHealth = type.Health;
            data.HealthPerPlayerLevel = type.HealthTimesLevel ? type.Health : 0;
            data.DamageToPlayer = type.Damage;
            data.MoveSpeed = type.MoveSpeed;
            data.Knockback = type.Knockback;
            data.MaxKnockback = type.MaxKnockback;
            data.ExperienceValue = type.ExperienceValue;
            data.Movement = EnemyMovement.Homing; // the wiki gives these variants no movement skill
            data.DropsChest = false;
            data.FreezeResistance = type.FreezeResistance;
            data.InstantKillResistance = type.InstantKillResistance;
            data.DebuffResistance = type.DebuffResistance;
            data.Abilities.Clear();
            data.VisualTint = type.Tint;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void WritePrefab(MadForestEnemies.EnemyType type, EnemyData data)
        {
            string basePath = type.EliteModel ? BaseElitePrefab : BaseEnemyPrefab;
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null) throw new FileNotFoundException("Missing Level 1 gameplay prefab", basePath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                instance.name = $"SH10_L01_{type.Id}_Gameplay";
                instance.transform.localScale = basePrefab.transform.localScale * type.Scale;
                var controller = instance.GetComponent<EnemyController>();
                if (controller == null) throw new System.ArgumentException($"{basePath} has no EnemyController.");
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("_enemyData").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(instance, MadForestEnemies.PrefabPath(type.Id));
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
