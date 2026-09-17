if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var samples = new System.Collections.Generic.List<float>(600);
var gc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame", 1);
int lastFrame=-1, peakEnemies=0;
long peakGC=0;
double started=UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction update=null;
update=()=> {
 if(!UnityEditor.EditorApplication.isPlaying) { gc.Dispose(); UnityEditor.EditorApplication.update-=update; return; }
 if(UnityEngine.Time.frameCount==lastFrame) return;
 lastFrame=UnityEngine.Time.frameCount;
 if(UnityEngine.Time.timeScale <= 0) return;
 samples.Add(UnityEngine.Time.unscaledDeltaTime*1000f);
 peakEnemies=System.Math.Max(peakEnemies,SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.Count);
 if(gc.Valid) peakGC=System.Math.Max(peakGC,gc.LastValue);
 if(samples.Count>=600 || UnityEditor.EditorApplication.timeSinceStartup-started>30) {
  samples.Sort();
  var result=new { scope="Editor early-run baseline; not low-power certification", frames=samples.Count, p50Ms=samples[samples.Count/2], p95Ms=samples[(int)((samples.Count-1)*0.95)], maxMs=samples[samples.Count-1], peakEnemies, gcRecorderValid=gc.Valid, peakGCBytes=peakGC, cpu=UnityEngine.SystemInfo.processorType, gpu=UnityEngine.SystemInfo.graphicsDeviceName, ramMB=UnityEngine.SystemInfo.systemMemorySize, width=UnityEngine.Screen.width,height=UnityEngine.Screen.height };
  System.IO.File.WriteAllText("Logs/EditorPerformanceBaseline.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
  gc.Dispose(); UnityEditor.EditorApplication.update-=update;
 }
};
UnityEditor.EditorApplication.update+=update;
return "Sampling up to 600 frames / 30 seconds";
