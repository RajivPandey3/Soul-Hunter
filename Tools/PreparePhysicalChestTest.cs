if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required");
var manager=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Combat.WeaponManager>();
var player=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
if(manager==null||player==null)throw new System.Exception("Gameplay not ready");
manager.GiveWeapon(SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel,7);
foreach(var name in new[]{"MetaglioLeft","MetaglioRight"}){
 manager.AcquireStagePassive(UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.UpgradeData>("Metaglio/"+name+"_Lv1"));
 for(int i=2;i<=9;i++)manager.ApplyUpgrade(UnityEngine.Resources.Load<SoulHunter.Gameplay.Combat.UpgradeData>("Metaglio/"+name+"_Lv"+i));
}
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Items/Chest.prefab");
var chest=UnityEngine.Object.Instantiate(prefab,player.transform.position,UnityEngine.Quaternion.identity);chest.name="PhysicalChestVerification";
chest.SetActive(true);UnityEngine.Physics.SyncTransforms();
return new{spawned=true,hasPickup=chest.GetComponent<SoulHunter.Gameplay.Pickups.ChestPickup>()!=null,logicPresent=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.ChestLogicController>()!=null};
