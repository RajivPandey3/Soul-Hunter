var nav=UnityEngine.Object.FindFirstObjectByType<SoulHunter.UI.MainMenuNavigation>();
nav.StartButton.onClick.Invoke();
bool selection=nav.CharacterPanel.activeSelf && !nav.HomePanel.activeSelf;
var start=nav.CharacterPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="StartGame_Button");
start.onClick.Invoke();
return new {selection,clicked=start.name};
