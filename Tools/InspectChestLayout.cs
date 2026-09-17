var ui=UnityEngine.Object.FindFirstObjectByType<SoulHunter.Gameplay.UI.ChestUI>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var panel=(UnityEngine.GameObject)ui.GetType().GetField("_chestPanel",flags).GetValue(ui);
return panel.GetComponentsInChildren<UnityEngine.RectTransform>(true).Select(t=>new{t.name,parent=t.parent.name,image=t.GetComponent<UnityEngine.UI.Image>()!=null,text=t.GetComponent<TMPro.TMP_Text>()!=null,button=t.GetComponent<UnityEngine.UI.Button>()!=null}).ToArray();
