#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using Object = UnityEngine.Object;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds the VS items Soul Hunter lacked (Docs/VS_DOT_Table.md rows Evolution, Union, Guns, Greed; owner decision
    /// 2026-09-30 to add them; built 2026-10-12). Every rule and number is in the runtime code next to its wiki quote;
    /// this only makes the assets, in the way EvolutionBuilder and WeaponDefinitionBuilder do, without touching a scene:
    /// - the level UpgradeData assets (Assets/Resources/Upgrades/&lt;Type&gt;_Lv&lt;n&gt;.asset), listed in GameContent.Upgrades;
    /// - the looks: tinted copies of approved projectiles and effects (Assets/Prefabs/Weapons/Projectiles/SH10_*);
    /// - the weapon prefabs (Assets/Prefabs/Weapons/Generated/&lt;Name&gt;.prefab);
    /// - one WeaponDefinition per new item type (WD_&lt;Type&gt;), listed in GameContent.WeaponDefinitions;
    /// - Evo_SunderedHour (GameContent.Evolutions and CollectionEvolutions) and the two unions (GameContent.Unions).
    /// Names are Soul Hunter's own and NOT YET OWNER-APPROVED; the VS source of each is in the table below and never
    /// shown in the game. Icons are a related item's icon and the looks are tints of existing approved effects:
    /// placeholders, NOT OWNER-APPROVED ART.
    /// It can be run again at any time: existing assets are updated in place (same GUIDs) and nothing is listed twice.
    /// Menu: Soul Hunter/Content/Build New VS Items, or
    /// -executeMethod SoulHunter.Editor.Content.NewVsItemsBuilder.BuildFromCommandLine -quit
    /// </summary>
    public static class NewVsItemsBuilder
    {
        public const string UpgradeFolder = "Assets/Resources/Upgrades";
        public const string ProjectileFolder = "Assets/Prefabs/Weapons/Projectiles";
        public const string PrefabFolder = EvolutionBuilder.PrefabFolder;
        public const string DataFolder = EvolutionBuilder.DataFolder;

        /// <summary>One new item type: how many levels it has, whose icon it borrows, and its VS source page.</summary>
        public sealed class Item
        {
            public UpgradeData.UpgradeType Type;
            public int Levels;
            public UpgradeData.UpgradeType IconFrom;
            public string VsPage;
            /// <summary>The weapon prefab's name in <see cref="PrefabFolder"/> (null for a passive).</summary>
            public string Prefab;
        }

        public static readonly Item[] Items =
        {
            new Item { Type = UpgradeData.UpgradeType.MisersIdol, Levels = 5, IconFrom = UpgradeData.UpgradeType.Crown, VsPage = "Stone_Mask" },
            new Item { Type = UpgradeData.UpgradeType.CursedCoffer, Levels = 9, IconFrom = UpgradeData.UpgradeType.SkullOManiac, VsPage = "Torrona's_Box" },
            new Item { Type = UpgradeData.UpgradeType.RingOfLongHours, Levels = 9, IconFrom = UpgradeData.UpgradeType.ClockLancet, VsPage = "Silver_Ring" },
            new Item { Type = UpgradeData.UpgradeType.RingOfDarkHours, Levels = 9, IconFrom = UpgradeData.UpgradeType.SkullOManiac, VsPage = "Gold_Ring" },
            new Item { Type = UpgradeData.UpgradeType.DuskRaven, Levels = 8, IconFrom = UpgradeData.UpgradeType.Peachone, VsPage = "Ebony_Wings", Prefab = "DuskRaven" },
            new Item { Type = UpgradeData.UpgradeType.AzureRepeater, Levels = 8, IconFrom = UpgradeData.UpgradeType.Gun, VsPage = "Eight_The_Sparrow", Prefab = "AzureRepeater" },
            new Item { Type = UpgradeData.UpgradeType.TwilightFlock, Levels = 8, IconFrom = UpgradeData.UpgradeType.Peachone, VsPage = "Vandalier", Prefab = "TwilightFlock" },
        };

        // Tints of the placeholder looks (not owner-approved art).
        private static readonly Color DuskTint = new Color(0.62f, 0.45f, 0.95f);
        private static readonly Color AzureTint = new Color(0.45f, 0.75f, 1f);
        private static readonly Color WheelTint = new Color(0.5f, 0.7f, 1f);

        [MenuItem("Soul Hunter/Content/Build New VS Items")]
        public static void BuildFromMenu() => Build();

        /// <summary>-executeMethod entry point (use with -quit). A failure exits with code 1 in batch mode.</summary>
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
            var catalog = GameContentCatalog.Load();
            if (catalog == null) throw new InvalidOperationException("GameContentCatalog (Resources/GameContent) is missing.");
            EnsureFolder(UpgradeFolder);
            EnsureFolder(ProjectileFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(WeaponDefinitionBuilder.Folder);

            // 1. Level assets.
            var levels = new Dictionary<UpgradeData.UpgradeType, UpgradeData[]>();
            var upgrades = new List<UpgradeData>(catalog.Upgrades ?? new UpgradeData[0]);
            foreach (var item in Items)
            {
                var icon = IconOf(catalog, item.IconFrom);
                var built = new UpgradeData[item.Levels];
                for (int level = 1; level <= item.Levels; level++)
                {
                    built[level - 1] = BuildLevel(item, level, icon);
                    if (!upgrades.Contains(built[level - 1])) upgrades.Add(built[level - 1]);
                }
                levels[item.Type] = built;
            }
            upgrades.RemoveAll(u => u == null);
            catalog.Upgrades = upgrades.ToArray();

            // 2. Looks, then weapon prefabs.
            var peachone = LoadWeapon<PeachoneWeapon>("Peachone");
            var gun = LoadWeapon<GunWeapon>("Gun");
            var lancet = LoadWeapon<ClockLancetWeapon>("ClockLancet");
            var duskBird = TintedCopy(peachone.BirdVisual, "SH10_Fx_DuskRavenBird", DuskTint);
            var duskBlast = TintedCopy(peachone.BombPrefab, "SH10_Fx_DuskRavenBlast", DuskTint);
            var azureBullet = TintedCopy(gun.BulletPrefab, "SH10_Proj_AzureBullet", AzureTint);
            var wheelLaser = TintedCopy(lancet.FreezeBeamPrefab, "SH10_Fx_RevenantLaser", WheelTint);

            var prefabs = new Dictionary<string, GameObject>
            {
                ["DuskRaven"] = BuildWeapon<DuskRavenWeapon>("DuskRaven", w =>
                {
                    w.BirdVisual = duskBird;
                    w.BombPrefab = duskBlast;
                    w.CircleRadius = peachone.CircleRadius;
                }),
                ["AzureRepeater"] = BuildWeapon<AzureRepeaterWeapon>("AzureRepeater", w =>
                {
                    w.BulletPrefab = azureBullet;
                    w.ShootCorners = gun.ShootCorners;
                }),
                ["TwilightFlock"] = BuildWeapon<TwilightFlockWeapon>("TwilightFlock", w =>
                {
                    w.BirdVisual = peachone.BirdVisual;
                    w.BombPrefab = peachone.BombPrefab;
                    w.CircleRadius = peachone.CircleRadius;
                }),
                ["SunderedHour"] = BuildWeapon<SunderedHourWeapon>("SunderedHour", w => w.FreezeBeamPrefab = lancet.FreezeBeamPrefab),
                ["RevenantWheel"] = BuildWeapon<RevenantWheelWeapon>("RevenantWheel", w => w.LaserVisualPrefab = wheelLaser),
            };

            // 3. Definitions.
            var definitions = new List<WeaponDefinition>(catalog.WeaponDefinitions ?? new WeaponDefinition[0]);
            definitions.RemoveAll(d => d == null);
            foreach (var item in Items)
            {
                var definition = BuildDefinition(item, levels[item.Type], item.Prefab != null ? prefabs[item.Prefab] : null);
                definitions.RemoveAll(d => d != definition && d.Type == item.Type);
                if (!definitions.Contains(definition)) definitions.Add(definition);
            }
            catalog.WeaponDefinitions = definitions.ToArray(); // a new array: WeaponDefinitionLookup indexes it again

            // 4. Sundered Hour (VS Infinite Corridor): Clock Lancet at its max level (7) with both rings at max level (9).
            var evolution = LoadOrCreate<WeaponEvolutionData>($"{DataFolder}/Evo_SunderedHour.asset");
            evolution.BaseWeapon = UpgradeData.UpgradeType.ClockLancet;
            evolution.BaseWeaponMaxLevel = lancet.MaxLevel;
            evolution.RequiredPassive = UpgradeData.UpgradeType.RingOfLongHours;
            evolution.RequiredPassiveMaxLevel = 9;
            evolution.AdditionalPassives = new[]
            {
                new PassiveEvolutionRequirement { Type = UpgradeData.UpgradeType.RingOfDarkHours, MinimumLevel = 9 }
            };
            evolution.EvolvedWeaponPrefab = prefabs["SunderedHour"];
            evolution.EvolvedName = "Evo_SunderedHour";
            EditorUtility.SetDirty(evolution);
            catalog.Evolutions = With(catalog.Evolutions, evolution);
            catalog.CollectionEvolutions = With(catalog.CollectionEvolutions, evolution);

            // 5. The unions (wiki Evolution: "all base weapons must be fully leveled up").
            var flock = LoadOrCreate<WeaponUnionData>($"{DataFolder}/Union_TwilightFlock.asset");
            flock.WeaponA = UpgradeData.UpgradeType.Peachone;
            flock.WeaponAMaxLevel = peachone.MaxLevel;
            flock.WeaponB = UpgradeData.UpgradeType.DuskRaven;
            flock.WeaponBMaxLevel = prefabs["DuskRaven"].GetComponent<AutoAttackWeapon>().MaxLevel;
            flock.NeedsPassive = false;
            flock.HasResultType = true;
            flock.ResultType = UpgradeData.UpgradeType.TwilightFlock;
            flock.UnionWeaponPrefab = prefabs["TwilightFlock"];
            flock.UnionName = "Union_TwilightFlock";
            flock.UnionIcon = IconOf(catalog, UpgradeData.UpgradeType.Peachone);
            EditorUtility.SetDirty(flock);

            var wheel = LoadOrCreate<WeaponUnionData>($"{DataFolder}/Union_RevenantWheel.asset");
            wheel.WeaponA = UpgradeData.UpgradeType.Gun;
            wheel.WeaponAMaxLevel = gun.MaxLevel;
            wheel.WeaponB = UpgradeData.UpgradeType.AzureRepeater;
            wheel.WeaponBMaxLevel = prefabs["AzureRepeater"].GetComponent<AutoAttackWeapon>().MaxLevel;
            wheel.NeedsPassive = true; // wiki Phieraggi: "requires Tirajisu" (the wiki asks for no level of it)
            wheel.RequiredPassive = UpgradeData.UpgradeType.Tiragisu;
            wheel.RequiredPassiveMinLevel = 1;
            wheel.HasResultType = false; // the page has no level table
            wheel.UnionWeaponPrefab = prefabs["RevenantWheel"];
            wheel.UnionName = "Union_RevenantWheel";
            wheel.UnionIcon = IconOf(catalog, UpgradeData.UpgradeType.Gun);
            EditorUtility.SetDirty(wheel);

            catalog.Unions = With(With(catalog.Unions, flock), wheel);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[NewVsItemsBuilder] Built {Items.Length} item types ({Items.Sum(i => i.Levels)} level assets), {prefabs.Count} weapon prefabs, " +
                      $"1 evolution and 2 unions. GameContent: {catalog.Upgrades.Length} upgrades, {catalog.WeaponDefinitions.Length} definitions, " +
                      $"{catalog.Evolutions.Length} evolutions, {catalog.Unions.Length} unions.");
        }

        public static string LevelPath(UpgradeData.UpgradeType type, int level) => $"{UpgradeFolder}/{type}_Lv{level}.asset";

        private static string NameOf(UpgradeData.UpgradeType type) =>
            WeaponDefinitionBuilder.NameOverrides.TryGetValue(type, out var name) ? name : ObjectNames.NicifyVariableName(type.ToString());

        private static UpgradeData BuildLevel(Item item, int level, Sprite icon)
        {
            var upgrade = LoadOrCreate<UpgradeData>(LevelPath(item.Type, level));
            upgrade.UpgradeName = $"{NameOf(item.Type)} Lv.{level}";
            upgrade.Description = ""; // the card text comes from the definition and UpgradeText
            upgrade.Type = item.Type;
            upgrade.Level = level;
            if (icon != null) upgrade.Icon = icon;
            EditorUtility.SetDirty(upgrade);
            return upgrade;
        }

        private static WeaponDefinition BuildDefinition(Item item, UpgradeData[] levels, GameObject prefab)
        {
            var definition = LoadOrCreate<WeaponDefinition>(WeaponDefinitionBuilder.AssetPath(item.Type));
            definition.Type = item.Type;
            definition.IsPassive = !WeaponManager.LegacyIsWeaponType(item.Type);
            definition.DisplayName = NameOf(item.Type);
            definition.Description = UpgradeText.FallbackDescription(item.Type) ?? "";
            definition.Icon = levels.Select(u => u.Icon).FirstOrDefault(i => i != null);
            definition.Rarity = UpgradeRarity.VsWeight(item.Type);
            definition.Levels = levels;
            if (definition.IsPassive)
            {
                definition.WeaponPrefab = null;
                // VS max levels: 5 (Stone Mask) or 9 (the rings). Not through VsPassiveMaxLevel's weapon test, which would
                // read this very definition.
                definition.MaxLevel = item.Levels;
            }
            else
            {
                if (prefab == null) throw new InvalidOperationException($"{item.Type} has no weapon prefab.");
                definition.WeaponPrefab = prefab;
                definition.MaxLevel = prefab.GetComponent<AutoAttackWeapon>().MaxLevel;
                if (definition.MaxLevel != item.Levels)
                    throw new InvalidOperationException($"{item.Type}: {item.Levels} level assets but the weapon's max level is {definition.MaxLevel}.");
            }
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static T LoadWeapon<T>(string prefabName) where T : AutoAttackWeapon
        {
            string path = $"{PrefabFolder}/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var weapon = prefab != null ? prefab.GetComponent<T>() : null;
            if (weapon == null) throw new InvalidOperationException($"Missing weapon prefab {path} (with {typeof(T).Name}).");
            return weapon;
        }

        /// <summary>Writes Generated/&lt;name&gt;.prefab: an empty object with the weapon script, wired by <paramref name="wire"/>.</summary>
        private static GameObject BuildWeapon<T>(string name, Action<T> wire) where T : AutoAttackWeapon
        {
            var root = new GameObject(name);
            try
            {
                var weapon = root.AddComponent<T>();
                wire?.Invoke(weapon);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{name}.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>
        /// A copy of an approved projectile or effect prefab with its particles, lines and trails tinted (its meshes and
        /// materials are left as they are). Written next to the original as <paramref name="name"/>.prefab.
        /// </summary>
        private static GameObject TintedCopy(GameObject source, string name, Color tint)
        {
            if (source == null) throw new InvalidOperationException($"Nothing to make {name} from: the source weapon has no prefab in that field.");
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath)) throw new InvalidOperationException($"{source.name} is not a prefab asset.");
            string targetPath = $"{ProjectileFolder}/{name}.prefab";
            var contents = PrefabUtility.LoadPrefabContents(sourcePath);
            try
            {
                contents.name = name;
                foreach (var particles in contents.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particles.main;
                    var colour = main.startColor;
                    switch (colour.mode)
                    {
                        case ParticleSystemGradientMode.Color:
                            main.startColor = new ParticleSystem.MinMaxGradient(colour.color * tint);
                            break;
                        case ParticleSystemGradientMode.TwoColors:
                            main.startColor = new ParticleSystem.MinMaxGradient(colour.colorMin * tint, colour.colorMax * tint);
                            break;
                    }
                }
                foreach (var line in contents.GetComponentsInChildren<LineRenderer>(true))
                {
                    line.startColor *= tint;
                    line.endColor *= tint;
                }
                foreach (var trail in contents.GetComponentsInChildren<TrailRenderer>(true))
                {
                    trail.startColor *= tint;
                    trail.endColor *= tint;
                }
                return PrefabUtility.SaveAsPrefabAsset(contents, targetPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static Sprite IconOf(GameContentCatalog catalog, UpgradeData.UpgradeType type)
        {
            if (catalog.WeaponDefinitions != null)
                foreach (var definition in catalog.WeaponDefinitions)
                    if (definition != null && definition.Type == type && definition.Icon != null) return definition.Icon;
            if (catalog.Upgrades != null)
                foreach (var upgrade in catalog.Upgrades)
                    if (upgrade != null && upgrade.Type == type && upgrade.Icon != null) return upgrade.Icon;
            Debug.LogWarning($"[NewVsItemsBuilder] No icon found on {type} to borrow.");
            return null;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>The list with <paramref name="added"/> in it once, without empty entries.</summary>
        private static T[] With<T>(T[] list, T added) where T : Object
        {
            var result = new List<T>(list ?? new T[0]);
            result.RemoveAll(x => x == null);
            if (!result.Contains(added)) result.Add(added);
            return result.ToArray();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
        }
    }
}
#endif
