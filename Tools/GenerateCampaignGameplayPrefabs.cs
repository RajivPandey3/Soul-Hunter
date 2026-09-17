var enemyDir = "Assets/Prefabs/Enemies/Gameplay"; var bossDir = "Assets/Prefabs/Bosses/Gameplay";
if (!UnityEditor.AssetDatabase.IsValidFolder(enemyDir)) UnityEditor.AssetDatabase.CreateFolder("Assets/Prefabs/Enemies", "Gameplay");
if (!UnityEditor.AssetDatabase.IsValidFolder(bossDir)) UnityEditor.AssetDatabase.CreateFolder("Assets/Prefabs/Bosses", "Gameplay");
var baseEnemy = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Enemy_Entity.prefab");
if (baseEnemy == null) throw new System.Exception("Enemy_Entity gameplay prefab missing");
var enemyPaths = new string[10]; var bossPaths = new string[10];
for (int i = 1; i <= 10; i++)
{
    string n = i.ToString("00");
    var enemyRoot = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/Weapons/Enemy_Entity.prefab");
    var oldEnemyVisual = enemyRoot.transform.Find("SH10_Visual"); if (oldEnemyVisual != null) UnityEngine.Object.DestroyImmediate(oldEnemyVisual.gameObject);
    var enemyVisual = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>($"Assets/Prefabs/Enemies/SH10_L{n}_Enemy.prefab");
    if (enemyVisual != null) { var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(enemyVisual); instance.transform.SetParent(enemyRoot.transform, false); instance.name = "SH10_Visual"; instance.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f); }
    string enemyPath = $"{enemyDir}/SH10_L{n}_EnemyGameplay.prefab"; UnityEditor.PrefabUtility.SaveAsPrefabAsset(enemyRoot, enemyPath); UnityEditor.PrefabUtility.UnloadPrefabContents(enemyRoot); enemyPaths[i - 1] = enemyPath;
    var bossRoot = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/Weapons/Enemy_Entity.prefab");
    var oldBossVisual = bossRoot.transform.Find("SH10_Visual"); if (oldBossVisual != null) UnityEngine.Object.DestroyImmediate(oldBossVisual.gameObject);
    var bossVisual = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>($"Assets/Prefabs/Bosses/SH10_L{n}_Boss.prefab");
    if (bossVisual != null) { var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(bossVisual); instance.transform.SetParent(bossRoot.transform, false); instance.name = "SH10_Visual"; instance.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f); }
    string bossPath = $"{bossDir}/SH10_L{n}_BossGameplay.prefab"; UnityEditor.PrefabUtility.SaveAsPrefabAsset(bossRoot, bossPath); UnityEditor.PrefabUtility.UnloadPrefabContents(bossRoot); bossPaths[i - 1] = bossPath;
}
UnityEditor.AssetDatabase.SaveAssets(); UnityEditor.AssetDatabase.Refresh(); return new { enemies = enemyPaths.Length, bosses = bossPaths.Length };
