var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay/Main_Gameplay.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var progressionObject = UnityEngine.GameObject.Find("LevelProgression_Manager");
if (progressionObject == null) throw new System.Exception("LevelProgression_Manager not found");
if (!progressionObject.GetComponent<SoulHunter.Gameplay.Core.CampaignSceneConnector>()) progressionObject.AddComponent<SoulHunter.Gameplay.Core.CampaignSceneConnector>();
if (!progressionObject.GetComponent<SoulHunter.Gameplay.Core.CampaignSignatureSystem>()) progressionObject.AddComponent<SoulHunter.Gameplay.Core.CampaignSignatureSystem>();
var spawnerObject = UnityEngine.GameObject.Find("EnemySpawner_Manager");
if (spawnerObject == null) throw new System.Exception("EnemySpawner_Manager not found");
var spawner = spawnerObject.GetComponent<SoulHunter.Gameplay.AI.EnemySpawner>();
if (spawner == null) throw new System.Exception("EnemySpawner missing");
var so = new UnityEditor.SerializedObject(spawner);
var enemyPaths = new string[10]; var elitePaths = new string[10]; var bossPaths = new string[10];
for (int i = 0; i < 10; i++) { string n = (i + 1).ToString("00"); enemyPaths[i] = $"Assets/Prefabs/Enemies/SH10_L{n}_Enemy.prefab"; elitePaths[i] = $"Assets/Prefabs/Enemies/SH10_L{n}_Elite.prefab"; bossPaths[i] = $"Assets/Prefabs/Bosses/SH10_L{n}_Boss.prefab"; }
var lists = new[]{("_stageEnemyPrefabs", enemyPaths), ("_stageElitePrefabs", elitePaths), ("_stageBossPrefabs", bossPaths)};
foreach (var list in lists) { var prop = so.FindProperty(list.Item1); if (prop == null) throw new System.Exception("Missing serialized field " + list.Item1); prop.arraySize = list.Item2.Length; for (int i = 0; i < list.Item2.Length; i++) { var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(list.Item2[i]); if (asset == null) throw new System.Exception("Missing asset " + list.Item2[i]); prop.GetArrayElementAtIndex(i).objectReferenceValue = asset; } }
so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(progressionObject); UnityEditor.EditorUtility.SetDirty(spawnerObject); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); UnityEditor.AssetDatabase.SaveAssets();
return new { scene = scene.path, connectors = true, levelPrefabs = 10 };
