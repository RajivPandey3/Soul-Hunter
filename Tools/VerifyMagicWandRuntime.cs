if(!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
UnityEngine.Application.runInBackground=true;
var root=new UnityEngine.GameObject("MagicWandRuntimeTest"); root.transform.position=new UnityEngine.Vector3(10000,5,10000);
var stats=root.AddComponent<SoulHunter.Gameplay.Player.PlayerStats>(); stats.enabled=false;
stats.AddMight(1f); stats.AddAmount(1); stats.AddProjectileSpeed(1f); stats.AddArea(0.5f); stats.ReduceCooldown(0.4f);
var targets=new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.HealthController>();
for(int i=0;i<2;i++) {
 var target=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
 target.transform.SetParent(root.transform); target.transform.localPosition=new UnityEngine.Vector3(0,0,2+i*4);
 target.tag="Enemy";
 var health=target.AddComponent<SoulHunter.Gameplay.Combat.HealthController>(); health.Initialize(500); health.KnockbackResistance=1;
 var enemy=target.AddComponent<SoulHunter.Gameplay.AI.EnemyController>(); enemy.Target=root.transform;
 enemy.Rigidbody.useGravity=false; enemy.Rigidbody.constraints=UnityEngine.RigidbodyConstraints.FreezeAll;
 targets.Add(health);
}
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Wand_Weapon.prefab");
var host=UnityEngine.Object.Instantiate(prefab,root.transform);
var wand=host.GetComponent<SoulHunter.Gameplay.Weapons.MagicWandWeapon>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var times=new System.Collections.Generic.List<float>();
var shots=new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.Projectile>();
var damage=new System.Collections.Generic.List<int>();
var speeds=new System.Collections.Generic.List<float>();
var scales=new System.Collections.Generic.List<float>();
var shotPrefab=(UnityEngine.GameObject)wand.GetType().GetField("_projectilePrefab",flags).GetValue(wand);
wand.OnProjectileFired+=shot=>{
 times.Add(UnityEngine.Time.time); shots.Add(shot);
 damage.Add((int)shot.GetType().GetField("_damage",flags).GetValue(shot));
 speeds.Add((float)shot.GetType().GetField("_speed",flags).GetValue(shot));
 scales.Add(shot.transform.localScale.x/shotPrefab.transform.localScale.x);
 if(times.Count==2) wand.enabled=false;
};
float start=UnityEngine.Time.time;
double deadline=UnityEditor.EditorApplication.timeSinceStartup+20;
UnityEditor.EditorApplication.CallbackFunction update=null;
update=()=>{
 if(UnityEngine.Time.time-start<1f && UnityEditor.EditorApplication.timeSinceStartup<deadline && UnityEditor.EditorApplication.isPlaying) return;
 try {
  bool pass=times.Count==2 && damage.All(d=>d==20) && speeds.All(s=>UnityEngine.Mathf.Abs(s-40f)<0.001f) && scales.All(s=>UnityEngine.Mathf.Abs(s-1.5f)<0.001f) && targets[0].CurrentHealth==460 && targets[1].CurrentHealth==500 && times[1]-times[0]>=0.06f && times[1]-times[0]<0.2f;
  var result=new{passed=pass,shots=times.Count,damage,speeds,scales,interval=times.Count>=2?times[1]-times[0]:-1,nearestHealth=targets[0].CurrentHealth,farHealth=targets[1].CurrentHealth};
  System.IO.File.WriteAllText("Logs/MagicWandRuntimeVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 } finally {
  UnityEditor.EditorApplication.update-=update;
  foreach(var shot in shots) if(shot!=null) { if(shot.GetComponent<SoulHunter.Gameplay.Pickups.AutoReturnToPool>()!=null) shot.gameObject.SetActive(false); else UnityEngine.Object.Destroy(shot.gameObject); }
  if(root!=null) UnityEngine.Object.Destroy(root);
 }
};
UnityEditor.EditorApplication.update+=update;
return "Physics and burst test scheduled";
