var folder="Assets/Resources/Metaglio";
if(!UnityEditor.AssetDatabase.IsValidFolder(folder))UnityEditor.AssetDatabase.CreateFolder("Assets/Resources","Metaglio");
var catalog=SoulHunter.Gameplay.Data.GameContentCatalog.Load();
var items=new System.Collections.Generic.List<SoulHunter.Gameplay.Combat.UpgradeData>(catalog.Upgrades);
foreach(var type in new[]{SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MetaglioLeft,SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MetaglioRight}) {
 for(int level=1;level<=9;level++) {
  var path=folder+"/"+type+"_Lv"+level+".asset";
  var item=UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Combat.UpgradeData>(path);
  if(item==null){item=UnityEngine.ScriptableObject.CreateInstance<SoulHunter.Gameplay.Combat.UpgradeData>();UnityEditor.AssetDatabase.CreateAsset(item,path);}
  item.Type=type;item.Level=level;item.UpgradeName=(type==SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MetaglioLeft?"Metaglio Left":"Metaglio Right")+" Lv"+level;
  item.Description=level==1?"Found in the stage. Unlocks further upgrades.":type==SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MetaglioLeft?"Recovery +0.1 HP/s. Max Health multiplied by 1.05.":"Curse +5%.";
  UnityEditor.EditorUtility.SetDirty(item);if(!items.Contains(item))items.Add(item);
 }
}
catalog.Upgrades=items.ToArray();UnityEditor.EditorUtility.SetDirty(catalog);
var evoPath=folder+"/CrimsonShroudEvolution.asset";
var evo=UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Combat.WeaponEvolutionData>(evoPath);
if(evo==null){evo=UnityEngine.ScriptableObject.CreateInstance<SoulHunter.Gameplay.Combat.WeaponEvolutionData>();UnityEditor.AssetDatabase.CreateAsset(evo,evoPath);}
evo.BaseWeapon=SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel;evo.BaseWeaponMaxLevel=7;
evo.RequiredPassive=SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MetaglioLeft;evo.RequiredPassiveMaxLevel=9;
evo.AdditionalPassives=new[]{new SoulHunter.Gameplay.Combat.PassiveEvolutionRequirement{Type=SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MetaglioRight,MinimumLevel=9}};
evo.EvolvedWeaponPrefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Generated/CrimsonShroud.prefab");evo.EvolvedName="Crimson Shroud";UnityEditor.EditorUtility.SetDirty(evo);
int managers=0,recovery=0;
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"})){
 var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
 var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
 if(asset.GetComponentInChildren<SoulHunter.Gameplay.Combat.WeaponManager>(true)==null && asset.GetComponentInChildren<SoulHunter.Gameplay.Player.PlayerStats>(true)==null)continue;
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try {
  foreach(var manager in root.GetComponentsInChildren<SoulHunter.Gameplay.Combat.WeaponManager>(true)){
   var serialized=new UnityEditor.SerializedObject(manager);var list=serialized.FindProperty("_availableEvolutions");bool found=false;
   for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==evo)found=true;
   if(!found){int index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=evo;serialized.ApplyModifiedPropertiesWithoutUndo();}
   managers++;
  }
  foreach(var stats in root.GetComponentsInChildren<SoulHunter.Gameplay.Player.PlayerStats>(true)){
   if(stats.GetComponent<SoulHunter.Gameplay.Combat.HealthController>()!=null && stats.GetComponent<SoulHunter.Gameplay.Player.PlayerRecovery>()==null){stats.gameObject.AddComponent<SoulHunter.Gameplay.Player.PlayerRecovery>();recovery++;}
  }
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
 }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
UnityEditor.AssetDatabase.SaveAssets();return new{upgrades=18,managers,recovery};
