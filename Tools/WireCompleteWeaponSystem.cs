var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay/Main_Gameplay.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var managerObject = UnityEngine.GameObject.Find("Weapon_Manager");
if (managerObject == null) throw new System.Exception("Weapon_Manager not found");
var manager = managerObject.GetComponent<SoulHunter.Gameplay.Combat.WeaponManager>();
if (manager == null) throw new System.Exception("WeaponManager missing");
var bindings = new (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType type, string path)[]
{
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.LightningRing, "Assets/Prefabs/Weapons/Generated/LightningRing.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.FireWand, "Assets/Prefabs/Weapons/Generated/FireWand.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.SantaWater, "Assets/Prefabs/Weapons/Generated/SantaWater.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Runetracer, "Assets/Prefabs/Weapons/Generated/Runetracer.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Pentagram, "Assets/Prefabs/Weapons/Generated/Pentagram.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Peachone, "Assets/Prefabs/Weapons/Generated/Peachone.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Bone, "Assets/Prefabs/Weapons/Generated/Bone.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.CherryBomb, "Assets/Prefabs/Weapons/Generated/CherryBomb.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.ClockLancet, "Assets/Prefabs/Weapons/Generated/ClockLancet.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel, "Assets/Prefabs/Weapons/Generated/Laurel.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Knife, "Assets/Prefabs/Weapons/Generated/Knife.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.SongOfMana, "Assets/Prefabs/Weapons/Generated/SongOfMana.prefab"),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Gun, "Assets/Prefabs/Weapons/Generated/Gun.prefab")
};
var managerSO = new UnityEditor.SerializedObject(manager); var list = managerSO.FindProperty("_additionalWeaponBindings"); list.arraySize = bindings.Length;
for (int i = 0; i < bindings.Length; i++) { var element = list.GetArrayElementAtIndex(i); element.FindPropertyRelative("Type").enumValueIndex = (int)bindings[i].type; element.FindPropertyRelative("WeaponPrefab").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(bindings[i].path); }
managerSO.ApplyModifiedPropertiesWithoutUndo();
var evolutionPaths = UnityEditor.AssetDatabase.FindAssets("t:WeaponEvolutionData", new[]{"Assets/_Project/Data/Weapons"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).ToArray();
var unionPaths = UnityEditor.AssetDatabase.FindAssets("t:WeaponUnionData", new[]{"Assets/_Project/Data/Weapons"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).ToArray();
var evolutionPrefabMap = new System.Collections.Generic.Dictionary<string,string> { {"Evo_BloodyTear", "Assets/Prefabs/Weapons/Generated/BloodyTear.prefab"}, {"Evo_DeathSpiral", "Assets/Prefabs/Weapons/Generated/DeathSpiral.prefab"}, {"Evo_HolyWand", "Assets/Prefabs/Weapons/Generated/HolyWand.prefab"}, {"Evo_HeavenSword", "Assets/Prefabs/Weapons/Generated/HolyWand.prefab"}, {"Evo_SoulEater", "Assets/Prefabs/Weapons/Generated/SantaWater.prefab"}, {"Evo_UnholyVespers", "Assets/Prefabs/Weapons/Generated/Pentagram.prefab"} };
var evolutionAssets = new System.Collections.Generic.List<UnityEngine.Object>();
foreach (var path in evolutionPaths) { var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Combat.WeaponEvolutionData>(path); if (asset == null) continue; if (evolutionPrefabMap.TryGetValue(asset.name, out var prefabPath)) { asset.EvolvedWeaponPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath); UnityEditor.EditorUtility.SetDirty(asset); } evolutionAssets.Add(asset); }
var unionPrefabMap = new System.Collections.Generic.Dictionary<string,string> { {"Union_BloodyTear", "Assets/Prefabs/Weapons/Generated/BloodyTear.prefab"}, {"Union_DeathSpiral", "Assets/Prefabs/Weapons/Generated/DeathSpiral.prefab"}, {"Union_HolyWand", "Assets/Prefabs/Weapons/Generated/HolyWand.prefab"}, {"Union_HeavenSword", "Assets/Prefabs/Weapons/Generated/HolyWand.prefab"}, {"Union_SoulEater", "Assets/Prefabs/Weapons/Generated/SantaWater.prefab"}, {"Union_UnholyVespers", "Assets/Prefabs/Weapons/Generated/Pentagram.prefab"} };
var unionAssets = new System.Collections.Generic.List<UnityEngine.Object>();
foreach (var path in unionPaths) { var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Combat.WeaponUnionData>(path); if (asset == null) continue; if (unionPrefabMap.TryGetValue(asset.name, out var prefabPath)) { asset.UnionWeaponPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath); UnityEditor.EditorUtility.SetDirty(asset); } unionAssets.Add(asset); }
managerSO = new UnityEditor.SerializedObject(manager); var evoList = managerSO.FindProperty("_availableEvolutions"); evoList.arraySize = evolutionAssets.Count; for (int i = 0; i < evolutionAssets.Count; i++) evoList.GetArrayElementAtIndex(i).objectReferenceValue = evolutionAssets[i]; var unionList = managerSO.FindProperty("_availableUnions"); unionList.arraySize = unionAssets.Count; for (int i = 0; i < unionAssets.Count; i++) unionList.GetArrayElementAtIndex(i).objectReferenceValue = unionAssets[i]; managerSO.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(managerObject); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); UnityEditor.AssetDatabase.SaveAssets();
return new { bindings = bindings.Length, evolutions = evolutionAssets.Count, unions = unionAssets.Count };
