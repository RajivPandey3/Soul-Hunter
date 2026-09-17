if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay/Main_Gameplay.unity");
var heroSprite=UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/UI/sprite_60.png").OfType<UnityEngine.Sprite>().First();
int heroes=0;
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Characters"})){
var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
if(!System.IO.Path.GetFileName(path).StartsWith("Hero_"))continue;
var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try{foreach(var r in prefab.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))if(!r.sprite){r.sprite=heroSprite;r.sharedMaterial=new UnityEngine.Material(UnityEngine.Shader.Find("Sprites/Default"));r.sharedMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art/Materials/RecoverySprite.mat")??r.sharedMaterial;if(!UnityEditor.AssetDatabase.Contains(r.sharedMaterial))UnityEditor.AssetDatabase.CreateAsset(r.sharedMaterial,"Assets/Art/Materials/RecoverySprite.mat");heroes++;}UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);}}
var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_Project/ThirdParty/Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
int fonts=0;
foreach(var root in scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))if(!text.font){text.font=font;fonts++;}
var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Data.GameContentCatalog>("Assets/Resources/GameContent.asset");
foreach(var c in catalog.Characters)if(!c.CharacterIcon){c.CharacterIcon=heroSprite;UnityEditor.EditorUtility.SetDirty(c);}
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new {heroes,fonts,font=font?font.name:null};
