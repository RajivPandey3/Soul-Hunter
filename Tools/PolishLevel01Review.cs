var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(UnityEditor.EditorApplication.isPlaying || scene.path!="Assets/Scenes/Test/L01_AssetReview.unity")throw new System.Exception("Review scene in Edit mode required");
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("L01R_ t:Prefab",new[]{"Assets/Prefabs"})){
var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);if(!System.IO.Path.GetFileName(path).StartsWith("L01R_"))continue;
var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try{var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Models/"+root.name+".fbx");root.transform.GetChild(0).localRotation=UnityEngine.Quaternion.Euler(0,180,0)*model.transform.localRotation;UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}}
foreach(var root in scene.GetRootGameObjects()){
if(root.name.StartsWith("L01R_")){var pos=root.transform.position;pos.z=UnityEngine.Mathf.Round(pos.z/5.28f)*5.28f;root.transform.position=pos;}
}
var camera=UnityEngine.Camera.main;camera.transform.position=new UnityEngine.Vector3(0,17,-17);camera.transform.LookAt(new UnityEngine.Vector3(0,0,4.6f));camera.orthographicSize=9.4f;
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{scene=scene.path,missingScripts=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))};
