var player=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
player.GetComponent<SoulHunter.Gameplay.Combat.HealthController>().TakeDamage(new SoulHunter.Gameplay.Combat.DamagePacket(99999,player.transform.position,UnityEngine.Vector3.zero));
var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="Restart_Button");
bool shown=button!=null;
bool paused=UnityEngine.Time.timeScale==0;
if(button)button.onClick.Invoke();
return new{gameOver=shown,paused,restartClicked=shown};
