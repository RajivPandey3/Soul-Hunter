if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var root = new UnityEngine.GameObject("LaurelRuntimeVerification");
root.transform.position = new UnityEngine.Vector3(10000, 5, 10000);
var stats = root.AddComponent<SoulHunter.Gameplay.Player.PlayerStats>(); stats.enabled = false; stats.ReduceCooldown(0.5f);
var health = root.AddComponent<SoulHunter.Gameplay.Combat.HealthController>(); health.Initialize(100); health.KnockbackResistance=1;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Weapons/Generated/Laurel.prefab");
var host = UnityEngine.Object.Instantiate(prefab, root.transform); host.SetActive(true);
var shield = host.GetComponent<SoulHunter.Gameplay.Combat.LaurelWeapon>();
var hit = new SoulHunter.Gameplay.Combat.DamagePacket(10,UnityEngine.Vector3.zero,UnityEngine.Vector3.zero);
health.TakeDamage(hit);
if (health.CurrentHealth != 100 || shield.CurrentCharges != 0) { UnityEngine.Object.Destroy(root); throw new System.Exception("Runtime block failed"); }
float start = UnityEngine.Time.time;
double deadline = UnityEditor.EditorApplication.timeSinceStartup + 15;
int step = 0;
UnityEditor.EditorApplication.CallbackFunction update = null;
update = () => {
 try {
  if (!UnityEditor.EditorApplication.isPlaying || root == null) throw new System.Exception("Test interrupted");
  if (UnityEditor.EditorApplication.timeSinceStartup > deadline) throw new System.Exception("Timed out");
  float elapsed = UnityEngine.Time.time - start;
  if (step == 0 && elapsed > 0.7f) {
   health.TakeDamage(hit);
   if (health.CurrentHealth != 90 || shield.CurrentCharges != 0) throw new System.Exception("Grace expiry / recharge interval failed");
   step=1;
  }
  if (step == 1 && elapsed > shield.EffectiveRechargeCooldown + 0.3f) {
   if (shield.CurrentCharges != 1) throw new System.Exception("Real-time recharge failed");
   health.TakeDamage(hit);
   if (health.CurrentHealth != 90 || shield.CurrentCharges != 0) throw new System.Exception("Recharged block failed");
   System.IO.File.WriteAllText("Logs/LaurelRuntimeVerification.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed=true, checks=4, baseCooldown=shield.BaseRechargeCooldown, effectiveCooldown=shield.EffectiveRechargeCooldown, elapsed }));
   UnityEditor.EditorApplication.update -= update; UnityEngine.Object.Destroy(root);
  }
 } catch(System.Exception e) {
  System.IO.File.WriteAllText("Logs/LaurelRuntimeVerification.json", UnityEngine.JsonUtility.ToJson(new UnityEngine.GUIContent(e.Message)));
  UnityEngine.Debug.LogException(e); UnityEditor.EditorApplication.update -= update;
  if(root != null) UnityEngine.Object.Destroy(root);
 }
};
UnityEditor.EditorApplication.update += update;
return "Runtime test scheduled";


