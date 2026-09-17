int changed=0;
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Items"})){
 var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
 if(asset.GetComponent<SoulHunter.Gameplay.Pickups.HealthPickup>()==null)continue;
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{var data=new UnityEditor.SerializedObject(root.GetComponent<SoulHunter.Gameplay.Pickups.HealthPickup>());data.FindProperty("_playerLayer").intValue=UnityEngine.LayerMask.GetMask("Player");data.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);changed++;}
 finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}return new{changed};
