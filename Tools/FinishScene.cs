if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay/Main_Gameplay.unity");
var path="Assets/Prefabs/Environment/Ground.prefab";
var ground=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try{ground.transform.localScale=new UnityEngine.Vector3(2,1,2);
var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art/Materials/RecoveryGround.mat");
if(!mat){mat=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Unlit"));UnityEditor.AssetDatabase.CreateAsset(mat,"Assets/Art/Materials/RecoveryGround.mat");}
mat.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Art/Materials/SoulHunter_Grass_BestQuality.png"));mat.SetColor("_BaseColor",UnityEngine.Color.white);mat.SetTextureScale("_BaseMap",new UnityEngine.Vector2(4,4));UnityEditor.EditorUtility.SetDirty(mat);
ground.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial=mat;
UnityEditor.PrefabUtility.SaveAsPrefabAsset(ground,path);
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(ground);}
var map=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Environment.InfiniteMap>();var so=new UnityEditor.SerializedObject(map);so.FindProperty("_chunkSize").floatValue=20;so.ApplyModifiedPropertiesWithoutUndo();
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Characters"})){
var p=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);if(!System.IO.Path.GetFileName(p).StartsWith("Hero_"))continue;
var root=UnityEditor.PrefabUtility.LoadPrefabContents(p);
try{var sprite=root.GetComponent<UnityEngine.SpriteRenderer>();if(sprite&&sprite.sprite&&sprite.sprite.name=="sprite_60_0"){root.transform.localScale=UnityEngine.Vector3.one*(2f/sprite.sprite.bounds.size.y);UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,p);}}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}}
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Scenes/Bootstrap/Bootstrap.unity");
return new {scene=scene.path,playStart="Bootstrap",missing=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))};
