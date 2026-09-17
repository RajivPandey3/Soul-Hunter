var paths=UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"});
var results=new System.Collections.Generic.List<object>();
foreach(var guid in paths){var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid); var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path); if(prefab==null)continue; foreach(var weapon in prefab.GetComponentsInChildren<SoulHunter.Gameplay.Weapons.MagicWandWeapon>(true)) results.Add(new {path,components=weapon.GetComponents<UnityEngine.Component>().Select(c=>c==null?"MISSING":c.GetType().FullName).ToArray()});}
return results;
