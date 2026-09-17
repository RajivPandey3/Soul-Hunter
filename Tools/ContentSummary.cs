return new {
 prefabs=UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Select(p=>new{path=p, components=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponents<Component>().Select(c=>c?c.GetType().Name:"MISSING").ToArray()}).ToArray(),
 data=UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject",new[]{"Assets/_Project/Data"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Select(p=>new{path=p,json=UnityEditor.EditorJsonUtility.ToJson(UnityEditor.AssetDatabase.LoadMainAssetAtPath(p))}).ToArray()
};
