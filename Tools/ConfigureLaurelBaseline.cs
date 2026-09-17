var path="Assets/Prefabs/Weapons/Generated/Laurel.prefab";
var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try {
 var shield=root.GetComponent<SoulHunter.Gameplay.Combat.LaurelWeapon>();
 var data=new UnityEditor.SerializedObject(shield);
 data.FindProperty("AttackCooldown").floatValue=10f;
 data.FindProperty("CurrentLevel").intValue=1;
 data.FindProperty("_blockGraceSeconds").floatValue=0.2f;
 data.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
int updated=0;
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:UpgradeData",new[]{"Assets"})) {
 var item=UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Combat.UpgradeData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
 if(item==null || item.Type!=SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel || item.Level<1 || item.Level>7)continue;
 item.Description=item.Level==1?"Blocks a hit while a shield charge is available.":item.Level==4 || item.Level==7?"Adds one shield charge slot.":"Reduces base recharge by 0.5 seconds; adds 0.2 seconds of protection after a block.";
 UnityEditor.EditorUtility.SetDirty(item);UnityEditor.AssetDatabase.SaveAssetIfDirty(item);updated++;
}
var catalog=SoulHunter.Gameplay.Data.GameContentCatalog.Load();int removed=0;
if(catalog!=null && catalog.Upgrades!=null){int before=catalog.Upgrades.Length;catalog.Upgrades=catalog.Upgrades.Where(u=>u==null || u.Type!=SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Laurel || u.Level<=7).ToArray();removed=before-catalog.Upgrades.Length;UnityEditor.EditorUtility.SetDirty(catalog);UnityEditor.AssetDatabase.SaveAssetIfDirty(catalog);}
return new{prefab=path,updatedDescriptions=updated,removedOverlevelCatalogEntries=removed};
