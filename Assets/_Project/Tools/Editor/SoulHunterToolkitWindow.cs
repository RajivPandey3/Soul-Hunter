using UnityEngine;
using UnityEditor;

namespace SoulHunter.EditorSetup
{
    public class SoulHunterToolkitWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<SoulHunterToolkitWindow>("Soul Hunter Toolkit");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Soul Hunter - Developer Toolkit", EditorStyles.boldLabel);
            GUILayout.Label("Ek jagah sab tools (Single Responsibility ke asool par mabni)", EditorStyles.miniLabel);
            GUILayout.Space(10);

            DrawUILine(Color.gray);

            // --- SECTION 1: MAGIC BUILDERS ---
            GUILayout.Label("✨ Setup & Builders", EditorStyles.boldLabel);
            GUILayout.Space(5);
            if (GUILayout.Button("Run Phase 6: Evolution Magic Builder", GUILayout.Height(30)))
            {
                EvolutionMagicBuilder.RunMagic();
            }
            GUILayout.Label("  => Automatically duplicates base weapons to create Evolved/Union prefabs.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(10);

            DrawUILine(Color.gray);

            // --- SECTION 2: VALIDATORS ---
            GUILayout.Label("🔍 Scanners & Validators", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            if (GUILayout.Button("Run Project Diagnostics", GUILayout.Height(30)))
            {
                // Find and execute the DiagnosticTool if it exists
                EditorApplication.ExecuteMenuItem("Soul Hunter/Tools/Diagnostic Tool"); 
            }
            GUILayout.Label("  => Checks layers, tags, and project settings.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(10);

            DrawUILine(Color.gray);

            // --- SECTION 3: CLEANERS ---
            GUILayout.Label("🧹 Cleaners", EditorStyles.boldLabel);
            GUILayout.Space(5);
            if (GUILayout.Button("Clean Missing Scripts", GUILayout.Height(30)))
            {
                 EditorApplication.ExecuteMenuItem("Soul Hunter/Tools/Remove Missing Scripts");
            }
            GUILayout.Label("  => Removes empty missing script references from prefabs and scenes.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(10);
            
            DrawUILine(Color.gray);

            // --- SECTION 4: PROGRESSION & BALANCING (NEWLY ADDED) ---
            GUILayout.Label("⚖️ Balancing & Cheats", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            if (GUILayout.Button("Global Combat Balancer", GUILayout.Height(30))) 
            { 
                 EditorApplication.ExecuteMenuItem("Soul Hunter/Tools/Global Combat Balancer");
            }
            GUILayout.Label("  => Adjust Damage and Cooldown of all weapons from a single grid.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(5);

            if (GUILayout.Button("Progression Cheat Engine", GUILayout.Height(30))) 
            { 
                 EditorApplication.ExecuteMenuItem("Soul Hunter/Tools/Progression Cheat Engine");
            }
            GUILayout.Label("  => Wipe save data, grant Gold, or force Level 100 instantly.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(5);

            // Keep the others as future
            GUI.enabled = false;
            if (GUILayout.Button("Enemy Wave Balancer", GUILayout.Height(30))) { }
            GUILayout.Label("  => [Future] Adjust enemy spawn rates across different minutes.", EditorStyles.wordWrappedMiniLabel);
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            DrawUILine(Color.gray);
            GUILayout.Label("Made with ❤️ for Soul Hunter", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(5);
        }

        private void DrawUILine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }
}
