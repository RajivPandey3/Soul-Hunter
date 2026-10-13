using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Names in GameContent the enemy types three Omens use on every level (2026-10-13): Nightfall's bats and Rootbound's
    /// plants (VS Random Events "Summon Night" and "Summon II Molise") and the Thorn Circle's member (VS "Flower Wall").
    /// They are Level 1's own types (MadForestEnemies: Grave Bat, Red-Eye Bat, Blood-Eye Bat; Grave Lily, Thorn Wall),
    /// which already carry the wiki's statboxes, so no new asset is made.
    /// Menu: Soul Hunter/Content/Build Omen Content, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.OmenContentBuilder.BuildFromCommandLine -quit
    /// </summary>
    public static class OmenContentBuilder
    {
        public const string TypeFolder = "Assets/Prefabs/Enemies/Gameplay/MadForest";
        public static readonly string[] Bats = { "GraveBat", "RedEyeBat", "BloodEyeBat" };
        public static readonly string[] Plants = { "GraveLily", "ThornWall" };
        public const string ThornWall = "ThornWall";

        public static string PathOf(string type) => $"{TypeFolder}/SH10_L01_{type}_Gameplay.prefab";

        [MenuItem("Soul Hunter/Content/Build Omen Content")]
        public static void BuildFromMenu() => Build();

        public static void BuildFromCommandLine()
        {
            try { Build(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        public static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(StageEnemyTypeBuilder.CatalogPath);
            if (catalog == null) throw new FileNotFoundException("Missing GameContent", StageEnemyTypeBuilder.CatalogPath);
            catalog.OmenBats = Bats.Select(Load).ToArray();
            catalog.OmenPlants = Plants.Select(Load).ToArray();
            catalog.OmenThornWall = Load(ThornWall);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[OmenContentBuilder] GameContent: {catalog.OmenBats.Length} bats, {catalog.OmenPlants.Length} plants and the Thorn Wall.");
        }

        private static GameObject Load(string type)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(type));
            if (prefab == null) throw new FileNotFoundException("Missing Level 1 enemy type", PathOf(type));
            if (prefab.GetComponent<EnemyController>() == null) throw new InvalidOperationException($"{PathOf(type)} is not an enemy.");
            return prefab;
        }
    }
}
