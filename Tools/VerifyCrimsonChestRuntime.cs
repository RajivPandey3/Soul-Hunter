if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required");
var manager=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Combat.WeaponManager>();
var player=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
if(manager==null||player==null)throw new System.Exception("Gameplay not ready");
UnityEngine.Application.runInBackground=true;
int checks=0;
manager.GiveWeapon(SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel,7);
foreach(var name in new[]{"MetaglioLeft","MetaglioRight"}){
 var first=UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.UpgradeData>("Metaglio/"+name+"_Lv1");
 if(!manager.AcquireStagePassive(first))throw new System.Exception("Stage acquisition failed");
 for(int i=2;i<=9;i++)manager.ApplyUpgrade(UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.UpgradeData>("Metaglio/"+name+"_Lv"+i));
}
var old=player.GetComponentInChildren<SoulHunter.Gameplay.Combat.LaurelWeapon>();
if(old==null||old.CurrentLevel!=7)throw new System.Exception("Laurel not active at level 7");checks++;
var chest=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.ChestLogicController>();
bool created=chest==null;
if(created)chest=new UnityEngine.GameObject("ChestVerification").AddComponent<SoulHunter.Gameplay.Core.ChestLogicController>();
try {
 string reward=chest.ProcessChest();
 if(!reward.Contains("Crimson Shroud"))throw new System.Exception("Chest reward mismatch");checks++;
 if(old.gameObject.activeSelf)throw new System.Exception("Old Laurel still active");checks++;
 var shield=player.GetComponentInChildren<SoulHunter.Gameplay.Combat.CrimsonShroudWeapon>();
 if(shield==null||shield.CurrentLevel!=1||shield.CurrentCharges!=3)throw new System.Exception("Shroud spawn failed");checks++;
 var health=player.GetComponent<SoulHunter.Gameplay.Combat.HealthController>();
 health.IsInvincible=false;int before=health.CurrentHealth;
 health.TakeDamage(new SoulHunter.Gameplay.Combat.DamagePacket(999,UnityEngine.Vector3.zero,UnityEngine.Vector3.zero));
 if(health.CurrentHealth!=before||shield.CurrentCharges!=2||shield.PendingPulses<1)throw new System.Exception("Evolved shield not owning damage");checks++;
 string duplicate;if(manager.TryEvolveWeapon(out duplicate))throw new System.Exception("Duplicate evolution");checks++;
 var evidence=new{passed=true,checks,reward,charges=shield.CurrentCharges,oldLaurelActive=old.gameObject.activeSelf};
 System.IO.File.WriteAllText("Logs/CrimsonChestRuntimeVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(evidence));
 return evidence;
}finally{if(created)UnityEngine.Object.Destroy(chest.gameObject);}
