var paths = new[]{"Assets/Prefabs/Weapons/Enemy_Entity.prefab","Assets/Prefabs/Bosses/SH10_L01_BossGameplay.prefab"}; int cleaned = 0;
foreach (var path in paths) { var root = UnityEditor.PrefabUtility.LoadPrefabContents(path); try { cleaned += UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root); UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path); } finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); } }
UnityEditor.AssetDatabase.SaveAssets(); UnityEditor.AssetDatabase.Refresh(); return new { cleaned };
