UnityEditor.EditorApplication.isPaused=false;UnityEngine.Application.runInBackground=true;
var settings=UnityEngine.Resources.Load<TMPro.TMP_Settings>("TMP Settings");
var labels=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(UnityEngine.FindObjectsSortMode.None);
foreach(var label in labels)label.ForceMeshUpdate();
UnityEngine.ScreenCapture.CaptureScreenshot("Logs/TMPVerified.png");
return new{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,settings=settings!=null,version=settings?new UnityEditor.SerializedObject(settings).FindProperty("assetVersion").stringValue:null,labels=labels.Select(t=>new{t.name,text=t.text,characters=t.textInfo.characterCount,vertices=t.mesh?t.mesh.vertexCount:0,font=t.font?t.font.name:null}).ToArray()};
