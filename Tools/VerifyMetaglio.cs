var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=new UnityEngine.GameObject("MetaglioTest");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
int checks=0;
try {
 var health=root.AddComponent<SoulHunter.Gameplay.Combat.HealthController>();health.Initialize(100);health.KnockbackResistance=1;
 var stats=root.AddComponent<SoulHunter.Gameplay.Player.PlayerStats>();
 var manager=root.AddComponent<SoulHunter.Gameplay.Combat.WeaponManager>();
 var left=UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.UpgradeData>("Metaglio/MetaglioLeft_Lv1");
 var right=UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.UpgradeData>("Metaglio/MetaglioRight_Lv1");
 manager.ApplyUpgrade(left);if(manager.GetWeaponLevel(left.Type)!=0)throw new System.Exception("Initial level-up acquisition allowed");checks++;
 typeof(SoulHunter.Gameplay.Combat.WeaponManager).GetField("_maxPassiveSlots",flags).SetValue(manager,0);
 if(!manager.AcquireStagePassive(left)||!manager.AcquireStagePassive(right)||manager.GetPassiveCount()!=2||manager.GetActiveWeaponsCount()!=0)throw new System.Exception("Stage acquisition/slots");checks++;
 if(manager.AcquireStagePassive(left)||health.MaxHealth!=100||stats.Regen!=0||stats.Curse!=1)throw new System.Exception("Level one/duplicate");checks++;
 for(int level=2;level<=9;level++){
  var l=UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.UpgradeData>("Metaglio/MetaglioLeft_Lv"+level);
  var r=UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.UpgradeData>("Metaglio/MetaglioRight_Lv"+level);
  manager.ApplyUpgrade(l);manager.ApplyUpgrade(r);
  if(health.MaxHealth!=UnityEngine.Mathf.RoundToInt(100*UnityEngine.Mathf.Pow(1.05f,level-1)) || UnityEngine.Mathf.Abs(stats.Regen-(level-1)*0.1f)>0.001f || UnityEngine.Mathf.Abs(stats.Curse-(1+(level-1)*0.05f))>0.001f)throw new System.Exception("Progression "+level);
  checks++;
 }
 manager.ApplyUpgrade(UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.UpgradeData>("Metaglio/MetaglioLeft_Lv9"));
 if(health.MaxHealth!=148 || manager.GetMaxWeaponLevel(left.Type)!=9)throw new System.Exception("Duplicate/max level");checks++;
 var recovery=root.AddComponent<SoulHunter.Gameplay.Player.PlayerRecovery>();
 recovery.GetType().GetMethod("Awake",flags).Invoke(recovery,null);
 health.TakeDamage(new SoulHunter.Gameplay.Combat.DamagePacket(10,UnityEngine.Vector3.zero,UnityEngine.Vector3.zero));
 var tick=recovery.GetType().GetMethod("AdvanceRecovery",flags);
 tick.Invoke(recovery,new object[]{0f});if(health.CurrentHealth!=138)throw new System.Exception("Paused recovery");
 tick.Invoke(recovery,new object[]{1f});if(health.CurrentHealth!=138)throw new System.Exception("Fractional recovery early");
 tick.Invoke(recovery,new object[]{0.3f});if(health.CurrentHealth!=139)throw new System.Exception("Fractional recovery lost");checks++;
 var evo=UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.WeaponEvolutionData>("Metaglio/CrimsonShroudEvolution");
 if(evo.RequirementsMet(manager))throw new System.Exception("Missing Laurel accepted");
 manager.GiveWeapon(evo.BaseWeapon,7);
 if(!evo.RequirementsMet(manager)||evo.EvolvedWeaponPrefab==null)throw new System.Exception("Authored evolution invalid");checks++;
 manager.GiveWeapon(right.Type,8);if(evo.RequirementsMet(manager))throw new System.Exception("Right level 8 accepted");checks++;
 manager.GiveWeapon(right.Type,9);
 typeof(SoulHunter.Gameplay.Combat.WeaponManager).GetField("_availableEvolutions",flags).SetValue(manager,new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.WeaponEvolutionData>{evo});
 string evolved;
 if(!manager.TryEvolveWeapon(out evolved)||evolved!="Crimson Shroud"||root.GetComponentInChildren<SoulHunter.Gameplay.Combat.CrimsonShroudWeapon>()==null)throw new System.Exception("Evolution spawn failed");
 if(manager.TryEvolveWeapon(out evolved))throw new System.Exception("Duplicate evolution");checks++;
 return new{passed=checks,maxHealth=health.MaxHealth,recovery=stats.Regen,curse=stats.Curse};
}finally{UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
