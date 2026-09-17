var outputDir = "Assets/Prefabs/Weapons/Generated";
if (!UnityEditor.AssetDatabase.IsValidFolder(outputDir)) UnityEditor.AssetDatabase.CreateFolder("Assets/Prefabs/Weapons", "Generated");
var bullet = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Bullet.prefab");
var definitions = new (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType type, System.Type script, string visual, string[] fields)[]
{
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.LightningRing, typeof(SoulHunter.Gameplay.Combat.LightningRingWeapon), "Assets/Prefabs/Weapons/SH10_LightningRing.prefab", new[]{"LightningStrikePrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.FireWand, typeof(SoulHunter.Gameplay.Combat.FireWandWeapon), "Assets/Prefabs/Weapons/SH10_FireWand.prefab", new[]{"FireballPrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.SantaWater, typeof(SoulHunter.Gameplay.Combat.SantaWaterWeapon), "Assets/Prefabs/Weapons/SH10_SantaWater.prefab", new[]{"WaterZonePrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Runetracer, typeof(SoulHunter.Gameplay.Combat.RunetracerWeapon), "Assets/Prefabs/Weapons/SH10_Runetracer.prefab", new[]{"RunePrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Pentagram, typeof(SoulHunter.Gameplay.Combat.PentagramWeapon), "Assets/Prefabs/Weapons/SH10_Pentagram.prefab", new[]{"PentagramVFXPrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Peachone, typeof(SoulHunter.Gameplay.Combat.PeachoneWeapon), "Assets/Prefabs/Weapons/SH10_Peachone.prefab", new[]{"BombPrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Bone, typeof(SoulHunter.Gameplay.Combat.BoneWeapon), "Assets/Prefabs/Weapons/SH10_Bone.prefab", new[]{"BonePrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.CherryBomb, typeof(SoulHunter.Gameplay.Combat.CherryBombWeapon), "Assets/Prefabs/Weapons/SH10_CherryBomb.prefab", new[]{"BombPrefab","ExplosionPrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.ClockLancet, typeof(SoulHunter.Gameplay.Combat.ClockLancetWeapon), "Assets/Prefabs/Weapons/SH10_ClockLancet.prefab", new[]{"FreezeBeamPrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel, typeof(SoulHunter.Gameplay.Combat.LaurelWeapon), "Assets/Prefabs/Weapons/SH10_Laurel.prefab", new[]{"ShieldVisualPrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Knife, typeof(SoulHunter.Gameplay.Combat.KnifeWeapon), "Assets/Prefabs/Weapons/SH10_Knife.prefab", new[]{"_projectilePrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.SongOfMana, typeof(SoulHunter.Gameplay.Combat.SongOfManaWeapon), "Assets/Prefabs/Weapons/SH10_SongOfMana.prefab", new[]{"VerticalBeamPrefab"}),
    (SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Gun, typeof(SoulHunter.Gameplay.Combat.GunWeapon), "Assets/Prefabs/Weapons/SH10_Gun.prefab", new[]{"BulletPrefab"})
};
var bindingMap = new System.Collections.Generic.List<string>();
foreach (var definition in definitions)
{
    string path = $"{outputDir}/{definition.type}.prefab";
    var root = new UnityEngine.GameObject(definition.type + "_Weapon");
    var component = root.AddComponent(definition.script);
    foreach (var fieldName in definition.fields)
    {
        var serialized = new UnityEditor.SerializedObject(component);
        var field = serialized.FindProperty(fieldName);
        if (field != null) field.objectReferenceValue = fieldName == "ShieldVisualPrefab" ? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(definition.visual) : bullet;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    var visual = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(definition.visual);
    if (visual != null)
    {
        var visualInstance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(visual);
        visualInstance.transform.SetParent(root.transform, false);
        visualInstance.name = "SH10_Visual";
    }
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEngine.Object.DestroyImmediate(root);
    bindingMap.Add($"{definition.type}:{path}");
}
UnityEditor.AssetDatabase.SaveAssets(); UnityEditor.AssetDatabase.Refresh();
return new { generated = bindingMap.Count, bindings = string.Join(",", bindingMap) };
