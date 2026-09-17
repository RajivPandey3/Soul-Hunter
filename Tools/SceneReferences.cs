var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var rows = new System.Collections.Generic.List<object>();
foreach(var go in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject)) {
 foreach(var c in go.GetComponents<Component>()) {
  if(c==null) { rows.Add(new {go=go.name,type="MISSING"}); continue; }
  if(!c.GetType().FullName.StartsWith("SoulHunter")) continue;
  var so = new UnityEditor.SerializedObject(c); var p=so.GetIterator(); var fields=new System.Collections.Generic.List<object>();
  while(p.NextVisible(true)) { if(p.propertyType==UnityEditor.SerializedPropertyType.ObjectReference) fields.Add(new {field=p.propertyPath,value=p.objectReferenceValue ? p.objectReferenceValue.name : "NULL", id=p.objectReferenceInstanceIDValue}); }
  rows.Add(new {go=go.name,type=c.GetType().Name,fields});
 }
}
return new {scene=scene.path,rows};
