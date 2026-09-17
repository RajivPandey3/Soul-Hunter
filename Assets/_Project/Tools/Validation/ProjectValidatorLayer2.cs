using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SoulHunter.EditorScripts.Validator
{
    /// <summary>Method-scoped source hints. This is not a profiler or a full C# semantic analyzer.</summary>
    public class ProjectValidatorLayer2 : EditorWindow
    {
        public static void RunRuntimeValidation()
        {
            string folder = Application.dataPath + "/_Project";
            if (!Directory.Exists(folder)) return;
            int count = 0;
            foreach (string file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
            {
                // Editor utilities are not part of gameplay frame loops.
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Editor/") || normalized.Contains("/_Project/Tools/")) continue;
                foreach (string finding in AnalyzeRuntimeMethods(File.ReadAllText(file)))
                {
                    Debug.LogWarning("[Runtime source review] " + Path.GetFileName(file) + ": " + finding);
                    count++;
                }
            }
            Debug.Log("[Layer 2] " + count + " direct-call hints. Indirect calls, interpolated expressions and runtime cost require separate review/profiling.");
        }

        public static List<string> AnalyzeRuntimeMethods(string source)
        {
            var findings = new List<string>();
            string code = MaskNonCode(source);
            var methods = Regex.Matches(code, @"\bvoid\s+(Update|FixedUpdate|LateUpdate)\s*\(\s*\)\s*(\{|=>)");
            foreach (Match method in methods)
            {
                int start = method.Index + method.Length;
                int end = start;
                if (method.Groups[2].Value == "=>")
                {
                    end = code.IndexOf(';', start);
                    if (end < 0) continue;
                }
                else
                {
                    int depth = 1;
                    while (end < code.Length && depth > 0)
                    {
                        if (code[end] == '{') depth++;
                        else if (code[end] == '}') depth--;
                        end++;
                    }
                    if (depth != 0) continue;
                }
                string body = code.Substring(start, end - start);
                if (Regex.IsMatch(body, @"\b(?:Find(?:First|Any)ObjectByType|FindObjectsByType|FindObjects?OfType|Find(?:GameObjectWithTag|GameObjectsWithTag|WithTag))\s*(?:<[^>]*>)?\s*\(|\bGameObject\s*\.\s*Find\s*\("))
                    findings.Add(method.Groups[1].Value + " directly searches scene objects; review caching.");
                if (Regex.IsMatch(body, @"\bGetComponent(?:InParent|InChildren)?\s*(?:<[^>]*>)?\s*\("))
                    findings.Add(method.Groups[1].Value + " directly looks up a component; review caching.");
            }
            return findings;
        }

        private static string MaskNonCode(string source)
        {
            char[] code = source.ToCharArray();
            for (int i = 0; i < source.Length; i++)
            {
                int start = i;
                if (source[i] == '/' && i + 1 < source.Length && source[i + 1] == '/')
                {
                    while (i < source.Length && source[i] != '\n') i++;
                }
                else if (source[i] == '/' && i + 1 < source.Length && source[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/')) i++;
                    i = System.Math.Min(source.Length, i + 2);
                }
                else if (source[i] == '"' || source[i] == '\'')
                {
                    char quote = source[i];
                    bool verbatim = quote == '"' && i > 0 && (source[i - 1] == '@' || (i > 1 && source[i - 1] == '$' && source[i - 2] == '@'));
                    i++;
                    while (i < source.Length)
                    {
                        if (!verbatim && source[i] == '\\') { i = System.Math.Min(source.Length, i + 2); continue; }
                        if (source[i] == quote)
                        {
                            if (verbatim && i + 1 < source.Length && source[i + 1] == quote) { i += 2; continue; }
                            i++;
                            break;
                        }
                        i++;
                    }
                }
                else continue;
                for (int j = start; j < i; j++) if (code[j] != '\n' && code[j] != '\r') code[j] = ' ';
                i--;
            }
            return new string(code);
        }
    }
}
