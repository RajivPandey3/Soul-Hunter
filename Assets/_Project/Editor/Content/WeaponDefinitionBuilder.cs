#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds one WeaponDefinition per UpgradeData.UpgradeType from the current data (2026-09-28) and lists them in
    /// GameContentCatalog.WeaponDefinitions (Assets/Resources/GameContent.asset):
    /// - WeaponPrefab: the WeaponManager in Main_Gameplay (its six weapon fields and _additionalWeaponBindings). The scene
    ///   is opened additively and read only; it is never saved, and closed again unless it was already open.
    /// - IsPassive: the built-in split (WeaponManager.LegacyIsWeaponType).
    /// - DisplayName / Description: UpgradeText (the level-1 asset's name, and the built-in weapon or passive text).
    /// - Icon: the first icon among the item's UpgradeData assets; Levels: those assets (catalog Upgrades), level 1 first.
    /// - MaxLevel: the weapon prefab's AutoAttackWeapon.MaxLevel; passives the VS max level (UpgradeRarity.VsPassiveMaxLevel).
    /// - Rarity: the VS item rarity (UpgradeRarity.VsRarity, each value read from the item's VS wiki infobox,
    ///   https://vampire.survivors.wiki/w/&lt;Page&gt;?action=raw, 2026-09-28).
    /// Existing definition assets are updated in place (same GUIDs). Menu: Soul Hunter/Content/Build Weapon Definitions, or
    /// -executeMethod SoulHunter.Editor.Content.WeaponDefinitionBuilder.BuildFromCommandLine -quit
    /// </summary>
    public static class WeaponDefinitionBuilder
    {
        /// <summary>
        /// Bug MIS v2 C15/C16 (2026-10-02): names made from the enum read as stats or had odd casing ("Max Health", "Player
        /// Speed", "Song Of Mana"). Placeholder item names until Soul Hunter's own names land (owner: after the game is final).
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<UpgradeData.UpgradeType, string> NameOverrides =
            new System.Collections.Generic.Dictionary<UpgradeData.UpgradeType, string>
            {
                { UpgradeData.UpgradeType.SongOfMana, "Song of Mana" },
                { UpgradeData.UpgradeType.MaxHealth, "Hollow Heart" },
                { UpgradeData.UpgradeType.PlayerSpeed, "Wings" },
                { UpgradeData.UpgradeType.SkullOManiac, "Skull O'Maniac" },
                { UpgradeData.UpgradeType.Tiragisu, "Tirajisu" },
            };

        public const string Folder = "Assets/_Project/Data/Weapons/Definitions";
        public const string GameplayScenePath = "Assets/Scenes/Gameplay/Main_Gameplay.unity";
        public const int StagePassiveMaxLevel = 9;

        private static readonly string[] LegacyWeaponFields =
            { "_magicWandObject", "_garlicWeaponObject", "_whipWeaponObject", "_axeWeaponObject", "_bibleWeaponObject", "_crossWeaponObject" };

        private static readonly UpgradeData.UpgradeType[] LegacyWeaponFieldTypes =
        {
            UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Garlic, UpgradeData.UpgradeType.Whip,
            UpgradeData.UpgradeType.Axe, UpgradeData.UpgradeType.Bible, UpgradeData.UpgradeType.Cross
        };

        public static string AssetPath(UpgradeData.UpgradeType type) => $"{Folder}/WD_{type}.asset";

        [MenuItem("Soul Hunter/Content/Build Weapon Definitions")]
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

        public static List<WeaponDefinition> Build()
        {
            var catalog = GameContentCatalog.Load();
            if (catalog == null) throw new InvalidOperationException("GameContentCatalog (Resources/GameContent) is missing.");
            EnsureFolder(Folder);

            var prefabs = ReadScenePrefabs();
            var definitions = new List<WeaponDefinition>();
            var warnings = new List<string>();

            foreach (UpgradeData.UpgradeType type in Enum.GetValues(typeof(UpgradeData.UpgradeType)))
            {
                string path = AssetPath(type);
                var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
                bool created = definition == null;
                if (created) definition = ScriptableObject.CreateInstance<WeaponDefinition>();

                var levels = (catalog.Upgrades ?? Array.Empty<UpgradeData>())
                    .Where(u => u != null && u.Type == type && u.Level > 0)
                    .GroupBy(u => u.Level).Select(g => g.First())
                    .OrderBy(u => u.Level).ToArray();
                var levelOne = levels.FirstOrDefault(u => u.Level == 1);

                definition.Type = type;
                definition.IsPassive = !WeaponManager.LegacyIsWeaponType(type);
                definition.Levels = levels;
                definition.DisplayName = NameOverrides.TryGetValue(type, out var named) ? named
                    : levelOne != null ? UpgradeText.NameFromAsset(levelOne) : ObjectNames.NicifyVariableName(type.ToString());
                definition.Description = UpgradeText.FallbackDescription(type) ?? "";
                definition.Icon = levels.Select(u => u.Icon).FirstOrDefault(i => i != null);
                definition.Rarity = UpgradeRarity.VsWeight(type);
                if (!UpgradeRarity.VsRarity.ContainsKey(type)) warnings.Add($"{type}: no VS rarity in UpgradeRarity.VsRarity, used {UpgradeRarity.DefaultWeight}.");

                if (definition.IsPassive)
                {
                    definition.WeaponPrefab = null;
                    definition.MaxLevel = UpgradeRarity.VsPassiveMaxLevel(type); // VS wiki max levels (5, 2 or 9)
                }
                else
                {
                    prefabs.TryGetValue(type, out var prefab);
                    definition.WeaponPrefab = prefab;
                    var weapon = prefab != null ? prefab.GetComponent<AutoAttackWeapon>() : null;
                    definition.MaxLevel = weapon != null ? weapon.MaxLevel : 0;
                    if (prefab == null) warnings.Add($"{type}: no weapon prefab in {GameplayScenePath}'s WeaponManager.");
                    else if (weapon == null) warnings.Add($"{type}: prefab {prefab.name} has no AutoAttackWeapon.");
                }
                if (levelOne == null) warnings.Add($"{type}: no level-1 UpgradeData in the catalog.");
                if (string.IsNullOrWhiteSpace(definition.Description)) warnings.Add($"{type}: no built-in description.");

                if (created) AssetDatabase.CreateAsset(definition, path);
                else EditorUtility.SetDirty(definition);
                definitions.Add(definition);
            }

            catalog.WeaponDefinitions = definitions.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            foreach (var warning in warnings) Debug.LogWarning("[WeaponDefinitionBuilder] " + warning);
            Debug.Log($"[WeaponDefinitionBuilder] Built {definitions.Count} weapon definitions in {Folder} " +
                      $"({definitions.Count(d => !d.IsPassive)} weapons, {definitions.Count(d => d.IsPassive)} passives), {warnings.Count} warnings.");
            return definitions;
        }

        /// <summary>Weapon prefab per type, read from Main_Gameplay's WeaponManager without saving the scene.</summary>
        public static Dictionary<UpgradeData.UpgradeType, GameObject> ReadScenePrefabs()
        {
            var result = new Dictionary<UpgradeData.UpgradeType, GameObject>();
            var scene = SceneManager.GetSceneByPath(GameplayScenePath);
            bool wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var manager = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WeaponManager>(true)).FirstOrDefault();
                if (manager == null) throw new InvalidOperationException($"No WeaponManager in {GameplayScenePath}.");

                var serialized = new SerializedObject(manager);
                for (int i = 0; i < LegacyWeaponFields.Length; i++)
                {
                    var prefab = serialized.FindProperty(LegacyWeaponFields[i])?.objectReferenceValue as GameObject;
                    if (prefab != null) result[LegacyWeaponFieldTypes[i]] = prefab;
                }
                var bindings = serialized.FindProperty("_additionalWeaponBindings");
                if (bindings != null)
                    for (int i = 0; i < bindings.arraySize; i++)
                    {
                        var element = bindings.GetArrayElementAtIndex(i);
                        var type = (UpgradeData.UpgradeType)element.FindPropertyRelative("Type").intValue;
                        var prefab = element.FindPropertyRelative("WeaponPrefab").objectReferenceValue as GameObject;
                        // WeaponManager.GetWeaponObject returns the first binding of a type; keep the same rule.
                        if (prefab != null && !result.ContainsKey(type)) result[type] = prefab;
                    }
            }
            finally
            {
                // Read only: never save; close it again unless it was open before.
                if (!wasOpen && scene.IsValid() && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
            return result;
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
