var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay/Main_Gameplay.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var managerRoot = UnityEngine.GameObject.Find("GameSystem_Manager");
if (managerRoot == null) throw new System.Exception("GameSystem_Manager not found");
var manager = managerRoot.GetComponent<SoulHunter.Gameplay.Core.ArcanaManager>();
if (manager == null) manager = managerRoot.AddComponent<SoulHunter.Gameplay.Core.ArcanaManager>();
var folder = "Assets/_Project/Data/Abilities";
var specs = new[]{("Arcana_IronBlueWill", "X", "Iron Blue Will", "Projectiles bounce once.", SoulHunter.Gameplay.Data.ArcanaData.ArcanaType.IronBlueWill), ("Arcana_Gemini", "I", "Gemini", "Compatible projectiles duplicate.", SoulHunter.Gameplay.Data.ArcanaData.ArcanaType.Gemini)};
foreach (var spec in specs)
{
    string path = $"{folder}/{spec.Item1}.asset";
    var data = UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Data.ArcanaData>(path);
    if (data == null) { data = UnityEngine.ScriptableObject.CreateInstance<SoulHunter.Gameplay.Data.ArcanaData>(); UnityEditor.AssetDatabase.CreateAsset(data, path); }
    data.CardNumber = spec.Item2; data.CardName = spec.Item3; data.Description = spec.Item4; data.Type = spec.Item5; UnityEditor.EditorUtility.SetDirty(data);
}
UnityEditor.AssetDatabase.SaveAssets();
var arcanaAssets = UnityEditor.AssetDatabase.FindAssets("t:ArcanaData", new[]{folder}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Select(UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Data.ArcanaData>).Where(x => x != null).ToArray();
var so = new UnityEditor.SerializedObject(manager); var available = so.FindProperty("_availableArcanas"); available.arraySize = arcanaAssets.Length; for (int i = 0; i < arcanaAssets.Length; i++) available.GetArrayElementAtIndex(i).objectReferenceValue = arcanaAssets[i]; so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(managerRoot); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); UnityEditor.AssetDatabase.SaveAssets();
return new { arcana = arcanaAssets.Length, scene = scene.path };
