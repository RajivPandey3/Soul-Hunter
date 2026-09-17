if(UnityEditor.EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.Exception("Clean Edit mode required");
const string scenePath="Assets/Scenes/Test/L01_AssetReview.unity";
if(System.IO.File.Exists(scenePath))throw new System.Exception("Review scene already exists; refusing overwrite");
UnityEditor.AssetDatabase.Refresh();
void Folder(string path){if(UnityEditor.AssetDatabase.IsValidFolder(path))return;var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);UnityEditor.AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
Folder("Assets/Scenes/Test");
var names=new[]{"Gravestone","CrossGrave","Coffin","Crypt","Gate","Fence","DeadTree","RockCluster","SoulLantern","SoulWand","XPGem","TreasureChest","KaelStudy","GraveGhoulStudy"};
var colors=new System.Collections.Generic.Dictionary<string,UnityEngine.Color>{
{"Slate",new UnityEngine.Color(.20f,.25f,.30f)}, {"Edge",new UnityEngine.Color(.34f,.40f,.44f)},
{"Dark",new UnityEngine.Color(.055f,.075f,.095f)}, {"Iron",new UnityEngine.Color(.13f,.18f,.23f)},
{"Bronze",new UnityEngine.Color(.48f,.28f,.10f)}, {"Wood",new UnityEngine.Color(.16f,.095f,.065f)},
{"Soul",new UnityEngine.Color(.10f,.85f,.80f)}, {"Cloth",new UnityEngine.Color(.26f,.035f,.055f)},
{"Bone",new UnityEngine.Color(.58f,.57f,.40f)}, {"Moss",new UnityEngine.Color(.16f,.23f,.12f)}};
var shader=UnityEngine.Shader.Find("SoulHunter/Review/FacetedPreview");if(!shader||UnityEditor.ShaderUtil.ShaderHasError(shader))throw new System.Exception("Preview shader invalid");
var mats=new System.Collections.Generic.Dictionary<string,UnityEngine.Material>();
foreach(var kv in colors){var path="Assets/Art/Materials/L01R_"+kv.Key+".mat";if(System.IO.File.Exists(path))throw new System.Exception("Material already exists: "+path);var m=new UnityEngine.Material(shader);m.color=kv.Value;UnityEditor.AssetDatabase.CreateAsset(m,path);mats.Add("L01R_"+kv.Key,m);}
var oldScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var records=new System.Collections.Generic.List<object>();
for(int i=0;i<names.Length;i++){
var name=names[i];var modelPath="Assets/Art/Models/L01R_"+name+".fbx";
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(modelPath);if(!model)throw new System.Exception("Missing model: "+modelPath);
string category=i<9?"Environment":name=="SoulWand"?"Weapons":i<12?"Items":"Characters";
Folder("Assets/Prefabs/"+category);
var prefabPath="Assets/Prefabs/"+category+"/L01R_"+name+".prefab";if(System.IO.File.Exists(prefabPath))throw new System.Exception("Prefab exists: "+prefabPath);
var root=new UnityEngine.GameObject("L01R_"+name);var visual=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model,root.transform);
foreach(var r in visual.GetComponentsInChildren<UnityEngine.Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>{if(!m||!mats.ContainsKey(m.name))throw new System.Exception("Unknown material "+m?.name);return mats[m.name];}).ToArray();
var renderers=root.GetComponentsInChildren<UnityEngine.Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
if(bounds.size.y<.1f||bounds.size.y>5f)throw new System.Exception("Unexpected model scale: "+name+" "+bounds.size);
UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(root,prefabPath,UnityEditor.InteractionMode.AutomatedAction);
root.transform.position=new UnityEngine.Vector3((i%5-2)*4.1f,0,(i/5)*4.4f);
records.Add(new{name,prefabPath,size=bounds.size.ToString(),status="static art review, not gameplay-wired"});
var label=new UnityEngine.GameObject("Label_"+name).AddComponent<TMPro.TextMeshPro>();label.text=name.Replace("Study"," (study)");label.font=TMPro.TMP_Settings.defaultFontAsset;label.fontSize=3;label.alignment=TMPro.TextAlignmentOptions.Center;label.color=new UnityEngine.Color(.7f,.83f,.83f);label.rectTransform.sizeDelta=new UnityEngine.Vector2(3.9f,.8f);label.transform.position=root.transform.position+new UnityEngine.Vector3(0,.04f,-1.3f);label.transform.rotation=UnityEngine.Quaternion.Euler(65,0,0);
}
var cam=new UnityEngine.GameObject("Review Camera").AddComponent<UnityEngine.Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=8.5f;cam.transform.position=new UnityEngine.Vector3(0,16,-14);cam.transform.LookAt(new UnityEngine.Vector3(0,0,4));cam.clearFlags=UnityEngine.CameraClearFlags.SolidColor;cam.backgroundColor=new UnityEngine.Color(.028f,.045f,.059f);cam.nearClipPlane=.1f;cam.farClipPlane=100;
var title=new UnityEngine.GameObject("ReviewTitle").AddComponent<TMPro.TextMeshPro>();title.text="SOUL-HUNTER / LEVEL 01\nSTATIC 3D ART REVIEW - v01";title.font=TMPro.TMP_Settings.defaultFontAsset;title.fontSize=4;title.alignment=TMPro.TextAlignmentOptions.Center;title.rectTransform.sizeDelta=new UnityEngine.Vector2(15,2);title.transform.position=new UnityEngine.Vector3(0,1.5f,11);title.transform.rotation=UnityEngine.Quaternion.Euler(45,0,0);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath);UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(oldScene,false);
var reportPath="Logs/L01ReviewImport.json";System.IO.File.WriteAllText(reportPath,Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));
return new{scenePath,prefabs=records.Count,shaderErrors=UnityEditor.ShaderUtil.ShaderHasError(shader),reportPath};
