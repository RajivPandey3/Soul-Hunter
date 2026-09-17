if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode first");
int layer=UnityEngine.LayerMask.NameToLayer("Player");if(layer<0)throw new System.Exception("Player layer missing");
int changed=0;
System.Action<UnityEngine.GameObject> fix=root=>{
 foreach(var player in root.GetComponentsInChildren<SoulHunter.Gameplay.Player.PlayerController>(true)){
  player.gameObject.layer=layer;
  foreach(var collider in player.GetComponentsInChildren<UnityEngine.Collider>(true))collider.gameObject.layer=layer;
  changed++;
 }
};
var prefabPath="Assets/Prefabs/Player/Player.prefab";
var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try{fix(prefab);UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);}
var path="Assets/Scenes/Gameplay/Main_Gameplay.unity";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{foreach(var root in scene.GetRootGameObjects())fix(root);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);}
finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
return new{changed,layer};
