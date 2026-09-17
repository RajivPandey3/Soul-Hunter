UnityEngine.ScreenCapture.CaptureScreenshot("Logs/ActualGame.png");
return new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,time=UnityEngine.Time.time,scale=UnityEngine.Time.timeScale,
renderers=UnityEngine.Object.FindObjectsByType<UnityEngine.SpriteRenderer>(UnityEngine.FindObjectsSortMode.None).Take(8).Select(r=>new {r.name,r.enabled,sprite=r.sprite?r.sprite.name:null,position=r.transform.position.ToString(),scale=r.transform.lossyScale.ToString(),material=r.sharedMaterial?r.sharedMaterial.name:null}).ToArray(),
canvases=UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None).Select(c=>new{c.name,c.enabled,mode=c.renderMode.ToString()}).ToArray()};
