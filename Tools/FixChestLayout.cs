if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop Play first");
var path="Assets/Scenes/Gameplay/Main_Gameplay.unity";
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
 var ui=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SoulHunter.Gameplay.UI.ChestUI>(true)).First();
 var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 var panel=(UnityEngine.GameObject)ui.GetType().GetField("_chestPanel",flags).GetValue(ui);
 var label=(TMPro.TextMeshProUGUI)ui.GetType().GetField("_rewardText",flags).GetValue(ui);
 var button=(UnityEngine.UI.Button)ui.GetType().GetField("_claimButton",flags).GetValue(ui);
 var rect=panel.GetComponent<UnityEngine.RectTransform>();rect.anchorMin=UnityEngine.Vector2.zero;rect.anchorMax=UnityEngine.Vector2.one;rect.offsetMin=rect.offsetMax=UnityEngine.Vector2.zero;
 panel.GetComponent<UnityEngine.UI.Image>().color=new UnityEngine.Color(0.035f,0.045f,0.075f,0.98f);
 var art=panel.transform.Find("Chest_Image");if(art!=null)art.gameObject.SetActive(false);
 var open=panel.transform.Find("Open_Button");if(open!=null)open.gameObject.SetActive(false);
 var textRect=label.rectTransform;textRect.anchorMin=new UnityEngine.Vector2(0.15f,0.40f);textRect.anchorMax=new UnityEngine.Vector2(0.85f,0.75f);textRect.offsetMin=textRect.offsetMax=UnityEngine.Vector2.zero;
 label.enableAutoSizing=true;label.fontSizeMin=18;label.fontSizeMax=40;label.alignment=TMPro.TextAlignmentOptions.Center;
 var br=button.GetComponent<UnityEngine.RectTransform>();br.anchorMin=new UnityEngine.Vector2(0.35f,0.20f);br.anchorMax=new UnityEngine.Vector2(0.65f,0.29f);br.offsetMin=br.offsetMax=UnityEngine.Vector2.zero;
 var bt=button.GetComponentInChildren<TMPro.TMP_Text>();bt.text="Claim and Continue";bt.enableAutoSizing=true;bt.fontSizeMin=14;bt.fontSizeMax=28;
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return new{saved=true};
}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
