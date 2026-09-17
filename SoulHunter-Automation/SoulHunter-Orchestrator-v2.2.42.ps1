param(
    [string]$Task = "Implement/fix whatever Soul Hunter task is next.",
    [ValidateSet("auto","agy","codex")]
    [string]$Agent = "auto",
    [string]$Canonical = "D:\Unity Projects\Soul-Hunter",
    [string]$AutomationRoot = "D:\SoulHunter-Automation",
    [string]$UnityExe = "D:\6000.0.36f1\Editor\Unity.exe",
    [string]$RecoverRun = "",
    [string]$ProviderSnapshotJson = "",
    [switch]$SelfTest,
    [switch]$NoApply,
    [switch]$AllowDelete,
    [switch]$AllowGovernance,
    [switch]$SkipUnityValidation,
    [int]$MaxRetries = 1,
    [int]$MaxAutoTasks = 0
)

$ErrorActionPreference = "Stop"

$ProviderSnapshot = @()
if (-not [string]::IsNullOrWhiteSpace($ProviderSnapshotJson)) {
    try { $ProviderSnapshot = @($ProviderSnapshotJson | ConvertFrom-Json -ErrorAction Stop) }
    catch { throw "Invalid ProviderSnapshotJson; refusing provider routing by executable presence." }
}

# ============================================================================
# Soul Hunter Autonomous Orchestrator v2.2.31 - CORE FINAL
#
# PowerShell owns deterministic control/safety only.
# AGY/ASTRA owns project scan + task discovery.
# AGY/Codex own implementation.
# Unity owns import/compile validation.
# Git is evidence/checkpoint only; no destructive Git operations are used.
# ============================================================================

function Write-Section([string]$Title) {
    Write-Host ""
    Write-Host ("=" * 78)
    Write-Host (" " + $Title)
    Write-Host ("=" * 78)
}

function Find-Exe([string]$Name, [string[]]$KnownPaths) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    foreach ($p in $KnownPaths) {
        if ($p -and (Test-Path -LiteralPath $p -PathType Leaf -ErrorAction SilentlyContinue)) {
            return $p
        }
    }
    return $null
}

function Get-RelPath([string]$Root, [string]$FullPath) {
    $r = [System.IO.Path]::GetFullPath($Root).TrimEnd("\")
    $f = [System.IO.Path]::GetFullPath($FullPath)

    if (-not $f.StartsWith($r, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escaped root: $FullPath"
    }

    return $f.Substring($r.Length).TrimStart("\")
}

function Test-IsExcluded([string]$RelativePath) {
    $p = $RelativePath.Replace("/","\")
    $parts = @($p.Split("\"))

    foreach ($x in @(
        ".git",
        ".vs",
        "Library",
        "Temp",
        "Logs",
        "obj",
        "UserSettings"
    )) {
        if ($parts -contains $x) { return $true }
    }

    return $false
}

function Test-IsForbiddenChange([string]$RelativePath) {
    $p = $RelativePath.Replace("/","\")

    if (Test-IsExcluded $p) { return $true }
    if (-not $AllowGovernance -and $p -ieq "AGENTS.md") { return $true }
    if ($p -like "Backups\*") { return $true }

    return $false
}

function New-Manifest([string]$Root) {
    $map = @{}

    Get-ChildItem -LiteralPath $Root -Recurse -Force -File -ErrorAction SilentlyContinue |
        ForEach-Object {
            $rel = Get-RelPath $Root $_.FullName

            if (-not (Test-IsExcluded $rel)) {
                $map[$rel] = [PSCustomObject]@{
                    RelativePath = $rel
                    SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
                    Length = $_.Length
                }
            }
        }

    return $map
}

function Compare-Manifests($Before, $After) {
    $all = @($Before.Keys + $After.Keys | Sort-Object -Unique)
    $changes = @()

    foreach ($rel in $all) {
        if (-not $Before.ContainsKey($rel)) {
            $changes += [PSCustomObject]@{
                RelativePath = $rel
                ChangeType = "ADDED"
                BeforeSHA256 = ""
                AfterSHA256 = $After[$rel].SHA256
            }
        }
        elseif (-not $After.ContainsKey($rel)) {
            $changes += [PSCustomObject]@{
                RelativePath = $rel
                ChangeType = "DELETED"
                BeforeSHA256 = $Before[$rel].SHA256
                AfterSHA256 = ""
            }
        }
        elseif ($Before[$rel].SHA256 -ne $After[$rel].SHA256) {
            $changes += [PSCustomObject]@{
                RelativePath = $rel
                ChangeType = "MODIFIED"
                BeforeSHA256 = $Before[$rel].SHA256
                AfterSHA256 = $After[$rel].SHA256
            }
        }
    }

    return @($changes)
}

function Copy-CanonicalToWorkspace([string]$Source, [string]$Destination) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    $robocopy = Find-Exe "robocopy" @("$env:SystemRoot\System32\robocopy.exe")

    if ($robocopy) {
        $args = @(
            $Source,
            $Destination,
            "/E",
            "/COPY:DAT",
            "/DCOPY:DAT",
            "/R:1",
            "/W:1",
            "/NFL",
            "/NDL",
            "/NJH",
            "/NJS",
            "/NP",
            "/XD",
            ".git",
            ".vs",
            "Library",
            "Temp",
            "Logs",
            "obj",
            "UserSettings"
        )

        & $robocopy @args | Out-Null
        $code = $LASTEXITCODE

        if ($code -gt 7) {
            throw "robocopy failed with exit code $code"
        }

        return
    }

    # Fallback if robocopy is unavailable.
    Get-ChildItem -LiteralPath $Source -Recurse -Force -File |
        ForEach-Object {
            $rel = Get-RelPath $Source $_.FullName
            if (Test-IsExcluded $rel) { return }

            $dst = Join-Path $Destination $rel
            $parent = Split-Path -Parent $dst
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $dst -Force
        }
}

function ConvertTo-WindowsProcessArgument([string]$Value) {
    if ($null -eq $Value) { return '""' }

    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    # Our native control arguments never contain embedded quotes; preserve
    # Windows path backslashes exactly and quote only the whole argument.
    if ($Value.Contains('"')) {
        $Value = $Value.Replace('"','\"')
    }

    return '"' + $Value + '"'
}

function Invoke-NativeCapture(
    [string]$Exe,
    [string[]]$Args,
    [string]$WorkingDirectory,
    [string]$LogPath
) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Exe
    if ($WorkingDirectory) { $psi.WorkingDirectory = $WorkingDirectory }
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $psi.Arguments = (($Args | ForEach-Object {
        ConvertTo-WindowsProcessArgument ([string]$_)
    }) -join " ")

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    if (-not $proc.Start()) {
        throw "Failed to start: $Exe"
    }

    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    $proc.WaitForExit()

    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result
    $combined = @($stdout,$stderr) -join [Environment]::NewLine

    if ($LogPath) {
        $combined | Out-File -LiteralPath $LogPath -Encoding UTF8
    }

    return [PSCustomObject]@{
        ExitCode = $proc.ExitCode
        Output = $combined
    }
}

function Get-FinalAgentText([string]$Output) {
    if ([string]::IsNullOrWhiteSpace($Output)) { return "" }

    $latestCodex = ""

    foreach ($raw in @($Output -split "`r?`n")) {
        $line = $raw.Trim()
        if (-not $line.StartsWith("{")) { continue }

        try {
            $obj = $line | ConvertFrom-Json -ErrorAction Stop

            # AGY stream-json final envelope.
            if ($obj.event -eq "result" -and
                $obj.result -and
                $obj.result.response) {
                return [string]$obj.result.response
            }

            # Codex JSON events.
            if ($obj.type -eq "item.completed" -and
                $obj.item -and
                $obj.item.type -eq "agent_message" -and
                $obj.item.text) {
                $latestCodex = [string]$obj.item.text
            }

            if ($obj.type -eq "message" -and $obj.message) {
                $latestCodex = [string]$obj.message
            }
        }
        catch {
            # Ignore non-JSON lines.
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($latestCodex)) {
        return $latestCodex
    }

    return $Output.Trim()
}

function Invoke-AgyAgent(
    [string]$Exe,
    [string]$Prompt,
    [string]$Workspace,
    [string]$LogPath,
    [string]$ModelId
) {
    $event = [ordered]@{
        event = "user"
        message = [ordered]@{
            content = $Prompt
        }
    } | ConvertTo-Json -Depth 6 -Compress

    if ([string]::IsNullOrWhiteSpace($ModelId)) {
        throw "AGY model id is required; refusing implicit/default model routing."
    }

    $args = @(
        "--model", $ModelId,
        "--input-format", "stream-json",
        "--output-format", "stream-json",
        "--sandbox",
        "--print-timeout", "20m",
        "--dangerously-skip-permissions"
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Exe
    $psi.WorkingDirectory = $Workspace
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $psi.Arguments = (($args | ForEach-Object {
        ConvertTo-WindowsProcessArgument ([string]$_)
    }) -join " ")

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    if (-not $proc.Start()) {
        throw "Failed to start Antigravity CLI."
    }

    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    $null = ConvertFrom-Json $event -ErrorAction Stop
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $bytes = $utf8NoBom.GetBytes($event + "`n")
    $proc.StandardInput.BaseStream.Write($bytes, 0, $bytes.Length)
    $proc.StandardInput.BaseStream.Flush()
    $proc.StandardInput.Close()

    $finished = $proc.WaitForExit(1500000) # 25 minutes

    if (-not $finished) {
        try { $proc.Kill() } catch {}
        $timeout = "AGY HARD TIMEOUT after 25 minutes."
        $timeout | Out-File -LiteralPath $LogPath -Encoding UTF8

        return [PSCustomObject]@{
            ExitCode = 124
            Output = $timeout
            Backend = "agy"
        }
    }

    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result
    $combined = @($stdout,$stderr) -join [Environment]::NewLine
    $combined | Out-File -LiteralPath $LogPath -Encoding UTF8

    $effectiveExit = $proc.ExitCode

    # Treat explicit non-success AGY terminal envelopes as failure.
    foreach ($raw in @($combined -split "`r?`n")) {
        $line = $raw.Trim()
        if (-not $line.StartsWith("{")) { continue }

        try {
            $obj = $line | ConvertFrom-Json -ErrorAction Stop
            if ($obj.event -eq "result" -and
                $obj.result -and
                $obj.result.status -and
                ([string]$obj.result.status).ToUpperInvariant() -ne "SUCCESS") {
                $effectiveExit = 1
            }
        }
        catch {}
    }

    return [PSCustomObject]@{
        ExitCode = $effectiveExit
        Output = $combined
        Backend = "agy"
    }
}

function Invoke-CodexAgent(
    [string]$Exe,
    [string]$Prompt,
    [string]$Workspace,
    [string]$LogPath
) {
    $args = @("exec","--skip-git-repo-check", "--experimental-json", "--sandbox", "workspace-write")

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Exe
    $psi.WorkingDirectory = $Workspace
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $psi.Arguments = (($args | ForEach-Object {
        ConvertTo-WindowsProcessArgument ([string]$_)
    }) -join " ")

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    if (-not $proc.Start()) {
        throw "Failed to start Codex CLI."
    }

    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $bytes = $utf8NoBom.GetBytes($Prompt + "`n")
    $proc.StandardInput.BaseStream.Write($bytes, 0, $bytes.Length)
    $proc.StandardInput.BaseStream.Flush()
    $proc.StandardInput.Close()

    $finished = $proc.WaitForExit(1500000)

    if (-not $finished) {
        try { $proc.Kill() } catch {}
        $timeout = "CODEX HARD TIMEOUT after 25 minutes."
        $timeout | Out-File -LiteralPath $LogPath -Encoding UTF8

        return [PSCustomObject]@{
            ExitCode = 124
            Output = $timeout
            Backend = "codex"
        }
    }

    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result
    $combined = @($stdout,$stderr) -join [Environment]::NewLine
    $combined | Out-File -LiteralPath $LogPath -Encoding UTF8

    return [PSCustomObject]@{
        ExitCode = $proc.ExitCode
        Output = $combined
        Backend = "codex"
    }
}

function Invoke-Astra(
    [string]$AgyExe,
    [string]$CodexExe,
    [string]$Prompt,
    [string]$Workspace,
    [string]$LogPath,
    [string]$ModelId
) {
    $candidates = @(Get-EligibleSemanticBackends -Providers $ProviderSnapshot -Capability "CODE_REASONING" -AgyExe $AgyExe -CodexExe $CodexExe)
    if ($candidates.Count -eq 0) {
        return [PSCustomObject]@{ ExitCode=127; Output="No AVAILABLE semantic provider for CODE_REASONING."; Backend="none"; Provider=""; ModelId="" }
    }

    foreach ($candidate in $candidates) {
        $selectedModel = [string]$candidate.ModelId
        $logForAttempt = $LogPath
        if ($candidate.Name -ne $candidates[0].Name) {
            $logForAttempt = [System.IO.Path]::ChangeExtension($LogPath, (".{0}.log" -f $candidate.Name.Replace('/','-')))
        }
        Write-Host ("SELECTED PROVIDER: {0} status={1} usable={2} model={3}" -f $candidate.Name,$candidate.Provider.status,$candidate.Provider.usable_capacity,$selectedModel)

        if ($candidate.Name -like "AGY/*") {
            $r = Invoke-AgyAgent $candidate.Exe $Prompt $Workspace $logForAttempt $selectedModel
        } else {
            $r = Invoke-CodexAgent $candidate.Exe $Prompt $Workspace $logForAttempt
        }

        if ($r.ExitCode -eq 0) {
            $r | Add-Member -NotePropertyName Provider -NotePropertyValue $candidate.Name -Force
            $r | Add-Member -NotePropertyName ModelId -NotePropertyValue $selectedModel -Force
            return $r
        }

        Write-Host ("PROVIDER FAILED: {0} -> trying next AVAILABLE capability provider." -f $candidate.Name)
    }

    return [PSCustomObject]@{ ExitCode=1; Output="All AVAILABLE semantic providers for CODE_REASONING failed."; Backend="none"; Provider=""; ModelId="" }
}

function Test-NoUnresolvedPromptVariables([string]$Prompt) {
    # If the prompt still contains '$' followed by an identifier, it wasn't fully interpolated.
    return -not ($Prompt -match '\$[A-Za-z_][A-Za-z0-9_]*')
}

function Get-EligibleSemanticBackends {
    param([object[]]$Providers,[string]$Capability,[string]$AgyExe,[string]$CodexExe)
    if (@($Providers).Count -eq 0) {
        return @()
    }
    $rows = @($Providers) | Where-Object {
        $null -ne $_ -and $_.kind -eq "SEMANTIC" -and
        ([string]$_.status).ToUpperInvariant() -eq "AVAILABLE" -and
        [int]$_.usable_capacity -gt 0 -and
        [bool]$_.quota_verified -and @($_.capabilities) -contains $Capability
    }
    $result = @()
    foreach ($p in $rows) {
        if ([string]$p.name -like "AGY/*") {
            if ($AgyExe -and $p.model_id) { $result += [pscustomobject]@{Name=[string]$p.name;Exe=$AgyExe;ModelId=[string]$p.model_id;Provider=$p} }
        } elseif ([string]$p.name -eq "Codex") {
            if ($CodexExe) { $result += [pscustomobject]@{Name="Codex";Exe=$CodexExe;ModelId="codex";Provider=$p} }
        }
    }
    return @($result | Sort-Object -Property @{Expression={[int]$_.Provider.usable_capacity};Descending=$true}, @{Expression={$_.Name};Descending=$false})
}

function Get-NextObjectiveFromProjectScan(
    [string]$AgyExe,
    [string]$CodexExe,
    [string]$Workspace,
    [string]$EvidenceDir,
    [string]$RootGoal,
    [object[]]$Providers
) {
    $ledger = @(Read-ObjectiveLedger)
    $ledgerText = if ($ledger.Count -gt 0) {
        ($ledger | ForEach-Object {
            "- ID=$($_.objective_id) | STATUS=$($_.status) | OBJECTIVE=$($_.objective_text)"
        }) -join "`n"
    }
    else {
        "NONE"
    }

    $prompt = @"
SOUL HUNTER — AUTONOMOUS READ-ONLY OBJECTIVE DISCOVERY

WORKSPACE:
$Workspace

ROOT GOAL:
$RootGoal

PREVIOUS OBJECTIVES:
$ledgerText

YOUR ROLE:
You are the project-level objective supervisor.
Inspect the ACTUAL isolated project deeply and select up to 8 concrete objectives
that advance the ROOT GOAL.

RULES:
- Do not modify any file.
- Do not launch Unity.
- Do not mutate Git.
- Do not hardcode/assume subsystems like combat, ui, save. Discover them from evidence.
- Do not return terminal objectives (EXHAUSTED, COMPLETED, REJECTED, OWNER_BLOCKED) from the ledger.
- You are operating in an isolated workspace. Do not ask the owner to close Unity or manage Editor locks.
- If the project requires genuine owner/human input (e.g., creative decisions), set overall_state to OWNER_INPUT_REQUIRED.
- If the root goal is completely achieved based on project evidence, set overall_state to PROJECT_COMPLETE.

RETURN ONLY VALID JSON, NO MARKDOWN:
{
  "overall_state": "OBJECTIVES_AVAILABLE|OWNER_INPUT_REQUIRED|PROJECT_COMPLETE",
  "owner_question": "if overall_state is OWNER_INPUT_REQUIRED, one concrete question",
  "objectives": [
    {
      "id": "UPPERCASE-SLUG",
      "priority": 1,
      "objective": "concrete objective description",
      "risk": "LOW|MEDIUM",
      "evidence_files": ["relative/path"],
      "acceptance_summary": "what done looks like",
      "why_now": "why this is the next logical step"
    }
  ]
}
"@

    if (-not (Test-NoUnresolvedPromptVariables $prompt)) {
        return [PSCustomObject]@{
            ScannerFailed = $true
            Reason = "Internal controller error: template tokens were not resolved."
        }
    }

    $before = New-Manifest $Workspace

    $backends = @(Get-EligibleSemanticBackends -Providers $Providers -Capability "PROJECT_SCAN" -AgyExe $AgyExe -CodexExe $CodexExe)

    if ($backends.Count -eq 0) {
        return [PSCustomObject]@{
            ScannerFailed = $true
            Reason = "No AVAILABLE semantic provider for PROJECT_SCAN capability."
        }
    }

    $lastErrorReason = "Objective scanner backend failed."

    foreach ($b in $backends) {
        $log = Join-Path $EvidenceDir "objective-scan-$($b.Name).log"
        Write-Host ("SELECTED PROVIDER: {0} status={1} usable={2} model={3}" -f $b.Name,$b.Provider.status,$b.Provider.usable_capacity,$b.ModelId)
        if ($b.Name -like "AGY/*") {
            $scan = Invoke-AgyAgent $b.Exe $prompt $Workspace $log $b.ModelId
        } else {
            $scan = Invoke-CodexAgent $b.Exe $prompt $Workspace $log
        }

        $after = New-Manifest $Workspace
        $scanDelta = @(Compare-Manifests $before $after)
        if ($scanDelta.Count -gt 0) {
            throw "Read-only objective scanner modified the workspace."
        }

        $final = Get-FinalAgentText $scan.Output

        if ($scan.ExitCode -ne 0 -and [string]::IsNullOrWhiteSpace($final)) {
            $lastErrorReason = "Backend $($b.Name) failed with no usable response."
            continue
        }

        if ([string]::IsNullOrWhiteSpace($final)) {
            $lastErrorReason = "Backend $($b.Name) returned an empty response."
            continue
        }

        $final | Out-File -LiteralPath (Join-Path $EvidenceDir "objective-scan-final-$($b.Name).txt") -Encoding UTF8

        $obj = ConvertFrom-AgentJson $final
        if (-not $obj) {
            if ($final.Contains("{")) {
                $recoveredText = Invoke-ScannerJsonRecovery -AgyExe $AgyExe -CodexExe $CodexExe -Workspace $Workspace -EvidenceDir $EvidenceDir -SchemaKind "OBJECTIVE_SCAN" -RawText $final
                if ($recoveredText) {
                    $obj = ConvertFrom-AgentJson $recoveredText
                }
            }
        }

        if (-not $obj) {
            if ($scan.ExitCode -ne 0) {
                $errTrim = $final.Trim()
                if ($errTrim.Length -gt 150) { $errTrim = $errTrim.Substring(0,150) + "..." }
                $lastErrorReason = "Backend $($b.Name) failed: $errTrim"
            } else {
                $lastErrorReason = "Objective scanner returned invalid JSON that could not be recovered."
            }
            continue
        }

        $obj | ConvertTo-Json -Depth 12 | Out-File -LiteralPath (Join-Path $EvidenceDir "objective-scan.json") -Encoding UTF8
        
        $state = ([string]$obj.overall_state).Trim().ToUpperInvariant()
        
        if ($state -notin @("OBJECTIVES_AVAILABLE","OWNER_INPUT_REQUIRED","PROJECT_COMPLETE")) {
            $lastErrorReason = "Invalid overall_state from objective scanner."
            continue
        }
        
        return [PSCustomObject]@{
            ScannerFailed = $false
            OverallState = $state
            OwnerQuestion = [string]$obj.owner_question
            Objectives = @($obj.objectives)
        }
    }

    return [PSCustomObject]@{
        ScannerFailed = $true
        Reason = $lastErrorReason
    }
    
    return [PSCustomObject]@{
        ScannerFailed = $false
        OverallState = $state
        OwnerQuestion = [string]$obj.owner_question
        Objectives = @($obj.objectives)
    }
}

function Get-NextTaskFromProjectScan(
    [string]$AgyExe,
    [string]$CodexExe,
    [string]$Workspace,
    [string]$EvidenceDir,
    [string]$CurrentObjective,
    [object[]]$Providers
) {
    $ledger = @(Read-TaskLedger)

    $ledgerText = if ($ledger.Count -gt 0) {
        ($ledger | ForEach-Object {
            "- ID=$($_.id) | STATUS=$($_.status) | OBJECTIVE=$($_.objective)"
        }) -join "`n"
    }
    else {
        "NONE"
    }

    $prompt = @"
SOUL HUNTER — AUTONOMOUS READ-ONLY PROJECT SCAN

WORKSPACE:
$Workspace

CURRENT OBJECTIVE:
$CurrentObjective

PREVIOUS TERMINAL TASKS:
$ledgerText

YOUR ROLE:
You are the project brain. PowerShell is only the deterministic controller.
Inspect the ACTUAL isolated project deeply and select the best next safe task
that advances the CURRENT OBJECTIVE.


MANDATORY SCAN:
- Read AGENTS.md, but verify every claim against CURRENT files.
- Inspect Assets/, ProjectSettings/, Packages/ and relevant Backups/.
- Trace C# call sites, managers/bootstrap, prefabs/scenes, serialized references,
  pooling/spawning, combat, save/load, progression, Addressables/content,
  null/error paths and existing tests.
- Search before declaring something missing.
- Current files beat stale notes.
- Do not modify any file during this scan.
- Do not launch Unity.
- Do not mutate Git.
- You are operating in an isolated workspace. Do not ask the owner to close Unity or manage Editor locks.

SELECT ONLY:
- a current evidence-backed defect;
- a technically clear incomplete owner-approved requirement;
- a deterministic robustness/integration defect;
- an unambiguous missing reference/asset recovery;
- a focused validation/test gap with already-defined expected behavior.

DO NOT SELECT:
- a previous terminal task under a renamed ID unless current evidence proves regression;
- cleanup/refactor without a concrete defect;
- architecture redesign;
- balance/narrative/art/audio/progression/product decisions;
- guessed GUIDs or serialized values;
- collider/physics/visual tuning;
- runtime/PlayMode/physics/visual acceptance unless an EXISTING automated test proves it;
- anything requiring owner input before implementation.

RETURN ONLY VALID JSON, NO MARKDOWN:
{
  "verdict": "PASS|BLOCKED",
  "scan_summary": "short factual summary",
  "tasks": [
    {
      "id": "UPPERCASE-STABLE-SLUG",
      "priority": 1,
      "objective": "one small concrete implementation objective",
      "kind": "BUGFIX|REQUIREMENT_SLICE|HARDENING|VALIDATION",
      "required_agent": "AGY|CODEX",
      "validation_level": "STATIC|UNITY_IMPORT",
      "risk": "LOW|MEDIUM",
      "evidence_files": ["relative/path"],
      "acceptance_checks": ["objective deterministic check"],
      "why_now": "why current evidence proves this task exists"
    }
  ]
}

RULES:
- Return at most 10 tasks, best task first.
- Every evidence path must already exist.
- At least one evidence path for each task must be under Assets/,
  ProjectSettings/ or Packages/.
- Every acceptance check must be objective.
- Prefer small high-confidence work.
- If no safe task exists, verdict=BLOCKED and tasks=[].
"@

    if (-not (Test-NoUnresolvedPromptVariables $prompt)) {
        return [PSCustomObject]@{
            ScannerFailed = $true
            Reason = "Internal controller error: template tokens were not resolved."
            Backend = "none"
            SelectionSource = "project-deep-scan"
        }
    }

    $before = New-Manifest $Workspace

    $backends = @(Get-EligibleSemanticBackends -Providers $Providers -Capability "PROJECT_SCAN" -AgyExe $AgyExe -CodexExe $CodexExe)

    if ($backends.Count -eq 0) {
        return [PSCustomObject]@{
            ScannerFailed = $true
            Reason = "No AVAILABLE semantic provider for PROJECT_SCAN capability."
            Backend = "none"
            SelectionSource = "project-deep-scan"
        }
    }

    $lastErrorReason = "Project scanner backend failed."
    $lastBackend = "none"

    foreach ($b in $backends) {
        $log = Join-Path $EvidenceDir "project-scan-$($b.Name).log"
        Write-Host ("SELECTED PROVIDER: {0} status={1} usable={2} model={3}" -f $b.Name,$b.Provider.status,$b.Provider.usable_capacity,$b.ModelId)
        if ($b.Name -like "AGY/*") {
            $scan = Invoke-AgyAgent $b.Exe $prompt $Workspace $log $b.ModelId
        } else {
            $scan = Invoke-CodexAgent $b.Exe $prompt $Workspace $log
        }

        $after = New-Manifest $Workspace
        $scanDelta = @(Compare-Manifests $before $after)

        if ($scanDelta.Count -gt 0) {
            throw "Read-only project scanner modified the workspace."
        }

        $final = Get-FinalAgentText $scan.Output

        if ($scan.ExitCode -ne 0 -and [string]::IsNullOrWhiteSpace($final)) {
            $lastErrorReason = "Backend $($b.Name) failed with no usable response."
            $lastBackend = $b.Name
            continue
        }

        if ([string]::IsNullOrWhiteSpace($final)) {
            $lastErrorReason = "Backend $($b.Name) returned an empty response."
            $lastBackend = $b.Name
            continue
        }

        $final | Out-File -LiteralPath (Join-Path $EvidenceDir "project-scan-final-$($b.Name).txt") -Encoding UTF8

        $obj = ConvertFrom-AgentJson $final

        if (-not $obj) {
            if ($final.Contains("{")) {
                $recoveredText = Invoke-ScannerJsonRecovery -AgyExe $AgyExe -CodexExe $CodexExe -Workspace $Workspace -EvidenceDir $EvidenceDir -SchemaKind "TASK_SCAN" -RawText $final
                if ($recoveredText) {
                    $obj = ConvertFrom-AgentJson $recoveredText
                }
            }
        }

        if (-not $obj) {
            if ($scan.ExitCode -ne 0) {
                $errTrim = $final.Trim()
                if ($errTrim.Length -gt 150) { $errTrim = $errTrim.Substring(0,150) + "..." }
                $lastErrorReason = "Backend $($b.Name) failed: $errTrim"
            } else {
                $lastErrorReason = "Project scanner returned invalid JSON that could not be recovered."
            }
            $lastBackend = $b.Name
            continue
        }

        $obj | ConvertTo-Json -Depth 12 | Out-File -LiteralPath (Join-Path $EvidenceDir "project-scan.json") -Encoding UTF8

        if (([string]$obj.verdict).ToUpperInvariant() -ne "PASS") {
            return [PSCustomObject]@{
                NoSafeTask = $true
                Reason = [string]$obj.scan_summary
                Backend = $b.Name
                SelectionSource = "project-deep-scan"
            }
        }
        
        $lastBackend = $b.Name
        break
    }

    if (-not $obj) {
        return [PSCustomObject]@{
            ScannerFailed = $true
            Reason = $lastErrorReason
            Backend = $lastBackend
            SelectionSource = "project-deep-scan"
        }
    }

    $terminalIds = @{}
    foreach ($x in $ledger) {
        if ($x.id) {
            $terminalIds[([string]$x.id).ToUpperInvariant()] = $true
        }
    }

    $ordered = @(
        @($obj.tasks) |
            Sort-Object @{Expression={
                try { [int]$_.priority } catch { 9999 }
            }; Ascending=$true}
    )

    foreach ($c in $ordered) {
        $id = ([string]$c.id).Trim().ToUpperInvariant()
        $objective = ([string]$c.objective).Trim()
        $agent = ([string]$c.required_agent).Trim().ToUpperInvariant()
        $validation = ([string]$c.validation_level).Trim().ToUpperInvariant()
        $risk = ([string]$c.risk).Trim().ToUpperInvariant()

        if ($id -notmatch '^[A-Z0-9][A-Z0-9-]{2,79}$') { continue }
        if ($terminalIds.ContainsKey($id)) { continue }
        if ([string]::IsNullOrWhiteSpace($objective)) { continue }
        if ($agent -notin @("AGY","CODEX")) { continue }
        if ($validation -notin @("STATIC","UNITY_IMPORT")) { continue }
        if ($risk -notin @("LOW","MEDIUM")) { continue }

        $evidenceFiles = @(
            @($c.evidence_files) |
                ForEach-Object { ([string]$_).Trim() } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )

        if ($evidenceFiles.Count -eq 0 -or $evidenceFiles.Count -gt 8) {
            continue
        }

        $evidenceOk = $true
        $hasProjectEvidence = $false

        foreach ($rel in $evidenceFiles) {
            if (-not (Test-EvidencePath $Workspace $rel)) {
                $evidenceOk = $false
                break
            }

            $n = $rel.Replace("/","\")
            if ($n -match '^(Assets|ProjectSettings|Packages)\\') {
                $hasProjectEvidence = $true
            }
        }

        if (-not $evidenceOk -or -not $hasProjectEvidence) { continue }

        $checks = @(
            @($c.acceptance_checks) |
                ForEach-Object { ([string]$_).Trim() } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )

        if ($checks.Count -eq 0) { continue }

        if (Test-RequiresRuntimeProof $objective $checks $validation) {
            continue
        }

        $unsafe = ($objective + " " + ($checks -join " ")).ToLowerInvariant()
        $reject = $false

        foreach ($bad in @(
            "owner decision",
            "choose audio",
            "choose art",
            "balance value",
            "narrative decision",
            "progression decision",
            "collider radius",
            "collider size",
            "physics tuning",
            "gameplay feel",
            "visual feel",
            "rewrite architecture",
            "from scratch"
        )) {
            if ($unsafe.Contains($bad)) {
                $reject = $true
                break
            }
        }

        if ($reject) { continue }

        return [PSCustomObject]@{
            NoSafeTask = $false
            Id = $id
            Objective = $objective
            Kind = [string]$c.kind
            RequiredAgent = $agent
            ValidationLevel = $validation
            Risk = $risk
            EvidenceFiles = $evidenceFiles
            AcceptanceChecks = $checks
            WhyNow = [string]$c.why_now
            Backend = $lastBackend
            SelectionSource = "project-deep-scan"
        }
    }

    return [PSCustomObject]@{
        NoSafeTask = $true
        Reason = "AGY scanned the project, but no returned candidate passed deterministic safety/evidence checks."
        Backend = $lastBackend
        SelectionSource = "project-deep-scan"
    }
}

function Build-ImplementationPrompt($Selection) {
    $evidenceText = ($Selection.EvidenceFiles | ForEach-Object { "- $_" }) -join "`n"
    $checkText = ($Selection.AcceptanceChecks | ForEach-Object { "- $_" }) -join "`n"

    return @"
SOUL HUNTER IMPLEMENTATION TASK

TASK ID:
$($Selection.Id)

OBJECTIVE:
$($Selection.Objective)

KIND:
$($Selection.Kind)

VALIDATION LEVEL:
$($Selection.ValidationLevel)

RISK:
$($Selection.Risk)

EVIDENCE FILES:
$evidenceText

MANDATORY ACCEPTANCE CHECKS:
$checkText

WHY NOW:
$($Selection.WhyNow)

WORK RULES:
- Work ONLY inside the current isolated workspace.
- Re-check the evidence before editing.
- Make the smallest coherent change satisfying ALL acceptance checks.
- Use current project patterns/interfaces/data.
- Do not modify AGENTS.md.
- Do not invent balance/design/GUID/serialized/tuning values.
- Do not broaden scope.
- Do not use destructive Git.
- If an owner/product decision is required, do not guess.
- If current code already satisfies everything, make no changes.

RETURN EXACTLY THESE FIELDS:
VERDICT: CHANGED / NO_CHANGE / BLOCKED
REQUIREMENTS_SATISFIED: YES / NO
BLOCK_REASON: NONE / OWNER_INPUT_REQUIRED / EVIDENCE_STALE / TECHNICAL_BLOCKER
FILES_CHANGED: <semicolon-separated relative paths or NONE>
SUMMARY: <concise factual summary>
VALIDATION_PERFORMED: <what you actually checked>
RISKS_OR_FOLLOWUP: <concise or NONE>
"@
}

function Get-AgentField([string]$FinalText, [string]$Name) {
    $m = [regex]::Match(
        $FinalText,
        ('(?im)^\s*' + [regex]::Escape($Name) + '\s*:\s*(.+?)\s*$')
    )

    if ($m.Success) {
        return $m.Groups[1].Value.Trim()
    }

    return ""
}

function Invoke-ReadOnlyResultVerifier(
    [string]$AgyExe,
    [string]$CodexExe,
    $Selection,
    [string]$Workspace,
    [string]$EvidenceDir,
    $Changes
) {
    $changedText = if (@($Changes).Count -gt 0) {
        (@($Changes) | ForEach-Object {
            "- $($_.ChangeType): $($_.RelativePath)"
        }) -join "`n"
    }
    else {
        "NONE"
    }

    $evidenceText = if (@($Selection.EvidenceFiles).Count -gt 0) {
        (@($Selection.EvidenceFiles) | ForEach-Object { "- $_" }) -join "`n"
    }
    else {
        "NONE"
    }

    $checksText = if (@($Selection.AcceptanceChecks).Count -gt 0) {
        (@($Selection.AcceptanceChecks) | ForEach-Object { "- $_" }) -join "`n"
    }
    else {
        "NONE"
    }

    $prompt = @"
SOUL HUNTER — READ-ONLY IMPLEMENTATION RESULT VERIFIER

WORKSPACE:
$Workspace

TASK ID:
$($Selection.Id)

OBJECTIVE:
$($Selection.Objective)

EVIDENCE FILES:
$evidenceText

MANDATORY ACCEPTANCE CHECKS:
$checksText

EXACT WORKSPACE DELTA:
$changedText

ROLE:
The implementation agent finished with exit code 0, but its final response was
not machine-parseable. Do NOT implement or repair anything. Inspect the current
workspace read-only and independently determine whether the CURRENT workspace
satisfies the task.

RULES:
- Do not modify any file.
- Do not launch Unity.
- Do not mutate Git.
- Verify every mandatory acceptance check against current files.
- Do not infer success merely because files changed.
- If the task is only partially implemented, requirements_satisfied must be NO.
- If owner/product input is required, verdict must be BLOCKED.
- If exact delta is non-empty and all requirements are satisfied, verdict=CHANGED.
- If exact delta is empty and all requirements were already satisfied, verdict=NO_CHANGE.
- Do not invent missing requirements or values.

RETURN ONLY VALID JSON, NO MARKDOWN:
{
  "verdict": "CHANGED|NO_CHANGE|BLOCKED",
  "requirements_satisfied": "YES|NO",
  "block_reason": "NONE|OWNER_INPUT_REQUIRED|EVIDENCE_STALE|TECHNICAL_BLOCKER",
  "summary": "short factual verification summary"
}
"@

    if (-not (Test-NoUnresolvedPromptVariables $prompt)) {
        return [PSCustomObject]@{
            Success = $false
            Reason = "Internal controller error: template tokens were not resolved."
            Verdict = ""
            Requirements = ""
            BlockReason = ""
            Summary = ""
        }
    }

    $before = New-Manifest $Workspace
    $log = Join-Path $EvidenceDir "result-verifier.log"
    $r = Invoke-Astra $AgyExe $CodexExe $prompt $Workspace $log
    $after = New-Manifest $Workspace
    $verifierDelta = @(Compare-Manifests $before $after)

    if ($verifierDelta.Count -gt 0) {
        return [PSCustomObject]@{
            Success = $false
            Reason = "Read-only result verifier modified the workspace."
            Verdict = ""
            Requirements = ""
            BlockReason = ""
            Summary = ""
        }
    }

    if ($r.ExitCode -ne 0) {
        return [PSCustomObject]@{
            Success = $false
            Reason = "Result verifier backend failed."
            Verdict = ""
            Requirements = ""
            BlockReason = ""
            Summary = ""
        }
    }

    $final = Get-FinalAgentText $r.Output
    $final | Out-File -LiteralPath (Join-Path $EvidenceDir "result-verifier-final.txt") -Encoding UTF8

    $obj = ConvertFrom-AgentJson $final
    if (-not $obj) {
        return [PSCustomObject]@{
            Success = $false
            Reason = "Result verifier returned invalid JSON."
            Verdict = ""
            Requirements = ""
            BlockReason = ""
            Summary = ""
        }
    }

    $verdict = ([string]$obj.verdict).Trim().ToUpperInvariant()
    $requirements = ([string]$obj.requirements_satisfied).Trim().ToUpperInvariant()
    $blockReason = ([string]$obj.block_reason).Trim().ToUpperInvariant()

    if ($verdict -notin @("CHANGED","NO_CHANGE","BLOCKED")) {
        return [PSCustomObject]@{
            Success = $false
            Reason = "Result verifier returned invalid verdict."
            Verdict = ""
            Requirements = ""
            BlockReason = ""
            Summary = [string]$obj.summary
        }
    }

    if ($requirements -notin @("YES","NO")) {
        return [PSCustomObject]@{
            Success = $false
            Reason = "Result verifier returned invalid requirements status."
            Verdict = ""
            Requirements = ""
            BlockReason = ""
            Summary = [string]$obj.summary
        }
    }

    if ($verdict -eq "BLOCKED" -and
        $blockReason -notin @(
            "OWNER_INPUT_REQUIRED",
            "EVIDENCE_STALE",
            "TECHNICAL_BLOCKER"
        )) {
        $blockReason = "TECHNICAL_BLOCKER"
    }

    if ($verdict -ne "BLOCKED") {
        $blockReason = "NONE"
    }

    return [PSCustomObject]@{
        Success = $true
        Reason = ""
        Verdict = $verdict
        Requirements = $requirements
        BlockReason = $blockReason
        Summary = [string]$obj.summary
    }
}

function Invoke-ImplementationAgent(
    [string]$SelectedAgent,
    [string]$AgyExe,
    [string]$CodexExe,
    [string]$Prompt,
    [string]$Workspace,
    [string]$EvidenceDir
) {
    $candidates = @(Get-EligibleSemanticBackends -Providers $ProviderSnapshot -Capability "CODE_GENERATION" -AgyExe $AgyExe -CodexExe $CodexExe)
    if ($candidates.Count -eq 0) {
        return [PSCustomObject]@{ ExitCode=127; Output="No AVAILABLE semantic provider for CODE_GENERATION."; Backend="none"; Provider=""; ModelId="" }
    }

    # Explicit Agent is only a preference. Capability/availability is authoritative.
    $preferred = $SelectedAgent.ToLowerInvariant()
    $ordered = @($candidates | Sort-Object -Property @{Expression={ if (($preferred -eq "agy" -and $_.Name -like "AGY/*") -or ($preferred -eq "codex" -and $_.Name -eq "Codex")) { 0 } else { 1 } }}, @{Expression={[int]$_.Provider.usable_capacity};Descending=$true}, @{Expression={$_.Name};Descending=$false})
    $attempts = @()

    for ($attempt = 0; $attempt -lt $ordered.Count; $attempt++) {
        $candidate = $ordered[$attempt]
        Write-Section ("Run provider: " + $candidate.Name + " / attempt " + ($attempt + 1))
        Write-Host ("SELECTED PROVIDER: {0} status={1} usable={2} model={3}" -f $candidate.Name,$candidate.Provider.status,$candidate.Provider.usable_capacity,$candidate.ModelId)
        $logStem = if ($candidate.Name -eq "Codex") { "codex" } else { "agy-" + $candidate.Provider.name.Replace('/','-') }
        $log = Join-Path $EvidenceDir ($logStem + "-attempt-" + ($attempt + 1) + ".log")

        if ($candidate.Name -like "AGY/*") {
            $r = Invoke-AgyAgent $candidate.Exe $Prompt $Workspace $log $candidate.ModelId
        } else {
            $r = Invoke-CodexAgent $candidate.Exe $Prompt $Workspace $log
        }
        $attempts += $r

        if ($r.ExitCode -eq 0) {
            $r | Add-Member -NotePropertyName Provider -NotePropertyValue $candidate.Name -Force
            $r | Add-Member -NotePropertyName ModelId -NotePropertyValue $candidate.ModelId -Force
            return $r
        }

        Write-Host ("PROVIDER FAILED: {0}; trying next AVAILABLE provider for CODE_GENERATION." -f $candidate.Name)
    }

    return [PSCustomObject]@{
        ExitCode = 1
        Output = "All AVAILABLE semantic providers for CODE_GENERATION failed."
        Backend = "none"
        Provider = ""
        ModelId = ""
    }
}

function Invoke-UnityValidator(
    [string]$Workspace,
    [string]$EvidenceDir
) {
    $log = Join-Path $EvidenceDir "unity-validator.log"

    if ($SkipUnityValidation) {
        return [PSCustomObject]@{
            Verdict = "SKIPPED"
            CanProceed = $true
            ExitCode = $null
            Log = $log
            Note = "Unity validation explicitly skipped."
        }
    }

    if (-not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
        return [PSCustomObject]@{
            Verdict = "UNAVAILABLE"
            CanProceed = $false
            ExitCode = $null
            Log = $log
            Note = "Unity executable not found."
        }
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $UnityExe
    $psi.WorkingDirectory = $Workspace
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.Arguments = (
        "-batchmode -nographics -quit -projectPath " +
        (Quote-CliArg $Workspace) +
        " -logFile " +
        (Quote-CliArg $log)
    )

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    if (-not $proc.Start()) {
        return [PSCustomObject]@{
            Verdict = "FAILED_TO_START"
            CanProceed = $false
            ExitCode = $null
            Log = $log
            Note = "Unity failed to start."
        }
    }

    $finished = $proc.WaitForExit(1800000) # 30 minutes

    if (-not $finished) {
        return [PSCustomObject]@{
            Verdict = "TIMEOUT"
            CanProceed = $false
            ExitCode = $null
            Log = $log
            Note = ("Unity validator exceeded 30 minutes; PID=" + $proc.Id)
        }
    }

    $content = ""
    if (Test-Path -LiteralPath $log -PathType Leaf) {
        $content = Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue
    }

    $compiler = @(
        $content -split "`r?`n" |
            Where-Object { $_ -match "error CS[0-9]+" }
    )

    $knownMarkers = @(
        "U2DToURPPixelPerfectConverter.cs",
        "com.unity.2d.pixel-perfect",
        "UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera"
    )

    $otherCompiler = @(
        $compiler | Where-Object {
            $line = $_
            $matchedKnown = $false

            foreach ($m in $knownMarkers) {
                if ($line -like ("*" + $m + "*")) {
                    $matchedKnown = $true
                    break
                }
            }

            -not $matchedKnown
        }
    )

    $knownCompiler = @(
        $compiler | Where-Object {
            $line = $_
            $matchedKnown = $false

            foreach ($m in $knownMarkers) {
                if ($line -like ("*" + $m + "*")) {
                    $matchedKnown = $true
                    break
                }
            }

            $matchedKnown
        }
    )

    if ($otherCompiler.Count -gt 0) {
        return [PSCustomObject]@{
            Verdict = "FAIL"
            CanProceed = $false
            ExitCode = $proc.ExitCode
            Log = $log
            Note = "Task workspace has compiler errors beyond the known Pixel Perfect/URP blocker."
        }
    }

    if ($knownCompiler.Count -gt 0) {
        return [PSCustomObject]@{
            Verdict = "BLOCKED_KNOWN_EDITOR_BUG"
            CanProceed = $true
            ExitCode = $proc.ExitCode
            Log = $log
            Note = "Only the known Unity 6000.0.36f1 Pixel Perfect/URP converter blocker was detected; task-specific compile acceptance remains unproven."
        }
    }

    if ($proc.ExitCode -eq 0) {
        return [PSCustomObject]@{
            Verdict = "PASS"
            CanProceed = $true
            ExitCode = 0
            Log = $log
            Note = "Unity batch import/compile completed without detected compiler errors."
        }
    }

    return [PSCustomObject]@{
        Verdict = "FAIL"
        CanProceed = $false
        ExitCode = $proc.ExitCode
        Log = $log
        Note = "Unity exited non-zero without matching the known blocker."
    }
}

function Get-GitEvidence(
    [string]$Repo,
    [string]$EvidenceDir,
    [string]$Label
) {
    $git = Find-Exe "git" @()

    if (-not $git) {
        return [PSCustomObject]@{
            Available = $false
            Head = ""
            Status = ""
        }
    }

    $headLog = Join-Path $EvidenceDir ("git-head-" + $Label + ".txt")
    $statusLog = Join-Path $EvidenceDir ("git-status-" + $Label + ".txt")

    $head = Invoke-NativeCapture $git @("-C",$Repo,"rev-parse","HEAD") $Repo $headLog
    $status = Invoke-NativeCapture $git @("-C",$Repo,"status","--porcelain=v1","--untracked-files=all") $Repo $statusLog

    return [PSCustomObject]@{
        Available = $true
        Head = $head.Output.Trim()
        Status = $status.Output
    }
}

function Test-CanonicalPathsUnchanged(
    $CanonicalSnapshot,
    $Changes
) {
    foreach ($c in @($Changes)) {
        $rel = [string]$c.RelativePath
        $path = Join-Path $Canonical $rel

        $snapshotHas = $CanonicalSnapshot.ContainsKey($rel)
        $currentHas = Test-Path -LiteralPath $path -PathType Leaf

        if ($snapshotHas -ne $currentHas) {
            return [PSCustomObject]@{
                Pass = $false
                Path = $rel
                Reason = "Canonical file existence changed since snapshot."
            }
        }

        if ($snapshotHas -and $currentHas) {
            $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
            if ($hash -ne $CanonicalSnapshot[$rel].SHA256) {
                return [PSCustomObject]@{
                    Pass = $false
                    Path = $rel
                    Reason = "Canonical file content changed since snapshot."
                }
            }
        }
    }

    return [PSCustomObject]@{
        Pass = $true
        Path = ""
        Reason = ""
    }
}

function Apply-ChangesSafely(
    [string]$RunId,
    [string]$Workspace,
    [string]$EvidenceDir,
    $CanonicalSnapshot,
    $Changes
) {
    $safety = Test-CanonicalPathsUnchanged $CanonicalSnapshot $Changes

    if (-not $safety.Pass) {
        return [PSCustomObject]@{
            Applied = $false
            Reason = ("Canonical concurrency guard failed: " + $safety.Path + " - " + $safety.Reason)
            Checkpoint = ""
        }
    }

    $backupRoot = Join-Path $EvidenceDir "canonical-backup"
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null

    $checkpointItems = @()

    foreach ($c in @($Changes)) {
        $rel = [string]$c.RelativePath
        $canonicalPath = Join-Path $Canonical $rel
        $workspacePath = Join-Path $Workspace $rel
        $backupPath = Join-Path $backupRoot $rel

        $existedBefore = Test-Path -LiteralPath $canonicalPath -PathType Leaf

        if ($existedBefore) {
            $parent = Split-Path -Parent $backupPath
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
            Copy-Item -LiteralPath $canonicalPath -Destination $backupPath -Force
        }

        $checkpointItems += [PSCustomObject]@{
            relative_path = $rel
            change_type = [string]$c.ChangeType
            existed_before = $existedBefore
            before_sha256 = if ($existedBefore) {
                (Get-FileHash -LiteralPath $canonicalPath -Algorithm SHA256).Hash
            } else { "" }
            applied_sha256 = if ([string]$c.ChangeType -ne "DELETED") {
                (Get-FileHash -LiteralPath $workspacePath -Algorithm SHA256).Hash
            } else { "" }
            backup_relative_path = if ($existedBefore) { $rel } else { "" }
        }
    }

    if ($NoApply) {
        return [PSCustomObject]@{
            Applied = $false
            Reason = "NoApply requested."
            Checkpoint = ""
        }
    }

    foreach ($c in @($Changes)) {
        $rel = [string]$c.RelativePath
        $canonicalPath = Join-Path $Canonical $rel
        $workspacePath = Join-Path $Workspace $rel

        if ([string]$c.ChangeType -eq "DELETED") {
            if (-not $AllowDelete) {
                return [PSCustomObject]@{
                    Applied = $false
                    Reason = "Deletion detected without -AllowDelete."
                    Checkpoint = ""
                }
            }

            if (Test-Path -LiteralPath $canonicalPath -PathType Leaf) {
                Remove-Item -LiteralPath $canonicalPath -Force
            }
        }
        else {
            $parent = Split-Path -Parent $canonicalPath
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
            Copy-Item -LiteralPath $workspacePath -Destination $canonicalPath -Force
        }
    }

    # Verify the canonical state equals the approved workspace delta.
    foreach ($c in @($Changes)) {
        $rel = [string]$c.RelativePath
        $canonicalPath = Join-Path $Canonical $rel
        $workspacePath = Join-Path $Workspace $rel

        if ([string]$c.ChangeType -eq "DELETED") {
            if (Test-Path -LiteralPath $canonicalPath) {
                throw "Post-apply verification failed; deleted file still exists: $rel"
            }
        }
        else {
            if (-not (Test-Path -LiteralPath $canonicalPath -PathType Leaf)) {
                throw "Post-apply verification failed; canonical file missing: $rel"
            }

            $a = (Get-FileHash -LiteralPath $canonicalPath -Algorithm SHA256).Hash
            $b = (Get-FileHash -LiteralPath $workspacePath -Algorithm SHA256).Hash

            if ($a -ne $b) {
                throw "Post-apply hash mismatch: $rel"
            }
        }
    }

    $checkpoint = [ordered]@{
        schema = 1
        run_id = $RunId
        canonical = $Canonical
        created_at = (Get-Date).ToString("o")
        backup_root = $backupRoot
        items = $checkpointItems
    }

    $checkpointPath = Join-Path $EvidenceDir "CHECKPOINT.json"
    $checkpoint | ConvertTo-Json -Depth 10 |
        Out-File -LiteralPath $checkpointPath -Encoding UTF8

    return [PSCustomObject]@{
        Applied = $true
        Reason = ""
        Checkpoint = $checkpointPath
    }
}

function Invoke-Recovery([string]$RunId) {
    $evidence = Join-Path (Join-Path $AutomationRoot "orchestrator-runs") $RunId
    $checkpointPath = Join-Path $evidence "CHECKPOINT.json"

    if (-not (Test-Path -LiteralPath $checkpointPath -PathType Leaf)) {
        throw "Checkpoint not found for run: $RunId"
    }

    $cp = Get-Content -LiteralPath $checkpointPath -Raw |
        ConvertFrom-Json -ErrorAction Stop

    $backupRoot = [string]$cp.backup_root

    foreach ($item in @($cp.items)) {
        $rel = [string]$item.relative_path
        $canonicalPath = Join-Path $Canonical $rel
        $currentExists = Test-Path -LiteralPath $canonicalPath -PathType Leaf

        # Never overwrite newer user changes.
        if ([string]$item.change_type -eq "DELETED") {
            if ($currentExists) {
                throw "Recovery refused: newer file exists at $rel"
            }
        }
        else {
            if (-not $currentExists) {
                throw "Recovery refused: applied file is missing at $rel"
            }

            $currentHash = (Get-FileHash -LiteralPath $canonicalPath -Algorithm SHA256).Hash

            if ($currentHash -ne [string]$item.applied_sha256) {
                throw "Recovery refused: canonical changed after orchestrator apply at $rel"
            }
        }
    }

    foreach ($item in @($cp.items)) {
        $rel = [string]$item.relative_path
        $canonicalPath = Join-Path $Canonical $rel

        if ([bool]$item.existed_before) {
            $backupPath = Join-Path $backupRoot $rel
            if (-not (Test-Path -LiteralPath $backupPath -PathType Leaf)) {
                throw "Recovery backup missing: $rel"
            }

            $parent = Split-Path -Parent $canonicalPath
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
            Copy-Item -LiteralPath $backupPath -Destination $canonicalPath -Force
        }
        else {
            if (Test-Path -LiteralPath $canonicalPath -PathType Leaf) {
                Remove-Item -LiteralPath $canonicalPath -Force
            }
        }
    }

    "Recovery: PASS" | Out-File -LiteralPath (Join-Path $evidence "RECOVERY.txt") -Encoding UTF8

    Write-Section "RECOVERY"
    Write-Host "Run      :" $RunId
    Write-Host "Recovery : PASS"
}

function Invoke-OneAutonomousTask([string]$Objective) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
    $runId = "AUTO-" + $stamp
    $workspace = Join-Path (Join-Path (Join-Path $AutomationRoot "workspaces") $runId) "project"
    $evidence = Join-Path (Join-Path $AutomationRoot "orchestrator-runs") $runId

    New-Item -ItemType Directory -Path $evidence -Force | Out-Null

    Write-Section "SOUL HUNTER CORE ORCHESTRATOR"
    Write-Host "Run ID       :" $runId
    Write-Host "Canonical    :" $Canonical
    Write-Host "Workspace    :" $workspace
    Write-Host "Objective    :" $Objective

    if (-not (Test-Path -LiteralPath $Canonical -PathType Container)) {
        throw "Canonical project not found: $Canonical"
    }

    $agy = Find-Exe "agy" @(
        "$env:LOCALAPPDATA\agy\bin\agy.exe"
    )

    $codex = Find-Exe "codex" @(
        "$env:APPDATA\npm\codex.cmd",
        "$env:LOCALAPPDATA\Programs\OpenAI\Codex\bin\codex.exe",
        "$env:LOCALAPPDATA\Programs\codex\codex.exe"
    )

    $gitBefore = Get-GitEvidence $Canonical $evidence "before"
    $canonicalSnapshot = New-Manifest $Canonical

    Write-Section "Create isolated workspace"
    Copy-CanonicalToWorkspace $Canonical $workspace

    Write-Section "AGY/ASTRA project deep scan"
    $selection = Get-NextTaskFromProjectScan $agy $codex $workspace $evidence $Objective $ProviderSnapshot

    if ($selection.ScannerFailed) {
        $summary = [ordered]@{
            run_id = $runId
            generated_at = (Get-Date).ToString("o")
            status = "SCANNER_FAILED"
            objective = $Objective
            resolved_objective = ""
            task_id = ""
            applied_to_canonical = $false
            evidence_dir = $evidence
            reason = $selection.Reason
            selection = $selection
        }

        $summaryPath = Join-Path $evidence "SUMMARY.json"
        $summary | ConvertTo-Json -Depth 12 |
            Out-File -LiteralPath $summaryPath -Encoding UTF8

        Write-Section "FINAL"
        Write-Host "STATUS        : SCANNER_FAILED"
        Write-Host "REASON        :" $selection.Reason
        Write-Host "APPLIED       : False"
        Write-Host "EVIDENCE      :" $evidence

        return $summary
    }

    if ($selection.NoSafeTask) {
        $summary = [ordered]@{
            run_id = $runId
            generated_at = (Get-Date).ToString("o")
            status = "NO_SAFE_TASK"
            objective = $Objective
            resolved_objective = ""
            task_id = ""
            applied_to_canonical = $false
            evidence_dir = $evidence
            reason = $selection.Reason
            selection = $selection
        }

        $summaryPath = Join-Path $evidence "SUMMARY.json"
        $summary | ConvertTo-Json -Depth 12 |
            Out-File -LiteralPath $summaryPath -Encoding UTF8

        Write-Section "FINAL"
        Write-Host "STATUS        : NO_SAFE_TASK"
        Write-Host "REASON        :" $selection.Reason
        Write-Host "APPLIED       : False"
        Write-Host "EVIDENCE      :" $evidence

        return $summary
    }

    Write-Host "Task ID        :" $selection.Id
    Write-Host "Concrete task  :" $selection.Objective
    Write-Host "Required agent :" $selection.RequiredAgent
    Write-Host "Validation     :" $selection.ValidationLevel
    Write-Host "Risk           :" $selection.Risk

    $selectedAgent = if ($Agent -ne "auto") {
        $Agent
    }
    else {
        $selection.RequiredAgent.ToLowerInvariant()
    }

    if ($selectedAgent -eq "codex" -and -not $codex -and $agy) {
        $selectedAgent = "agy"
    }

    if ($selectedAgent -eq "agy" -and -not $agy -and $codex) {
        $selectedAgent = "codex"
    }

    $taskBaseline = New-Manifest $workspace
    $prompt = Build-ImplementationPrompt $selection

    if (-not (Test-NoUnresolvedPromptVariables $prompt)) {
        $summary = [ordered]@{
            run_id = $runId
            generated_at = (Get-Date).ToString("o")
            status = "SCANNER_FAILED"
            objective = $Objective
            resolved_objective = $selection.Objective
            task_id = $selection.Id
            applied_to_canonical = $false
            evidence_dir = $evidence
            reason = "Internal controller error: template tokens were not resolved."
            selection = $selection
        }

        $summaryPath = Join-Path $evidence "SUMMARY.json"
        $summary | ConvertTo-Json -Depth 12 |
            Out-File -LiteralPath $summaryPath -Encoding UTF8

        Write-Section "FINAL"
        Write-Host "STATUS        : SCANNER_FAILED"
        Write-Host "REASON        : Internal controller error: template tokens were not resolved."
        Write-Host "APPLIED       : False"
        Write-Host "EVIDENCE      :" $evidence

        return $summary
    }

    $agentResult = Invoke-ImplementationAgent `
        $selectedAgent $agy $codex $prompt $workspace $evidence

    $finalText = Get-FinalAgentText $agentResult.Output
    $finalText | Out-File -LiteralPath (Join-Path $evidence "agent-final.txt") -Encoding UTF8

    $verdict = (Get-AgentField $finalText "VERDICT").ToUpperInvariant()
    $requirements = (Get-AgentField $finalText "REQUIREMENTS_SATISFIED").ToUpperInvariant()
    $blockReason = (Get-AgentField $finalText "BLOCK_REASON").ToUpperInvariant()

    $taskAfter = New-Manifest $workspace
    $changes = @(Compare-Manifests $taskBaseline $taskAfter)

    $structuredResultOk = (
        $verdict -in @("CHANGED","NO_CHANGE","BLOCKED") -and
        $requirements -in @("YES","NO") -and
        ($verdict -ne "BLOCKED" -or
            $blockReason -in @(
                "OWNER_INPUT_REQUIRED",
                "EVIDENCE_STALE",
                "TECHNICAL_BLOCKER"
            ))
    )

    $verification = $null

    $needsVerification = (-not $structuredResultOk) -or
                         ($verdict -eq "CHANGED" -and $changes.Count -eq 0) -or
                         ($verdict -eq "NO_CHANGE" -and $changes.Count -gt 0)

    if ($agentResult.ExitCode -eq 0 -and $needsVerification) {
        Write-Section "Recover malformed agent result / read-only verification"

        $verification = Invoke-ReadOnlyResultVerifier `
            $agy $codex $selection $workspace $evidence $changes

        if ($verification.Success) {
            $verdict = $verification.Verdict
            $requirements = $verification.Requirements
            $blockReason = $verification.BlockReason

            Write-Host "Result verifier : PASS"
            Write-Host "Verified verdict:" $verdict
            Write-Host "Requirements    :" $requirements
        }
        else {
            Write-Host "Result verifier : FAILED"
            Write-Host "Reason          :" $verification.Reason
        }
    }

    Write-Host "Agent backend  :" $agentResult.Backend
    Write-Host "Agent exit     :" $agentResult.ExitCode
    Write-Host "Agent verdict  :" $verdict
    Write-Host "Requirements   :" $requirements

    Write-Section "Exact workspace delta"
    Write-Host "Changed files  :" $changes.Count

    $forbidden = @(
        $changes | Where-Object {
            Test-IsForbiddenChange ([string]$_.RelativePath)
        }
    )

    $deleted = @(
        $changes | Where-Object { $_.ChangeType -eq "DELETED" }
    )

    Write-Host "Forbidden      :" $forbidden.Count
    Write-Host "Deleted        :" $deleted.Count

    $status = ""
    $applied = $false
    $checkpoint = ""
    $applyReason = ""
    $unity = [PSCustomObject]@{
        Verdict = "NOT_REQUIRED"
        CanProceed = $true
        ExitCode = $null
        Log = ""
        Note = "No Unity-relevant files changed."
    }

    if ($agentResult.ExitCode -ne 0) {
        $status = "AGENT_FAILED"
    }
    elseif ($verdict -eq "BLOCKED") {
        if ($blockReason -eq "OWNER_INPUT_REQUIRED") {
            $status = "OWNER_BLOCKED"
            Add-TaskLedgerEntry `
                $selection.Id $selection.Objective $status $runId $selection.EvidenceFiles
        }
        elseif ($blockReason -eq "EVIDENCE_STALE") {
            $status = "CANDIDATE_REJECTED"
            Add-TaskLedgerEntry `
                $selection.Id $selection.Objective "REJECTED" $runId $selection.EvidenceFiles
            $applyReason = "Evidence stale."
        }
        elseif ($blockReason -eq "TECHNICAL_BLOCKER") {
            if ($changes.Count -eq 0) {
                $status = "TASK_BLOCKED"
                Add-TaskLedgerEntry `
                    $selection.Id $selection.Objective "TASK_BLOCKED" $runId $selection.EvidenceFiles
            }
            else {
                $status = "CANDIDATE_REJECTED"
                Add-TaskLedgerEntry `
                    $selection.Id $selection.Objective "REJECTED" $runId $selection.EvidenceFiles
                $applyReason = "Technical blocker but unsafe exact delta."
            }
        }
        else {
            $status = "BLOCKED"
        }
    }
    elseif ($requirements -ne "YES") {
        $status = "CANDIDATE_REJECTED"
        Add-TaskLedgerEntry `
            $selection.Id $selection.Objective "REJECTED" $runId $selection.EvidenceFiles
        $applyReason = "Candidate rejected: Requirements not satisfied."
    }
    elseif ($verdict -eq "NO_CHANGE") {
        if ($changes.Count -eq 0) {
            $status = "NO_CHANGE"
            Add-TaskLedgerEntry `
                $selection.Id $selection.Objective $status $runId $selection.EvidenceFiles
        }
        else {
            $status = "AGENT_FAILED"
            $applyReason = "Agent claimed NO_CHANGE but workspace changed."
        }
    }
    elseif ($verdict -eq "CHANGED") {
        if ($changes.Count -eq 0) {
            $status = "AGENT_FAILED"
            $applyReason = "Agent claimed CHANGED but exact delta is empty."
        }
        elseif ($forbidden.Count -gt 0) {
            $status = "REVIEW_REQUIRED"
            $applyReason = "Forbidden file changes detected."
        }
        elseif ($deleted.Count -gt 0 -and -not $AllowDelete) {
            $status = "REVIEW_REQUIRED"
            $applyReason = "Deletion detected without -AllowDelete."
        }
        else {
            if (Test-NeedsUnityValidation $changes) {
                Write-Section "Unity CLI validator"
                $unity = Invoke-UnityValidator $workspace $evidence
                Write-Host "Unity verdict :" $unity.Verdict
                Write-Host "Can proceed   :" $unity.CanProceed
                Write-Host "Note          :" $unity.Note
            }

            if (-not $unity.CanProceed) {
                $status = "VALIDATION_FAILED"
                $applyReason = $unity.Note
            }
            else {
                Write-Section "Canonical safety + apply"
                $apply = Apply-ChangesSafely `
                    $runId $workspace $evidence $canonicalSnapshot $changes

                $applied = [bool]$apply.Applied
                $checkpoint = [string]$apply.Checkpoint
                $applyReason = [string]$apply.Reason

                if ($applied) {
                    $status = "APPLIED"
                    Add-TaskLedgerEntry `
                        $selection.Id $selection.Objective $status $runId $selection.EvidenceFiles
                }
                elseif ($NoApply) {
                    $status = "NO_APPLY"
                }
                else {
                    $status = "REVIEW_REQUIRED"
                }
            }
        }
    }
    else {
        $status = "AGENT_FAILED"
        $applyReason = "Agent returned an unknown verdict."
    }

    $gitAfter = Get-GitEvidence $Canonical $evidence "after"

    $summary = [ordered]@{
        run_id = $runId
        generated_at = (Get-Date).ToString("o")
        objective = $Objective
        resolved_objective = $selection.Objective
        task_id = $selection.Id
        status = $status
        agent = $agentResult.Backend
        verdict = $verdict
        requirements_satisfied = $requirements
        block_reason = $blockReason
        result_contract_recovered = $(
            $agentResult.ExitCode -eq 0 -and
            -not $structuredResultOk -and
            $verification -and
            $verification.Success
        )
        changed_files = @($changes)
        forbidden_changes = @($forbidden)
        unity = $unity
        applied_to_canonical = $applied
        checkpoint = $checkpoint
        apply_note = $applyReason
        evidence_dir = $evidence
        selection = $selection
        git_before = $gitBefore
        git_after = $gitAfter
    }

    $summaryPath = Join-Path $evidence "SUMMARY.json"
    $summary | ConvertTo-Json -Depth 14 |
        Out-File -LiteralPath $summaryPath -Encoding UTF8

    Write-Section "FINAL"
    Write-Host "STATUS        :" $status
    Write-Host "TASK ID       :" $selection.Id
    Write-Host "RESOLVED TASK :" $selection.Objective
    Write-Host "AGENT         :" $agentResult.Backend
    Write-Host "VERDICT       :" $verdict
    Write-Host "REQUIREMENTS  :" $requirements
    Write-Host "CHANGED FILES :" $changes.Count
    Write-Host "UNITY         :" $unity.Verdict
    Write-Host "APPLIED       :" $applied
    if ($checkpoint) { Write-Host "CHECKPOINT    :" $checkpoint }
    if ($applyReason) { Write-Host "NOTE          :" $applyReason }
    Write-Host "EVIDENCE      :" $evidence
    Write-Host "SUMMARY       :" $summaryPath

    return $summary
}

function Invoke-Autopilot([string]$RootGoal) {
    $sessionId = "AUTOPILOT-" + (Get-Date -Format "yyyyMMdd-HHmmss-fff")
    $sessionDir = Join-Path (Join-Path $AutomationRoot "autopilot-sessions") $sessionId

    New-Item -ItemType Directory -Path $sessionDir -Force | Out-Null

    $cycles = @()
    $seen = @{}
    $stopReason = ""
    $cycle = 0

    $agy = Find-Exe "agy" @(
        "$env:LOCALAPPDATA\agy\bin\agy.exe"
    )

    $codex = Find-Exe "codex" @(
        "$env:APPDATA\npm\codex.cmd",
        "$env:LOCALAPPDATA\Programs\OpenAI\Codex\bin\codex.exe",
        "$env:LOCALAPPDATA\Programs\codex\codex.exe"
    )

    Write-Section "SOUL HUNTER AUTONOMOUS CORE LOOP"
    Write-Host "Session    :" $sessionId
    Write-Host "Root Goal  :" $RootGoal
    Write-Host "Task limit :" $(if ($MaxAutoTasks -gt 0) { $MaxAutoTasks } else { "UNLIMITED" })

    $currentObjective = $null
    $currentObjectiveId = $null

    while ($true) {
        $cycle++

        if ($MaxAutoTasks -gt 0 -and $cycle -gt $MaxAutoTasks) {
            $stopReason = "MAX_AUTO_TASKS_REACHED"
            break
        }

        Write-Section ("AUTOPILOT CYCLE " + $cycle)

        if ($null -eq $currentObjective) {
            Write-Section "OBJECTIVE DISCOVERY"
            $objWorkspace = Join-Path (Join-Path (Join-Path $AutomationRoot "workspaces") $sessionId) "OBJ-DISCOVERY-$cycle"
            Copy-CanonicalToWorkspace $Canonical $objWorkspace
            
            $objEvidence = Join-Path (Join-Path $AutomationRoot "orchestrator-runs") "OBJ-DISCOVERY-$sessionId-$cycle"
            New-Item -ItemType Directory -Path $objEvidence -Force | Out-Null

            $discovery = Get-NextObjectiveFromProjectScan $agy $codex $objWorkspace $objEvidence $RootGoal $ProviderSnapshot
            
            if ($discovery.ScannerFailed) {
                $stopReason = "SCANNER_FAILED"
                Write-Host "STATUS: SCANNER_FAILED"
                Write-Host "REASON: $($discovery.Reason)"
                break
            }

            if ($discovery.OverallState -eq "OWNER_INPUT_REQUIRED") {
                $stopReason = "OWNER_INPUT_REQUIRED"
                Write-Host "STATUS: OWNER_INPUT_REQUIRED"
                Write-Host "QUESTION: $($discovery.OwnerQuestion)"
                break
            }

            if ($discovery.OverallState -eq "PROJECT_COMPLETE") {
                $stopReason = "PROJECT_COMPLETE"
                Write-Host "STATUS: PROJECT_COMPLETE"
                break
            }

            $terminalIds = @{}
            $ledger = Read-ObjectiveLedger
            foreach ($x in $ledger) {
                if ($x.status -in @("EXHAUSTED", "OWNER_BLOCKED", "REJECTED", "COMPLETED")) {
                    $terminalIds[([string]$x.objective_id).ToUpperInvariant()] = $true
                }
            }

            $selected = $null
            $ordered = @(
                @($discovery.Objectives) | Sort-Object @{Expression={try{[int]$_.priority}catch{9999}}; Ascending=$true}
            )

            foreach ($o in $ordered) {
                $id = ([string]$o.id).Trim().ToUpperInvariant()
                if (-not $terminalIds.ContainsKey($id)) {
                    $evidenceOk = $true
                    $hasProjectEvidence = $false
                    foreach ($rel in $o.evidence_files) {
                        if (-not (Test-EvidencePath $objWorkspace $rel)) {
                            $evidenceOk = $false
                            break
                        }
                        $n = $rel.Replace("/","\")
                        if ($n -match '^(Assets|ProjectSettings|Packages)\\') {
                            $hasProjectEvidence = $true
                        }
                    }
                    
                    if ($evidenceOk -and $hasProjectEvidence) {
                        $selected = $o
                        break
                    }
                }
            }

            if ($null -eq $selected) {
                $stopReason = "NO_EVIDENCE_BACKED_OBJECTIVES"
                Write-Host "STATUS: NO_EVIDENCE_BACKED_OBJECTIVES"
                break
            }

            $currentObjective = $selected.objective
            $currentObjectiveId = $selected.id
            Add-ObjectiveLedgerEntry $currentObjectiveId $currentObjective "ACTIVE" "Discovered by project scan."
            Write-Host "NEW OBJECTIVE: $currentObjectiveId - $currentObjective"
        }

        try {
            $r = Invoke-OneAutonomousTask $currentObjective
        }
        catch {
            $stopReason = "ORCHESTRATOR_EXCEPTION"
            $cycles += [PSCustomObject]@{
                cycle = $cycle
                status = "ORCHESTRATOR_EXCEPTION"
                error = $_.Exception.Message
            }
            break
        }

        $taskId = [string]$r.task_id
        $status = [string]$r.status

        $cycles += [PSCustomObject]@{
            cycle = $cycle
            run_id = [string]$r.run_id
            task_id = $taskId
            task = [string]$r.resolved_objective
            status = $status
            applied = [bool]$r.applied_to_canonical
            evidence_dir = [string]$r.evidence_dir
        }

        if ($taskId) {
            if ($seen.ContainsKey($taskId)) {
                Write-Host "Task level REPEATED_TASK_GUARD -> Exhausting objective $currentObjectiveId"
                Add-ObjectiveLedgerEntry $currentObjectiveId $currentObjective "EXHAUSTED" "Repeated task loop detected."
                $currentObjective = $null
                $currentObjectiveId = $null
                continue
            }
            $seen[$taskId] = $true
        }

        if ($status -eq "NO_SAFE_TASK") {
            Write-Host "Task level NO_SAFE_TASK -> Exhausting objective $currentObjectiveId"
            Add-ObjectiveLedgerEntry $currentObjectiveId $currentObjective "EXHAUSTED" "No safe tasks remain."
            $currentObjective = $null
            $currentObjectiveId = $null
            continue
        }

        if ($status -in @("APPLIED","NO_CHANGE","OWNER_BLOCKED","CANDIDATE_REJECTED","TASK_BLOCKED")) {
            Write-Host "Next action : RESCAN UPDATED CANONICAL"
            continue
        }

        $stopReason = $status
        break
    }

    $session = [ordered]@{
        schema = 1
        session_id = $sessionId
        generated_at = (Get-Date).ToString("o")
        root_goal = $RootGoal
        stop_reason = $stopReason
        cycles = $cycles
    }

    $sessionPath = Join-Path $sessionDir "SESSION.json"
    $session | ConvertTo-Json -Depth 12 |
        Out-File -LiteralPath $sessionPath -Encoding UTF8

    Write-Section "AUTOPILOT FINAL"
    Write-Host "SESSION     :" $sessionId
    Write-Host "CYCLES      :" $cycles.Count
    Write-Host "STOP REASON :" $stopReason
    Write-Host "SESSION LOG :" $sessionPath
}

function Invoke-SelfTest {
    Write-Section "SOUL HUNTER CORE ORCHESTRATOR SELF-TEST"

    $agy = Find-Exe "agy" @(
        "$env:LOCALAPPDATA\agy\bin\agy.exe"
    )

    $codex = Find-Exe "codex" @(
        "$env:APPDATA\npm\codex.cmd",
        "$env:LOCALAPPDATA\Programs\OpenAI\Codex\bin\codex.exe",
        "$env:LOCALAPPDATA\Programs\codex\codex.exe"
    )

    $git = Find-Exe "git" @()

    $unityFound = $false
    try { $unityFound = Test-Path -LiteralPath $UnityExe -PathType Leaf -ErrorAction Stop } catch {}
    
    $canonicalFound = $false
    try { $canonicalFound = Test-Path -LiteralPath $Canonical -PathType Container -ErrorAction Stop } catch {}

    Write-Host "AGY          :" $(if ($agy) { "READY - " + $agy } else { "NOT FOUND" })
    Write-Host "Codex        :" $(if ($codex) { "FOUND - " + $codex } else { "NOT FOUND" })
    Write-Host "Project AI   :" $(if ($agy) { "READY - AGY/ASTRA project scan" } elseif ($codex) { "CODEX FALLBACK ONLY" } else { "NO BACKEND" })
    Write-Host "Unity CLI    :" $(if ($unityFound) { "READY - " + $UnityExe } else { "NOT FOUND" })
    Write-Host "Git evidence :" $(if ($git) { "READY - " + $git } else { "NOT FOUND" })
    Write-Host "Canonical    :" $(if ($canonicalFound) { "READY - " + $Canonical } else { "NOT FOUND" })
    Write-Host "Core loop    : READY - scan -> select -> execute -> validate -> apply -> rescan"
}

# ============================================================================
# Entry point
# ============================================================================

if ($SelfTest) {
    Invoke-SelfTest
    return
}

if (-not [string]::IsNullOrWhiteSpace($RecoverRun)) {
    Invoke-Recovery $RecoverRun
    return
}

if ([string]::IsNullOrWhiteSpace($Task)) {
    throw "Task/objective is empty."
}

Invoke-Autopilot $Task
