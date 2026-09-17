if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var rows=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/SH10_v01/manifest.json"));
const string progressPath="Logs/SH10_import_progress.json";
var completed=System.IO.File.Exists(progressPath)?Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(progressPath)):new Newtonsoft.Json.Linq.JArray();
void Folder(string path){if(UnityEditor.AssetDatabase.IsValidFolder(path))return;var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);UnityEditor.AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
UnityEditor.AssetDatabase.Refresh();
var shader=UnityEngine.Shader.Find("SoulHunter/Review/FacetedPreview");if(!shader||UnityEditor.ShaderUtil.ShaderHasError(shader))throw new System.Exception("Preview shader invalid");
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(original.isDirty)throw new System.Exception("Current scene has unsaved work");
var temp=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);UnityEngine.SceneManagement.SceneManager.SetActiveScene(temp);
int start=completed.Count;
try{
foreach(var row in rows.Skip(start).Take(3)){
string name=(string)row["name"],category=(string)row["category"];bool actor=(bool)row["actor"];
string path="Assets/Art/Models/"+name+".fbx",prefabPath="Assets/Prefabs/"+category+"/"+name+".prefab";
if(System.IO.File.Exists(prefabPath))throw new System.Exception("Unrecorded existing prefab; refusing overwrite: "+prefabPath);
var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(path);if(importer==null)throw new System.Exception("Missing importer "+path);
if(actor){importer.animationType=UnityEditor.ModelImporterAnimationType.Generic;var clips=importer.defaultClipAnimations;foreach(var c in clips){c.name=c.name.Split('|').Last();c.loopTime=c.name=="Idle"||c.name=="Run";}importer.clipAnimations=clips;importer.SaveAndReimport();}
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);if(!model)throw new System.Exception("Missing model "+path);
var root=new UnityEngine.GameObject(name);var visual=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model,root.transform);visual.transform.localRotation=UnityEngine.Quaternion.Euler(0,180,0)*model.transform.localRotation;
var matMap=new System.Collections.Generic.Dictionary<string,UnityEngine.Material>();
foreach(var property in ((Newtonsoft.Json.Linq.JObject)row["materials"]).Properties()){
string matPath="Assets/Art/Materials/"+property.Name+".mat";var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(matPath);
if(!mat){mat=new UnityEngine.Material(shader);var v=(Newtonsoft.Json.Linq.JArray)property.Value;mat.color=new UnityEngine.Color((float)v[0],(float)v[1],(float)v[2]);UnityEditor.AssetDatabase.CreateAsset(mat,matPath);}matMap.Add(property.Name,mat);}
foreach(var renderer in root.GetComponentsInChildren<UnityEngine.Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{if(!m||!matMap.ContainsKey(m.name))throw new System.Exception("Unknown material "+m?.name);return matMap[m.name];}).ToArray();
string[] clipNames=new string[0];int bones=0;
if(actor){
var clips=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();clipNames=clips.Select(c=>c.name).ToArray();
foreach(var needed in new[]{"Idle","Run","Attack","Death"})if(!clips.Any(c=>c.name==needed))throw new System.Exception(name+" missing clip "+needed+" actual "+string.Join(",",clipNames));
Folder("Assets/_Project/Animation/Controllers");string controllerPath="Assets/_Project/Animation/Controllers/"+name+".controller";
var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if(!controller){controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Speed",UnityEngine.AnimatorControllerParameterType.Float);controller.AddParameter("Attack",UnityEngine.AnimatorControllerParameterType.Trigger);controller.AddParameter("Death",UnityEngine.AnimatorControllerParameterType.Trigger);
var sm=controller.layers[0].stateMachine;var states=new System.Collections.Generic.Dictionary<string,UnityEditor.Animations.AnimatorState>();foreach(var clip in clips){var s=sm.AddState(clip.name);s.motion=clip;states.Add(clip.name,s);}sm.defaultState=states["Idle"];
var run=states["Idle"].AddTransition(states["Run"]);run.hasExitTime=false;run.duration=.12f;run.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater,.1f,"Speed");
var idle=states["Run"].AddTransition(states["Idle"]);idle.hasExitTime=false;idle.duration=.12f;idle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less,.1f,"Speed");
var attack=sm.AddAnyStateTransition(states["Attack"]);attack.hasExitTime=false;attack.canTransitionToSelf=false;attack.duration=.1f;attack.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If,0,"Attack");
var back=states["Attack"].AddTransition(states["Idle"]);back.hasExitTime=true;back.exitTime=1;back.duration=.1f;
var death=sm.AddAnyStateTransition(states["Death"]);death.hasExitTime=false;death.canTransitionToSelf=false;death.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If,0,"Death");UnityEditor.EditorUtility.SetDirty(controller);}
var animator=visual.GetComponentInChildren<UnityEngine.Animator>();if(!animator)animator=visual.AddComponent<UnityEngine.Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
bones=root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Sum(r=>r.bones.Length);if(bones<5)throw new System.Exception(name+" has no valid imported skin");
}
if(category=="Environment")foreach(var mesh in visual.GetComponentsInChildren<UnityEngine.MeshFilter>()){var collider=mesh.gameObject.AddComponent<UnityEngine.MeshCollider>();collider.sharedMesh=mesh.sharedMesh;}
var renderers=root.GetComponentsInChildren<UnityEngine.Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
if(bounds.size.y>10||bounds.size.y<.02f)throw new System.Exception("Invalid bounds "+name+" "+bounds.size);
Folder("Assets/Prefabs/"+category);UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,prefabPath);UnityEngine.Object.DestroyImmediate(root);
completed.Add(Newtonsoft.Json.Linq.JObject.FromObject(new{name,pack=(string)row["pack"],prefabPath,actor,bones,clips=clipNames,height=bounds.size.y,status=actor?"rigged visual prefab; gameplay not connected":"visual prefab; no gameplay logic"}));
UnityEditor.AssetDatabase.SaveAssets();System.IO.File.WriteAllText(progressPath,completed.ToString());
}
}finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(temp,true);}
return new{done=completed.Count,total=rows.Count,last=completed.Last?["name"]?.ToString()};
