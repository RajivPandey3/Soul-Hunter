var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay/Main_Gameplay.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var managerRoot = UnityEngine.GameObject.Find("GameSystem_Manager");
if (managerRoot == null) throw new System.Exception("GameSystem_Manager not found");
var manager = managerRoot.GetComponent<SoulHunter.Gameplay.Core.ArcanaManager>();
if (manager == null) manager = managerRoot.AddComponent<SoulHunter.Gameplay.Core.ArcanaManager>();
var paths = new[]{"Assets/_Project/Data/Abilities/Arcana_Awake.asset","Assets/_Project/Data/Abilities/Arcana_Waltz.asset","Assets/_Project/Data/Abilities/Arcana_IronBlueWill.asset","Assets/_Project/Data/Abilities/Arcana_Gemini.asset"};
var so = new UnityEditor.SerializedObject(manager); var available = so.FindProperty("_availableArcanas"); if (available == null) throw new System.Exception("_availableArcanas missing"); available.arraySize = paths.Length;
for (int i = 0; i < paths.Length; i++) { var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Data.ArcanaData>(paths[i]); if (asset == null) throw new System.Exception("Missing " + paths[i]); available.GetArrayElementAtIndex(i).objectReferenceValue = asset; }
so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(managerRoot); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); UnityEditor.AssetDatabase.SaveAssets();
return new { arcana = paths.Length, scene = scene.path };
