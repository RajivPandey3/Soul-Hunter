if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var levels=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/SH10_v01/levels.json"));
var assets=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Logs/SH10_import_progress.json"));
var paths=System.IO.File.Exists("Logs/SH10_scenes.json")?Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Logs/SH10_scenes.json")):new Newtonsoft.Json.Linq.JArray();
int index=paths.Count;if(index>=10)return new{done=index,total=10};
string pack=(string)levels[index]["folder"],title=(string)levels[index]["name"],scenePath="Assets/Scenes/Levels/SH10_L"+(index+1).ToString("00")+"_Showcase.unity";
if(System.IO.File.Exists(scenePath))throw new System.Exception("Existing scene; refusing overwrite");
if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Scenes/Levels"))UnityEditor.AssetDatabase.CreateFolder("Assets/Scenes","Levels");
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(original.isDirty)throw new System.Exception("Save current work first");
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var rows=assets.Where(a=>(string)a["pack"]==pack).ToArray();if(rows.Length!=9)throw new System.Exception("Level needs nine validated art prefabs");
UnityEngine.GameObject Spawn(string path,UnityEngine.Vector3 position,float scale=1){var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);var go=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset);go.transform.position=position;go.transform.localScale=UnityEngine.Vector3.one*scale;return go;}
foreach(var row in rows){string name=(string)row["name"],path=(string)row["prefabPath"];
if(name.EndsWith("_Ground")){for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)Spawn(path,new UnityEngine.Vector3(x*10,0,z*10));}}
var props=rows.Where(r=>!(bool)r["actor"]&&!((string)r["name"]).EndsWith("_Ground")).ToArray();
var positions=new[]{new UnityEngine.Vector3(-5,0,-3),new UnityEngine.Vector3(5,0,-3),new UnityEngine.Vector3(0,0,6),new UnityEngine.Vector3(-6,0,4),new UnityEngine.Vector3(6,0,4)};
for(int i=0;i<props.Length;i++){Spawn((string)props[i]["prefabPath"],positions[i]);}
var actors=rows.Where(r=>(bool)r["actor"]).ToArray();for(int i=0;i<actors.Length;i++)Spawn((string)actors[i]["prefabPath"],new UnityEngine.Vector3((i-1)*2.3f,0,0),i==2?1.35f:1);
var camera=new UnityEngine.GameObject("Showcase Camera").AddComponent<UnityEngine.Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=10.5f;camera.transform.position=new UnityEngine.Vector3(10,16,-18);camera.transform.LookAt(new UnityEngine.Vector3(0,0,1));camera.backgroundColor=new UnityEngine.Color(.025f,.035f,.055f);camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;
var label=new UnityEngine.GameObject("Asset pack title").AddComponent<TMPro.TextMeshPro>();label.font=TMPro.TMP_Settings.defaultFontAsset;label.text="LEVEL "+(index+1).ToString("00")+" / "+title+"\n3D ART SHOWCASE - NOT A GAMEPLAY LEVEL";label.fontSize=3.2f;label.alignment=TMPro.TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new UnityEngine.Vector2(23,2);label.transform.position=new UnityEngine.Vector3(0,4,9);label.transform.rotation=camera.transform.rotation;
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath);UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.CloseScene(original,false);
paths.Add(Newtonsoft.Json.Linq.JObject.FromObject(new{pack,scenePath,title}));System.IO.File.WriteAllText("Logs/SH10_scenes.json",paths.ToString());
return new{done=paths.Count,total=10,scenePath,pack};
