using System;
using System.Collections.Generic;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds the VS evolutions Soul Hunter lacked (Level 1 plan, step 3; owner names and "approved VFX first",
    /// 2026-10-01). For each one it writes the evolved weapon prefab (Assets/Prefabs/Weapons/Generated/&lt;Name&gt;.prefab)
    /// from its base weapon's own projectiles and effects, its WeaponEvolutionData (Assets/_Project/Data/Weapons/Evo_&lt;Name&gt;),
    /// and lists it in GameContentCatalog.Evolutions, which WeaponManager adds to the scene's evolutions.
    /// Menu: Soul Hunter/Content/Build Evolutions, or -executeMethod SoulHunter.Editor.Content.EvolutionBuilder.BuildAll -quit
    /// </summary>
    public static class EvolutionBuilder
    {
        public const string PrefabFolder = "Assets/Prefabs/Weapons/Generated";
        public const string DataFolder = "Assets/_Project/Data/Weapons";

        /// <summary>One evolution: its name, recipe, script, and how its prefab takes the base weapon's assets.</summary>
        public sealed class Recipe
        {
            public string Name;
            public UpgradeData.UpgradeType BaseWeapon;
            public UpgradeData.UpgradeType Passive;
            public string BasePrefabPath;
            public Type Script;
            /// <summary>Copies what the evolved weapon needs from the base weapon's component.</summary>
            public Action<Component, Component> Wire;
        }

        public static readonly List<Recipe> Recipes = new List<Recipe>
        {
            new Recipe
            {
                Name = "HellmawComet", BaseWeapon = UpgradeData.UpgradeType.FireWand, Passive = UpgradeData.UpgradeType.Spinach,
                BasePrefabPath = PrefabFolder + "/FireWand.prefab", Script = typeof(HellmawCometWeapon),
                Wire = (from, to) => ((HellmawCometWeapon)to).MeteorPrefab = ((FireWandWeapon)from).FireballPrefab,
            },
            new Recipe
            {
                Name = "DrowningChalice", BaseWeapon = UpgradeData.UpgradeType.SantaWater, Passive = UpgradeData.UpgradeType.Attractorb,
                BasePrefabPath = PrefabFolder + "/SantaWater.prefab", Script = typeof(DrowningChaliceWeapon),
                Wire = (from, to) => ((DrowningChaliceWeapon)to).WaterZonePrefab = ((SantaWaterWeapon)from).WaterZonePrefab,
            },
            new Recipe
            {
                Name = "SeveredFate", BaseWeapon = UpgradeData.UpgradeType.Runetracer, Passive = UpgradeData.UpgradeType.Armor,
                BasePrefabPath = PrefabFolder + "/Runetracer.prefab", Script = typeof(SeveredFateWeapon),
                Wire = (from, to) => ((SeveredFateWeapon)to).LaserPrefab = ((RunetracerWeapon)from).RunePrefab,
            },
            new Recipe
            {
                Name = "StormboundHalo", BaseWeapon = UpgradeData.UpgradeType.LightningRing, Passive = UpgradeData.UpgradeType.Duplicator,
                BasePrefabPath = PrefabFolder + "/LightningRing.prefab", Script = typeof(StormboundHaloWeapon),
                Wire = (from, to) => ((StormboundHaloWeapon)to).LightningStrikePrefab = ((LightningRingWeapon)from).LightningStrikePrefab,
            },
            new Recipe
            {
                Name = "EclipseSigil", BaseWeapon = UpgradeData.UpgradeType.Pentagram, Passive = UpgradeData.UpgradeType.Crown,
                BasePrefabPath = PrefabFolder + "/Pentagram.prefab", Script = typeof(EclipseSigilWeapon),
                Wire = (from, to) => ((EclipseSigilWeapon)to).GlyphPrefab = ((PentagramWeapon)from).PentagramVFXPrefab,
            },
            new Recipe
            {
                Name = "RequiemOfSouls", BaseWeapon = UpgradeData.UpgradeType.SongOfMana, Passive = UpgradeData.UpgradeType.SkullOManiac,
                BasePrefabPath = PrefabFolder + "/SongOfMana.prefab", Script = typeof(RequiemOfSoulsWeapon),
                Wire = null,
            },
        };

        [MenuItem("Soul Hunter/Content/Build Evolutions")]
        public static void BuildAll()
        {
            var catalog = GameContentCatalog.Load();
            var evolutions = new List<WeaponEvolutionData>(catalog.Evolutions ?? new WeaponEvolutionData[0]);
            foreach (var recipe in Recipes)
            {
                var data = Build(recipe);
                if (data != null && !evolutions.Contains(data)) evolutions.Add(data);
            }
            evolutions.RemoveAll(e => e == null);
            catalog.Evolutions = evolutions.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EvolutionBuilder] {evolutions.Count} evolution(s) in GameContent.");
        }

        private static WeaponEvolutionData Build(Recipe recipe)
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(recipe.BasePrefabPath);
            if (basePrefab == null) throw new InvalidOperationException("Missing base weapon prefab " + recipe.BasePrefabPath);
            var baseComponent = basePrefab.GetComponent<AutoAttackWeapon>();

            var root = new GameObject(recipe.Name);
            try
            {
                var weapon = (AutoAttackWeapon)root.AddComponent(recipe.Script);
                recipe.Wire?.Invoke(baseComponent, weapon);
                string prefabPath = $"{PrefabFolder}/{recipe.Name}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                string dataPath = $"{DataFolder}/Evo_{recipe.Name}.asset";
                var data = AssetDatabase.LoadAssetAtPath<WeaponEvolutionData>(dataPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<WeaponEvolutionData>();
                    AssetDatabase.CreateAsset(data, dataPath);
                }
                data.BaseWeapon = recipe.BaseWeapon;
                data.BaseWeaponMaxLevel = 8;
                data.RequiredPassive = recipe.Passive;
                data.RequiredPassiveMaxLevel = 1; // VS: the passive is needed, at any level
                data.EvolvedWeaponPrefab = prefab;
                data.EvolvedName = "Evo_" + recipe.Name;
                EditorUtility.SetDirty(data);
                return data;
            }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>-executeMethod entry point.</summary>
        public static void RunFromCommandLine()
        {
            BuildAll();
            EditorApplication.Exit(0);
        }
    }
}
