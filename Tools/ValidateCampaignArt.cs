if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var records=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Logs/SH10_import_progress.json"));
if(records.Count!=121)throw new System.Exception("Import incomplete");
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(original.isDirty)throw new System.Exception("Unsaved scene");
var temp=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);UnityEngine.SceneManagement.SceneManager.SetActiveScene(temp);
var results=new System.Collections.Generic.List<object>();int missing=0;int invalidMaterials=0;
try{foreach(var row in records){var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>((string)row["prefabPath"]);if(!prefab)throw new System.Exception("Missing prefab "+row["name"]);
missing+=prefab.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
invalidMaterials+=prefab.GetComponentsInChildren<UnityEngine.Renderer>(true).Sum(r=>r.sharedMaterials.Count(m=>!m||!m.shader||UnityEditor.ShaderUtil.ShaderHasError(m.shader)));
if(!(bool)row["actor"])continue;
var instance=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);var animator=instance.GetComponentInChildren<UnityEngine.Animator>();var skin=instance.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();var run=animator.runtimeAnimatorController.animationClips.First(c=>c.name=="Run");
var a=new UnityEngine.Mesh();var b=new UnityEngine.Mesh();run.SampleAnimation(animator.gameObject,0);skin.BakeMesh(a);run.SampleAnimation(animator.gameObject,.25f);skin.BakeMesh(b);
float movement=0;var av=a.vertices;var bv=b.vertices;for(int i=0;i<av.Length;i++)movement=UnityEngine.Mathf.Max(movement,(av[i]-bv[i]).magnitude);
results.Add(new{name=(string)row["name"],bones=skin.bones.Length,clips=animator.runtimeAnimatorController.animationClips.Length,maxRunVertexMovement=movement,runDeformsMesh=movement>.001f});
UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);UnityEngine.Object.DestroyImmediate(instance);
}}finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(temp,true);}
var report=new{prefabs=records.Count,missingScripts=missing,invalidMaterials,actors=results,scope="Import and sampled skin deformation only; gameplay tests deferred"};System.IO.File.WriteAllText("Logs/SH10_validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
