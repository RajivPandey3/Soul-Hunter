var paths=new[]{"Assets/Prefabs/Player/SH10_Kael.prefab","Assets/Prefabs/Enemies/SH10_L01_Enemy.prefab"};
var rows=new System.Collections.Generic.List<object>();
foreach(var path in paths){var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);try{rows.Add(new{name=path,children=root.GetComponentsInChildren<UnityEngine.Transform>(true).Select(t=>new{t=t.name,components=t.GetComponents<UnityEngine.Component>().Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray(),localScale=t.localScale}).ToArray()});}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}}
return rows.ToArray();
