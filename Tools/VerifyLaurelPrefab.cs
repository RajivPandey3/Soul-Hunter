var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root = new UnityEngine.GameObject("ShieldPrefabVerification");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
try {
 var health = root.AddComponent<SoulHunter.Gameplay.Combat.HealthController>(); health.Initialize(100);
 var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Generated/Laurel.prefab");
 if (prefab == null) throw new System.Exception("Missing prefab");
 var host = UnityEngine.Object.Instantiate(prefab, root.transform); host.SetActive(true);
 var shield = host.GetComponent<SoulHunter.Gameplay.Combat.LaurelWeapon>();
 if (shield == null || shield.ShieldVisualPrefab == null) throw new System.Exception("Missing shield references");
 shield.GetType().GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(shield,null);
 health.TakeDamage(new SoulHunter.Gameplay.Combat.DamagePacket(10,UnityEngine.Vector3.zero,UnityEngine.Vector3.zero));
 if (health.CurrentHealth != 100 || shield.CurrentCharges != 0) throw new System.Exception("Prefab protection failed");
 return new { passed=true, visualAssigned=true, blocksDamage=true, cooldown=shield.AttackCooldown };
} finally { UnityEngine.Object.DestroyImmediate(root); UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
