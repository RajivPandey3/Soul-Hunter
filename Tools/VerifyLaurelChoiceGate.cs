if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required");
var root=new UnityEngine.GameObject("LaurelChoiceGateTest");
var bad=UnityEngine.ScriptableObject.CreateInstance<SoulHunter.Gameplay.Combat.UpgradeData>();
var good=UnityEngine.ScriptableObject.CreateInstance<SoulHunter.Gameplay.Combat.UpgradeData>();
try {
 var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 var manager=root.AddComponent<SoulHunter.Gameplay.Combat.WeaponManager>();
 if(UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Combat.WeaponManager>()!=manager)throw new System.Exception("Run this isolated test in MainMenu");
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Generated/Laurel.prefab");
 manager.GetType().GetField("_additionalWeaponBindings",flags).SetValue(manager,new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.WeaponBinding>{new SoulHunter.Gameplay.Combat.WeaponBinding{Type=SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel,WeaponPrefab=prefab}});
 good.Type=bad.Type=SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel;good.Level=7;bad.Level=8;
 var levelUp=root.AddComponent<SoulHunter.Gameplay.Core.LevelUpManager>();
 levelUp.GetType().GetField("_allUpgrades",flags).SetValue(levelUp,new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.UpgradeData>{good,bad});
 manager.GiveWeapon(good.Type,6);
 var choices=levelUp.GetRandomValidUpgrades(3);
 if(choices.Count!=1 || choices[0]!=good)throw new System.Exception("Level 7 choice missing");
 manager.GiveWeapon(good.Type,7);
 if(levelUp.GetRandomValidUpgrades(3).Count!=0)throw new System.Exception("Level 8 choice offered");
 int events=0;manager.OnWeaponAcquiredOrUpgraded+=u=>events++;
 manager.ApplyUpgrade(bad);
 if(manager.GetWeaponLevel(good.Type)!=7 || events!=0)throw new System.Exception("Invalid upgrade applied");
 return new{passed=3,validLevel7Offered=true,level8Excluded=true,invalidUpgradeRejected=true};
}finally{UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(good);UnityEngine.Object.Destroy(bad);}
