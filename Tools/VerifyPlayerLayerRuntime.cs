if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play required");
var player=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
if(player==null)throw new System.Exception("Gameplay required");
var health=player.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
var xp=player.GetComponent<SoulHunter.Gameplay.Player.PlayerExperience>();
int beforeXP=xp.CurrentXP;health.Initialize(100);health.IsInvincible=false;health.KnockbackResistance=1;
player.transform.position=new UnityEngine.Vector3(10000,1,10000);
var enemy=new UnityEngine.GameObject("LayerContactTest");enemy.layer=UnityEngine.LayerMask.NameToLayer("Enemy");if(enemy.layer<0)enemy.layer=0;
enemy.transform.position=player.transform.position;
var col=enemy.AddComponent<UnityEngine.SphereCollider>();col.isTrigger=true;col.radius=1;
var rb=enemy.AddComponent<UnityEngine.Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
var damage=enemy.AddComponent<SoulHunter.Gameplay.Combat.TouchDamage>();damage.DamageAmount=20;damage.DamageInterval=100;
UnityEngine.GameObject chicken=null,gem=null;
float start=UnityEngine.Time.time;double deadline=UnityEditor.EditorApplication.timeSinceStartup+20;int step=0;
UnityEditor.EditorApplication.CallbackFunction update=null;
update=()=>{
 try {
  if(!UnityEditor.EditorApplication.isPlaying||player==null)throw new System.Exception("Interrupted");
  if(UnityEditor.EditorApplication.timeSinceStartup>deadline)throw new System.Exception("Layer regression timed out at step "+step);
  if(step==0 && health.CurrentHealth==80){
   UnityEngine.Object.Destroy(enemy);
   chicken=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Items/HealthChiken.prefab"),player.transform.position,UnityEngine.Quaternion.identity);
   gem=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Items/XP_Gem.prefab"),player.transform.position,UnityEngine.Quaternion.identity);
   step=1;
  }
  if(step==1 && !chicken.activeSelf && !gem.activeSelf){
   if(health.CurrentHealth!=100||xp.CurrentXP<=beforeXP)throw new System.Exception("Pickup effects failed HP="+health.CurrentHealth+" XP="+xp.CurrentXP+" before="+beforeXP);
   System.IO.File.WriteAllText("Logs/PlayerLayerRuntimeVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,checks=3,hp=health.CurrentHealth,xp=xp.CurrentXP,elapsed=UnityEngine.Time.time-start}));
   UnityEditor.EditorApplication.update-=update;UnityEngine.Object.Destroy(chicken);UnityEngine.Object.Destroy(gem);
  }
 }catch(System.Exception e){UnityEditor.EditorApplication.update-=update;UnityEngine.Object.Destroy(enemy);if(chicken!=null)UnityEngine.Object.Destroy(chicken);if(gem!=null)UnityEngine.Object.Destroy(gem);System.IO.File.WriteAllText("Logs/PlayerLayerRuntimeVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=false,error=e.Message}));}
};
UnityEditor.EditorApplication.update+=update;UnityEngine.Physics.SyncTransforms();return new{started=true};
