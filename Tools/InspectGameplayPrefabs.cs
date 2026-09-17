var paths=new[]{"Assets/Prefabs/Player/Player.prefab","Assets/Prefabs/Weapons/Enemy_Entity.prefab"};
var rows=new System.Collections.Generic.List<object>();
foreach(var path in paths){var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);try{rows.Add(new{name=path,children=root.GetComponentsInChildren<UnityEngine.Transform>(true).Select(t=>new{t=t.name,components=t.GetComponents<UnityEngine.Component>().Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray(),mesh=t.GetComponent<UnityEngine.MeshRenderer>()!=null,skin=t.GetComponent<UnityEngine.SkinnedMeshRenderer>()!=null}).ToArray()});}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}}
return rows.ToArray();
