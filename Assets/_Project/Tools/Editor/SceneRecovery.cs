using UnityEditor;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.UI;
using SoulHunter.Gameplay.AI;

public static class SceneRecovery
{
    public static object RepairGameplay()
    {
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/Gameplay/Main_Gameplay.unity" || EditorApplication.isPlaying) throw new System.Exception("Open gameplay in Edit mode first.");
var backup = "Backups/SceneRecovery/" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
System.IO.Directory.CreateDirectory(backup);
void Backup(string path) { var dest=backup+"/"+path; System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest)); System.IO.File.Copy(path,dest,true); if(System.IO.File.Exists(path+".meta"))System.IO.File.Copy(path+".meta",dest+".meta",true); }
foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/") && (p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".asset")))) Backup(path);
void Folder(string p) { if(AssetDatabase.IsValidFolder(p))return; var parent=System.IO.Path.GetDirectoryName(p).Replace('\\','/'); Folder(parent); AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(p)); }
T Load<T>(string p) where T: UnityEngine.Object { var a=AssetDatabase.LoadAssetAtPath<T>(p); if(!a)throw new System.Exception("Missing asset: "+p); return a; }
GameObject Find(string name) => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).FirstOrDefault(g=>g.name==name);
GameObject Node(string name,Transform parent=null) { var g=Find(name); if(!g){g=new GameObject(name); g.transform.SetParent(parent,false);}return g; }
T Ensure<T>(GameObject g) where T:Component { var c=g.GetComponent<T>(); return c ? c : g.AddComponent<T>(); }
void Ref(UnityEngine.Object o,string field,UnityEngine.Object value) {var so=new SerializedObject(o);var p=so.FindProperty(field);if(p==null)throw new System.Exception(o.name+" missing field "+field);p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
void Value(UnityEngine.Object o,string field,float value) {var so=new SerializedObject(o);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
void Refs(UnityEngine.Object o,string field,UnityEngine.Object[] values) {var so=new SerializedObject(o);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
var managers=Find("--- MANAGERS ---").transform;
var runtime=Node("UI_Bindings",managers);
var boot=Ensure<SoulHunter.Core.Bootstrap.GameBootstrap>(Node("Scene_Services"));
var bootSo=new SerializedObject(boot);bootSo.FindProperty("_loadMenuOnStartup").boolValue=false;bootSo.ApplyModifiedPropertiesWithoutUndo();
Folder("Assets/Resources");Folder("Assets/Prefabs/Player");Folder("Assets/Prefabs/Enemies");
var catalog=AssetDatabase.LoadAssetAtPath<GameContentCatalog>("Assets/Resources/GameContent.asset");
if(!catalog){catalog=ScriptableObject.CreateInstance<GameContentCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Resources/GameContent.asset");}
catalog.Characters=AssetDatabase.FindAssets("t:CharacterData").Select(AssetDatabase.GUIDToAssetPath).Select(p=>Load<CharacterData>(p)).OrderBy(c=>c.CharacterName).ToArray();
var defaultHero=Load<GameObject>("Assets/Prefabs/Characters/Hero_1_0.prefab");
foreach(var c in catalog.Characters){if(!c.CharacterModelPrefab)c.CharacterModelPrefab=defaultHero;if(!c.CharacterIcon)c.CharacterIcon=c.CharacterModelPrefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite;EditorUtility.SetDirty(c);}
catalog.Upgrades=AssetDatabase.FindAssets("t:UpgradeData").Select(AssetDatabase.GUIDToAssetPath).Select(p=>Load<UpgradeData>(p)).Where(u=>u.Level>0).ToArray();EditorUtility.SetDirty(catalog);

// Repair the reusable weapon prefab, including its projectile dependency.
var wandPath="Assets/Prefabs/Weapons/Wand_Weapon.prefab";
var wand=PrefabUtility.LoadPrefabContents(wandPath);
try { Ref(Ensure<SoulHunter.Gameplay.Weapons.MagicWandWeapon>(wand),"_projectilePrefab",Load<GameObject>("Assets/Prefabs/Weapons/Bullet.prefab"));PrefabUtility.SaveAsPrefabAsset(wand,wandPath); } finally{PrefabUtility.UnloadPrefabContents(wand);}
var weapons=Find("Weapon_Manager").GetComponent<WeaponManager>();
string[] weaponFields={"_magicWandObject","_garlicWeaponObject","_whipWeaponObject","_axeWeaponObject","_bibleWeaponObject","_crossWeaponObject"};
string[] weaponNames={"Wand","Garlic","Whip","Axe","Bible","Cross"};
for(int i=0;i<weaponFields.Length;i++)Ref(weapons,weaponFields[i],Load<GameObject>("Assets/Prefabs/Weapons/"+weaponNames[i]+"_Weapon.prefab"));
Ensure<WeaponPoolManager>(Node("Weapon_Pool_Manager",managers));Ensure<RunStatsTracker>(Node("Run_Stats",managers));

// Restore the missing playable actor; use the existing character art at runtime.
var player=Node("Player",Find("--- ENTITIES ---").transform); player.tag="Player";player.transform.position=new Vector3(0,1,0);
var rb=Ensure<Rigidbody>(player);rb.useGravity=false;rb.isKinematic=false;rb.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionY;rb.interpolation=RigidbodyInterpolation.Interpolate;
var capsule=Ensure<CapsuleCollider>(player);capsule.height=2;capsule.radius=.4f;
Ensure<HealthController>(player);Ensure<PlayerStats>(player);Ensure<PlayerExperience>(player);Ensure<SoulHunter.Gameplay.Physics.EnvironmentScanner>(player);Ensure<PlayerController>(player);Ensure<SoulHunter.Gameplay.Combat.SoulBurstController>(player);
PrefabUtility.SaveAsPrefabAsset(player,"Assets/Prefabs/Player/Player.prefab");
var levelUpManager=Find("LevelUp_Manager").GetComponent<LevelUpManager>();
Ref(levelUpManager,"_experience",player.GetComponent<PlayerExperience>());
Ref(levelUpManager,"_weaponManager",weapons);
Ref(levelUpManager,"_playerController",player.GetComponent<PlayerController>());
Ref(Find("Main Camera").GetComponent<SoulHunter.Gameplay.CameraSystem.CameraFollow>(),"_target",player.transform);
var camera=Find("Main Camera").GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=12;camera.transform.position=player.transform.position+new Vector3(0,15,-10);camera.transform.LookAt(player.transform);camera.farClipPlane=250;camera.cullingMask=-1;

var enemyPath="Assets/Prefabs/Weapons/Enemy_Entity.prefab";
var enemy=PrefabUtility.LoadPrefabContents(enemyPath);
try {enemy.tag="Enemy";enemy.layer=LayerMask.NameToLayer("Enemy");var body=Ensure<Rigidbody>(enemy);body.useGravity=false;body.isKinematic=false;body.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionY;enemy.transform.localScale=Vector3.one;
 Ref(Ensure<EnemyController>(enemy),"_enemyData",Load<EnemyData>("Assets/_Project/Data/Config/Bat_Enemy_Data.asset"));Ensure<EnemyDrop>(enemy);Ensure<TouchDamage>(enemy);PrefabUtility.SaveAsPrefabAsset(enemy,enemyPath);
}finally{PrefabUtility.UnloadPrefabContents(enemy);}
var enemyPrefab=Load<GameObject>(enemyPath);
var waves=AssetDatabase.FindAssets("t:WaveData").Select(AssetDatabase.GUIDToAssetPath).Select(p=>Load<WaveData>(p)).OrderBy(w=>w.StartTimeInSeconds).ToArray();
foreach(var wave in waves){wave.EnemyPrefab=enemyPrefab;EditorUtility.SetDirty(wave);}
Refs(Find("EnemySpawner_Manager").GetComponent<EnemySpawner>(),"_waves",waves);Value(Find("EnemySpawner_Manager").GetComponent<EnemySpawner>(),"_spawnRadius",15);
var ground=Load<GameObject>("Assets/Prefabs/Environment/Ground.prefab");
Ref(Find("InfiniteMap_Manager").GetComponent<SoulHunter.Gameplay.Environment.InfiniteMap>(),"_chunkPrefab",ground);
float size=ground.GetComponent<MeshFilter>().sharedMesh.bounds.size.x*ground.transform.localScale.x;
Value(Find("InfiniteMap_Manager").GetComponent<SoulHunter.Gameplay.Environment.InfiniteMap>(),"_chunkSize",size);
var pool=Find("Pickup_Pool_Manager").GetComponent<SoulHunter.Gameplay.Pickups.PickupPoolManager>();
Ref(pool,"_playerController",player.GetComponent<PlayerController>());
Ref(pool,"_xpGemPrefab",Load<GameObject>("Assets/Prefabs/Items/XP_Gem.prefab"));Ref(pool,"_healthChickenPrefab",Load<GameObject>("Assets/Prefabs/Items/HealthChiken.prefab"));Ref(pool,"_chestPrefab",Load<GameObject>("Assets/Prefabs/Items/Chest.prefab"));

var canvas=Find("HUD_Canvas").GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
var healthSlider=Find("Player_HealthBar_Slider");var wrongXp=healthSlider.GetComponent<XPBarUI>();if(wrongXp)wrongXp.enabled=false;
Ref(Find("XP_Bar_Slider").GetComponent<XPBarUI>(),"_playerExperience",player.GetComponent<PlayerExperience>());
Ref(Find("GameHUD").GetComponent<GameHUDController>(),"_playerHealth",player.GetComponent<HealthController>());
Ref(Find("GameHUD").GetComponent<GameHUDController>(),"_weaponManager",weapons);
var gm=Find("GameSystem_Manager").GetComponent<GameSystemManager>();Ref(gm,"PauseMenuPanel",Find("PauseMenu_Panel"));Ref(gm,"VolumeSlider",Find("VolumeSlider").GetComponent<Slider>());
var level=Ensure<LevelUpUI>(runtime);Ref(level,"_levelUpManager",Find("LevelUp_Manager").GetComponent<LevelUpManager>());Ref(level,"_levelUpPanel",Find("LevelUp_Panel"));Refs(level,"_upgradeButtons",Enumerable.Range(0,3).Select(i=>(UnityEngine.Object)Find("Upgrade_Button_"+i).GetComponent<Button>()).ToArray());Find("LevelUp_Panel").GetComponent<LevelUpUI>().enabled=false;
var over=Ensure<GameOverUI>(runtime);Ref(over,"_gameOverPanel",Find("GameOver_Panel"));Ref(over,"_survivalTimeText",Find("SurvivalTime_Text").GetComponent<TMPro.TextMeshProUGUI>());Ref(over,"_restartButton",Find("Restart_Button").GetComponent<Button>());Find("GameOver_Panel").GetComponent<GameOverUI>().enabled=false;
var chest=Ensure<ChestUI>(runtime);Ref(chest,"_chestPanel",Find("Chest_Panel"));Ref(chest,"_rewardText",Find("Reward_Text").GetComponent<TMPro.TextMeshProUGUI>());Ref(chest,"_claimButton",Find("Claim_Button").GetComponent<Button>());Ref(chest,"_weaponManager",weapons);Find("Chest_Panel").GetComponent<ChestUI>().enabled=false;Find("Chest_Panel").GetComponent<ChestLogicController>().enabled=false;
Ref(pool,"_chestLogicController",Ensure<ChestLogicController>(Node("ChestLogic_Manager",managers)));
Ref(pool,"_chestUI",chest);
foreach(var name in new[]{"LevelUp_Panel","GameOver_Panel","Chest_Panel","PauseMenu_Panel"})Find(name).SetActive(false);
if(!scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Any()){var es=Node("EventSystem");Ensure<UnityEngine.EventSystems.EventSystem>(es);Ensure<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(es).AssignDefaultActions();}
EditorBuildSettings.scenes=new[]{"Assets/Scenes/Bootstrap/Bootstrap.unity","Assets/Scenes/Bootstrap/Loading.unity","Assets/Scenes/MainMenu/MainMenu.unity","Assets/Scenes/Gameplay/Main_Gameplay.unity"}.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
return new {backup,player=player.name,waves=waves.Length,characters=catalog.Characters.Length,upgrades=catalog.Upgrades.Length,groundSize=size,missing=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))};


    }
}
