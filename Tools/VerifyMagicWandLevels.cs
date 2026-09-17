var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=new UnityEngine.GameObject("MagicWandLevelVerification");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var checks=new System.Collections.Generic.List<string>();
try {
 var stats=root.AddComponent<SoulHunter.Gameplay.Player.PlayerStats>();
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Wand_Weapon.prefab");
 var host=UnityEngine.Object.Instantiate(prefab,root.transform);
 var wand=host.GetComponent<SoulHunter.Gameplay.Weapons.MagicWandWeapon>();
 wand.GetType().GetMethod("Awake",flags).Invoke(wand,null);
 int[] damages={10,10,10,10,20,20,20,30};
 int[] amounts={1,2,2,3,3,4,4,4};
 for(int level=1;level<=8;level++) {
  if(wand.CurrentLevel!=level || wand.BaseDamage!=damages[level-1] || wand.ShotCount!=amounts[level-1] || wand.Pierce!=(level>=7?2:1) || UnityEngine.Mathf.Abs(wand.EffectiveCooldown-(level>=3?1f:1.2f))>0.001f) throw new System.Exception("Level table mismatch: "+level);
  checks.Add("level "+level+" damage/amount/pierce/cooldown"); wand.LevelUp();
 }
 if(wand.CurrentLevel!=8) throw new System.Exception("Max level exceeded");
 stats.AddAmount(2); stats.ReduceCooldown(0.4f);
 if(wand.ShotCount!=6 || UnityEngine.Mathf.Abs(wand.EffectiveCooldown-0.6f)>0.001f) throw new System.Exception("Passive modifiers failed");
 checks.Add("Amount and cooldown modifiers");
 wand.CurrentLevel=1;
 var manager=root.AddComponent<SoulHunter.Gameplay.Combat.WeaponManager>();
 manager.GetType().GetMethod("ApplyWeaponLevel",flags).Invoke(manager,new object[]{host,8});
 if(wand.CurrentLevel!=8 || wand.BaseDamage!=30) throw new System.Exception("Manager did not apply upgrade");
 checks.Add("real WeaponManager upgrade route reaches level 8");
 manager.GetType().GetMethod("ApplyWeaponLevel",flags).Invoke(manager,new object[]{host,99});
 if(wand.CurrentLevel!=8) throw new System.Exception("Manager bypassed max level");
 checks.Add("manager respects base-weapon level cap");
 return new {passed=checks.Count,checks};
} finally { UnityEngine.Object.DestroyImmediate(root); UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }

