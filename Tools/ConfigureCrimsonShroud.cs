var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Generated/Laurel.prefab");
var root=new UnityEngine.GameObject("CrimsonShroud");
try {
 var shield=root.AddComponent<SoulHunter.Gameplay.Combat.CrimsonShroudWeapon>();
 shield.ShieldVisualPrefab=source.GetComponent<SoulHunter.Gameplay.Combat.LaurelWeapon>().ShieldVisualPrefab;
 shield.CurrentLevel=1;shield.AttackCooldown=8;
 var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Weapons/Generated/CrimsonShroud.prefab");
 return new{created=prefab!=null};
}finally{UnityEngine.Object.DestroyImmediate(root);}
