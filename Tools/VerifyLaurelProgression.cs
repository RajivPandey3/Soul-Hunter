var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=new UnityEngine.GameObject("LaurelProgressionTest");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var checks=new System.Collections.Generic.List<string>();
try {
 var health=root.AddComponent<SoulHunter.Gameplay.Combat.HealthController>();health.Initialize(100);health.KnockbackResistance=1;
 var stats=root.AddComponent<SoulHunter.Gameplay.Player.PlayerStats>();
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Generated/Laurel.prefab");
 var host=UnityEngine.Object.Instantiate(prefab,root.transform);var shield=host.GetComponent<SoulHunter.Gameplay.Combat.LaurelWeapon>();var type=shield.GetType();
 type.GetMethod("Awake",flags).Invoke(shield,null);type.GetMethod("OnEnable",flags).Invoke(shield,null);
 var tick=type.GetMethod("AdvanceTime",flags);
 float[] cooldown={10,9.5f,9,9,8.5f,8,8};int[] charges={1,1,1,2,2,2,3};float[] grace={0.2f,0.4f,0.6f,0.6f,0.8f,1,1};
 for(int i=0;i<7;i++){
  if(shield.CurrentLevel!=i+1 || shield.MaxCharges!=charges[i] || UnityEngine.Mathf.Abs(shield.BaseRechargeCooldown-cooldown[i])>0.001f || UnityEngine.Mathf.Abs(shield.BlockGraceSeconds-grace[i])>0.001f)throw new System.Exception("Level table mismatch "+(i+1));
  tick.Invoke(shield,new object[]{100f});if(shield.CurrentCharges!=charges[i])throw new System.Exception("Capacity recharge failed");
  checks.Add("level "+(i+1)+" cooldown/charge/grace");shield.LevelUp();
 }
 if(shield.CurrentLevel!=7)throw new System.Exception("Level cap failed");checks.Add("seven-level cap");
 stats.AddAmount(5);stats.AddDuration(5);stats.AddMight(5);stats.AddArea(5);stats.AddProjectileSpeed(5);
 if(shield.MaxCharges!=3 || shield.BlockGraceSeconds!=1f || shield.EffectiveRechargeCooldown!=8f)throw new System.Exception("Unsupported stat affects shield");checks.Add("non-cooldown modifiers ignored");
 var hit=new SoulHunter.Gameplay.Combat.DamagePacket(10,UnityEngine.Vector3.zero,UnityEngine.Vector3.zero);
 int blocks=0;shield.OnBlocked+=p=>blocks++;
 health.TakeDamage(hit);health.TakeDamage(hit);
 if(shield.CurrentCharges!=2 || blocks!=1 || health.CurrentHealth!=100)throw new System.Exception("Block event/grace failed");checks.Add("one block event and charge during grace");
 tick.Invoke(shield,new object[]{0f});if(shield.CurrentCharges!=2)throw new System.Exception("Pause recharged shield");
 var visual=(UnityEngine.GameObject)type.GetField("_activeShield",flags).GetValue(shield); var renderer=visual.GetComponentInChildren<UnityEngine.Renderer>(true);var block=new UnityEngine.MaterialPropertyBlock();renderer.GetPropertyBlock(block);
 if(block.GetColor("_Color")!=UnityEngine.Color.green)throw new System.Exception("Two-charge feedback incorrect");checks.Add("two-charge color feedback");
 tick.Invoke(shield,new object[]{4f});stats.ReduceCooldown(0.5f);
 tick.Invoke(shield,new object[]{1.9f});if(shield.CurrentCharges!=2)throw new System.Exception("Recharged too early");
 tick.Invoke(shield,new object[]{0.11f});if(shield.CurrentCharges!=3)throw new System.Exception("Mid-recharge cooldown bonus lost progress");checks.Add("cooldown bonus preserves partial recharge");
 renderer.GetPropertyBlock(block);if(block.GetColor("_Color")!=UnityEngine.Color.yellow)throw new System.Exception("Three-charge feedback incorrect");checks.Add("three-charge color feedback");
 var manager=root.AddComponent<SoulHunter.Gameplay.Combat.WeaponManager>();
 typeof(SoulHunter.Gameplay.Combat.WeaponManager).GetField("_additionalWeaponBindings",flags).SetValue(manager,new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.WeaponBinding>{new SoulHunter.Gameplay.Combat.WeaponBinding{Type=SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel,WeaponPrefab=prefab}});
 manager.GiveWeapon(SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel,99);
 if(manager.GetWeaponLevel(SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel)!=7)throw new System.Exception("Inventory level bypassed cap");checks.Add("inventory respects prefab level cap");
 return new{passed=checks.Count,checks};
}finally{UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}

