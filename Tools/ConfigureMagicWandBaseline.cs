var path="Assets/Prefabs/Weapons/Wand_Weapon.prefab";
var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try {
 var wand=prefab.GetComponent<SoulHunter.Gameplay.Weapons.MagicWandWeapon>();
 if(wand==null) throw new System.Exception("Missing Magic Wand component");
 var serialized=new UnityEditor.SerializedObject(wand);
 var oldDamage=serialized.FindProperty("_damage").intValue;
 var oldCooldown=serialized.FindProperty("_cooldown").floatValue;
 serialized.FindProperty("_damage").intValue=10;
 serialized.FindProperty("_cooldown").floatValue=1.2f;
 serialized.FindProperty("CurrentLevel").intValue=1;
 serialized.ApplyModifiedPropertiesWithoutUndo();
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,path);
 return new {oldDamage,oldCooldown,damage=10,cooldown=1.2f};
} finally { UnityEditor.PrefabUtility.UnloadPrefabContents(prefab); }
