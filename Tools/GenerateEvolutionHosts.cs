var outputDir = "Assets/Prefabs/Weapons/Generated";
var bullet = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Bullet.prefab");
var definitions = new (string name, System.Type script, string visual, string[] fields)[]
{
    ("BloodyTear", typeof(SoulHunter.Gameplay.Combat.BloodyTearWeapon), "Assets/Prefabs/Weapons/SH10_BloodyTear.prefab", new[]{"WhipVisualPrefab"}),
    ("DeathSpiral", typeof(SoulHunter.Gameplay.Combat.DeathSpiralWeapon), "Assets/Prefabs/Weapons/SH10_DeathSpiral.prefab", new[]{"ScythePrefab"}),
    ("HolyWand", typeof(SoulHunter.Gameplay.Combat.HolyWandWeapon), "Assets/Prefabs/Weapons/SH10_HolyWand.prefab", new[]{"ProjectilePrefab"}),
    ("ThousandEdge", typeof(SoulHunter.Gameplay.Combat.ThousandEdgeWeapon), "Assets/Prefabs/Weapons/SH10_ThousandEdge.prefab", new[]{"KnifePrefab"})
};
foreach (var definition in definitions)
{
    string path = $"{outputDir}/{definition.name}.prefab";
    var root = new UnityEngine.GameObject(definition.name + "_Weapon");
    var component = root.AddComponent(definition.script);
    foreach (var fieldName in definition.fields)
    {
        var serialized = new UnityEditor.SerializedObject(component);
        var field = serialized.FindProperty(fieldName);
        if (field != null) field.objectReferenceValue = fieldName == "WhipVisualPrefab" || fieldName == "ScythePrefab" ? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(definition.visual) : bullet;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    var visual = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(definition.visual);
    if (visual != null)
    {
        var visualInstance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(visual);
        visualInstance.transform.SetParent(root.transform, false); visualInstance.name = "SH10_Visual";
    }
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
}
UnityEditor.AssetDatabase.SaveAssets(); UnityEditor.AssetDatabase.Refresh();
return new { generated = definitions.Length };
