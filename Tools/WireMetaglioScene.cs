var path="Assets/Scenes/Gameplay/Main_Gameplay.unity";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
bool opened=!scene.isLoaded;
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
int managers=0,recovery=0;
try {
 var evo=UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.WeaponEvolutionData>("Metaglio/CrimsonShroudEvolution");
 foreach(var root in scene.GetRootGameObjects()) {
  foreach(var manager in root.GetComponentsInChildren<SoulHunter.Gameplay.Combat.WeaponManager>(true)) {
   var data=new UnityEditor.SerializedObject(manager);var list=data.FindProperty("_availableEvolutions");bool found=false;
   for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==evo)found=true;
   if(!found){int i=list.arraySize;list.InsertArrayElementAtIndex(i);list.GetArrayElementAtIndex(i).objectReferenceValue=evo;data.ApplyModifiedPropertiesWithoutUndo();}
   managers++;
  }
  foreach(var stats in root.GetComponentsInChildren<SoulHunter.Gameplay.Player.PlayerStats>(true))
   if(stats.GetComponent<SoulHunter.Gameplay.Combat.HealthController>()!=null && stats.GetComponent<SoulHunter.Gameplay.Player.PlayerRecovery>()==null){UnityEditor.Undo.AddComponent<SoulHunter.Gameplay.Player.PlayerRecovery>(stats.gameObject);recovery++;}
 }
 if(managers==0)throw new System.Exception("No scene manager found");
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Scene save failed");
 return new{managers,recovery,saved=true};
}finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
