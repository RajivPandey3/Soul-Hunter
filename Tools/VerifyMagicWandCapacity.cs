if(!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var root=new UnityEngine.GameObject("MagicWandCapacityTest");root.transform.position=new UnityEngine.Vector3(20000,5,20000);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var shots=new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.Projectile>();
var checks=new System.Collections.Generic.List<string>();
try {
 SoulHunter.Gameplay.AI.EnemyController nearest=null;
 for(int i=0;i<61;i++) {
  var obj=new UnityEngine.GameObject("Target"+i);obj.transform.SetParent(root.transform);obj.transform.localPosition=UnityEngine.Vector3.forward*(i==60?2:10);
  var enemy=obj.AddComponent<SoulHunter.Gameplay.AI.EnemyController>(); enemy.Rigidbody.useGravity=false;enemy.Rigidbody.constraints=UnityEngine.RigidbodyConstraints.FreezeAll;
  if(i==60) nearest=enemy;
 }
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Wand_Weapon.prefab");
 var host=UnityEngine.Object.Instantiate(prefab,root.transform);
 var wand=host.GetComponent<SoulHunter.Gameplay.Weapons.MagicWandWeapon>();
 var find=wand.GetType().GetMethod("FindClosestEnemy",flags);
 if((UnityEngine.Transform)find.Invoke(wand,null)!=nearest.transform) throw new System.Exception("Nearest target beyond index 50 missed");
 checks.Add("nearest target found beyond first 50 enemies");
 nearest.gameObject.SetActive(false);
 if((UnityEngine.Transform)find.Invoke(wand,null)==nearest.transform) throw new System.Exception("Inactive enemy targeted");
 checks.Add("inactive targets ignored");
 wand.OnProjectileFired+=p=>shots.Add(p);
 var fire=wand.GetType().GetMethod("FireWand",flags);
 for(int i=0;i<61;i++) fire.Invoke(wand,null);
 if(shots.Count!=60) throw new System.Exception("Active projectile limit failed: "+shots.Count);
 checks.Add("60 active projectile limit");
 shots[0].gameObject.SetActive(false);
 fire.Invoke(wand,null);
 if(shots.Count!=61) throw new System.Exception("Deactivation did not release capacity");
 checks.Add("returned projectile releases capacity");
 var arcana=SoulHunter.Gameplay.Core.ArcanaManager.Instance;
 if(arcana!=null) {
  var property=arcana.GetType().GetProperty("GeminiEnabled"); bool old=(bool)property.GetValue(arcana);
  try { property.SetValue(arcana,true); shots[1].gameObject.SetActive(false);int before=shots.Count;fire.Invoke(wand,null);if(shots.Count!=before+1)throw new System.Exception("Gemini incorrectly duplicates Wand"); }
  finally {property.SetValue(arcana,old);}
  checks.Add("Gemini does not duplicate Magic Wand");
 }
 return new{passed=checks.Count,checks};
} finally {
 foreach(var shot in shots) if(shot!=null) { if(shot.GetComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>()!=null) shot.gameObject.SetActive(false); else UnityEngine.Object.Destroy(shot.gameObject); }
 UnityEngine.Object.Destroy(root);
}
