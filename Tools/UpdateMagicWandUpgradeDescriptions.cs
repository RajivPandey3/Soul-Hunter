var rows=new System.Collections.Generic.List<object>();
string[] descriptions={"Fires at the nearest enemy.","Fires one additional projectile.","Reduces base cooldown by 0.2 seconds.","Fires one additional projectile.","Increases base damage by 10.","Fires one additional projectile.","Passes through one additional enemy.","Increases base damage by 10."};
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:UpgradeData",new[]{"Assets"})) {
 var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
 var upgrade=UnityEditor.AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Combat.UpgradeData>(path);
 if(upgrade==null || upgrade.Type!=SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MagicWand || upgrade.Level<1 || upgrade.Level>8) continue;
 upgrade.Description=descriptions[upgrade.Level-1]; UnityEditor.EditorUtility.SetDirty(upgrade);
 rows.Add(new{path,level=upgrade.Level,description=upgrade.Description});
}
UnityEditor.AssetDatabase.SaveAssets();return rows;
