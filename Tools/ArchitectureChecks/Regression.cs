using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SoulHunter.Core.Services;
using SoulHunter.Core.Events;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Scenes;
using SoulHunter.EditorScripts.Validator;

// Native Unity APIs are represented by explicit test doubles. Production C# is compiled unchanged.
namespace UnityEditor { public class EditorWindow {} public class MenuItem : Attribute { public MenuItem(string s) {} } }
namespace UnityEngine
{
    public struct Vector3 { public float x,y,z; public static Vector3 zero => new Vector3(); }
    public static class Debug { public static void Log(object o) {} public static void LogWarning(object o) {} public static void LogError(object o) {} public static void LogException(Exception e) { throw e; } }
    public static class Application
    {
        public static string dataPath = ".";
        public static string persistentDataPath = Path.Combine(Path.GetTempPath(), "soul-hunter-save-test-" + Guid.NewGuid());
        public static bool CanStreamedLevelBeLoaded(string s) => s == "Bootstrap" || s == "Loading";
    }
    public static class Mathf { public static float Clamp01(float v)=>Math.Clamp(v,0,1); public static float Max(float a,float b)=>Math.Max(a,b); }
    public class AsyncOperation { public volatile bool isDone; public float progress; }
    public static class PlayerPrefs
    {
        public static Dictionary<string,float> Data = new();
        public static float GetFloat(string k,float d=0)=>Data.TryGetValue(k,out var v)?v:d;
    }
    public static class JsonUtility
    {
        private static System.Text.Json.JsonSerializerOptions options = new() {IncludeFields=true};
        public static string ToJson(object value,bool pretty)=>System.Text.Json.JsonSerializer.Serialize(value,options);
        public static T FromJson<T>(string text)=>System.Text.Json.JsonSerializer.Deserialize<T>(text,options);
    }
}
namespace UnityEngine.SceneManagement
{
    public static class SceneManager
    {
        public static List<string> Requests = new();
        public static UnityEngine.AsyncOperation Current;
        public static UnityEngine.AsyncOperation LoadSceneAsync(string s)
        { Requests.Add(s); return Current = new UnityEngine.AsyncOperation(); }
    }
}
class A : IGameService,IDisposable
{
    public List<string> log; public bool fail;
    public void Initialize()=>log.Add("A+");
    public void Dispose(){log.Add("A-");if(fail)throw new Exception("dispose");}
}
class B : IGameService,IDisposable
{
    public List<string> log; public bool fail;
    public void Initialize(){log.Add("B+");if(fail)throw new Exception("initialize");}
    public void Dispose()=>log.Add("B-");
}
struct TestEvent : IGameEvent { public int Value; }
class Program
{
    static int passed;
    static void Check(bool result,string name){if(!result)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS "+name);}
    static void Throws(Action action,string name){try{action();}catch{Check(true,name);return;}Check(false,name);}
    static async Task Until(Func<bool> condition){for(int i=0;i<100&&!condition();i++)await Task.Delay(10);if(!condition())throw new Exception("Timed out");}
    static async Task Main()
    {
        var log=new List<string>();
        var services=new GameServices();services.Register(new A{log=log});services.Register(new B{log=log});
        services.InitializeAll();services.InitializeAll();
        Check(string.Join(",",log)=="A+,B+","initialization follows registration order exactly once");
        Throws(()=>services.Register(new A{log=log}),"late registration rejected");
        services.Dispose();services.Dispose();
        Check(string.Join(",",log)=="A+,B+,B-,A-","reverse cleanup exactly once");
        Check(GameServices.Instance==null,"disposed registry not globally reachable");
        Throws(()=>services.Get<A>(),"disposed lookup rejected");
        log.Clear();var previous=new GameServices();previous.Register(new A{log=log});previous.InitializeAll();
        var next=new GameServices();Check(log.Last()=="A-","replacement releases previous services");next.Dispose();
        log.Clear();services=new GameServices();services.Register(new A{log=log});
        Throws(()=>services.Register(new A{log=log}),"duplicate registration rejected");
        services.Register(new B{log=log,fail=true});Throws(()=>services.InitializeAll(),"failed startup propagates");
        Check(log.SequenceEqual(new[]{"A+","B+","B-","A-"})&&GameServices.Instance==null,"failed startup cleans all resources");
        log.Clear();services=new GameServices();services.Register(new B{log=log});services.Register(new A{log=log,fail=true});
        Throws(()=>services.Dispose(),"cleanup failure reported");
        Check(log.SequenceEqual(new[]{"A-","B-"})&&GameServices.Instance==null,"cleanup continues after one service fails");

        var bus=new EventBus();int sum=0;
        var subscription=bus.Subscribe<TestEvent>(e=>sum+=e.Value);
        bus.Raise(new TestEvent{Value=2});subscription.Dispose();subscription.Dispose();bus.Raise(new TestEvent{Value=10});
        Check(sum==2,"subscription disposal actually removes handler");
        bus.Subscribe<TestEvent>(e=>sum+=e.Value);bus.Dispose();bus.Raise(new TestEvent{Value=10});Check(sum==2,"bus disposal clears listeners");

        Check(ProjectValidatorLayer2.AnalyzeRuntimeMethods("void Update(){Tick();} void Start(){GetComponent<Camera>();}").Count==0,"Start lookup is not attributed to Update");
        Check(ProjectValidatorLayer2.AnalyzeRuntimeMethods("void Update(){if(true){GetComponent<Camera>();}}").Count==1,"nested frame lookup detected");
        Check(ProjectValidatorLayer2.AnalyzeRuntimeMethods("void FixedUpdate()=>FindFirstObjectByType<Player>();").Count==1,"expression-bodied physics lookup detected");
        Check(ProjectValidatorLayer2.AnalyzeRuntimeMethods("void Update(){/* } GetComponent<Foo>(); */ var s=\"GetComponent<Camera>(); }\"; // GetComponent<X>();\n}").Count==0,"comments and string braces ignored");
        Check(ProjectValidatorLayer2.AnalyzeRuntimeMethods("void LateUpdate(){var s=@\"a \"\" } b\"; GetComponent<Camera>();}").Count==1,"verbatim quoted strings preserve boundaries");

        Directory.CreateDirectory(UnityEngine.Application.persistentDataPath);
        string savePath=Path.Combine(UnityEngine.Application.persistentDataPath,"soulhunter_save.json");
        File.WriteAllText(savePath,"{\"Currency\":123,\"SelectedCharacterName\":\"OtherHero\"}");
        UnityEngine.PlayerPrefs.Data["MasterVolume"]=0.25f;UnityEngine.PlayerPrefs.Data["HighScoreTime"]=99f;
        var save=new SaveService();save.Initialize();
        Check(save.CurrentData.Currency==123&&save.CurrentData.SelectedCharacterName=="OtherHero","existing progression preserved during settings migration");
        Check(save.CurrentData.SettingsMigrated&&save.CurrentData.MasterVolume==0.25f&&save.CurrentData.HighScoreTime==99,"legacy settings migrated");
        Check(UnityEngine.PlayerPrefs.Data.Count==2,"legacy keys retained");
        save.SetMasterVolume(2f);save.RecordHighScore(50f);
        Check(save.CurrentData.MasterVolume==1f&&save.CurrentData.HighScoreTime==99,"volume clamped and lower score ignored");
        save.RecordHighScore(120f);var reloaded=new SaveService();reloaded.Initialize();
        Check(reloaded.CurrentData.HighScoreTime==120&&reloaded.CurrentData.MasterVolume==1f,"JSON settings persist and legacy values do not overwrite them");

        var scenes=new SceneService();scenes.Initialize();
        var requests=UnityEngine.SceneManagement.SceneManager.Requests;
        scenes.LoadSceneAsync("Missing");Check(requests.Count==0,"unavailable scene rejected");
        scenes.LoadSceneAsync("Bootstrap");scenes.LoadSceneAsync("Bootstrap");scenes.LoadSceneAsync("Loading");
        Check(requests.SequenceEqual(new[]{"Bootstrap"}),"duplicate and chained loads do not overlap");
        UnityEngine.SceneManagement.SceneManager.Current.isDone=true;
        await Until(()=>requests.Count==2);Check(requests[1]=="Loading","Bootstrap activation can queue Loading");
        scenes.Dispose();UnityEngine.SceneManagement.SceneManager.Current.isDone=true;await Until(()=>!scenes.IsLoading);
        scenes.LoadSceneAsync("Bootstrap");Check(requests.Count==2,"disposed scene service cannot launch transitions");
        Console.WriteLine("TOTAL PASSED: "+passed);
    }
}
