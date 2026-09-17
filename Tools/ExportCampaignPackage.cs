if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
const string outputRoot="D:/Downloads/Soul-Hunter-Assets/Campaign_Art_v01";
if(!System.IO.Directory.Exists(outputRoot))throw new System.Exception("Prepare delivery first");
var assets=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Logs/SH10_import_progress.json"));
var scenes=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Logs/SH10_scenes.json"));
var exported=System.IO.File.Exists("Logs/SH10_exported.json")?Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Logs/SH10_exported.json")):new Newtonsoft.Json.Linq.JArray();
string[] packs=scenes.Select(s=>(string)s["pack"]).Concat(new[]{"Shared"}).ToArray();
int index=exported.Count;if(index>=packs.Length)return new{done=index,total=packs.Length};
string pack=packs[index];var selected=assets.Where(a=>(string)a["pack"]==pack).Select(a=>(string)a["prefabPath"]).Concat(scenes.Where(s=>(string)s["pack"]==pack).Select(s=>(string)s["scenePath"])).ToArray();
string output=outputRoot+"/"+pack+"/SoulHunter_"+pack+".unitypackage";
if(System.IO.File.Exists(output))throw new System.Exception("Package already exists; verify before resuming");
try{
UnityEditor.AssetDatabase.ExportPackage(selected,output,UnityEditor.ExportPackageOptions.IncludeDependencies);
if(!System.IO.File.Exists(output)||new System.IO.FileInfo(output).Length==0)throw new System.Exception("Empty package");
exported.Add(Newtonsoft.Json.Linq.JObject.FromObject(new{pack,output,bytes=new System.IO.FileInfo(output).Length,selectedAssets=selected.Length}));System.IO.File.WriteAllText("Logs/SH10_exported.json",exported.ToString());
}catch(System.Exception e){System.IO.File.WriteAllText("Logs/SH10_export_error.txt",e.ToString());UnityEngine.Debug.LogException(e);}
return new{done=exported.Count,total=packs.Length,pack,status="completed; verify completion record"};
