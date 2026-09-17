# Environment

Discovery date: 2026-09-11, Asia/Calcutta. Root: D:\Unity Projects\Soul-Hunter.

| Item | Observed value / qualification |
|---|---|
| Git | 2.48.1.windows.1; existing repository on main |
| HEAD | 78d82fa90abbcdae4321c9b716ac2f7145c4e00e |
| PowerShell | Desktop 5.1.19041.6456; CLR 4.0.30319.42000 |
| Python | 3.13.2 |
| Unity project | 6000.0.36f1, revision 9fe3b5f71dbb |
| Unity executable | D:\6000.0.36f1\Editor\Unity.exe; product version 6000.0.36f1_9fe3b5f71dbb |
| Unity CLI | C:\Users\Lenovo\AppData\Local\Unity\bin\unity.exe; 1.0.0-beta.8 |
| PATH ambiguity | Get-Command unity resolves D:\Unity first; use explicit executable |
| Pipeline | manifest 0.6.0-exp.1; connected port 7800, project instance PID 9288 |
| Editor status | ready, stopped, compiling=false reported; not fresh compile proof |
| Compile verdict | UNKNOWN: read-only scriptCompilationFailed query timed out after 5000ms |
| Licensing/build modules | NOT VERIFIED; no build/license mutation attempted |
| Target hardware | NOT VERIFIED for final budgets |
| .NET SDK | TASK-001 verified D:/dotnet SDKs 8.0.423, 8.0.424, 10.0.400; regression runner uses .NET 8 |

Manifest pins URP 17.0.3, Input System 1.12.0, Test Framework 1.4.5, UGUI 2.0.0, Timeline 1.8.7, Visual Scripting 1.9.5, Rider 3.0.31, Visual Studio 2.0.22, Collab Proxy 2.13.5, Multiplayer Center 1.0.0 and 2D feature 2.0.1. Packages/packages-lock.json exists; resolution/install was not run.

ProjectSettings: 26 files; product Soul Hunter / Nimrita Games; default screen 1920x1080, activeInputHandler=2; timeScale=1, maximum allowed timestep=0.33333334. Build settings list 14 enabled scenes: Bootstrap, Loading, MainMenu, Main_Gameplay and ten showcase scenes. Settings were inspected, not changed.

Three Unity processes were visible using the same executable; CLI identified the target project explicitly. Other process project ownership was not inspected. A read-only CLI timeout produced a diagnostic console error during this audit; it is not a newly introduced gameplay failure.

TASK-001 update (2026-09-11 local): exact Unity executable batch attempt with 120s timeout exited 1073741845 in about 9 seconds, before compilation, reporting that another instance has this project open. stdout/stderr/Editor.log and UTC timings are retained under Reports/Validation/TASK-001/compile. Compilation remains UNKNOWN; this does not establish a C# compile failure. No existing Editor was closed.

Fresh CLI version query returned 1.0.0-beta.8, exit 0; editor_status reported ready/stopped/not compiling, exit 0 at 2026-09-10T20:04:26Z (2026-09-11 local). These are availability facts only. Final standalone .NET regression exited 0 with 27 passed checks. Reports/Validation/TASK-001 contains exact command arrays and timestamps.
