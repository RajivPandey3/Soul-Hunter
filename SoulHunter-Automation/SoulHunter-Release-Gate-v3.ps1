param(
    [string]$ProjectPath = "D:\Unity Projects\Soul-Hunter",
    [string]$AutomationRoot = "D:\SoulHunter-Automation",
    [string]$UnityExe = "D:\6000.0.36f1\Editor\Unity.exe",
    [string]$ExpectedOrchestratorSha256 = "DA2177D6B64AFB7303AC3CAE20DBC3E42CFFEB9D20820AB427C5FD496EF1A7A9"
)

$ErrorActionPreference = "Stop"

function Section([string]$Title) {
    Write-Host ""
    Write-Host ("=" * 78)
    Write-Host (" " + $Title)
    Write-Host ("=" * 78)
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory=$true)]$Object,
        [Parameter(Mandatory=$true)][string]$Path,
        [int]$Depth = 12
    )

    $json = $Object | ConvertTo-Json -Depth $Depth
    [System.IO.File]::WriteAllText(
        $Path,
        $json,
        (New-Object System.Text.UTF8Encoding($false))
    )
}

function Get-GitState {
    param([string]$Repo)

    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) {
        return [ordered]@{
            available = $false
            head = $null
            branch = $null
            status = $null
        }
    }

    Push-Location $Repo
    try {
        $head = (& git rev-parse HEAD 2>$null | Out-String).Trim()
        $branch = (& git branch --show-current 2>$null | Out-String).Trim()
        $status = (& git status --porcelain=v1 -uall 2>$null | Out-String).TrimEnd()

        return [ordered]@{
            available = $true
            head = $head
            branch = $branch
            status = $status
        }
    }
    finally {
        Pop-Location
    }
}

function Get-UnityProjectVersion {
    param([string]$Project)

    $p = Join-Path $Project "ProjectSettings\ProjectVersion.txt"
    if (-not (Test-Path -LiteralPath $p -PathType Leaf)) {
        return $null
    }

    $line = Get-Content -LiteralPath $p |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
        Select-Object -First 1

    if ($line -match '^m_EditorVersion:\s*(.+)$') {
        return $Matches[1].Trim()
    }

    return $null
}

function Copy-ProjectIsolated {
    param(
        [string]$Source,
        [string]$Destination
    )

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    $excludeDirs = @(
        (Join-Path $Source ".git"),
        (Join-Path $Source "Library"),
        (Join-Path $Source "Temp"),
        (Join-Path $Source "Obj"),
        (Join-Path $Source "Logs"),
        (Join-Path $Source "UserSettings"),
        (Join-Path $Source "Build"),
        (Join-Path $Source "Builds")
    )

    $args = @(
        $Source,
        $Destination,
        "/MIR",
        "/R:1",
        "/W:1",
        "/COPY:DAT",
        "/DCOPY:DAT",
        "/NFL",
        "/NDL",
        "/NJH",
        "/NJS",
        "/NP",
        "/XD"
    ) + $excludeDirs

    & robocopy @args | Out-Null
    $rc = $LASTEXITCODE

    if ($rc -ge 8) {
        throw "Isolated workspace copy failed. Robocopy exit code: $rc"
    }
}

function Invoke-Unity {
    param(
        [string]$Exe,
        [string[]]$Arguments
    )

    # Unity.exe is a Windows GUI executable. Invoking it with the PowerShell
    # call operator can return before the batch-mode process has actually
    # finished. Use ProcessStartInfo + WaitForExit so every release-gate phase
    # is supervised synchronously and its real exit code is captured.
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Exe
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $quoted = foreach ($arg in $Arguments) {
        $value = [string]$arg
        '"' + $value.Replace('"', '\"') + '"'
    }

    $psi.Arguments = ($quoted -join " ")

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    if (-not $proc.Start()) {
        throw "Failed to start Unity: $Exe"
    }

    Write-Host "Unity PID     :" $proc.Id
    $proc.WaitForExit()
    $proc.Refresh()

    return [int]$proc.ExitCode
}

function Get-UnityFailureClassification {
    param(
        [string]$LogPath,
        [int]$ExitCode,
        [bool]$ReportExists
    )

    $raw = ""
    if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
        $raw = Get-Content -LiteralPath $LogPath -Raw -ErrorAction SilentlyContinue
    }

    $knownPixelPerfect = (
        (
            $raw -match 'com\.unity\.2d\.pixel-perfect' -or
            $raw -match 'PackageCache[\\/].*pixel-perfect'
        ) -and (
            $raw -match 'PixelPerfectCamera' -or
            $raw -match 'Pixel Perfect' -or
            $raw -match 'URP.*Converter' -or
            $raw -match 'Converter.*URP'
        )
    )

    $compileFailure = (
        $raw -match 'error CS\d+' -or
        $raw -match 'Compilation failed' -or
        $raw -match 'Scripts have compiler errors'
    )

    if (-not $ReportExists -and $knownPixelPerfect) {
        return [ordered]@{
            kind = "KNOWN_EDITOR_BUG"
            reason = "Unity 6000.0.36f1 / Pixel Perfect package compile-converter blocker prevented trustworthy task-specific compile acceptance."
        }
    }

    if (-not $ReportExists -and ($compileFailure -or $ExitCode -ne 0)) {
        return [ordered]@{
            kind = "PROJECT_OR_UNITY_FAILURE"
            reason = "Unity release validation did not produce a gate report. Inspect the Unity log for compile/import/build errors."
        }
    }

    return [ordered]@{
        kind = "NONE"
        reason = $null
    }
}

function Read-TestResult {
    param(
        [string]$Path,
        [int]$ExitCode,
        [string]$Platform
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [ordered]@{
            platform = $Platform
            state = if ($ExitCode -eq 0) { "NO_RESULT_FILE" } else { "FAILED_TO_RUN" }
            total = $null
            passed = $null
            failed = $null
            result = $null
        }
    }

    try {
        [xml]$xml = Get-Content -LiteralPath $Path -Raw
        $root = $xml.'test-run'

        $total = 0
        $passed = 0
        $failed = 0
        $result = $null

        if ($root) {
            if ($root.total) { $total = [int]$root.total }
            if ($root.passed) { $passed = [int]$root.passed }
            if ($root.failed) { $failed = [int]$root.failed }
            $result = [string]$root.result
        }

        $state = "UNKNOWN"
        if ($ExitCode -ne 0 -or $failed -gt 0 -or $result -eq "Failed") {
            $state = "FAIL"
        }
        elseif ($total -eq 0) {
            $state = "NO_TESTS"
        }
        elseif ($result -eq "Passed" -or $failed -eq 0) {
            $state = "PASS"
        }

        return [ordered]@{
            platform = $Platform
            state = $state
            total = $total
            passed = $passed
            failed = $failed
            result = $result
        }
    }
    catch {
        return [ordered]@{
            platform = $Platform
            state = "UNREADABLE_RESULT"
            total = $null
            passed = $null
            failed = $null
            result = $_.Exception.Message
        }
    }
}


function Write-UnityDiagnostic {
    param(
        [string]$LogPath,
        [string]$OutputPath
    )

    if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) {
        [System.IO.File]::WriteAllText(
            $OutputPath,
            "Unity log was not created.",
            (New-Object System.Text.UTF8Encoding($false))
        )
        return
    }

    $lines = @(Get-Content -LiteralPath $LogPath -ErrorAction SilentlyContinue)
    $interesting = New-Object System.Collections.Generic.List[string]

    foreach ($line in $lines) {
        if (
            $line -match 'error CS\d+' -or
            $line -match 'Compilation failed' -or
            $line -match 'Scripts have compiler errors' -or
            $line -match 'PixelPerfect' -or
            $line -match 'pixel-perfect' -or
            $line -match 'PackageCache' -or
            $line -match 'executeMethod' -or
            $line -match 'SoulHunterReleaseGateRunner' -or
            $line -match 'Exception' -or
            $line -match 'BuildFailedException' -or
            $line -match 'Aborting batchmode'
        ) {
            $interesting.Add([string]$line)
        }
    }

    $tailCount = [Math]::Min(220, $lines.Count)
    $tail = @()
    if ($tailCount -gt 0) {
        $tail = @($lines[($lines.Count - $tailCount)..($lines.Count - 1)])
    }

    $body = @(
        "=== INTERESTING UNITY LOG LINES ==="
        $interesting.ToArray()
        ""
        "=== UNITY LOG TAIL ==="
        $tail
    ) -join [Environment]::NewLine

    [System.IO.File]::WriteAllText(
        $OutputPath,
        $body,
        (New-Object System.Text.UTF8Encoding($false))
    )
}

Section "SOUL HUNTER RELEASE GATE - SYNCHRONOUS UNITY SUPERVISION"

if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
    throw "Project path missing: $ProjectPath"
}

if (-not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
    throw "Unity executable missing: $UnityExe"
}

$orchestrator = Join-Path $AutomationRoot "SoulHunter-Orchestrator.ps1"
$orchestratorHash = $null
$orchestratorBaselineMatch = $null

if (Test-Path -LiteralPath $orchestrator -PathType Leaf) {
    $orchestratorHash = (Get-FileHash -LiteralPath $orchestrator -Algorithm SHA256).Hash
    $orchestratorBaselineMatch = ($orchestratorHash -eq $ExpectedOrchestratorSha256)
}

$runId = "RELEASE-" + (Get-Date -Format "yyyyMMdd-HHmmss-fff")
$runDir = Join-Path $AutomationRoot ("release-gate-runs\" + $runId)
$workspace = Join-Path $runDir "workspace\project"
$logPath = Join-Path $runDir "unity-release-gate.log"
$unityReportPath = Join-Path $runDir "unity-release-report.json"
$summaryPath = Join-Path $runDir "RELEASE_SUMMARY.json"
$blockersPath = Join-Path $runDir "BLOCKERS.json"
$diagnosticPath = Join-Path $runDir "UNITY_DIAGNOSTIC.txt"
$nextGoalPath = Join-Path $runDir "NEXT_ROOT_GOAL.txt"
$buildDir = Join-Path $runDir "Build"
$buildExe = Join-Path $buildDir "SoulHunter.exe"

New-Item -ItemType Directory -Path $runDir -Force | Out-Null
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null

$canonicalGitBefore = Get-GitState $ProjectPath
$projectUnityVersion = Get-UnityProjectVersion $ProjectPath

Write-Host "Run ID       :" $runId
Write-Host "Canonical    :" $ProjectPath
Write-Host "Unity        :" $UnityExe
Write-Host "Project ver  :" $projectUnityVersion
Write-Host "Orchestrator :" $orchestratorHash

if ($orchestratorBaselineMatch -eq $false) {
    Write-Host "Baseline     : WARNING - orchestrator hash differs from frozen release-gate baseline."
}
elseif ($orchestratorBaselineMatch -eq $true) {
    Write-Host "Baseline     : MATCH"
}

Section "CREATE ISOLATED RELEASE WORKSPACE"

Copy-ProjectIsolated -Source $ProjectPath -Destination $workspace

Write-Host "Workspace    :" $workspace
Write-Host "Canonical    : untouched by release gate"

Section "INSTALL TEMPORARY RELEASE-GATE RUNNER IN WORKSPACE"

$editorDir = Join-Path $workspace "Assets\Editor\ReleaseGate"
New-Item -ItemType Directory -Path $editorDir -Force | Out-Null
$runnerPath = Join-Path $editorDir "SoulHunterReleaseGateRunner.cs"

$runner = @'
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SoulHunterReleaseGateRunner
{
    [Serializable]
    private sealed class GateReport
    {
        public bool success;
        public string unityVersion;
        public string buildTarget;
        public string buildPath;
        public int enabledSceneCount;
        public List<string> enabledScenes = new List<string>();
        public List<string> openedScenes = new List<string>();
        public List<string> blockers = new List<string>();
        public int buildErrors;
        public int buildWarnings;
        public string buildResult;
    }

    public static void Run()
    {
        var report = new GateReport();
        report.unityVersion = Application.unityVersion;
        report.buildTarget = BuildTarget.StandaloneWindows64.ToString();
        report.buildPath = Environment.GetEnvironmentVariable("SOULHUNTER_RELEASE_BUILD_PATH") ?? "";
        var reportPath = Environment.GetEnvironmentVariable("SOULHUNTER_RELEASE_REPORT") ?? "";

        try
        {
            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (!entry.enabled)
                    continue;

                report.enabledScenes.Add(entry.path);
            }

            report.enabledSceneCount = report.enabledScenes.Count;

            if (report.enabledSceneCount == 0)
                report.blockers.Add("No enabled scenes exist in EditorBuildSettings.");

            foreach (var scenePath in report.enabledScenes)
            {
                if (!File.Exists(scenePath))
                {
                    report.blockers.Add("Enabled build scene is missing: " + scenePath);
                    continue;
                }

                try
                {
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    if (!scene.IsValid() || !scene.isLoaded)
                        report.blockers.Add("Scene failed to load: " + scenePath);
                    else
                        report.openedScenes.Add(scenePath);
                }
                catch (Exception ex)
                {
                    report.blockers.Add("Scene open failed: " + scenePath + " :: " + ex.Message);
                }
            }

            if (report.blockers.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(report.buildPath))
                {
                    report.blockers.Add("Release build output path was not provided.");
                }
                else
                {
                    var buildFolder = Path.GetDirectoryName(report.buildPath);
                    if (!string.IsNullOrEmpty(buildFolder))
                        Directory.CreateDirectory(buildFolder);

                    var options = new BuildPlayerOptions
                    {
                        scenes = report.enabledScenes.ToArray(),
                        locationPathName = report.buildPath,
                        target = BuildTarget.StandaloneWindows64,
                        options = BuildOptions.None
                    };

                    BuildReport build = BuildPipeline.BuildPlayer(options);
                    report.buildErrors = build.summary.totalErrors;
                    report.buildWarnings = build.summary.totalWarnings;
                    report.buildResult = build.summary.result.ToString();

                    if (build.summary.result != BuildResult.Succeeded)
                        report.blockers.Add("StandaloneWindows64 player build failed: " + build.summary.result);
                }
            }
        }
        catch (Exception ex)
        {
            report.blockers.Add("Release gate runner exception: " + ex);
        }

        report.success =
            report.blockers.Count == 0 &&
            report.enabledSceneCount > 0 &&
            report.openedScenes.Count == report.enabledSceneCount &&
            string.Equals(report.buildResult, BuildResult.Succeeded.ToString(), StringComparison.Ordinal);

        try
        {
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var folder = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);

                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            }
        }
        finally
        {
            EditorApplication.Exit(report.success ? 0 : 2);
        }
    }
}
#endif
'@

[System.IO.File]::WriteAllText(
    $runnerPath,
    $runner,
    (New-Object System.Text.UTF8Encoding($false))
)

Write-Host "Runner       :" $runnerPath

Section "UNITY IMPORT + COMPILE + SCENE + PLAYER BUILD"

$oldReport = $env:SOULHUNTER_RELEASE_REPORT
$oldBuild = $env:SOULHUNTER_RELEASE_BUILD_PATH

try {
    $env:SOULHUNTER_RELEASE_REPORT = $unityReportPath
    $env:SOULHUNTER_RELEASE_BUILD_PATH = $buildExe

    $unityArgs = @(
        "-batchmode",
        "-nographics",
        "-projectPath", $workspace,
        "-executeMethod", "SoulHunterReleaseGateRunner.Run",
        "-logFile", $logPath
    )

    $unityExit = Invoke-Unity -Exe $UnityExe -Arguments $unityArgs
}
finally {
    $env:SOULHUNTER_RELEASE_REPORT = $oldReport
    $env:SOULHUNTER_RELEASE_BUILD_PATH = $oldBuild
}

$reportExists = Test-Path -LiteralPath $unityReportPath -PathType Leaf
$failureClass = Get-UnityFailureClassification -LogPath $logPath -ExitCode $unityExit -ReportExists $reportExists
Write-UnityDiagnostic -LogPath $logPath -OutputPath $diagnosticPath

$unityGate = $null
if ($reportExists) {
    try {
        $unityGate = Get-Content -LiteralPath $unityReportPath -Raw | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        $unityGate = $null
        $failureClass = [ordered]@{
            kind = "PROJECT_OR_UNITY_FAILURE"
            reason = "Unity gate report was created but could not be parsed."
        }
    }
}

Write-Host "Unity exit   :" $unityExit
Write-Host "Gate report  :" $reportExists
Write-Host "Class        :" $failureClass.kind

if ($unityGate) {
    Write-Host "Scenes       :" $unityGate.enabledSceneCount
    Write-Host "Build result :" $unityGate.buildResult
    Write-Host "Build errors :" $unityGate.buildErrors
}

$testsConfigured = $false
$manifest = Join-Path $workspace "Packages\manifest.json"
$lock = Join-Path $workspace "Packages\packages-lock.json"

foreach ($p in @($manifest, $lock)) {
    if (Test-Path -LiteralPath $p -PathType Leaf) {
        $raw = Get-Content -LiteralPath $p -Raw -ErrorAction SilentlyContinue
        if ($raw -match 'com\.unity\.test-framework') {
            $testsConfigured = $true
            break
        }
    }
}

$editMode = [ordered]@{
    platform = "editmode"
    state = "NOT_RUN"
    total = $null
    passed = $null
    failed = $null
    result = $null
}

$playMode = [ordered]@{
    platform = "playmode"
    state = "NOT_RUN"
    total = $null
    passed = $null
    failed = $null
    result = $null
}

$buildSucceeded = (
    $unityGate -and
    $unityGate.success -eq $true -and
    $unityGate.buildResult -eq "Succeeded" -and
    (Test-Path -LiteralPath $buildExe -PathType Leaf)
)

if ($buildSucceeded -and $testsConfigured) {
    Section "UNITY AUTOMATED TESTS"

    $editResults = Join-Path $runDir "editmode-results.xml"
    $editLog = Join-Path $runDir "editmode-tests.log"

    $editArgs = @(
        "-batchmode",
        "-nographics",
        "-projectPath", $workspace,
        "-runTests",
        "-testPlatform", "editmode",
        "-testResults", $editResults,
        "-logFile", $editLog
    )

    $editExit = Invoke-Unity -Exe $UnityExe -Arguments $editArgs
    $editMode = Read-TestResult -Path $editResults -ExitCode $editExit -Platform "editmode"

    Write-Host "EditMode     :" $editMode.state " total=" $editMode.total " failed=" $editMode.failed

    $playResults = Join-Path $runDir "playmode-results.xml"
    $playLog = Join-Path $runDir "playmode-tests.log"

    $playArgs = @(
        "-batchmode",
        "-nographics",
        "-projectPath", $workspace,
        "-runTests",
        "-testPlatform", "playmode",
        "-testResults", $playResults,
        "-logFile", $playLog
    )

    $playExit = Invoke-Unity -Exe $UnityExe -Arguments $playArgs
    $playMode = Read-TestResult -Path $playResults -ExitCode $playExit -Platform "playmode"

    Write-Host "PlayMode     :" $playMode.state " total=" $playMode.total " failed=" $playMode.failed
}

Section "CANONICAL SAFETY CHECK"

$canonicalGitAfter = Get-GitState $ProjectPath
$canonicalStateUnchanged = (
    $canonicalGitBefore.available -eq $canonicalGitAfter.available -and
    $canonicalGitBefore.head -eq $canonicalGitAfter.head -and
    $canonicalGitBefore.branch -eq $canonicalGitAfter.branch -and
    $canonicalGitBefore.status -eq $canonicalGitAfter.status
)

Write-Host "Git snapshot unchanged :" $canonicalStateUnchanged

$blockers = New-Object System.Collections.Generic.List[object]
$warnings = New-Object System.Collections.Generic.List[string]

if (-not $canonicalStateUnchanged) {
    $blockers.Add([ordered]@{
        type = "CANONICAL_CONCURRENCY"
        actionable_by_orchestrator = $false
        message = "Canonical Git state changed while release validation was running. Results are stale."
    })
}

if ($orchestratorBaselineMatch -eq $false) {
    $warnings.Add("Installed orchestrator hash differs from the frozen baseline captured when this release gate was created.")
}

if ($failureClass.kind -eq "KNOWN_EDITOR_BUG") {
    $blockers.Add([ordered]@{
        type = "KNOWN_EDITOR_BUG"
        actionable_by_orchestrator = $false
        message = $failureClass.reason
    })
}
elseif (-not $buildSucceeded) {
    if ($unityGate -and $unityGate.blockers) {
        foreach ($b in @($unityGate.blockers)) {
            $blockers.Add([ordered]@{
                type = "PROJECT_RELEASE_BLOCKER"
                actionable_by_orchestrator = $true
                message = [string]$b
            })
        }
    }
    else {
        $blockers.Add([ordered]@{
            type = "PROJECT_OR_UNITY_FAILURE"
            actionable_by_orchestrator = $true
            message = $failureClass.reason
        })
    }
}

if ($buildSucceeded) {
    if (-not $testsConfigured) {
        $warnings.Add("Unity Test Framework is not configured; no automated EditMode/PlayMode release evidence is available.")
    }
    else {
        if ($editMode.state -eq "FAIL" -or $editMode.state -eq "FAILED_TO_RUN" -or $editMode.state -eq "UNREADABLE_RESULT") {
            $blockers.Add([ordered]@{
                type = "EDITMODE_TEST_FAILURE"
                actionable_by_orchestrator = $true
                message = "EditMode release tests failed or could not be executed."
            })
        }

        if ($playMode.state -eq "FAIL" -or $playMode.state -eq "FAILED_TO_RUN" -or $playMode.state -eq "UNREADABLE_RESULT") {
            $blockers.Add([ordered]@{
                type = "PLAYMODE_TEST_FAILURE"
                actionable_by_orchestrator = $true
                message = "PlayMode release tests failed or could not be executed."
            })
        }

        if ($editMode.state -eq "NO_TESTS") {
            $warnings.Add("Unity Test Framework exists, but no EditMode tests were discovered.")
        }

        if ($playMode.state -eq "NO_TESTS") {
            $warnings.Add("Unity Test Framework exists, but no PlayMode tests were discovered.")
        }
    }
}

$verdict = "RELEASE_BLOCKED"
$projectState = "RELEASE_BLOCKED"

if ($blockers.Count -eq 0 -and $buildSucceeded) {
    $hasRealEditTests = ($editMode.state -eq "PASS" -and [int]$editMode.total -gt 0)
    $hasRealPlayTests = ($playMode.state -eq "PASS" -and [int]$playMode.total -gt 0)

    if ($testsConfigured -and $hasRealEditTests -and $hasRealPlayTests) {
        $verdict = "PASS"
        $projectState = "PROJECT_COMPLETE"
    }
    else {
        $verdict = "PASS_WITH_EVIDENCE_GAPS"
        $projectState = "RELEASE_CANDIDATE"
    }
}

$actionableBlockers = @(
    $blockers | Where-Object { $_.actionable_by_orchestrator -eq $true }
)

if ($actionableBlockers.Count -gt 0) {
    $messages = @($actionableBlockers | ForEach-Object { "- " + $_.message })
    $nextGoal = @"
Resolve the current Soul Hunter release-gate blockers using only evidence-backed changes.

Release gate evidence:
$summaryPath

Concrete blockers:
$($messages -join [Environment]::NewLine)

After fixing them, stop and allow the release gate to re-run against the updated canonical project.
"@

    [System.IO.File]::WriteAllText(
        $nextGoalPath,
        $nextGoal,
        (New-Object System.Text.UTF8Encoding($false))
    )
}

$summary = [ordered]@{
    schema = 1
    generated_at = (Get-Date).ToString("o")
    run_id = $runId
    verdict = $verdict
    project_state = $projectState
    canonical = $ProjectPath
    isolated_workspace = $workspace
    unity_exe = $UnityExe
    project_unity_version = $projectUnityVersion
    orchestrator = [ordered]@{
        path = $orchestrator
        sha256 = $orchestratorHash
        expected_sha256 = $ExpectedOrchestratorSha256
        baseline_match = $orchestratorBaselineMatch
    }
    canonical_git_state_unchanged = $canonicalStateUnchanged
    unity_gate = [ordered]@{
        exit_code = $unityExit
        report_created = $reportExists
        failure_class = $failureClass
        report = $unityGate
        log = $logPath
        diagnostic = $diagnosticPath
        player_build = $buildExe
        build_exists = (Test-Path -LiteralPath $buildExe -PathType Leaf)
    }
    tests = [ordered]@{
        framework_configured = $testsConfigured
        editmode = $editMode
        playmode = $playMode
    }
    blockers = [object[]]$blockers.ToArray()
    warnings = [string[]]$warnings.ToArray()
    next_root_goal = if (Test-Path -LiteralPath $nextGoalPath -PathType Leaf) { $nextGoalPath } else { $null }
}

Write-JsonFile -Object $summary -Path $summaryPath -Depth 16
Write-JsonFile -Object ([object[]]$blockers.ToArray()) -Path $blockersPath -Depth 10

Section "RELEASE GATE FINAL"

Write-Host "VERDICT       :" $verdict
Write-Host "PROJECT STATE :" $projectState
Write-Host "BUILD         :" $(if ($buildSucceeded) { "PASS" } else { "FAIL/BLOCKED" })
Write-Host "TEST FRAMEWORK:" $testsConfigured
Write-Host "EDITMODE      :" $editMode.state
Write-Host "PLAYMODE      :" $playMode.state
Write-Host "BLOCKERS      :" $blockers.Count
Write-Host "WARNINGS      :" $warnings.Count
Write-Host "CANONICAL SAFE:" $canonicalStateUnchanged
Write-Host "SUMMARY       :" $summaryPath
Write-Host "BLOCKER FILE  :" $blockersPath
Write-Host "UNITY DIAG    :" $diagnosticPath

if (-not $buildSucceeded -and (Test-Path -LiteralPath $diagnosticPath -PathType Leaf)) {
    Write-Host ""
    Write-Host "Unity diagnostic excerpt:"
    Get-Content -LiteralPath $diagnosticPath -Tail 80 -ErrorAction SilentlyContinue
}

if (Test-Path -LiteralPath $nextGoalPath -PathType Leaf) {
    Write-Host "NEXT ROOT GOAL:" $nextGoalPath
}

if ($blockers.Count -gt 0) {
    Write-Host ""
    Write-Host "Blockers:"
    foreach ($b in $blockers) {
        Write-Host (" - [" + $b.type + "] " + $b.message)
    }
}

if ($warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "Warnings:"
    foreach ($w in $warnings) {
        Write-Host (" - " + $w)
    }
}

