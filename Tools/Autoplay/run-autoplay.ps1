<#
.SYNOPSIS
  Lets an agent (or a developer) watch Soul Hunter play itself: builds a Windows player in the isolated
  validation copy and runs AutoplayBot (Assets/_Project/Core/Game/AutoplayBot.cs), which records
  screenshots, a timeline, events and a summary. The canonical project and the real save are never touched:
  the copy is used for building, and the bot plays on a throw-away save.

.EXAMPLE
  ./Tools/Autoplay/run-autoplay.ps1 -Minutes 3 -Character Kael
  ./Tools/Autoplay/run-autoplay.ps1 -Minutes 2 -Stage 7 -God -Rebuild
#>
param(
    [double]$Minutes = 3,
    [string]$Character = "Kael",
    [int]$Stage = 1,
    [double]$ShotEverySeconds = 10,
    [switch]$God,
    [switch]$Rebuild,
    [string]$Copy = "D:\SoulHunter-Validation\editmode-20260924-134853\project",
    [string]$Unity = "D:\6000.0.36f1\Editor\Unity.exe",
    [string]$Out = ""
)

$ErrorActionPreference = "Stop"
$canonical = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
if (-not $Out) { $Out = "D:\SoulHunter-Validation\autoplay-$stamp" }
New-Item -ItemType Directory -Force $Out | Out-Null
$exe = Join-Path (Split-Path $Copy) "autoplay-build\SoulHunter.exe"

if ($Rebuild -or -not (Test-Path $exe)) {
    Write-Host "Syncing Assets into the isolated copy and building..."
    robocopy (Join-Path $canonical "Assets") (Join-Path $Copy "Assets") /MIR /XF UniversalRenderPipelineGlobalSettings.asset /NFL /NDL /NJH /NJS /NP | Out-Null
    $env:SOULHUNTER_BUILD_OUTPUT_EXE = $exe
    & $Unity -batchmode -nographics -projectPath $Copy -executeMethod SoulHunter.Editor.Build.SoulHunterBuildRunner.BuildStandaloneWindows -logFile (Join-Path $Out "build.log") | Out-Null
    if (-not (Test-Path $exe)) { throw "Build failed, see $Out\build.log" }
}

$playerArgs = @("-sh-autoplay", "`"$Out`"", "-sh-autoplay-minutes", "$Minutes", "-sh-autoplay-character", $Character,
          "-sh-autoplay-stage", "$Stage", "-sh-autoplay-shots", "$ShotEverySeconds",
          "-screen-width", "1280", "-screen-height", "720", "-screen-fullscreen", "0", "-logFile", "`"$Out\player.log`"")
if ($God) { $playerArgs += "-sh-autoplay-god" }

Write-Host "Playing for up to $Minutes min as $Character (stage $Stage)..."
$process = Start-Process $exe -ArgumentList $playerArgs -PassThru
$limit = [int](($Minutes * 60 + 120) * 1000)
if (-not $process.WaitForExit($limit)) { $process.Kill(); Write-Warning "The run did not finish in time; it was stopped." }

if (Test-Path "$Out\summary.json") { Get-Content "$Out\summary.json" } else { Write-Warning "No summary written (see $Out\player.log)." }
$shots = Get-ChildItem "$Out\shots" -Filter *.png -ErrorAction SilentlyContinue
Write-Host "$($shots.Count) screenshots in $Out\shots"
Write-Host "Timeline: $Out\timeline.jsonl   Events: $Out\events.jsonl"
