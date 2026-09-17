if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop Play mode before moving resources.");
const string root="Assets/_Project/ThirdParty/Assets/TextMesh Pro";
void Folder(string path){if(UnityEditor.AssetDatabase.IsValidFolder(path))return;var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);UnityEditor.AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
string[] names={"TMP Settings.asset","LineBreaking Leading Characters.txt","LineBreaking Following Characters.txt","LiberationSans SDF.asset","LiberationSans SDF - Fallback.asset","LiberationSans SDF - Outline.mat","LiberationSans SDF - Drop Shadow.mat","Default Style Sheet.asset","EmojiOne.asset"};
var moved=new System.Collections.Generic.List<string>();
foreach(var name in names){var source=root+"/"+name;string folder=name.StartsWith("LiberationSans")?"Fonts & Materials/":name=="EmojiOne.asset"?"Sprite Assets/":name=="Default Style Sheet.asset"?"Style Sheets/":"";var destination=root+"/Resources/"+folder+name;
if(!System.IO.File.Exists(source))continue;if(System.IO.File.Exists(destination))throw new System.Exception("Destination already exists: "+destination);
Folder(System.IO.Path.GetDirectoryName(destination).Replace('\\','/'));var error=UnityEditor.AssetDatabase.MoveAsset(source,destination);if(!string.IsNullOrEmpty(error))throw new System.Exception(error);moved.Add(destination);}
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.AssetDatabase.Refresh();
var settings=UnityEngine.Resources.Load<TMPro.TMP_Settings>("TMP Settings");
if(!settings)throw new System.Exception("TMP Settings still unavailable through Resources.Load");
return new{moved,settings=UnityEditor.AssetDatabase.GetAssetPath(settings),version=new UnityEditor.SerializedObject(settings).FindProperty("assetVersion").stringValue,font=TMPro.TMP_Settings.defaultFontAsset?TMPro.TMP_Settings.defaultFontAsset.name:null};
