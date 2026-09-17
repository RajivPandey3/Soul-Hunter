if(UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Stable Edit mode required");
object References(UnityEngine.Object obj){
var rows=new System.Collections.Generic.List<object>();var so=new UnityEditor.SerializedObject(obj);var p=so.GetIterator();
while(p.Next(true)){if(p.propertyType!=UnityEditor.SerializedPropertyType.ObjectReference || p.propertyPath=="m_Script")continue;
var value=p.objectReferenceValue;rows.Add(new{field=p.propertyPath,target=value?value.name:null,path=value?UnityEditor.AssetDatabase.GetAssetPath(value):null,broken=!value&&p.objectReferenceInstanceIDValue!=0});}
return rows;}
object Inspect(UnityEngine.GameObject root){var ts=root.GetComponentsInChildren<UnityEngine.Transform>(true);return new{
missingScripts=ts.Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
components=ts.SelectMany(t=>t.GetComponents<UnityEngine.MonoBehaviour>()).Where(c=>c&&c.GetType().Namespace!=null&&c.GetType().Namespace.StartsWith("SoulHunter")).Select(c=>new{objectName=c.name,type=c.GetType().FullName,enabled=c.enabled,refs=References(c)}).ToArray(),
renderers=root.GetComponentsInChildren<UnityEngine.Renderer>(true).Select(r=>new{r.name,type=r.GetType().Name,materials=r.sharedMaterials.Select(m=>m?UnityEditor.AssetDatabase.GetAssetPath(m):null).ToArray()}).ToArray(),
animators=root.GetComponentsInChildren<UnityEngine.Animator>(true).Select(a=>new{a.name,controller=a.runtimeAnimatorController?UnityEditor.AssetDatabase.GetAssetPath(a.runtimeAnimatorController):null,avatar=a.avatar?UnityEditor.AssetDatabase.GetAssetPath(a.avatar):null}).ToArray()};}
var prefabs=UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".prefab")).Select(p=>new{path=p,details=Inspect(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p))}).ToArray();
var data=UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject",new[]{"Assets/_Project/Data","Assets/Resources"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Distinct().Select(p=>UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(p)).Where(o=>o&&o.GetType().Namespace!=null&&o.GetType().Namespace.StartsWith("SoulHunter")).Select(o=>new{path=UnityEditor.AssetDatabase.GetAssetPath(o),type=o.GetType().FullName,refs=References(o)}).ToArray();
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var report=new{utc=System.DateTime.UtcNow.ToString("O"),unity=UnityEngine.Application.unityVersion,scene=scene.path,dirty=scene.isDirty,sceneRoots=scene.GetRootGameObjects().Select(g=>new{g.name,details=Inspect(g)}).ToArray(),buildScenes=UnityEditor.EditorBuildSettings.scenes.Select(s=>new{s.path,s.enabled}).ToArray(),prefabs,data};
var output="Logs/AssetProductionAudit-"+System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json";
System.IO.File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return new{output,prefabCount=prefabs.Length,dataCount=data.Length,scene=scene.path};
