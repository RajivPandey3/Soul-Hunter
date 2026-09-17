var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=new UnityEngine.GameObject("WandContactVerification"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var checks=new System.Collections.Generic.List<string>();
try {
 var targets=new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.HealthController>();
 var colliders=new System.Collections.Generic.List<UnityEngine.Collider>();
 for(int i=0;i<5;i++) {
  var obj=new UnityEngine.GameObject("Target"+i); obj.transform.SetParent(root.transform); obj.transform.localPosition=UnityEngine.Vector3.forward*(i+1);
  obj.tag="Enemy"; colliders.Add(obj.AddComponent<UnityEngine.BoxCollider>());
  var health=obj.AddComponent<SoulHunter.Gameplay.Combat.HealthController>(); health.Initialize(100); health.KnockbackResistance=1; targets.Add(health);
 }
 var shot=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Sphere); shot.transform.SetParent(root.transform);
 var projectile=shot.AddComponent<SoulHunter.Gameplay.Combat.Projectile>();
 var hit=projectile.GetType().GetMethod("OnTriggerEnter",flags);
 projectile.Initialize(UnityEngine.Vector3.forward,10,20,3); projectile.ConfigureContacts(2,0,true);
 hit.Invoke(projectile,new object[]{colliders[0]}); hit.Invoke(projectile,new object[]{colliders[0]});
 if(targets[0].CurrentHealth!=80 || !shot.activeSelf) throw new System.Exception("Pierce/duplicate contact failure");
 checks.Add("piercing hit survives; repeated contact does not consume a hit");
 hit.Invoke(projectile,new object[]{colliders[1]});
 if(targets[1].CurrentHealth!=80 || shot.activeSelf) throw new System.Exception("Pierce budget failure");
 checks.Add("second distinct target consumes final pierce");
 shot.SetActive(true); projectile.Initialize(UnityEngine.Vector3.forward,10,10,3); projectile.ConfigureContacts(1,3,true);
 for(int i=0;i<4;i++) {hit.Invoke(projectile,new object[]{colliders[i]}); if(shot.activeSelf!=(i<3)) throw new System.Exception("Bounce budget failure "+i);}
 checks.Add("three bounces followed by final hit");
 shot.SetActive(true); projectile.Initialize(UnityEngine.Vector3.forward,10,10,3); projectile.ConfigureContacts(1,0,true);
 var wall=new UnityEngine.GameObject("Wall"); wall.transform.SetParent(root.transform); var wallCollider=wall.AddComponent<UnityEngine.BoxCollider>();
 hit.Invoke(projectile,new object[]{wallCollider}); if(shot.activeSelf) throw new System.Exception("Wall ignored");
 checks.Add("wand is blocked by solid obstacles");
 shot.SetActive(true); projectile.Initialize(UnityEngine.Vector3.forward,10,10,3); projectile.ConfigureContacts(1,0,true); wallCollider.isTrigger=true;
 hit.Invoke(projectile,new object[]{wallCollider}); if(!shot.activeSelf) throw new System.Exception("Trigger incorrectly blocked shot");
 checks.Add("non-damage trigger volumes do not block shots");
 projectile.Initialize(UnityEngine.Vector3.forward,10,10,3); wallCollider.isTrigger=false;
 hit.Invoke(projectile,new object[]{wallCollider}); if(!shot.activeSelf) throw new System.Exception("Reused projectile retained wall flag");
 hit.Invoke(projectile,new object[]{colliders[4]}); if(shot.activeSelf) throw new System.Exception("Reused projectile retained bounce/pierce");
 checks.Add("reuse resets wall, bounce and pierce configuration");
 return new {passed=checks.Count,checks};
} finally {UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
