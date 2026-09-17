param(
    [switch]$Status,
    [switch]$SelfTest,
    [switch]$Resume,
    [switch]$PreflightOnly,
    [string]$Goal = "",
    [ValidateSet("Economy", "Balanced", "MaximumAutonomy")]
    [string]$Mode = "Balanced",
    [int]$MinimumFreeGB = 15,
    [int]$MaxNoProgress = 3,
    [int]$MaxCycles = 50,
    [ValidateRange(10, 40)]
    [int]$CapacityReservePercent = 25,
    [int]$ProviderRecheckMinutes = 30,
    [string[]]$SimulateExhausted = @(),
    [string]$AstraAdapterPath = "",
    [string]$AgyCapacityStatePath = ""
)

$ErrorActionPreference = "Stop"
Import-Module (Join-Path $PSScriptRoot 'LiveQuotaProbe.psm1') -Force

# =============================================================================
# SOUL HUNTER AUTOPILOT v2.2.42
# Capability-aware, capacity-aware, resumable controller.
#
# Core rules:
#   1) Exhaust capabilities, not providers.
#   2) Plan work according to remaining capacity.
#   3) Shrink the work unit before giving up.
#   4) Deterministic work belongs to deterministic tools.
#   5) A provider quota failure is not a project failure.
#
# IMPORTANT: ASTRA in the legacy orchestrator is a logical role backed by
# AGY/Codex. It is NOT counted as an independent provider here unless a real
# adapter is configured via -AstraAdapterPath or SOULHUNTER_ASTRA_ADAPTER.
# =============================================================================

$AutomationRoot = "D:\SoulHunter-Automation"
$Canonical = "D:\Unity Projects\Soul-Hunter"
$OrchestratorPath = Join-Path $AutomationRoot "SoulHunter-Orchestrator.ps1"
$ReleaseGatePath = Join-Path $AutomationRoot "SoulHunter-Release-Gate-v3.ps1"
$UnityExe = "D:\6000.0.36f1\Editor\Unity.exe"
$AgyExe = "C:\Users\Lenovo\AppData\Local\agy\bin\agy.EXE"
$CodexExe = "C:\Users\Lenovo\AppData\Local\Programs\OpenAI\Codex\bin\codex.exe"
$SessionsDir = Join-Path $AutomationRoot "autopilot-v2-sessions"
$StateRoot = Join-Path $AutomationRoot "autopilot-v2-state"
$KnowledgePath = Join-Path $StateRoot "autopilot-knowledge.json"
$ProviderStatePath = Join-Path $StateRoot "provider-state.json"
$DefaultAgyCapacityStatePath = Join-Path $StateRoot "agy-model-capacity.json"
$QuotaRoot = Join-Path $AutomationRoot "Quota"
$AgentRoot = Join-Path $AutomationRoot "Agents"
$QuotaReporterPath = Join-Path $QuotaRoot "Quota-Reporter.ps1"
$AgentRegisterPath = Join-Path $AgentRoot "Agent-Register.json"
$AgentStatePath = Join-Path $AgentRoot "Agent-State.json"

if ([string]::IsNullOrWhiteSpace($AstraAdapterPath)) {
    $AstraAdapterPath = $env:SOULHUNTER_ASTRA_ADAPTER
}
if ([string]::IsNullOrWhiteSpace($AgyCapacityStatePath)) {
    $AgyCapacityStatePath = if ($env:SOULHUNTER_AGY_CAPACITY_STATE) { $env:SOULHUNTER_AGY_CAPACITY_STATE } else { $DefaultAgyCapacityStatePath }
}

$KnownCapabilities = @(
    "PROJECT_SCAN",
    "CODE_REASONING",
    "CODE_GENERATION",
    "FILE_PATCHING",
    "UNITY_COMPILE",
    "UNITY_TEST",
    "PLAYER_BUILD",
    "GIT_SAFETY"
)

# =============================================================================
# SAFETY CONSTITUTION v1
# These are hard controller invariants, not suggestions. Provider failure,
# degraded mode, retries, or capacity pressure must never weaken them.
# =============================================================================
$SafetyConstitutionVersion = "1.0"
$SafetyRuleIds = @(
    "OWNER_RULES_ARE_HARD_CONSTRAINTS",
    "NO_BYPASS_BY_PROVIDER_OR_FALLBACK",
    "NO_GUESSING_WITH_INSUFFICIENT_EVIDENCE",
    "ISOLATED_VALIDATION_BEFORE_TRUST",
    "PRESERVE_UNRELATED_USER_WORK",
    "NO_DESTRUCTIVE_GIT",
    "NO_FORCE_KILL_UNITY",
    "NO_LIVE_WORKSPACE_DELETION",
    "NO_TEST_WEAKENING_OR_VALIDATION_BYPASS",
    "RELEASE_GATE_IS_FINAL_AUTHORITY",
    "VALIDATION_OVERRIDES_EXECUTOR_CLAIMS",
    "FAIL_CLOSED_ON_UNPROVEN_SAFETY",
    "PROVIDER_EXHAUSTION_NEVER_REDUCES_SAFETY",
    "FALLBACKS_OBEY_IDENTICAL_RULES",
    "UNVERIFIED_CHUNKS_ARE_NOT_COMPLETE"
)

function Get-SafetyConstitutionText {
    return @"
SAFETY CONSTITUTION v$SafetyConstitutionVersion -- HARD CONSTRAINTS:
1. Owner-approved requirements and safety rules are mandatory. They may not be relaxed by any planner, executor, verifier, retry, provider, fallback, or degraded mode.
2. If an action conflicts with a safety rule, do not perform it. Choose another safe route; if none exists, stop that action.
3. If evidence is insufficient or contradictory, DO NOT GUESS. Gather evidence, shrink the chunk, reroute, or return BLOCKED.
4. Treat executor output as a candidate only. A change is not trusted or complete until independent validation proves it.
5. Preserve unrelated existing user work and never overwrite unrelated changes.
6. Never use destructive Git history/worktree cleanup operations to make a task pass.
7. Never force-kill Unity. Never delete a workspace or file that may be in use by a live process.
8. Never weaken tests, suppress real exceptions, disable validation, alter requirements, or change gameplay semantics merely to obtain PASS.
9. Release Gate / independent validation is authoritative. If implementation claims conflict with validation evidence, validation wins.
10. Provider exhaustion may reduce speed or capability, but must never reduce safety. Every fallback obeys the same rules.
11. Successful chunks may be checkpointed; failed, blocked, or unverified chunks must never be recorded as complete.
12. If a required safety invariant cannot be proven, FAIL CLOSED rather than continue optimistically.
CORE PRINCIPLE: when progress conflicts with safety or correctness, choose safety and correctness.
"@
}

function Get-ControllerSafetyViolations {
    $violations = @()

    # Canonical and automation roots must never overlap.
    if ((Test-IsUnderPath -Child $Canonical -Parent $AutomationRoot) -or
        (Test-IsUnderPath -Child $AutomationRoot -Parent $Canonical)) {
        $violations += "Canonical and AutomationRoot overlap."
    }

    # Controller source itself must not contain destructive execution primitives.
    $source = ""
    try { $source = Get-Content $PSCommandPath -Raw -ErrorAction Stop } catch {
        $violations += "Unable to inspect controller source for safety invariants."
    }
    if ($source) {
        if ($source -match 'git\s+reset\s+--hard' -or $source -match 'git\s+clean\b' -or $source -match 'git\s+checkout\s+--') {
            $violations += "Destructive Git primitive detected in controller source."
        }
        $unityKillPatternA = '(?is)Stop-' + 'Process[^\r\n]*Unity'
        $unityKillPatternB = '(?is)task' + 'kill[^\r\n]*Unity'
        if ($source -match $unityKillPatternA -or $source -match $unityKillPatternB) {
            $violations += "Unity force-kill primitive detected in controller source."
        }
    }

    return @($violations)
}

function Assert-ControllerSafetyInvariants {
    $violations = @(Get-ControllerSafetyViolations)
    if ($violations.Count -gt 0) {
        Write-Host ""
        Write-Host "================ SAFETY CONSTITUTION VIOLATION ================"
        $violations | ForEach-Object { Write-Host "[FAIL-CLOSED] $_" }
        Write-Host "================================================================"
        throw ("Safety constitution failed: " + ($violations -join '; '))
    }
}

$StopWords = @(
    "about","after","again","against","also","been","before","being","between",
    "canonical","current","following","from","have","into","only","project","release",
    "resolve","soul","hunter","that","their","there","these","this","using","where",
    "which","with","without","your"
)

function Ensure-Directory {
    param([string]$Path)
    if (!(Test-Path $Path)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

Ensure-Directory $SessionsDir
Ensure-Directory $StateRoot
Ensure-Directory $QuotaRoot
Ensure-Directory $AgentRoot

function Write-FinalStatus {
    param([string]$Value)
    # Use the live controller provider matrix. The older implementation
    # referenced $matrix, while the real controller stores it in $Providers.
    $liveMatrix = if ($null -ne $Providers) { @($Providers) } elseif ($null -ne $script:Providers) { @($script:Providers) } elseif ($null -ne $matrix) { @($matrix) } else { @() }
    $effectiveCtoDecision = if ($null -ne $ctoDecision) { $ctoDecision } elseif ($null -ne $script:ctoDecision) { $script:ctoDecision } else { $null }
    $astraCtoRow = $liveMatrix | Where-Object name -eq "ASTRA/CTO" | Select-Object -First 1
    if ($astraCtoRow.status -eq "AVAILABLE" -and $astraCtoRow.kind -eq "CTO") {
        Write-Host "[PASS] ASTRA CTO core-handler role"
    } else {
        Write-Host "[FAIL] ASTRA CTO core-handler role"
        $ok = $false
    }

    if ($astraCtoRow.status -eq "AVAILABLE" -and
        $effectiveCtoDecision.authority -eq "ASTRA/CTO" -and
        $effectiveCtoDecision.deputy_authority -eq "ChatGPT/Deputy-CTO" -and
        $effectiveCtoDecision.deputy_can_override_cto -eq $false -and
        $effectiveCtoDecision.deputy_can_override_safety -eq $false) {
        Write-Host "[PASS] ASTRA CTO + ChatGPT Deputy CTO hierarchy"
    } else {
        Write-Host "[FAIL] ASTRA CTO + ChatGPT Deputy CTO hierarchy"
        $ok = $false
    }

    Write-Host ""
    Write-Host "STATUS      : $Value"
    Write-Host ""
}

function Write-ConsoleStatus {
    param(
        [string]$SessionId,
        [int]$Cycle,
        [string]$ReleaseState,
        [string]$BuildState,
        [string]$EditMode,
        [string]$PlayMode,
        [string]$Action,
        [string]$GoalText,
        [string]$Route,
        [string]$ChunkSize
    )

    Write-Host "============================================================"
    Write-Host " SOUL HUNTER AUTOPILOT v2.2.42"
    Write-Host "============================================================"
    Write-Host "Session       : $SessionId"
    Write-Host "Cycle         : $Cycle"
    Write-Host "Release state : $ReleaseState"
    Write-Host "Build         : $BuildState"
    Write-Host "EditMode      : $EditMode"
    Write-Host "PlayMode      : $PlayMode"
    if (![string]::IsNullOrWhiteSpace($Route)) { Write-Host "Route         : $Route" }
    if (![string]::IsNullOrWhiteSpace($ChunkSize)) { Write-Host "Chunk size    : $ChunkSize" }
    Write-Host "Next action   : $Action"
    if (![string]::IsNullOrWhiteSpace($GoalText)) { Write-Host "Goal          : $GoalText" }
    Write-Host "============================================================"
}

function ConvertTo-SafeJson {
    param($Object, [int]$Depth = 10)
    return ($Object | ConvertTo-Json -Depth $Depth)
}

function Save-JsonFile {
    param([string]$Path, $Object)
    $json = ConvertTo-SafeJson -Object $Object -Depth 20
    $temp = "$Path.tmp"
    $json | Set-Content -Path $temp -Encoding UTF8
    Move-Item -Path $temp -Destination $Path -Force
}

function Read-JsonFile {
    param([string]$Path)
    if (!(Test-Path $Path)) { return $null }
    try {
        return (Get-Content $Path -Raw | ConvertFrom-Json)
    } catch {
        return $null
    }
}

function Get-FreeDiskGB {
    $driveLetter = (Get-Item $AutomationRoot).PSDrive.Name
    $drive = Get-CimInstance -ClassName Win32_LogicalDisk -Filter "DeviceID='$($driveLetter):'"
    return [math]::Round($drive.FreeSpace / 1GB, 2)
}

# Learning Comment:
# In PowerShell, double quotes do not treat "\\" as an escape sequence for a single backslash.
# Concatenating "\\" created a double-backslash trailing path ("D:\SoulHunter-Automation\\"),
# causing StartsWith to return false for all valid workspace subpaths.
# We normalize paths using single [IO.Path]::DirectorySeparatorChar ('\').
function Test-IsUnderPath {
    param([string]$Child, [string]$Parent)
    try {
        $sep = [IO.Path]::DirectorySeparatorChar
        $childFull = [IO.Path]::GetFullPath($Child).TrimEnd('\', '/')
        $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd('\', '/')
        return ($childFull -eq $parentFull) -or $childFull.StartsWith($parentFull + $sep, [StringComparison]::OrdinalIgnoreCase)
    } catch {
        return $false
    }
}

function Test-PathReferencedByLiveProcess {
    param([string]$Path)
    try {
        $escaped = [Regex]::Escape($Path)
        $procs = Get-CimInstance Win32_Process | Where-Object {
            $_.CommandLine -and $_.CommandLine -match $escaped
        }
        return ($null -ne $procs -and @($procs).Count -gt 0)
    } catch {
        # If process inspection fails, prefer safety and treat it as in use.
        return $true
    }
}

function Check-DiskSpace {
    $freeBefore = Get-FreeDiskGB
    if ($freeBefore -ge $MinimumFreeGB) { return $freeBefore }

    Write-Host "Disk space low ($freeBefore GB < $MinimumFreeGB GB). Safe cleanup starting..."

    $candidates = @()
    $workspacesRoot = Join-Path $AutomationRoot "workspaces"
    if (Test-Path $workspacesRoot) {
        $candidates += Get-ChildItem -Path $workspacesRoot -Directory -ErrorAction SilentlyContinue
    }

    $releaseRoot = Join-Path $AutomationRoot "release-gate-runs"
    if (Test-Path $releaseRoot) {
        $candidates += Get-ChildItem -Path $releaseRoot -Directory -ErrorAction SilentlyContinue |
            ForEach-Object {
                $workspace = Join-Path $_.FullName "workspace"
                if (Test-Path $workspace) { Get-Item $workspace }
            }
    }

    $candidates = $candidates | Sort-Object CreationTime
    foreach ($dir in $candidates) {
        if ($null -eq $dir) { continue }
        $path = $dir.FullName

        # Strong path guards: disposable content must be under AutomationRoot,
        # never under canonical, and never the automation root itself.
        if (!(Test-IsUnderPath -Child $path -Parent $AutomationRoot)) { continue }
        if (Test-IsUnderPath -Child $path -Parent $Canonical) { continue }
        if ([IO.Path]::GetFullPath($path).TrimEnd('\\') -eq [IO.Path]::GetFullPath($AutomationRoot).TrimEnd('\\')) { continue }
        if (Test-PathReferencedByLiveProcess -Path $path) {
            Write-Host "[KEEP-LIVE] $path"
            continue
        }

        try {
            Remove-Item -Path $path -Recurse -Force -ErrorAction Stop
            Write-Host "[REMOVED] $path"
        } catch {
            Write-Host "[KEEP-LOCKED] $path"
        }

        if ((Get-FreeDiskGB) -ge $MinimumFreeGB) { break }
    }

    $freeAfter = Get-FreeDiskGB
    if ($freeAfter -lt $MinimumFreeGB) {
        Write-FinalStatus "DISK_SPACE_BLOCKED"
        exit 1
    }
    return $freeAfter
}

function Get-GitSnapshot {
    if (!(Test-Path $Canonical)) { return $null }
    $old = Get-Location
    try {
        Set-Location $Canonical
        # Capture Git stdout/stderr explicitly. Git may emit harmless CRLF/LF
        # warnings on stderr; those warnings must never become PowerShell failures.
        $runGitText = {
            param([string[]]$GitArgs)
            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.FileName = 'git.exe'
            $psi.WorkingDirectory = (Get-Location).Path
            $psi.UseShellExecute = $false
            $psi.CreateNoWindow = $true
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            $escapedArgs = foreach ($arg in $GitArgs) {
                if ($null -eq $arg -or $arg -eq '') {
                    '""'
                } elseif ($arg -match '[\s"]') {
                    '"' + ($arg -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
                } else {
                    $arg
                }
            }
            $psi.Arguments = ($escapedArgs -join ' ')
            $proc = [System.Diagnostics.Process]::new()
            $proc.StartInfo = $psi
            [void]$proc.Start()
            $stdout = $proc.StandardOutput.ReadToEnd()
            $stderr = $proc.StandardError.ReadToEnd()
            $proc.WaitForExit()

            # Git warnings (including LF/CRLF normalization warnings) are
            # diagnostic evidence, not command failures. Preserve real stderr
            # separately so callers can classify it without PowerShell turning
            # native stderr into NativeCommandError records.
            if ($stderr) {
                foreach ($line in ($stderr -split "`r?`n")) {
                    if ($line.Trim() -and ($line.Trim() -notmatch '^warning:')) {
                        Write-Verbose ("git stderr: " + $line.Trim())
                    }
                }
            }
            if ($proc.ExitCode -ne 0) {
                throw "git command failed (exit=$($proc.ExitCode)): $($stderr.Trim())"
            }
            return $stdout.Trim()
        }
        $head = & $runGitText @('rev-parse','HEAD')
        $status = & $runGitText @('status','--porcelain=v1')
        $diffNames = & $runGitText @('diff','--name-only')
        $cachedNames = & $runGitText @('diff','--cached','--name-only')
        return [ordered]@{
            head = $head
            status = $status
            diff_names = $diffNames
            cached_names = $cachedNames
        }
    } finally {
        Set-Location $old
    }
}

function Get-ChangedPathSet {
    param($Snapshot)
    $set = @{}
    if ($null -eq $Snapshot) { return $set }
    $text = @($Snapshot.diff_names, $Snapshot.cached_names) -join "`n"
    foreach ($line in ($text -split "`r?`n")) {
        $v = $line.Trim()
        if ($v) { $set[$v] = $true }
    }
    return $set
}

function Get-NewChangedPaths {
    param($Before, $After)
    $beforeSet = Get-ChangedPathSet $Before
    $afterSet = Get-ChangedPathSet $After
    $new = @()
    foreach ($k in $afterSet.Keys) {
        if (!$beforeSet.ContainsKey($k)) { $new += $k }
    }
    return @($new | Sort-Object)
}

function Get-RecentProviderEvidence {
    # Read recent evidence only. This does not consume provider quota.
    $evidence = @{
        Codex = @{ status = "UNKNOWN"; reset_at = $null; reason = "" }
        AGY = @{ status = "UNKNOWN"; reset_at = $null; reason = "" }
    }

    $runsRoot = Join-Path $AutomationRoot "orchestrator-runs"
    if (!(Test-Path $runsRoot)) { return $evidence }

    $recentDirs = Get-ChildItem $runsRoot -Directory -ErrorAction SilentlyContinue |
        Sort-Object CreationTime -Descending |
        Select-Object -First 12

    $now = Get-Date
    foreach ($dir in $recentDirs) {
        $files = Get-ChildItem $dir.FullName -File -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.Length -lt 5MB -and $_.Extension -in ".txt", ".log", ".json" }

        foreach ($file in $files) {
            $content = ""
            try { $content = Get-Content $file.FullName -Raw -ErrorAction Stop } catch { continue }
            if ([string]::IsNullOrWhiteSpace($content)) { continue }

            # Codex exposes a very useful reset timestamp in its quota error.
            if ($content -match '(?is)(Backend\s+codex\s+failed|Codex.*?)(usage limit|quota|rate limit|too many requests)') {
                $evidence.Codex.status = "QUOTA_EXHAUSTED"
                $evidence.Codex.reason = "Recent orchestrator evidence reports Codex quota/rate exhaustion."

                $m = [Regex]::Match($content, '(?is)try again at\s+([^\r\n\"}]+)')
                if ($m.Success) {
                    $candidate = $m.Groups[1].Value.Trim().TrimEnd('.')
                    $candidate = [Regex]::Replace($candidate, '(\d{1,2})(st|nd|rd|th)\b', '$1', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
                    $parsed = [datetime]::MinValue
                    if ([datetime]::TryParse($candidate, [Globalization.CultureInfo]::GetCultureInfo("en-US"), [Globalization.DateTimeStyles]::AllowWhiteSpaces, [ref]$parsed)) {
                        $evidence.Codex.reset_at = $parsed.ToString("o")
                        if ($parsed -le $now) {
                            $evidence.Codex.status = "UNKNOWN"
                            $evidence.Codex.reason = "Previous Codex quota reset time has passed; requires live use verification."
                        }
                    }
                } elseif ($file.LastWriteTime -lt $now.AddHours(-12)) {
                    $evidence.Codex.status = "UNKNOWN"
                    $evidence.Codex.reason = "Old Codex quota evidence has no reset time and is treated as stale."
                }
            }

            if ($file.LastWriteTime -ge $now.AddHours(-12) -and $content -match '(?is)(Backend\s+(agy|antigravity)\s+failed|Antigravity.*?)(usage limit|quota|rate limit|too many requests|limit exceeded)') {
                $evidence.AGY.status = "QUOTA_EXHAUSTED"
                $evidence.AGY.reason = "Recent orchestrator evidence reports AGY/Antigravity quota/rate exhaustion."
            }
        }
    }

    return $evidence
}

function New-ProviderRecord {
    param(
        [string]$Name,
        [string]$Kind,
        [string]$Status,
        [int]$RawCapacity,
        [string[]]$Capabilities,
        [string]$Reason,
        [string]$Path = "",
        [string]$ResetAt = "",
        [string]$ModelId = ""
    )

    $usable = 0
    if ($RawCapacity -gt 0) {
        $usable = [math]::Max(0, [math]::Floor($RawCapacity * ((100 - $CapacityReservePercent) / 100.0)))
    }

    return [pscustomobject][ordered]@{
        name = $Name
        kind = $Kind
        status = $Status
        raw_capacity = $RawCapacity
        usable_capacity = $usable
        reserve_percent = $CapacityReservePercent
        capabilities = @($Capabilities)
        reason = $Reason
        path = $Path
        reset_at = $ResetAt
        model_id = $ModelId
        observed_at = ""
        quota_verified = $false
        quota_source = ""
        quota_scope = ""
        pool_id = ""
        five_hour_remaining_percent = $null
        weekly_remaining_percent = $null
    }
}

function Test-SimulatedExhaustion {
    param([string]$Name)

    # PowerShell -File invocation can bind `Codex,AGY` as one string.
    # Normalize every supplied item so both of these work:
    #   -SimulateExhausted Codex,AGY
    #   -SimulateExhausted @("Codex","AGY")
    foreach ($item in @($SimulateExhausted)) {
        if ([string]::IsNullOrWhiteSpace([string]$item)) { continue }
        foreach ($part in ([string]$item -split '[,;]')) {
            $candidate = $part.Trim()
            if ($candidate -and $candidate.Equals($Name, [StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        }
    }
    return $false
}


function Get-AGYModelInventory {
    # System evidence only: query the installed AGY CLI for the models it
    # actually exposes. Do not invent an AGY "native/default" path.
    $inventory = @()
    if (!(Test-Path $AgyExe)) { return @() }

    try {
        # Native PowerShell invocation can surface AGY stderr as ErrorRecord
        # objects and may lose the model rows when piped. Use Process so stdout
        # and stderr are captured independently, then parse both streams.
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = $AgyExe
        $psi.Arguments = "models"
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $proc = New-Object System.Diagnostics.Process
        $proc.StartInfo = $psi
        [void]$proc.Start()
        $stdout = $proc.StandardOutput.ReadToEnd()
        $stderr = $proc.StandardError.ReadToEnd()
        $proc.WaitForExit()
        $output = @($stdout -split "`r?`n") + @($stderr -split "`r?`n")
        foreach ($line in $output) {
            if ($line -match '^\s*(?<id>[a-z0-9][a-z0-9._-]+)\s+(?<label>.+?)\s*$') {
                $id = $Matches.id.Trim()
                $label = $Matches.label.Trim()

                # Ignore CLI progress/noise and keep only model rows.
                if ($id -match '^(gemini|claude|gpt)-') {
                    $family = if ($id -like 'gemini-*') {
                        'Gemini'
                    } elseif ($id -like 'claude-*') {
                        'Claude'
                    } elseif ($id -like 'gpt-*') {
                        'GPT'
                    } else {
                        continue
                    }

                    if (!($inventory | Where-Object { $_.id -eq $id })) {
                        $inventory += [pscustomobject]@{
                            id = $id
                            family = $family
                            label = $label
                        }
                    }
                }
            }
        }
    } catch {
        Write-Warning "AGY model inventory query failed; using capacity-state families only: $($_.Exception.Message)"
    }

    return @($inventory)
}

function Get-AGYModelCapacityState {
    # IMPORTANT: AGY CLI exposes model inventory, but does not expose a
    # machine-readable quota endpoint here. Therefore quota/capacity is never
    # synthesized. Only an observed state file is accepted as quota evidence.
    $result = @()
    if (!(Test-Path $AgyCapacityStatePath)) { return @() }

    try {
        $raw = Get-Content $AgyCapacityStatePath -Raw -ErrorAction Stop
        $obj = $raw | ConvertFrom-Json -ErrorAction Stop
        foreach ($item in @($obj.models)) {
            $model = [string]$item.model
            if ([string]::IsNullOrWhiteSpace($model)) { continue }
            $status = ([string]$item.status).ToUpperInvariant()
            $rawCapacity = 0
            if ($null -ne $item.raw_capacity) { $rawCapacity = [int]$item.raw_capacity }
            $observedAt = if ($item.observed_at) { [string]$item.observed_at } else { "" }
            $result += [pscustomobject]@{
                model=$model; status=$status; raw_capacity=$rawCapacity
                reason=[string]$item.reason
                source=if ($item.source) {[string]$item.source} else {"agy-model-capacity-state"}
                observed_at=$observedAt
            }
        }
    } catch {
        Write-Log "AGY quota evidence could not be read: $($_.Exception.Message)"
    }
    return @($result)
}

function Get-ProviderEvidenceFreshness {
    param([string]$ObservedAt)
    if ([string]::IsNullOrWhiteSpace($ObservedAt)) { return [pscustomobject]@{ Fresh=$false; AgeSeconds=-1 } }
    try {
        $dt=[datetimeoffset]::Parse($ObservedAt)
        $age=[math]::Max(0,([datetimeoffset]::Now-$dt).TotalSeconds)
        return [pscustomobject]@{ Fresh=($age -le 900); AgeSeconds=[int]$age }
    } catch {
        return [pscustomobject]@{ Fresh=$false; AgeSeconds=-1 }
    }
}


function Ensure-AgentRegister {
    if (Test-Path $AgentRegisterPath) {
        $reg = Read-JsonFile $AgentRegisterPath
        if ($null -eq $reg -or $null -eq $reg.agents) {
            throw "Agent-Register.json exists but is invalid; refusing to replace manual register."
        }
        return
    }
    $template = [ordered]@{
        schema = "soul-hunter.agent-register.v1"
        version = "1.0"
        description = "MANUAL MASTER REGISTER. Runtime systems must not overwrite identity/capability fields."
        agents = @(
            [ordered]@{ agent_id="Codex"; name="OpenAI Codex"; provider="OpenAI"; kind="SEMANTIC"; models=@("codex"); capabilities=@("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION"); tools=@("Git","PowerShell"); priority=1; enabled=$true },
            [ordered]@{ agent_id="AGY/Gemini"; name="Antigravity Gemini"; provider="AGY"; kind="SEMANTIC"; models=@(); capabilities=@("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION"); tools=@("AGY CLI"); priority=2; enabled=$true },
            [ordered]@{ agent_id="AGY/Claude"; name="Antigravity Claude"; provider="AGY"; kind="SEMANTIC"; models=@(); capabilities=@("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION"); tools=@("AGY CLI"); priority=3; enabled=$true },
            [ordered]@{ agent_id="AGY/GPT"; name="Antigravity GPT"; provider="AGY"; kind="SEMANTIC"; models=@(); capabilities=@("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION"); tools=@("AGY CLI"); priority=4; enabled=$true },
            [ordered]@{ agent_id="PowerShell"; name="Windows PowerShell"; provider="Microsoft"; kind="DETERMINISTIC"; models=@(); capabilities=@("PROJECT_SCAN","FILE_PATCHING"); tools=@("PowerShell"); priority=10; enabled=$true },
            [ordered]@{ agent_id="Git"; name="Git"; provider="Git"; kind="DETERMINISTIC"; models=@(); capabilities=@("GIT_SAFETY"); tools=@("git.exe"); priority=10; enabled=$true },
            [ordered]@{ agent_id="UnityCLI"; name="Unity CLI"; provider="Unity"; kind="DETERMINISTIC"; models=@(); capabilities=@("UNITY_COMPILE","UNITY_TEST","PLAYER_BUILD"); tools=@("Unity.exe"); priority=10; enabled=$true },
            [ordered]@{ agent_id="ReleaseGate"; name="Release Gate"; provider="SoulHunter"; kind="DETERMINISTIC"; models=@(); capabilities=@("UNITY_COMPILE","UNITY_TEST","PLAYER_BUILD","GIT_SAFETY"); tools=@("PowerShell"); priority=10; enabled=$true },
            [ordered]@{ agent_id="Orchestrator"; name="Soul Hunter Orchestrator"; provider="SoulHunter"; kind="EXECUTOR"; models=@(); capabilities=@("FILE_PATCHING"); tools=@("PowerShell"); priority=10; enabled=$true },
            [ordered]@{ agent_id="ASTRA/CTO"; name="ASTRA CTO"; provider="SoulHunter"; kind="CTO"; models=@(); capabilities=@("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION"); tools=@(); priority=0; enabled=$true }
        )
    }
    Save-JsonFile $AgentRegisterPath $template
    Write-Host "[INIT] Created manual Agent-Register.json template. Runtime state will never overwrite it."
}

# Initialize the manual register only after its function definition has executed.
Ensure-AgentRegister

function Invoke-QuotaReporterForAgent {
    param([Parameter(Mandatory)][string]$AgentId)
    if (!(Test-Path $QuotaReporterPath)) {
        return [pscustomobject]@{ Success=$false; AgentId=$AgentId; Status='UNVERIFIED'; Error='Quota reporter missing.'; Record=$null }
    }
    $reportPath = Join-Path $SessionDir ("quota-check-{0}-{1}.json" -f ($AgentId -replace '[^A-Za-z0-9._-]','_'), (Get-Date -Format 'yyyyMMdd-HHmmssfff'))
    $registerArg = if (Test-Path $AgentRegisterPath) { " -RegisterPath '" + $AgentRegisterPath.Replace("'","''") + "'" } else { "" }
    $stateArg = " -StatePath '" + $AgentStatePath.Replace("'","''") + "'"
    $command = "& '" + $QuotaReporterPath.Replace("'","''") + "' -AgentId '" + $AgentId.Replace("'","''") + "' -StatusPath '" + $reportPath.Replace("'","''") + "' -AutomationRoot '" + $AutomationRoot.Replace("'","''") + "'" + $registerArg + $stateArg + "; exit `$LASTEXITCODE"
    $bytes=[Text.Encoding]::Unicode.GetBytes($command)
    $encoded=[Convert]::ToBase64String($bytes)
    try {
        $proc=Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-EncodedCommand',$encoded) -Wait -PassThru -NoNewWindow
        if (!(Test-Path $reportPath)) {
            return [pscustomobject]@{ Success=$false; AgentId=$AgentId; Status='UNVERIFIED'; Error="Quota reporter produced no status report (exit=$($proc.ExitCode))."; Record=$null }
        }
        $obj=Read-JsonFile $reportPath
        if ($null -eq $obj -or $null -eq $obj.record) {
            return [pscustomobject]@{ Success=$false; AgentId=$AgentId; Status='UNVERIFIED'; Error='Quota reporter status JSON invalid or missing record.'; Record=$null }
        }
        return [pscustomobject]@{ Success=([bool]$obj.verified -and [string]$obj.status -eq 'AVAILABLE'); AgentId=$AgentId; Status=[string]$obj.status; Error=[string]$obj.error; Record=$obj.record }
    } catch {
        return [pscustomobject]@{ Success=$false; AgentId=$AgentId; Status='UNVERIFIED'; Error=$_.Exception.Message; Record=$null }
    }
}

function Apply-QuotaRecordToProvider {
    param([object]$Provider,[object]$Record)
    if ($null -eq $Provider -or $null -eq $Record) { return $Provider }
    $remaining = $null
    if ($null -ne $Record.effective_remaining_percent) { $remaining=[double]$Record.effective_remaining_percent }
    elseif ($null -ne $Record.remaining_percent) { $remaining=[double]$Record.remaining_percent }
    if ($null -eq $remaining) {
        $Provider.status='AVAILABLE_UNVERIFIED'; $Provider.raw_capacity=0; $Provider.usable_capacity=0
        $Provider.reason='Quota report contained no usable remaining capacity.'
        $Provider.quota_verified=$false
        return $Provider
    }
    $raw=[int][math]::Floor([math]::Max(0,[math]::Min(100,$remaining)))
    $Provider.raw_capacity=$raw
    $Provider.usable_capacity = if ($raw -gt 0) { [math]::Max(0,[math]::Floor($raw*((100-$CapacityReservePercent)/100.0))) } else { 0 }
    $Provider.status = if ($raw -le 0) { 'QUOTA_EXHAUSTED' } else { 'AVAILABLE' }
    $Provider.reason=[string]$Record.reason
    $Provider.reset_at=[string]$Record.reset_at
    $Provider.observed_at=[string]$Record.fetched_at
    $Provider.quota_source=[string]$Record.quota_source
    $Provider.quota_verified=[bool]$Record.verified
    $Provider.quota_scope=[string]$Record.quota_scope
    $Provider.pool_id=[string]$Record.pool_id
    $Provider.five_hour_remaining_percent=$Record.five_hour_remaining_percent
    $Provider.weekly_remaining_percent=$Record.weekly_remaining_percent
    return $Provider
}

function Refresh-QuotaBeforeSemanticSelection {
    param([object[]]$Providers,[string[]]$RequiredCapabilities)
    $updated=@($Providers)
    $candidateSet=@()
    foreach($cap in @($RequiredCapabilities)) {
        $candidateSet += @($updated | Where-Object { $_.kind -eq 'SEMANTIC' -and @($_.capabilities) -contains $cap -and $_.name -ne 'ASTRA/CTO' })
    }
    $candidateSet=@($candidateSet | Sort-Object -Property @{Expression={[int]$_.usable_capacity};Descending=$true}, @{Expression={$_.name};Descending=$false} | Group-Object name | ForEach-Object {$_.Group|Select-Object -First 1})
    if($candidateSet.Count -eq 0){ return $updated }
    Write-Host ''
    Write-Host '================ ONE-BY-ONE LIVE QUOTA CHECK ================'
    $index=0
    foreach($p in $candidateSet) {
        $index++
        Write-Host ("[{0}/{1}] Checking {2} / model={3} ..." -f $index,$candidateSet.Count,$p.name,$p.model_id)
        $q=Invoke-QuotaReporterForAgent -AgentId ([string]$p.name)
        if($q.Record) {
            Apply-QuotaRecordToProvider -Provider $p -Record $q.Record | Out-Null
            Write-Host ("      status={0}  usable={1}%  5h={2}%  weekly={3}%" -f $p.status,$p.usable_capacity,$p.five_hour_remaining_percent,$p.weekly_remaining_percent)
        } else {
            $p.status='AVAILABLE_UNVERIFIED'; $p.raw_capacity=0; $p.usable_capacity=0; $p.quota_verified=$false
            $p.reason=('QUOTA CHECK FAILED: ' + [string]$q.Error)
            Write-Host ('      status=UNVERIFIED  action=SKIP  reason=' + $p.reason)
        }
        if($p.status -eq 'AVAILABLE' -and [int]$p.usable_capacity -gt 0 -and [bool]$p.quota_verified) {
            Write-Host ("      VERIFIED + USABLE -> candidate remains eligible")
        } else {
            Write-Host ("      NOT USABLE -> next agent")
        }
        if($q.Record){$p.observed_at=[string]$q.Record.fetched_at}else{$p.observed_at=[string]$p.observed_at}
    }
    Write-Host '============================================================='
    return $updated
}

function Get-ProviderMatrix {
    param([switch]$FreshRuntime)
    $history = Get-RecentProviderEvidence
    $providers = @()

    # Semantic providers ------------------------------------------------------
    $codexLive = $null
    if ($FreshRuntime) { $codexLive = Get-LiveCodexQuotaEvidence -CodexExecutable $CodexExe }
    if (Test-SimulatedExhaustion "Codex") {
        $providers += New-ProviderRecord -Name "Codex" -Kind "SEMANTIC" -Status "QUOTA_EXHAUSTED" -RawCapacity 0 -Capabilities @("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION") -Reason "Simulated exhaustion." -Path $CodexExe -ModelId "codex"
    } elseif (!(Test-Path $CodexExe)) {
        $providers += New-ProviderRecord -Name "Codex" -Kind "SEMANTIC" -Status "MISSING" -RawCapacity 0 -Capabilities @("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION") -Reason "Codex executable not found." -Path $CodexExe -ModelId "codex"
    } elseif ($FreshRuntime -and $codexLive.Success) {
        $raw=[int][math]::Floor([double]$codexLive.RemainingPercent)
        $status="AVAILABLE"
        if($raw -le 0){$status="QUOTA_EXHAUSTED"}
        $providers += New-ProviderRecord -Name "Codex" -Kind "SEMANTIC" -Status $status -RawCapacity $raw -Capabilities @("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION") -Reason "LIVE Codex account/rateLimits/read." -Path $CodexExe -ResetAt ([string]$codexLive.ResetAt) -ModelId "codex"
        $providers[-1].quota_source='codex app-server account/rateLimits/read'; $providers[-1].observed_at=$codexLive.FetchedAt
    } elseif ($history.Codex.status -eq "QUOTA_EXHAUSTED") {
        $providers += New-ProviderRecord -Name "Codex" -Kind "SEMANTIC" -Status "QUOTA_EXHAUSTED" -RawCapacity 0 -Capabilities @("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION") -Reason ([string]$history.Codex.reason) -Path $CodexExe -ResetAt ([string]$history.Codex.reset_at) -ModelId "codex"
    } else {
        $reason = "Executable exists, but quota was not queried in this non-runtime matrix."
        if ($FreshRuntime) { $reason = "LIVE Codex quota probe unavailable; refusing to infer capacity." }
        $providers += New-ProviderRecord -Name "Codex" -Kind "SEMANTIC" -Status "AVAILABLE_UNVERIFIED" -RawCapacity 0 -Capabilities @("PROJECT_SCAN","CODE_REASONING","CODE_GENERATION") -Reason $reason -Path $CodexExe -ModelId "codex"
    }

    $agyInventory=@(Get-AGYModelInventory)
    $agyFamilies=@($agyInventory|Select-Object -ExpandProperty family -Unique)
    $agyLive=$null
    if ($FreshRuntime) { $agyLive = Get-LiveAGYQuotaEvidence }
    $agyState=@(Get-AGYModelCapacityState)

    foreach($family in @('Gemini','Claude','GPT')){
        if($agyFamilies -notcontains $family){continue}
        $familyModels=@($agyInventory|Where-Object family -eq $family|Select-Object -ExpandProperty id)
        if($familyModels.Count -eq 0){continue}
        $liveRows=@()
        if($agyLive -and $agyLive.Success){
            if($family -eq 'Gemini'){$liveRows=@($agyLive.Models|Where-Object {$_.family -eq 'Gemini' -or $_.model_id -in $familyModels})}
            elseif($family -in @('Claude','GPT')){$liveRows=@($agyLive.Models|Where-Object {$_.family -eq 'ThirdParty' -or $_.model_id -in $familyModels})}
        }
        if($liveRows.Count -gt 0){
            $selected=$liveRows|Sort-Object remaining_percent -Descending|Select-Object -First 1
            $raw=[int][math]::Floor([double]$selected.remaining_percent)
            $status='AVAILABLE'
            if($raw -le 0){$status='QUOTA_EXHAUSTED'}
            $reason=('LIVE AGY RetrieveUserQuotaSummary: '+$agyLive.Source)
            $providers += New-ProviderRecord -Name ('AGY/'+$family) -Kind 'SEMANTIC' -Status $status -RawCapacity $raw -Capabilities @('PROJECT_SCAN','CODE_REASONING','CODE_GENERATION') -Reason $reason -Path $AgyExe -ResetAt ([string]$selected.reset_time) -ModelId ([string]$familyModels[0])
            $providers[-1].quota_source=$agyLive.Source; $providers[-1].observed_at=$agyLive.FetchedAt; $providers[-1].quota_scope=[string]$selected.quota_scope; $providers[-1].pool_id=[string]$selected.pool_id; $providers[-1].five_hour_remaining_percent=$selected.five_hour; $providers[-1].weekly_remaining_percent=$selected.weekly
            continue
        }
        $state=@($agyState|Where-Object {[string]$_.model -eq $family}|Select-Object -First 1)
        if(-not $FreshRuntime -and $state.Count -gt 0){
            $providers += New-ProviderRecord -Name ('AGY/'+$family) -Kind 'SEMANTIC' -Status ([string]$state.status).ToUpperInvariant() -RawCapacity ([int]$state.raw_capacity) -Capabilities @('PROJECT_SCAN','CODE_REASONING','CODE_GENERATION') -Reason ([string]$state.reason) -Path $AgyExe -ResetAt ([string]$state.observed_at) -ModelId ([string]$familyModels[0])
            continue
        }
        $reason='AGY quota not queried in this non-runtime matrix.'
        if($FreshRuntime){$reason='LIVE AGY quota probe unavailable; refusing to infer runtime capacity.'}
        $providers += New-ProviderRecord -Name ('AGY/'+$family) -Kind 'SEMANTIC' -Status 'AVAILABLE_UNVERIFIED' -RawCapacity 0 -Capabilities @('PROJECT_SCAN','CODE_REASONING','CODE_GENERATION') -Reason $reason -Path $AgyExe -ModelId ([string]$familyModels[0])
    }

    # ASTRA/CTO is a logical authority, not an external quota-bearing model.
    $providers += New-ProviderRecord -Name 'ASTRA/CTO' -Kind 'CTO' -Status 'AVAILABLE' -RawCapacity 100 -Capabilities @('PROJECT_SCAN','CODE_REASONING','CODE_GENERATION') -Reason 'Logical CTO authority; no external model quota claimed.' -Path $AstraAdapterPath

    # Deterministic providers -------------------------------------------------
    $psPath=(Get-Command powershell.exe -ErrorAction SilentlyContinue).Source
    if($psPath){$providers+=New-ProviderRecord -Name 'PowerShell' -Kind 'DETERMINISTIC' -Status 'AVAILABLE' -RawCapacity 100 -Capabilities @('PROJECT_SCAN','FILE_PATCHING') -Reason 'Deterministic scanner/file operator available.' -Path $psPath}else{$providers+=New-ProviderRecord -Name 'PowerShell' -Kind 'DETERMINISTIC' -Status 'MISSING' -RawCapacity 0 -Capabilities @('PROJECT_SCAN','FILE_PATCHING') -Reason 'powershell.exe not found.'}
    $gitPath=(Get-Command git.exe -ErrorAction SilentlyContinue).Source;if(!$gitPath){$gitPath=(Get-Command git -ErrorAction SilentlyContinue).Source};if($gitPath){$providers+=New-ProviderRecord -Name 'Git' -Kind 'DETERMINISTIC' -Status 'AVAILABLE' -RawCapacity 100 -Capabilities @('GIT_SAFETY') -Reason 'Git available.' -Path $gitPath}else{$providers+=New-ProviderRecord -Name 'Git' -Kind 'DETERMINISTIC' -Status 'MISSING' -RawCapacity 0 -Capabilities @('GIT_SAFETY') -Reason 'Git not found.'}
    if(Test-Path $UnityExe){$providers+=New-ProviderRecord -Name 'UnityCLI' -Kind 'DETERMINISTIC' -Status 'AVAILABLE' -RawCapacity 100 -Capabilities @('UNITY_COMPILE','UNITY_TEST','PLAYER_BUILD') -Reason 'Unity CLI available.' -Path $UnityExe}else{$providers+=New-ProviderRecord -Name 'UnityCLI' -Kind 'DETERMINISTIC' -Status 'MISSING' -RawCapacity 0 -Capabilities @('UNITY_COMPILE','UNITY_TEST','PLAYER_BUILD') -Reason 'Unity executable not found.' -Path $UnityExe}
    if(Test-Path $ReleaseGatePath){$providers+=New-ProviderRecord -Name 'ReleaseGate' -Kind 'DETERMINISTIC' -Status 'AVAILABLE' -RawCapacity 100 -Capabilities @('UNITY_COMPILE','UNITY_TEST','PLAYER_BUILD','GIT_SAFETY') -Reason 'Release gate available.' -Path $ReleaseGatePath}else{$providers+=New-ProviderRecord -Name 'ReleaseGate' -Kind 'DETERMINISTIC' -Status 'MISSING' -RawCapacity 0 -Capabilities @('UNITY_COMPILE','UNITY_TEST','PLAYER_BUILD','GIT_SAFETY') -Reason 'Release gate script missing.' -Path $ReleaseGatePath}
    if(Test-Path $OrchestratorPath){$providers+=New-ProviderRecord -Name 'Orchestrator' -Kind 'EXECUTOR' -Status 'AVAILABLE' -RawCapacity 100 -Capabilities @('FILE_PATCHING') -Reason 'Legacy isolated-workspace executor available.' -Path $OrchestratorPath}else{$providers+=New-ProviderRecord -Name 'Orchestrator' -Kind 'EXECUTOR' -Status 'MISSING' -RawCapacity 0 -Capabilities @('FILE_PATCHING') -Reason 'Orchestrator script missing.' -Path $OrchestratorPath}
    return @($providers)
}

function Show-ProviderMatrix {
    param($Providers)
    Write-Host ""
    Write-Host "================ CAPABILITY / CAPACITY PREFLIGHT ================"
    foreach ($p in $Providers) {
        $caps = ($p.capabilities -join ",")
        $reset = ""
        if ($p.reset_at) { $reset = " reset=$($p.reset_at)" }
        Write-Host ("{0,-12} {1,-23} usable={2,3}%  {3}{4}" -f $p.name, $p.status, $p.usable_capacity, $caps, $reset)
    }
    Write-Host "Reserve      : $CapacityReservePercent%"
    Write-Host "Quota source : selection-time one-by-one live report required for semantic execution"
    Write-Host "================================================================="
}

function Get-ProvidersForCapability {
    param([object[]]$Providers, [string]$Capability)

    $result = @(
        @($Providers) | Where-Object {
            $null -ne $_ -and
            ([string]$_.status).ToUpperInvariant() -eq "AVAILABLE" -and
            [int]$_.usable_capacity -gt 0 -and
            (([string]$_.kind -ne "SEMANTIC") -or ([bool]$_.quota_verified)) -and
            @($_.capabilities) -contains $Capability
        } | Sort-Object -Property @{Expression={[int]$_.usable_capacity};Descending=$true}, @{Expression={$_.name};Descending=$false}
    )
    return $result
}

function Get-IndependentSemanticProviders {
    param([object[]]$Providers)

    $result = @(
        @($Providers) | Where-Object {
            $null -ne $_ -and
            $_.kind -eq "SEMANTIC" -and
            ([string]$_.status).ToUpperInvariant() -eq "AVAILABLE" -and
            [int]$_.usable_capacity -gt 0 -and
            [bool]$_.quota_verified
        } | Sort-Object -Property @{Expression={[int]$_.usable_capacity};Descending=$true}, @{Expression={$_.name};Descending=$false}
    )
    return $result
}

function Get-ChunkProfile {
    param([object[]]$Providers)
    $semantic = @(Get-IndependentSemanticProviders -Providers @($Providers))
    $best = 0
    if ($semantic.Count -gt 0) { $best = [int](($semantic | Measure-Object -Property usable_capacity -Maximum).Maximum) }

    switch ($Mode) {
        "Economy" {
            if ($best -ge 45) { return @{ Size="SMALL"; MaxFiles=2; Risk="LOW" } }
            return @{ Size="TINY"; MaxFiles=1; Risk="LOW" }
        }
        "MaximumAutonomy" {
            if ($best -ge 70) { return @{ Size="LARGE"; MaxFiles=8; Risk="MEDIUM" } }
            if ($best -ge 40) { return @{ Size="MEDIUM"; MaxFiles=5; Risk="MEDIUM" } }
            if ($best -gt 0) { return @{ Size="SMALL"; MaxFiles=2; Risk="LOW" } }
            return @{ Size="DETERMINISTIC_ONLY"; MaxFiles=0; Risk="LOW" }
        }
        default {
            if ($best -ge 60) { return @{ Size="MEDIUM"; MaxFiles=5; Risk="MEDIUM" } }
            if ($best -gt 0) { return @{ Size="SMALL"; MaxFiles=2; Risk="LOW" } }
            return @{ Size="DETERMINISTIC_ONLY"; MaxFiles=0; Risk="LOW" }
        }
    }
}

function Get-GoalKeywords {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return @() }
    $words = [Regex]::Matches($Text.ToLowerInvariant(), '[a-z][a-z0-9_]{4,}') | ForEach-Object { $_.Value }
    $unique = @()
    foreach ($w in $words) {
        if ($StopWords -contains $w) { continue }
        if ($unique -notcontains $w) { $unique += $w }
        if ($unique.Count -ge 8) { break }
    }
    return $unique
}

function New-EvidenceBundle {
    param([string]$SessionDir, [int]$Cycle, [string]$GoalText, $SummaryPath, $BlockersPath)

    $bundle = Join-Path $SessionDir ("evidence-cycle-{0:D3}.txt" -f $Cycle)
    "SOUL HUNTER AUTOPILOT v2.2.42 - DETERMINISTIC EVIDENCE BUNDLE" | Set-Content $bundle -Encoding UTF8
    "Generated: $(Get-Date -Format o)" | Add-Content $bundle
    "Goal: $GoalText" | Add-Content $bundle
    "" | Add-Content $bundle

    "===== GIT SNAPSHOT =====" | Add-Content $bundle
    $git = Get-GitSnapshot
    if ($git) {
        ($git | ConvertTo-Json -Depth 5) | Add-Content $bundle
    }

    if ($SummaryPath -and (Test-Path $SummaryPath)) {
        "" | Add-Content $bundle
        "===== RELEASE SUMMARY =====" | Add-Content $bundle
        Get-Content $SummaryPath -Raw | Add-Content $bundle
    }
    if ($BlockersPath -and (Test-Path $BlockersPath)) {
        "" | Add-Content $bundle
        "===== RELEASE BLOCKERS =====" | Add-Content $bundle
        Get-Content $BlockersPath -Raw | Add-Content $bundle
    }

    $keywords = Get-GoalKeywords $GoalText
    "" | Add-Content $bundle
    "===== TARGETED PROJECT MATCHES =====" | Add-Content $bundle
    "Keywords: $($keywords -join ', ')" | Add-Content $bundle

    if ($keywords.Count -gt 0) {
        $files = Get-ChildItem (Join-Path $Canonical "Assets") -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -in ".cs", ".asmdef", ".prefab", ".unity", ".asset", ".json" }

        $matchCount = 0
        foreach ($kw in $keywords) {
            if ($matchCount -ge 250) { break }
            $matches = $files | Select-String -Pattern $kw -SimpleMatch -ErrorAction SilentlyContinue | Select-Object -First 40
            foreach ($m in $matches) {
                "[$kw] $($m.Path):$($m.LineNumber) $($m.Line.Trim())" | Add-Content $bundle
                $matchCount++
                if ($matchCount -ge 250) { break }
            }
        }
    }

    return $bundle
}

function Get-RootGoal {
    param($summary, $blockers, $latestRunName)

    if (![string]::IsNullOrWhiteSpace($Goal)) {
        # Explicit read-only goals remain authoritative and must never be turned
        # into mutation. For a mutation-intent goal, however, a verified
        # PlayMode failure takes precedence: verification must identify the
        # failing test/root cause before any executor is allowed to patch.
        $readOnly = ($Goal -match '(?i)\b(read[- ]?only|do not modify|do not change|without modifying)\b')

        if ($readOnly) {
            if ($null -ne $blockers -and @($blockers).Count -gt 0) {
                $msgs = @($blockers | ForEach-Object { $_.message } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
                if ($msgs.Count -gt 0) {
                    return @{
                        Status = "AUTONOMOUS_FIX"
                        Goal = "$Goal`nCurrent release blocker evidence (diagnostic only; do not modify): $($msgs -join '; ')"
                    }
                }
            }
            return @{ Status = "AUTONOMOUS_FIX"; Goal = $Goal }
        }

        # Safety Constitution: validation result wins over implementation intent.
        # A PlayMode FAIL is therefore diagnostic-first even when the caller
        # explicitly asks to fix the project.
        if ($summary.tests.playmode.state -eq "FAIL") {
            return @{
                Status = "DIAGNOSTIC_FIRST"
                Goal = "Diagnose the single smallest PlayMode failure before making any change. User goal: $Goal`nIdentify the failing test, relevant stack trace/symptom, and smallest safe root-cause hypothesis. Do not modify files until the failure is identified and the proposed change passes workspace/safety validation."
            }
        }

        return @{ Status = "AUTONOMOUS_FIX"; Goal = $Goal }
    }

    $actionable = @()
    $external = @()
    if ($null -ne $blockers) {
        foreach ($b in @($blockers)) {
            if ($b.actionable_by_orchestrator) { $actionable += $b } else { $external += $b }
        }
    }

    # PlayMode failure is a verification failure, not permission to guess a fix.
    # The first autonomous chunk must identify the smallest failing test/root
    # cause from evidence before any mutation is considered.
    if ($summary.tests.playmode.state -eq "FAIL") {
        return @{
            Status = "DIAGNOSTIC_FIRST"
            Goal = "Diagnose the single smallest PlayMode failure from current canonical evidence. Identify the failing test, relevant stack trace/symptom, and the smallest safe root-cause hypothesis. Do not modify files until the failure is identified and the proposed change passes workspace/safety validation."
        }
    }

    if ($external.Count -gt 0) {
        return @{ Status = "EXTERNAL_BLOCKED"; Goal = "" }
    }

    if ($actionable.Count -gt 0) {
        $msgs = $actionable | ForEach-Object { $_.message }
        return @{
            Status = "AUTONOMOUS_FIX"
            Goal = "Resolve the following release blockers from run $latestRunName using current canonical evidence. Work in the smallest safe validated slice. Blockers: $($msgs -join '; ')"
        }
    }

    if ($summary.project_state -eq "RELEASE_CANDIDATE" -or $summary.verdict -eq "PASS_WITH_EVIDENCE_GAPS") {
        if ($summary.tests.playmode.state -eq "NO_TESTS") {
            return @{
                Status = "AUTONOMOUS_FIX"
                Goal = "Resolve the current PlayMode release-evidence gap from current canonical behavior. Add only meaningful production-backed coverage; never fabricate placeholder coverage."
            }
        }
        if ($summary.tests.editmode.state -eq "NO_TESTS") {
            return @{
                Status = "AUTONOMOUS_FIX"
                Goal = "Resolve the current EditMode release-evidence gap from current canonical behavior with meaningful automated coverage."
            }
        }
        return @{
            Status = "AUTONOMOUS_FIX"
            Goal = "Resolve the current release-evidence gap using only current canonical evidence."
        }
    }

    return @{
        Status = "AUTONOMOUS_FIX"
        Goal = "Resolve the current project failure state: $($summary.project_state) / $($summary.verdict). Work in the smallest safe validated slice."
    }
}


# =============================================================================
# v2.2.20 AUTOPILOT CORE â€” capability-first routing
# =============================================================================
function Get-RequiredCapabilitiesForGoal {
    param([string]$GoalText)

    $g = ([string]$GoalText).ToLowerInvariant()
    $caps = @()

    if ($g -match '\b(scan|inspect|find|search|evidence|inventory|status)\b') {
        $caps += "PROJECT_SCAN"
    }
    if ($g -match '\b(test|tests|playmode|editmode|verify|validate|compile|build)\b') {
        $caps += "UNITY_TEST"
        if ($g -match '\bcompile|build\b') { $caps += "UNITY_COMPILE" }
    }
    if ($g -match '\b(git|commit|branch|diff|history|repository)\b') {
        $caps += "GIT_SAFETY"
    }
    if ($g -match '\b(file|files|rename|move|copy|patch|edit|modify|update)\b') {
        $caps += "FILE_PATCHING"
    }
    if ($g -match '\b(implement|implementation|code|script|class|method|logic|refactor|generate|fix|develop|development|feature|mechanic|reconcil\w*|plan|planning|design|authoring|remediation|p1-\d+|p0-\d+|task)\b') {
        $caps += "CODE_REASONING"
        $caps += "CODE_GENERATION"
    }

    if ($caps.Count -eq 0) {
        # Unknown work is not assumed deterministic.
        $caps = @("PROJECT_SCAN","CODE_REASONING")
    }

    return @($caps | Select-Object -Unique)
}

function Invoke-AstraCTODecision {
    param(
        [Parameter(Mandatory)][string]$Goal,
        [Parameter(Mandatory)][array]$RequiredCapabilities,
        [Parameter(Mandatory)][array]$ProviderMatrix
    )

    # ASTRA owns strategic intent and routing constraints. It is never an executor.
    $semanticPaths = @($ProviderMatrix | Where-Object {
        $_.name -like "AGY/*" -and
        $_.status -eq "AVAILABLE" -and
        [bool]$_.quota_verified -and
        [int]$_.usable_capacity -gt 0
    } | ForEach-Object { $_.name })

    $deterministicPaths = @($ProviderMatrix | Where-Object {
        $_.name -in @("PowerShell","Git","UnityCLI","ReleaseGate","Orchestrator") -and
        $_.status -eq "AVAILABLE"
    } | ForEach-Object { $_.name })

    [pscustomobject]@{
        authority = "ASTRA/CTO"
        deputy_authority = "ChatGPT/Deputy-CTO"
        deputy_role = @("SECOND_OPINION","RISK_REVIEW","TASK_DECOMPOSITION","CAPABILITY_ROUTING_REVIEW")
        authority_order = @("SAFETY_CONSTITUTION","ASTRA/CTO","ChatGPT/Deputy-CTO","EXECUTOR")
        goal = $Goal
        required_capabilities = @($RequiredCapabilities)
        semantic_paths = @($semanticPaths)
        deterministic_paths = @($deterministicPaths)
        safety = "SAFETY_CONSTITUTION_MANDATORY"
        executor_can_declare_project_complete = $false
        deputy_can_override_cto = $false
        deputy_can_override_safety = $false
        decision_status = "CTO_DECISION_READY"
    }
}

function Invoke-ChatGPTDeputyCTOReview {
    param(
        [Parameter(Mandatory)][string]$Goal,
        [Parameter(Mandatory)][array]$RequiredCapabilities,
        [Parameter(Mandatory)][array]$ProviderMatrix,
        [Parameter(Mandatory)]$CTODecision
    )

    # Deputy CTO is advisory/cross-check only. It cannot override safety,
    # CTO authority, validation, or acceptance. Provider selection is still
    # capability-first and must use an AVAILABLE provider with usable capacity.
    $blockedPaths = @($ProviderMatrix | Where-Object {
        $_.status -in @("QUOTA_EXHAUSTED","MISSING","UNAVAILABLE","FAILED","COOLDOWN","AVAILABLE_UNVERIFIED") -or
        ($_.kind -eq "SEMANTIC" -and -not [bool]$_.quota_verified)
    } | ForEach-Object { $_.name })

    $required = @($RequiredCapabilities)
    $selected = $null
    foreach ($cap in $required) {
        $candidates = @(Get-ProvidersForCapability -Providers $ProviderMatrix -Capability $cap)
        if ($candidates.Count -gt 0) {
            $selected = $candidates[0]
            break
        }
    }

    if ($null -eq $selected) {
        return [pscustomobject]@{
            authority = "ChatGPT/Deputy-CTO"
            parent_authority = "ASTRA/CTO"
            role = "SECOND_OPINION"
            goal = $Goal
            required_capabilities = @($RequiredCapabilities)
            blocked_paths = @($blockedPaths)
            selected_provider = ""
            selected_model = ""
            safety = "SAFETY_CONSTITUTION_MANDATORY"
            may_override_cto = $false
            may_override_safety = $false
            may_execute = $false
            may_declare_project_complete = $false
            review_status = "DEPUTY_REVIEW_BLOCKED_NO_AVAILABLE_CAPABILITY"
        }
    }

    [pscustomobject]@{
        authority = "ChatGPT/Deputy-CTO"
        parent_authority = "ASTRA/CTO"
        role = "SECOND_OPINION"
        goal = $Goal
        required_capabilities = @($RequiredCapabilities)
        blocked_paths = @($blockedPaths)
        selected_provider = [string]$selected.name
        selected_model = [string]$selected.model_id
        safety = "SAFETY_CONSTITUTION_MANDATORY"
        may_override_cto = $false
        may_override_safety = $false
        may_execute = $false
        may_declare_project_complete = $false
        review_status = "DEPUTY_REVIEW_READY"
    }
}

function Select-CapabilityRoute {
    param([object[]]$Providers, [string[]]$RequiredCapabilities)

    $required = @($RequiredCapabilities)
    $routes = @()

    # First choose by capability, not provider identity.
    foreach ($cap in $required) {
        $matches = @(Get-ProvidersForCapability -Providers $Providers -Capability $cap)
        if ($matches.Count -gt 0) {
            $routes += [PSCustomObject]@{
                capability = $cap
                providers = @($matches)
                selected = $matches[0].name
            }
        } else {
            $routes += [PSCustomObject]@{
                capability = $cap
                providers = @()
                selected = ""
            }
        }
    }

    $missing = @($routes | Where-Object { [string]::IsNullOrWhiteSpace($_.selected) })
    $deterministic = @($routes | Where-Object {
        $_.selected -in @("PowerShell","Git","UnityCLI","ReleaseGate","Orchestrator")
    })

    [PSCustomObject]@{
        required = $required
        routes = $routes
        missing = @($missing | ForEach-Object { $_.capability })
        deterministic_available = ($deterministic.Count -gt 0)
        semantic_available = (@($routes | Where-Object {
            $_.selected -and $_.selected -notin @("PowerShell","Git","UnityCLI","ReleaseGate","Orchestrator")
        }).Count -gt 0)
    }
}

function Test-ObjectiveBoundary {
    param(
        [string]$GoalText,
        [string]$ExecutorText,
        $Summary
    )

    # Executor claims never establish completion. Boundary evidence comes only
    # from independent release/validation state or an explicit controller state.
    if ($Summary -and $Summary.project_state -eq "PROJECT_COMPLETE") { return $true }
    if ($Summary -and $Summary.project_state -eq "OBJECTIVE_COMPLETE") { return $true }

    return $false
}

function Write-CoreDecisionEvidence {
    param(
        [string]$SessionDir,
        [int]$Cycle,
        [string]$RootGoal,
        $CapacityProfile,
        $RouteDecision,
        $ChunkProfile
    )

    $path = Join-Path $SessionDir ("core-decision-{0:D3}.json" -f $Cycle)
    [ordered]@{
        schema = "soul-hunter.autopilot.core-decision.v1"
        version = "2.2.24"
        generated_at = (Get-Date -Format o)
        root_goal = $RootGoal
        routing = "CAPABILITY_FIRST"
        required_capabilities = @($RouteDecision.required)
        missing_capabilities = @($RouteDecision.missing)
        deterministic_available = [bool]$RouteDecision.deterministic_available
        semantic_available = [bool]$RouteDecision.semantic_available
        reserve_percent = $CapacityReservePercent
        chunk = $ChunkProfile
        capacity = $CapacityProfile
    } | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $path -Encoding UTF8
    return $path
}

function New-ChunkPrompt {
    param([string]$RootGoal, $ChunkProfile, [string]$EvidenceBundle)

    $safety = Get-SafetyConstitutionText

    $limits = switch ($ChunkProfile.Size) {
        "TINY" { "Perform exactly one minimal file-level change. Prefer one file; never exceed $($ChunkProfile.MaxFiles) newly changed file(s)." }
        "SMALL" { "Perform exactly one small coherent change. Never exceed $($ChunkProfile.MaxFiles) newly changed file(s)." }
        "MEDIUM" { "Perform one coherent requirement slice. Never exceed $($ChunkProfile.MaxFiles) newly changed file(s)." }
        "LARGE" { "Perform one bounded subsystem slice. Never exceed $($ChunkProfile.MaxFiles) newly changed file(s)." }
        default { "Do not modify production code; deterministic evidence collection only." }
    }

    return @"
ROOT GOAL:
$RootGoal

AUTOPILOT v2.2.4 CHUNK CONTRACT:
- Capacity profile: $($ChunkProfile.Size)
- $limits
- Inspect the deterministic evidence bundle first: $EvidenceBundle
- Re-scan only the files necessary to verify the current slice.
- Do not broaden the objective.
- Preserve unrelated existing user changes.
- Do not use destructive git reset/clean/checkout.
- Do not force-kill Unity.
- If the slice cannot be completed safely, return a precise blocker instead of guessing.
- Finish this slice with concrete evidence, changed-file list, and validation result.

$safety
"@
}

function Get-ExecutionStderrClassification {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return "NONE" }
    if ($Text -match '(?im)^(?:warning:|git\\.exe\\s*: warning:|warning:.*LF will be replaced by CRLF)') {
        return "NON_FATAL_GIT_WARNING"
    }
    return "ERROR"
}


function Invoke-PowerShellScriptCaptured {
    param(
        [string]$ScriptPath,
        [string]$TaskText,
        [string]$StdoutPath,
        [string]$StderrPath,
        [string]$ProviderSnapshotJson = ""
    )

    $normalizedTask = $TaskText -replace '[\u2018\u2019\u201A\u201B\u0091\u0092\u009D]', "'" -replace '[\u2013\u2014\u2015]', '--'
    $escapedTask = $normalizedTask.Replace("'", "''")
    $escapedPath = $ScriptPath.Replace("'", "''")
    $escapedSnapshot = ([string]$ProviderSnapshotJson).Replace("'", "''")
    $snapshotArg = if ([string]::IsNullOrWhiteSpace($escapedSnapshot)) { "" } else { " -ProviderSnapshotJson '$escapedSnapshot'" }
    $command = "& '$escapedPath' -Task '$escapedTask'$snapshotArg; exit `$LASTEXITCODE"
    $bytes = [Text.Encoding]::Unicode.GetBytes($command)
    $encoded = [Convert]::ToBase64String($bytes)

    $proc = Start-Process -FilePath "powershell.exe" `
        -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-EncodedCommand", $encoded) `
        -Wait -PassThru -NoNewWindow `
        -RedirectStandardOutput $StdoutPath `
        -RedirectStandardError $StderrPath

    if (Test-Path $StdoutPath) { Get-Content $StdoutPath | ForEach-Object { Write-Host $_ } }
    if (Test-Path $StderrPath) { Get-Content $StderrPath | ForEach-Object { Write-Host $_ } }
    return $proc.ExitCode
}

function Invoke-AstraAdapterCaptured {
    param(
        [string]$TaskText,
        [string]$EvidenceBundle,
        [string]$StdoutPath,
        [string]$StderrPath
    )

    if ([string]::IsNullOrWhiteSpace($AstraAdapterPath) -or !(Test-Path $AstraAdapterPath)) {
        return 127
    }

    # Adapter contract: a PowerShell script accepting -Task, -Canonical,
    # -EvidenceBundle. It owns its isolated workspace/change application rules.
    $escapedTask = $TaskText.Replace("'", "''")
    $escapedAdapter = $AstraAdapterPath.Replace("'", "''")
    $escapedCanonical = $Canonical.Replace("'", "''")
    $escapedEvidence = $EvidenceBundle.Replace("'", "''")
    $command = "& '$escapedAdapter' -Task '$escapedTask' -Canonical '$escapedCanonical' -EvidenceBundle '$escapedEvidence'; exit `$LASTEXITCODE"
    $bytes = [Text.Encoding]::Unicode.GetBytes($command)
    $encoded = [Convert]::ToBase64String($bytes)

    $proc = Start-Process -FilePath "powershell.exe" `
        -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-EncodedCommand", $encoded) `
        -Wait -PassThru -NoNewWindow `
        -RedirectStandardOutput $StdoutPath `
        -RedirectStandardError $StderrPath

    if (Test-Path $StdoutPath) { Get-Content $StdoutPath | ForEach-Object { Write-Host $_ } }
    if (Test-Path $StderrPath) { Get-Content $StderrPath | ForEach-Object { Write-Host $_ } }
    return $proc.ExitCode
}

function Classify-ProviderFailure {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return "UNKNOWN" }
    if ($Text -match '(?is)(usage limit|quota exhausted|rate limit|too many requests|try again at)') { return "PROVIDER_QUOTA_EXHAUSTED" }
    if ($Text -match '(?is)(unauthorized|authentication failed|not logged in|login required|invalid api key)') { return "PROVIDER_AUTH_FAILED" }
    if ($Text -match '(?is)(not found|is not recognized|missing executable)') { return "TOOL_MISSING" }
    if ($Text -match '(?is)(SCANNER_FAILED|backend .* failed)') { return "PROVIDER_FAILED" }
    return "UNKNOWN"
}

function Update-ProvidersFromFailureText {
    param($Providers, [string]$Text)
    $classification = Classify-ProviderFailure $Text
    if ($classification -notin @("PROVIDER_QUOTA_EXHAUSTED","PROVIDER_AUTH_FAILED","PROVIDER_FAILED")) {
        return @{ Providers=$Providers; Classification=$classification }
    }

    foreach ($p in $Providers) {
        if ($p.kind -ne "SEMANTIC") { continue }

        $mentioned = $false
        if ($p.name -eq "Codex" -and $Text -match '(?is)(codex|Backend\s+codex)') { $mentioned = $true }
        if ($p.name -like "AGY/*" -and $Text -match '(?is)(agy|antigravity|Backend\s+agy)') {
            $mentioned = $true
            if ($Text -match '(?is)gemini') { $mentioned = ($p.name -eq "AGY/Gemini") }
            elseif ($Text -match '(?is)claude') { $mentioned = ($p.name -eq "AGY/Claude") }
            elseif ($Text -match '(?is)\bgpt\b|openai') { $mentioned = ($p.name -eq "AGY/GPT") }
        }
        if ($p.name -eq "ASTRA/CTO" -and $Text -match '(?is)(astra)') { $mentioned = $true }

        if ($mentioned) {
            if ($classification -eq "PROVIDER_QUOTA_EXHAUSTED") { $p.status = "QUOTA_EXHAUSTED" }
            elseif ($classification -eq "PROVIDER_AUTH_FAILED") { $p.status = "AUTH_FAILED" }
            else { $p.status = "FAILED" }
            $p.raw_capacity = 0
            $p.usable_capacity = 0
            $p.reason = $classification
        }
    }
    return @{ Providers=$Providers; Classification=$classification }
}

function Invoke-ReleaseGate {
    param([string]$SessionDir, [int]$Cycle)
    $out = Join-Path $SessionDir ("release-gate-{0:D3}.stdout.txt" -f $Cycle)
    $err = Join-Path $SessionDir ("release-gate-{0:D3}.stderr.txt" -f $Cycle)

    $proc = Start-Process -FilePath "powershell.exe" `
        -ArgumentList @("-ExecutionPolicy", "Bypass", "-NoProfile", "-File", "`"$ReleaseGatePath`"") `
        -Wait -PassThru -NoNewWindow `
        -RedirectStandardOutput $out `
        -RedirectStandardError $err

    if (Test-Path $out) { Get-Content $out | ForEach-Object { Write-Host $_ } }
    if (Test-Path $err) { Get-Content $err | ForEach-Object { Write-Host $_ } }
    return $proc.ExitCode
}

function Get-LatestReleaseArtifacts {
    $root = Join-Path $AutomationRoot "release-gate-runs"
    if (!(Test-Path $root)) { return $null }
    $latest = Get-ChildItem $root -Directory -ErrorAction SilentlyContinue |
        Sort-Object CreationTime -Descending |
        Select-Object -First 1
    if (!$latest) { return $null }

    return [ordered]@{
        run = $latest
        summary = Join-Path $latest.FullName "RELEASE_SUMMARY.json"
        blockers = Join-Path $latest.FullName "BLOCKERS.json"
        diagnostic = Join-Path $latest.FullName "UNITY_DIAGNOSTIC.txt"
    }
}

function Get-PlayModeDiagnosticEvidence {
    param(
        [Parameter(Mandatory=$true)][string]$RunDirectory
    )

    $resultPath = Join-Path $RunDirectory "playmode-results.xml"
    $logPath = Join-Path $RunDirectory "playmode-tests.log"
    $items = New-Object System.Collections.Generic.List[string]

    $items.Add("PLAYMODE DIAGNOSTIC EVIDENCE")
    $items.Add("Run: $RunDirectory")

    if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
        try {
            [xml]$xml = Get-Content -LiteralPath $resultPath -Raw -ErrorAction Stop
            $root = $xml.'test-run'
            if ($root) {
                $items.Add("Result: $([string]$root.result)")
                $items.Add("Total: $([string]$root.total) Passed: $([string]$root.passed) Failed: $([string]$root.failed)")

                $failedCases = @($root.SelectNodes('//test-case') | Where-Object {
                    [string]$_.result -eq 'Failed' -or [string]$_.result -eq 'Error' -or [string]$_.result -eq 'Skipped'
                })

                if ($failedCases.Count -gt 0) {
                    $items.Add("Affected test cases:")
                    foreach ($tc in $failedCases | Select-Object -First 20) {
                        $name = [string]$tc.name
                        $result = [string]$tc.result
                        # NUnit XML nodes must be read via InnerText; casting an XmlElement
                        # directly to [string] produces the literal "System.Xml.XmlElement".
                        $msg = $null
                        $stack = $null
                        $failureNode = $tc.SelectSingleNode('./failure')
                        if ($failureNode) {
                            $messageNode = $failureNode.SelectSingleNode('./message')
                            $stackNode = $failureNode.SelectSingleNode('./stack-trace')
                            if ($messageNode) { $msg = $messageNode.InnerText }
                            if ($stackNode) { $stack = $stackNode.InnerText }
                            if ([string]::IsNullOrWhiteSpace($msg)) { $msg = $failureNode.InnerText }
                        }
                        if ([string]::IsNullOrWhiteSpace($msg)) {
                            $messageNode = $tc.SelectSingleNode('./message')
                            if ($messageNode) { $msg = $messageNode.InnerText }
                        }
                        if ([string]::IsNullOrWhiteSpace($msg)) { $msg = [string]$tc.message }
                        $items.Add("- [$result] $name")
                        if (-not [string]::IsNullOrWhiteSpace($msg)) { $items.Add("  message: $msg") }
                        if (-not [string]::IsNullOrWhiteSpace($stack)) {
                            $firstStack = ($stack -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 8) -join " | "
                            if ($firstStack) { $items.Add("  stack: $firstStack") }
                        }
                    }
                }
                else {
                    $items.Add("No failed/error test-case nodes were found in the XML.")
                }
            }
        }
        catch {
            $items.Add("Result XML unreadable: $($_.Exception.Message)")
        }
    }
    else {
        $items.Add("PlayMode result XML missing: $resultPath")
    }

    if (Test-Path -LiteralPath $logPath -PathType Leaf) {
        $logLines = @(Get-Content -LiteralPath $logPath -ErrorAction SilentlyContinue)
        $interesting = @($logLines | Where-Object {
            $_ -match '(?i)failed|failure|assert|exception|error|stack trace|test case'
        } | Select-Object -Last 40)
        if ($interesting.Count -gt 0) {
            $items.Add("Relevant PlayMode log evidence (last 40 matches):")
            foreach ($line in $interesting) { $items.Add([string]$line) }
        }
    }

    return ($items -join [Environment]::NewLine)
}

function Get-PlayModeRootCauseEvidence {
    param(
        [Parameter(Mandatory=$true)][string]$RunDirectory,
        [Parameter(Mandatory=$true)][string]$ProjectPath,
        [Parameter(Mandatory=$true)][string]$DiagnosticEvidence
    )

    $items = New-Object System.Collections.Generic.List[string]
    $items.Add("PLAYMODE ROOT-CAUSE EVIDENCE REQUEST")
    $items.Add("Rule: collect system evidence only; no canonical modification.")

    $testName = $null
    $testFile = $null
    $testLine = $null
    if ($DiagnosticEvidence -match '(?m)^- \[Failed\] (.+)$') { $testName = $matches[1].Trim() }
    if ($DiagnosticEvidence -match '(?m)^\s*stack:.*? in (.+?\.cs):(\d+)\s*\|') {
        $testFile = $matches[1].Trim()
        $testLine = [int]$matches[2]
    }

    if ([string]::IsNullOrWhiteSpace($testName)) {
        $items.Add("BLOCKED: failing test name unavailable.")
        return ($items -join [Environment]::NewLine)
    }

    $items.Add("Failing test: $testName")
    if ($testFile) { $items.Add("Failure source: ${testFile}:$testLine") }

    $sources = @()
    if ($testFile -and (Test-Path -LiteralPath $testFile -PathType Leaf)) { $sources += $testFile }
    $leaf = if ($testFile) { Split-Path -Leaf $testFile } else { $null }
    if ($leaf -and (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
        $sources += @(Get-ChildItem -LiteralPath $ProjectPath -Recurse -File -Filter $leaf -ErrorAction SilentlyContinue | Select-Object -First 3 -ExpandProperty FullName)
    }
    $sources = @($sources | Select-Object -Unique | Select-Object -First 3)

    $testSourceText = $null
    foreach ($src in $sources) {
        try {
            $lines = @(Get-Content -LiteralPath $src -ErrorAction Stop)
            $text = ($lines -join [Environment]::NewLine)
            if (-not $testSourceText) { $testSourceText = $text }
            $start = 1
            $end = [Math]::Min($lines.Count, 120)
            if ($testLine) {
                $start = [Math]::Max(1, $testLine - 20)
                $end = [Math]::Min($lines.Count, $testLine + 30)
            }
            $items.Add("Source context: $src [$start-$end]")
            for ($i=$start; $i -le $end; $i++) { $items.Add(("{0,5}: {1}" -f $i,$lines[$i-1])) }
        } catch {
            $items.Add("Source read failed: $src :: $($_.Exception.Message)")
        }
    }

    # Evidence-driven symbol extraction: trace only concrete symbols used by the failing test.
    $symbols = New-Object System.Collections.Generic.List[string]
    if ($testSourceText) {
        foreach ($m in [regex]::Matches($testSourceText, '\b(?:LevelProgressionManager|WanderingMerchant|CampaignSignatureSystem|StartStage|ForgottenVillage|WanderingMerchant)\b')) {
            if (-not $symbols.Contains($m.Value)) { $symbols.Add($m.Value) }
        }
    }
    if ($symbols.Count -eq 0) { $symbols.Add($testName) }

    $items.Add("Targeted project evidence for failing test symbols:")
    $items.Add("Symbols: $($symbols -join ', ')")
    try {
        $assetRoot = Join-Path $ProjectPath 'Assets'
        if (-not (Test-Path -LiteralPath $assetRoot -PathType Container)) {
            $items.Add("BLOCKED: Assets directory unavailable: $assetRoot")
        } else {
            foreach ($symbol in $symbols) {
                $items.Add("--- references: $symbol ---")
                $matches = @(Get-ChildItem -LiteralPath $assetRoot -Recurse -File -Include *.cs,*.unity,*.prefab -ErrorAction SilentlyContinue |
                    Select-String -Pattern ([regex]::Escape($symbol)) -ErrorAction SilentlyContinue |
                    Select-Object -First 30)
                if ($matches.Count -eq 0) {
                    $items.Add("No references found.")
                } else {
                    foreach ($r in $matches) { $items.Add("$($r.Path):$($r.LineNumber): $($r.Line.Trim())") }
                }
            }
        }
    } catch { $items.Add("Targeted symbol scan failed: $($_.Exception.Message)") }

    $items.Add("Decision gate: evidence collection complete; root-cause fix is NOT authorized by this step.")
    return ($items -join [Environment]::NewLine)
}
function Get-ReleaseFingerprint {
    param($Summary, $Blockers, $GitSnapshot)
    $blockerStrings = @()
    foreach ($b in @($Blockers)) {
        if ($null -eq $b) { continue }
        $blockerStrings += "$($b.message)|$($b.actionable_by_orchestrator)"
    }

    $obj = [ordered]@{
        project_state = $Summary.project_state
        verdict = $Summary.verdict
        build = $Summary.unity_gate.report.buildResult
        edit_state = $Summary.tests.editmode.state
        edit_total = $Summary.tests.editmode.total
        edit_failed = $Summary.tests.editmode.failed
        play_state = $Summary.tests.playmode.state
        play_total = $Summary.tests.playmode.total
        play_failed = $Summary.tests.playmode.failed
        blockers = @($blockerStrings | Sort-Object)
        git_head = $GitSnapshot.head
        git_status = $GitSnapshot.status
        git_diff_names = $GitSnapshot.diff_names
    }
    return ($obj | ConvertTo-Json -Depth 10 -Compress)
}

function Save-Knowledge {
    param($Providers, $Summary, [string]$LastStatus, [string]$SessionId)
    $knowledge = [ordered]@{
        updated_at = (Get-Date -Format o)
        last_session = $SessionId
        last_status = $LastStatus
        provider_states = @($Providers | ForEach-Object {
            [ordered]@{
                name=$_.name; status=$_.status; usable_capacity=$_.usable_capacity;
                reset_at=$_.reset_at; reason=$_.reason
            }
        })
        last_release = if ($Summary) {
            [ordered]@{
                project_state=$Summary.project_state
                verdict=$Summary.verdict
                build=$Summary.unity_gate.report.buildResult
                editmode=$Summary.tests.editmode.state
                playmode=$Summary.tests.playmode.state
            }
        } else { $null }
    }
    Save-JsonFile $KnowledgePath $knowledge
}

function Run-SelfTest {
    Write-Host "Running Soul Hunter Autopilot v2.2.42 self-test..."
    $ok = $true

    # Core path preservation contract.
    if (!(Test-Path $Canonical)) { Write-Host "[WARN] Canonical missing on this machine" }
    if (!(Test-Path $OrchestratorPath)) { Write-Host "[WARN] Orchestrator missing on this machine" }
    if (!(Test-Path $ReleaseGatePath)) { Write-Host "[WARN] ReleaseGate missing on this machine" }

    # Provider constructor invariant: named binding must preserve array capabilities
    # and capacity reserve math. This protects the live provider matrix path.
    $ctor = New-ProviderRecord -Name "ASTRA_TEST" -Kind "SEMANTIC" -Status "AVAILABLE" -RawCapacity 80 -Capabilities @("PROJECT_SCAN","CODE_REASONING") -Reason "selftest"
    if ([int]$ctor.usable_capacity -ne 60 -or @($ctor.capabilities).Count -ne 2 -or @($ctor.capabilities) -notcontains "CODE_REASONING") {
        Write-Host "[FAIL] provider constructor binding test"
        Write-Host ("       record=" + ($ctor | ConvertTo-Json -Compress))
        $ok = $false
    } else { Write-Host "[PASS] provider constructor binding test" }

    # Router invariant: a dead provider must not imply every capability is dead.
    # Build records directly so this router test cannot be invalidated by
    # PowerShell positional binding rules for array-valued parameters.
    $synthetic = @(
        [pscustomobject]@{ name="Codex"; kind="SEMANTIC"; status="QUOTA_EXHAUSTED"; raw_capacity=0; usable_capacity=0; quota_verified=$true; capabilities=@("CODE_REASONING"); reason="test"; path=""; reset_at=""; model_id="codex" },
        [pscustomobject]@{ name="AGY/Claude"; kind="SEMANTIC"; status="AVAILABLE"; raw_capacity=80; usable_capacity=60; quota_verified=$true; capabilities=@("CODE_REASONING"); reason="test"; path=""; reset_at=""; model_id="claude-sonnet-4-6" },
        [pscustomobject]@{ name="ASTRA"; kind="CTO"; status="AVAILABLE"; raw_capacity=80; usable_capacity=60; quota_verified=$false; capabilities=@("CODE_REASONING"); reason="test"; path=""; reset_at=""; model_id="" },
        [pscustomobject]@{ name="PowerShell"; kind="DETERMINISTIC"; status="AVAILABLE"; raw_capacity=100; usable_capacity=75; capabilities=@("PROJECT_SCAN","FILE_PATCHING"); reason="test"; path=""; reset_at="" },
        [pscustomobject]@{ name="UnityCLI"; kind="DETERMINISTIC"; status="AVAILABLE"; raw_capacity=100; usable_capacity=75; capabilities=@("UNITY_TEST"); reason="test"; path=""; reset_at="" }
    )
    $reasoners = @(Get-ProvidersForCapability -Providers $synthetic -Capability "CODE_REASONING" | Where-Object { $_.kind -eq "SEMANTIC" })
    if ($reasoners.Count -ne 1 -or $reasoners[0].name -ne "AGY/Claude") {
        Write-Host "[FAIL] capability failover test"
        Write-Host ("       reasoner-count=" + @($reasoners).Count + " names=" + ((@($reasoners) | ForEach-Object { $_.name }) -join ','))
        $ok = $false
    } else { Write-Host "[PASS] capability failover test" }

    # Quota safety invariant: semantic execution requires verified live quota.
    $quotaBlocked = [pscustomobject]@{ name="TEST-UNVERIFIED"; kind="SEMANTIC"; status="AVAILABLE"; raw_capacity=80; usable_capacity=60; quota_verified=$false; capabilities=@("CODE_REASONING") }
    $quotaAllowed = [pscustomobject]@{ name="TEST-VERIFIED"; kind="SEMANTIC"; status="AVAILABLE"; raw_capacity=80; usable_capacity=60; quota_verified=$true; capabilities=@("CODE_REASONING") }
    $quotaCandidates = @(Get-ProvidersForCapability -Providers @($quotaBlocked,$quotaAllowed) -Capability "CODE_REASONING")
    if($quotaCandidates.Count -eq 1 -and $quotaCandidates[0].name -eq "TEST-VERIFIED") {
        Write-Host "[PASS] unverified semantic quota is blocked"
    } else {
        Write-Host "[FAIL] unverified semantic quota is blocked"
        $ok=$false
    }

    # Agent register/state separation invariant. The manual register is never a runtime output.
    if ((Split-Path -Leaf $AgentRegisterPath) -eq "Agent-Register.json" -and (Split-Path -Leaf $AgentStatePath) -eq "Agent-State.json") {
        Write-Host "[PASS] manual Agent-Register / runtime Agent-State separation"
    } else {
        Write-Host "[FAIL] manual Agent-Register / runtime Agent-State separation"
        $ok = $false
    }
    $reporterText = if (Test-Path $QuotaReporterPath) { Get-Content $QuotaReporterPath -Raw } else { "" }
    if ($reporterText -and $reporterText -notmatch '(?m)Save-Json\s+\$RegisterPath\b|Save-Json\s+.*Agent-Register\.json') {
        Write-Host "[PASS] quota reporter does not write manual register"
    } else {
        Write-Host "[FAIL] quota reporter does not write manual register"
        $ok = $false
    }

    # Chunk shrinking invariant.
    $profile = Get-ChunkProfile -Providers $synthetic
    if ($profile.Size -eq "DETERMINISTIC_ONLY") {
        Write-Host "[FAIL] chunk capacity test"
        Write-Host ("       profile=" + ($profile | ConvertTo-Json -Compress))
        $ok = $false
    } else { Write-Host "[PASS] chunk capacity test -> $($profile.Size)" }

    # CLI simulation parser invariant: comma-separated values from -File must work.
    $oldSimulated = @($SimulateExhausted)
    $script:SimulateExhausted = @("Codex,AGY")
    $simOk = (Test-SimulatedExhaustion "Codex") -and (Test-SimulatedExhaustion "AGY") -and -not (Test-SimulatedExhaustion "ASTRA")
    $script:SimulateExhausted = $oldSimulated
    if (-not $simOk) {
        Write-Host "[FAIL] simulated exhaustion argument normalization"
        $ok = $false
    } else { Write-Host "[PASS] simulated exhaustion argument normalization" }

    # No destructive git commands in controller source.
    $self = Get-Content $PSCommandPath -Raw
    if ($self -match 'git\s+reset\s+--hard' -or $self -match 'git\s+clean\b' -or $self -match 'git\s+checkout\s+--') {
        Write-Host "[FAIL] destructive git command found"
        $ok = $false
    } else { Write-Host "[PASS] destructive git guard" }

    # Safety Constitution must be embedded in every semantic chunk contract.
    $safetyText = Get-SafetyConstitutionText
    $safetyMarkers = @(
        "DO NOT GUESS",
        "FAIL CLOSED",
        "Never weaken tests",
        "Release Gate / independent validation is authoritative",
        "Provider exhaustion may reduce speed or capability, but must never reduce safety"
    )
    foreach ($marker in $safetyMarkers) {
        if ($safetyText -notmatch [Regex]::Escape($marker)) {
            Write-Host "[FAIL] safety constitution marker missing: $marker"
            $ok = $false
        }
    }
    if ($ok) { Write-Host "[PASS] safety constitution markers" }

    $testChunk = New-ChunkPrompt -RootGoal "test goal" -ChunkProfile @{ Size="SMALL"; MaxFiles=2; Risk="LOW" } -EvidenceBundle "test-evidence.txt"
    if ($testChunk -notmatch "SAFETY CONSTITUTION" -or $testChunk -notmatch "FAIL CLOSED") {
        Write-Host "[FAIL] chunk safety propagation"
        $ok = $false
    } else { Write-Host "[PASS] chunk safety propagation" }

    # Runtime controller invariants must fail closed when violated.
    $runtimeViolations = @(Get-ControllerSafetyViolations)
    if ($runtimeViolations.Count -gt 0) {
        Write-Host "[FAIL] runtime safety invariant check"
        $runtimeViolations | ForEach-Object { Write-Host "       $_" }
        $ok = $false
    } else { Write-Host "[PASS] runtime safety invariant check" }

    # v2.2.20 capability-first routing invariant.
    $testRequiredCapabilities = @("CODE_REASONING")
    $ctoDecision = Invoke-AstraCTODecision -Goal "self-test goal" -RequiredCapabilities $testRequiredCapabilities -ProviderMatrix $synthetic
    $deputyCTOReview = Invoke-ChatGPTDeputyCTOReview -Goal "self-test goal" -RequiredCapabilities $testRequiredCapabilities -ProviderMatrix $synthetic -CTODecision $ctoDecision
    $route = Select-CapabilityRoute -Providers $synthetic -RequiredCapabilities $testRequiredCapabilities
    if ($route.semantic_available -and $route.missing.Count -eq 0) {
        Write-Host "[PASS] capability-first routing"
    } else {
        Write-Host "[FAIL] capability-first routing"
        $ok = $false
    }

    if ($ctoDecision.authority -eq "ASTRA/CTO" -and
        $ctoDecision.deputy_authority -eq "ChatGPT/Deputy-CTO" -and
        $ctoDecision.deputy_can_override_cto -eq $false -and
        $ctoDecision.deputy_can_override_safety -eq $false -and
        $deputyCTOReview.may_execute -eq $false -and
        $deputyCTOReview.may_declare_project_complete -eq $false) {
        Write-Host "[PASS] ASTRA CTO + ChatGPT Deputy CTO hierarchy"
    } else {
        Write-Host "[FAIL] ASTRA CTO + ChatGPT Deputy CTO hierarchy"
        $ok = $false
    }

    $detRoute = Select-CapabilityRoute -Providers $synthetic -RequiredCapabilities @("PROJECT_SCAN","UNITY_TEST")
    if ($detRoute.deterministic_available -and $detRoute.missing.Count -eq 0) {
        Write-Host "[PASS] deterministic capability routing"
    } else {
        Write-Host "[FAIL] deterministic capability routing"
        $ok = $false
    }

    $boundaryFalse = Test-ObjectiveBoundary -GoalText "test" -ExecutorText "PROJECT_COMPLETE" -Summary ([pscustomobject]@{project_state="IN_PROGRESS"})
    if (-not $boundaryFalse) {
        Write-Host "[PASS] executor cannot declare completion"
    } else {
        Write-Host "[FAIL] executor cannot declare completion"
        $ok = $false
    }

    # AGY model isolation: Gemini exhaustion must not exhaust Claude/GPT.
    $agyTestPath = Join-Path $StateRoot "agy-model-capacity-selftest.json"
    $agyTest = @{
        models = @(
            @{ model="Gemini"; status="QUOTA_EXHAUSTED"; raw_capacity=0; reason="selftest"; source="selftest" },
            @{ model="Claude"; status="AVAILABLE"; raw_capacity=100; reason="selftest"; source="selftest" },
            @{ model="GPT"; status="AVAILABLE"; raw_capacity=100; reason="selftest"; source="selftest" }
        )
    }
    $agyTest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $agyTestPath -Encoding UTF8
    $oldAgyState = $AgyCapacityStatePath
    $script:AgyCapacityStatePath = $agyTestPath
    $agyMatrix = Get-ProviderMatrix
    $geminiRow = $agyMatrix | Where-Object name -eq "AGY/Gemini" | Select-Object -First 1
    $claudeRow = $agyMatrix | Where-Object name -eq "AGY/Claude" | Select-Object -First 1
    $gptRow = $agyMatrix | Where-Object name -eq "AGY/GPT" | Select-Object -First 1
    if ($geminiRow.status -eq "QUOTA_EXHAUSTED" -and [int]$geminiRow.usable_capacity -eq 0 -and
        $claudeRow.status -eq "AVAILABLE" -and [int]$claudeRow.usable_capacity -eq 75 -and
        $gptRow.status -eq "AVAILABLE" -and [int]$gptRow.usable_capacity -eq 75) {
        Write-Host "[PASS] AGY model-level quota isolation"
    } else {
        Write-Host "[FAIL] AGY model-level quota isolation"
        $ok = $false
    }
    $script:AgyCapacityStatePath = $oldAgyState
    Remove-Item -LiteralPath $agyTestPath -Force -ErrorAction SilentlyContinue

    # System-evidence contract: when AGY is installed, its own `models`
    # command must prove the model families exposed to the router. No synthetic
    # native/default AGY row is allowed.
    if (Test-Path $AgyExe) {
        $inventory = @(Get-AGYModelInventory)
        $families = @($inventory | Select-Object -ExpandProperty family -Unique)
        if ($families -contains "Gemini" -and $families -contains "Claude" -and $families -contains "GPT") {
            Write-Host "[PASS] AGY CLI model inventory evidence"
        } else {
            Write-Host "[FAIL] AGY CLI model inventory evidence"
            Write-Host ("       families=" + ($families -join ","))
            $ok = $false
        }
    } else {
        Write-Host "[WARN] AGY CLI model inventory evidence skipped: executable missing"
    }

    # Safety: ASTRA without a bridge must not be counted independently.
    $oldAdapter = $AstraAdapterPath
    $script:AstraAdapterPath = ""
    $matrix = Get-ProviderMatrix
    # ASTRA is the CTO/core handler. The old "no bridge = unavailable" invariant
    # is obsolete; an adapter is optional and must not gate CTO authority.
    $astra = $matrix | Where-Object name -eq "ASTRA/CTO" | Select-Object -First 1
    if ($astra -and $astra.status -eq "AVAILABLE" -and $astra.kind -eq "CTO") {
        Write-Host "[PASS] ASTRA CTO availability guard"
    } else {
        Write-Host "[FAIL] ASTRA CTO availability guard"
        $ok = $false
    }
    $script:AstraAdapterPath = $oldAdapter

    if ($ok) { Write-FinalStatus "SELF_TEST_PASS"; exit 0 }
    Write-FinalStatus "SELF_TEST_FAIL"
    exit 1
}

if ($SelfTest) { Run-SelfTest }

# -----------------------------------------------------------------------------
# Session initialization / resume
# -----------------------------------------------------------------------------
$SessionId = "AUTO2-" + (Get-Date -Format "yyyyMMdd-HHmmss")
$SessionDir = ""
$StatePath = ""
$State = $null

if ($Resume) {
    $latestSession = Get-ChildItem $SessionsDir -Directory -ErrorAction SilentlyContinue |
        Sort-Object CreationTime -Descending |
        Select-Object -First 1
    if ($latestSession) {
        $SessionId = $latestSession.Name
        $SessionDir = $latestSession.FullName
        $StatePath = Join-Path $SessionDir "state.json"
        $State = Read-JsonFile $StatePath
        Write-Host "Resuming session $SessionId"
    }
}

if ([string]::IsNullOrWhiteSpace($SessionDir)) {
    $SessionDir = Join-Path $SessionsDir $SessionId
    Ensure-Directory $SessionDir
    $StatePath = Join-Path $SessionDir "state.json"
}

$LogPath = Join-Path $SessionDir "autopilot.log"
function Write-Log {
    param([string]$Message)
    "[$(Get-Date -Format o)] $Message" | Out-File $LogPath -Append -Encoding UTF8
}

# -----------------------------------------------------------------------------
# Preflight
# -----------------------------------------------------------------------------
Write-Log "Starting v2.2.42 preflight"
try {
    Assert-ControllerSafetyInvariants
} catch {
    Write-Log "Safety preflight failed: $($_.Exception.Message)"
    Write-FinalStatus "SAFETY_CONSTITUTION_BLOCKED"
    exit 1
}
if (!(Test-Path $Canonical)) { Write-FinalStatus "EXTERNAL_BLOCKED_CANONICAL_MISSING"; exit 1 }
if (!(Test-Path $ReleaseGatePath)) { Write-FinalStatus "CONTROLLER_FAILED_RELEASE_GATE_MISSING"; exit 1 }
if (!(Test-Path $UnityExe)) { Write-FinalStatus "EXTERNAL_BLOCKED_UNITY_MISSING"; exit 1 }
if (!(Get-Command git -ErrorAction SilentlyContinue)) { Write-FinalStatus "EXTERNAL_BLOCKED_GIT_MISSING"; exit 1 }

$null = Check-DiskSpace
$Providers = Get-ProviderMatrix -FreshRuntime

# Preflight hierarchy decision must exist before Write-FinalStatus is called.
# This is a local controller decision only; it does not invoke any provider.
$ctoDecision = Invoke-AstraCTODecision `
    -Goal "preflight hierarchy verification" `
    -RequiredCapabilities @("PROJECT_SCAN","CODE_REASONING") `
    -ProviderMatrix $Providers

Show-ProviderMatrix $Providers
Save-JsonFile $ProviderStatePath @($Providers)

if ($PreflightOnly) {
    $profile = Get-ChunkProfile -Providers $Providers
    Write-Host ""
    Write-Host "Recommended chunk size : $($profile.Size)"
    Write-Host "Max new files/chunk    : $($profile.MaxFiles)"
    Write-FinalStatus "PREFLIGHT_COMPLETE"
    exit 0
}

if ($Status) {
    $artifacts = Get-LatestReleaseArtifacts
    if ($artifacts -and (Test-Path $artifacts.summary)) {
        $summary = Get-Content $artifacts.summary -Raw | ConvertFrom-Json
        $profile = Get-ChunkProfile -Providers $Providers
        Write-ConsoleStatus -SessionId "STATUS-ONLY" -Cycle 0 `
            -ReleaseState $summary.project_state `
            -BuildState $summary.unity_gate.report.buildResult `
            -EditMode "$($summary.tests.editmode.state) ($($summary.tests.editmode.passed)/$($summary.tests.editmode.total))" `
            -PlayMode "$($summary.tests.playmode.state) ($($summary.tests.playmode.passed)/$($summary.tests.playmode.total))" `
            -Action "NONE" -GoalText "" -Route "STATUS" -ChunkSize $profile.Size
    }
    Write-FinalStatus "STATUS_COMPLETE"
    exit 0
}

$Cycle = 0
$NoProgressCount = 0
$LastFingerprint = ""
$AttemptedRoutes = @{}

if ($State) {
    if ($State.cycle) { $Cycle = [int]$State.cycle }
    if ($State.no_progress_count) { $NoProgressCount = [int]$State.no_progress_count }
    if ($State.last_fingerprint) { $LastFingerprint = [string]$State.last_fingerprint }
    if ($State.attempted_routes) {
        foreach ($prop in $State.attempted_routes.PSObject.Properties) {
            $AttemptedRoutes[$prop.Name] = @($prop.Value)
        }
    }
}

# -----------------------------------------------------------------------------
# Main controller loop
# -----------------------------------------------------------------------------
while ($Cycle -lt $MaxCycles) {
    $Cycle++
    Write-Log "Cycle $Cycle starting"
    try {
        Assert-ControllerSafetyInvariants
    } catch {
        Write-Log "Safety invariant failed during cycle: $($_.Exception.Message)"
        Write-FinalStatus "SAFETY_CONSTITUTION_BLOCKED"
        exit 1
    }
    $null = Check-DiskSpace

    # Capacity refresh at every major objective boundary.
    $Providers = Get-ProviderMatrix -FreshRuntime
    $ChunkProfile = Get-ChunkProfile -Providers $Providers
    Show-ProviderMatrix $Providers

    # Full Release Gate is reserved for objective/project boundaries.
    # A caller-supplied read-only/inspection goal is NOT a release boundary.
    # Use existing release evidence for inspection and diagnosis instead of
    # spending a full gate run.
    $goalTextForBoundary = ([string]$Goal).Trim()
    $readOnlyGoal = (
        $goalTextForBoundary -match '(?im)^\s*(read[- ]?only|inspection[- ]?only|diagnostic[- ]?only)\b' -or
        $goalTextForBoundary -match '(?is)^\s*(inspect|diagnose|identify|audit|review|analy[sz]e)\b.{0,300}\b(without modifying|without changing|do not modify|do not change)\b' -or
        $goalTextForBoundary -match '(?is)^\s*(do not modify|do not change)\b.{0,300}\b(inspect|diagnose|identify|audit|review|analy[sz]e|report)\b'
    )
    $runFullReleaseGate = ($Cycle -eq 1 -and [string]::IsNullOrWhiteSpace($goalTextForBoundary))
    if ($State -and $State.next_stage -eq "RELEASE_VERIFY") {
        $runFullReleaseGate = $true
    }
    if ($readOnlyGoal -and -not ($State -and $State.next_stage -eq "RELEASE_VERIFY")) {
        $runFullReleaseGate = $false
    }

    if ($runFullReleaseGate) {
        Write-Log "Running Full Release Gate at boundary."
        $rgExit = Invoke-ReleaseGate -SessionDir $SessionDir -Cycle $Cycle
        Write-Log "Release Gate exit code: $rgExit"
        if ($State -and $State.next_stage -eq "RELEASE_VERIFY") {
            $State.next_stage = $null
        }
    } else {
        if ($readOnlyGoal) {
            Write-Log "Skipping Full Release Gate: explicit read-only/inspection goal."
        } else {
            Write-Log "Skipping Full Release Gate: ordinary chunk cycle."
        }
    }

    $artifacts = Get-LatestReleaseArtifacts
    if (!$artifacts -or !(Test-Path $artifacts.summary)) {
        Save-Knowledge $Providers $null "CONTROLLER_FAILED" $SessionId
        Write-FinalStatus "CONTROLLER_FAILED_NO_RELEASE_SUMMARY"
        exit 1
    }

    $summary = Get-Content $artifacts.summary -Raw | ConvertFrom-Json
    $blockers = @()
    if (Test-Path $artifacts.blockers) {
        $rawBlockers = Get-Content $artifacts.blockers -Raw
        if (![string]::IsNullOrWhiteSpace($rawBlockers)) {
            $blockers = @($rawBlockers | ConvertFrom-Json)
        }
    }

    # Release Gate remains final authority when no custom goal is supplied.
    if ([string]::IsNullOrWhiteSpace($Goal) -and $summary.project_state -eq "PROJECT_COMPLETE") {
        Write-ConsoleStatus -SessionId $SessionId -Cycle $Cycle `
            -ReleaseState $summary.project_state `
            -BuildState $summary.unity_gate.report.buildResult `
            -EditMode "$($summary.tests.editmode.state)" `
            -PlayMode "$($summary.tests.playmode.state)" `
            -Action "DONE" -GoalText "" -Route "RELEASE_GATE" -ChunkSize $ChunkProfile.Size
        Save-Knowledge $Providers $summary "PROJECT_COMPLETE" $SessionId
        Write-FinalStatus "PROJECT_COMPLETE"
        exit 0
    }

    $gitBefore = Get-GitSnapshot
    $fingerprint = Get-ReleaseFingerprint -Summary $summary -Blockers $blockers -GitSnapshot $gitBefore

    if ($fingerprint -eq $LastFingerprint) {
        $NoProgressCount++
    } else {
        $NoProgressCount = 0
        $LastFingerprint = $fingerprint
    }

    $decision = Get-RootGoal -summary $summary -blockers $blockers -latestRunName $artifacts.run.Name
    if ($decision.Status -eq "EXTERNAL_BLOCKED") {
        Write-ConsoleStatus -SessionId $SessionId -Cycle $Cycle `
            -ReleaseState $summary.project_state `
            -BuildState $summary.unity_gate.report.buildResult `
            -EditMode "$($summary.tests.editmode.state)" `
            -PlayMode "$($summary.tests.playmode.state)" `
            -Action "EXTERNAL_BLOCKED" -GoalText "" -Route "NONE" -ChunkSize $ChunkProfile.Size
        Save-Knowledge $Providers $summary "EXTERNAL_BLOCKED" $SessionId
        Write-FinalStatus "EXTERNAL_BLOCKED"
        exit 1
    }

    $rootGoal = [string]$decision.Goal

    # Planner stage: cheap deterministic evidence first.
    $evidenceBundle = New-EvidenceBundle -SessionDir $SessionDir -Cycle $Cycle -GoalText $rootGoal -SummaryPath $artifacts.summary -BlockersPath $artifacts.blockers
    Write-Log "Evidence bundle: $evidenceBundle"

    $requiredCapabilities = @(Get-RequiredCapabilitiesForGoal $rootGoal)

    # RUNTIME CAPACITY AUTHORITY ------------------------------------------------
    # Startup Preflight is only an initial snapshot. Re-read provider/quota
    # evidence immediately before every routing/execution decision so the
    # controller does not make a chunk decision from stale capacity data.
    $Providers = Get-ProviderMatrix -FreshRuntime
    # Selection-time quota authority: every semantic candidate is checked one-by-one
    # immediately before routing. A failed/unverified probe is never executable.
    $Providers = Refresh-QuotaBeforeSemanticSelection -Providers $Providers -RequiredCapabilities $requiredCapabilities
    $script:Providers = $Providers
    Save-JsonFile $ProviderStatePath @($Providers)
    Write-Log "RUNTIME PROVIDER REFRESH: before routing/execution decision; selection-time quota checks completed."
    if (Test-Path $AgentStatePath) { Write-Log "Agent-State.json updated by quota reporter: $AgentStatePath" }
    Write-Host "RUNTIME PROVIDER REFRESH: before routing/execution decision"
    Show-ProviderMatrix $Providers
    $ChunkProfile = Get-ChunkProfile -Providers $Providers

    # Strategic authority is evaluated on every real controller cycle, not only
    # during self-test. ASTRA/CTO owns the strategic decision; ChatGPT/Deputy-CTO
    # is advisory and cannot override CTO or safety.
    $ctoDecision = Invoke-AstraCTODecision `
        -Goal $rootGoal `
        -RequiredCapabilities $requiredCapabilities `
        -ProviderMatrix $Providers
    $deputyCTOReview = Invoke-ChatGPTDeputyCTOReview `
        -Goal $rootGoal `
        -RequiredCapabilities $requiredCapabilities `
        -ProviderMatrix $Providers `
        -CTODecision $ctoDecision

    $routeDecision = Select-CapabilityRoute -Providers $Providers -RequiredCapabilities $requiredCapabilities

    # Explicit read-only goals are evidence/reporting tasks. Never enter the
    # executor path, even when semantic providers are available.
    # Classify the requested operation, not incidental safety constraints.
    # Example: "Do not modify the canonical project directly" inside an
    # implementation goal is a safety boundary, not read-only intent.
    $explicitReadOnlyIntent = (
        $rootGoal -match '(?im)^\s*(read[- ]?only|inspection[- ]?only|diagnostic[- ]?only)\b' -or
        $rootGoal -match '(?is)^\s*(inspect|diagnose|identify|audit|review|analy[sz]e)\b.{0,300}\b(without modifying|without changing|do not modify|do not change)\b' -or
        $rootGoal -match '(?is)^\s*(do not modify|do not change)\b.{0,300}\b(inspect|diagnose|identify|audit|review|analy[sz]e|report)\b'
    )
    $readOnlyGoal = [bool]$explicitReadOnlyIntent
    $diagnosticFirst = ($decision.Status -eq "DIAGNOSTIC_FIRST")
    if ($readOnlyGoal -or $diagnosticFirst) {
        $diagnosticAction = if ($readOnlyGoal) { "READ_ONLY_REPORT" } else { "DIAGNOSTIC_FIRST" }
        $diagnosticRoute = if ($readOnlyGoal) { "EVIDENCE_ONLY" } else { "DIAGNOSTIC_EVIDENCE" }

        if ($diagnosticFirst) {
            $playEvidence = Get-PlayModeDiagnosticEvidence -RunDirectory $artifacts.run.FullName
            $rootCauseEvidence = Get-PlayModeRootCauseEvidence -RunDirectory $artifacts.run.FullName -ProjectPath $Canonical -DiagnosticEvidence $playEvidence
            $playEvidencePath = Join-Path $SessionDir ("playmode-diagnostic-{0:D3}.txt" -f $Cycle)
            [System.IO.File]::WriteAllText($playEvidencePath, ($playEvidence + [Environment]::NewLine + [Environment]::NewLine + $rootCauseEvidence), (New-Object System.Text.UTF8Encoding($false)))
            Write-Log "PlayMode diagnostic evidence: $playEvidencePath"
            Write-Host $playEvidence
            Write-Host $rootCauseEvidence
        }

        Write-ConsoleStatus -SessionId $SessionId -Cycle $Cycle `
            -ReleaseState $summary.project_state `
            -BuildState $summary.unity_gate.report.buildResult `
            -EditMode "$($summary.tests.editmode.state)" `
            -PlayMode "$($summary.tests.playmode.state)" `
            -Action $diagnosticAction `
            -GoalText $rootGoal `
            -Route $diagnosticRoute `
            -ChunkSize $ChunkProfile.Size
        Save-Knowledge $Providers $summary $diagnosticAction $SessionId
        Write-FinalStatus $diagnosticAction
        exit 0
    }

    $coreDecisionEvidence = Write-CoreDecisionEvidence -SessionDir $SessionDir -Cycle $Cycle `
        -RootGoal $rootGoal -CapacityProfile $ChunkProfile -RouteDecision $routeDecision -ChunkProfile $ChunkProfile

    $chunkPrompt = New-ChunkPrompt -RootGoal $rootGoal -ChunkProfile $ChunkProfile -EvidenceBundle $evidenceBundle
    $chunkPrompt += @"

CAPABILITY ROUTING CONTRACT:
- Required capabilities: $($requiredCapabilities -join ', ')
- Route by capability, not by provider identity.
- Deterministic capabilities must prefer PowerShell/Git/UnityCLI.
- Semantic capability may use any currently viable semantic adapter.
- If a required capability is unavailable, do not guess; shrink, reroute, or BLOCK.
- Planner, executor, and verifier are separate roles.
- Executor output never establishes PROJECT_COMPLETE.
- Full Release Gate is reserved for objective/project boundaries.
"@
    $semanticProviders = @(Get-IndependentSemanticProviders -Providers $Providers)

    $routeKey = if ($semanticProviders.Count -gt 0) { ($semanticProviders | ForEach-Object { $_.name }) -join "+" } else { "DETERMINISTIC_ONLY" }
    if (!$AttemptedRoutes.ContainsKey($fingerprint)) { $AttemptedRoutes[$fingerprint] = @() }

    # Smart stall: same project state alone is not terminal. Stall only after
    # repeated no-progress AND every currently viable route for this fingerprint
    # has already been attempted.
    $routeAlreadyTried = $AttemptedRoutes[$fingerprint] -contains $routeKey
    if ($NoProgressCount -ge $MaxNoProgress -and $routeAlreadyTried) {
        Write-ConsoleStatus -SessionId $SessionId -Cycle $Cycle `
            -ReleaseState $summary.project_state `
            -BuildState $summary.unity_gate.report.buildResult `
            -EditMode "$($summary.tests.editmode.state)" `
            -PlayMode "$($summary.tests.playmode.state)" `
            -Action "STALL_AFTER_ALL_CURRENT_ROUTES" -GoalText $rootGoal -Route $routeKey -ChunkSize $ChunkProfile.Size
        Save-Knowledge $Providers $summary "AUTOPILOT_STALLED" $SessionId
        Write-FinalStatus "AUTOPILOT_STALLED"
        exit 1
    }

    $AttemptedRoutes[$fingerprint] += $routeKey

    Write-ConsoleStatus -SessionId $SessionId -Cycle $Cycle `
        -ReleaseState $summary.project_state `
        -BuildState $summary.unity_gate.report.buildResult `
        -EditMode "$($summary.tests.editmode.state)" `
        -PlayMode "$($summary.tests.playmode.state)" `
        -Action "EXECUTE_CHUNK" -GoalText $rootGoal -Route $routeKey -ChunkSize $ChunkProfile.Size

    $executorUsed = ""
    $execExit = 0
    $execText = ""

    # Executor stage ----------------------------------------------------------
    # Capability-first: only use semantic execution when the requested
    # capability cannot be safely satisfied deterministically.
    $deterministicOnlyTask = (
        @($requiredCapabilities | Where-Object {
            $_ -in @("PROJECT_SCAN","UNITY_COMPILE","UNITY_TEST","PLAYER_BUILD","GIT_SAFETY")
        }).Count -eq $requiredCapabilities.Count -and
        "FILE_PATCHING" -notin $requiredCapabilities -and
        "CODE_REASONING" -notin $requiredCapabilities -and
        "CODE_GENERATION" -notin $requiredCapabilities
    )
    $legacySemantic = @($semanticProviders | Where-Object { $_.name -eq "Codex" -or $_.name -like "AGY/*" })
    $independentAstra = @($semanticProviders | Where-Object { $_.name -eq "ASTRA/CTO" })

    if ($deterministicOnlyTask) {
        # Do not fabricate file patches here. Deterministic tools are used for
        # deterministic evidence/validation; actual semantic implementation
        # remains behind the isolated orchestrator contract.
        $executorUsed = "DETERMINISTIC_EVIDENCE"
        $execText = "Capability-first deterministic route selected. Evidence bundle=$evidenceBundle"
        $execExit = 0
    } elseif ($legacySemantic.Count -gt 0 -and (Test-Path $OrchestratorPath)) {
        $executorUsed = "ORCHESTRATOR"
        $out = Join-Path $SessionDir ("executor-{0:D3}.stdout.txt" -f $Cycle)
        $err = Join-Path $SessionDir ("executor-{0:D3}.stderr.txt" -f $Cycle)
        $providerSnapshotJson = $Providers | ConvertTo-Json -Depth 12 -Compress
        $execExit = Invoke-PowerShellScriptCaptured -ScriptPath $OrchestratorPath -TaskText $chunkPrompt -StdoutPath $out -StderrPath $err -ProviderSnapshotJson $providerSnapshotJson
        if (Test-Path $out) { $execText += Get-Content $out -Raw }
        if (Test-Path $err) {
            $stderrText = Get-Content $err -Raw
            if ((Get-ExecutionStderrClassification $stderrText) -eq "NON_FATAL_GIT_WARNING") {
                Write-Log "Executor emitted a non-fatal Git warning; preserving evidence without classifying execution as failed."
            } else {
                $execText += "`n" + $stderrText
            }
        }
    } elseif ($independentAstra.Count -gt 0) {
        $executorUsed = "ASTRA_ADAPTER"
        $out = Join-Path $SessionDir ("astra-{0:D3}.stdout.txt" -f $Cycle)
        $err = Join-Path $SessionDir ("astra-{0:D3}.stderr.txt" -f $Cycle)
        $execExit = Invoke-AstraAdapterCaptured -TaskText $chunkPrompt -EvidenceBundle $evidenceBundle -StdoutPath $out -StderrPath $err
        if (Test-Path $out) { $execText += Get-Content $out -Raw }
        if (Test-Path $err) { $execText += "`n" + (Get-Content $err -Raw) }
    } else {
        # We still used PowerShell/Git/ReleaseGate/Unity to gather and validate
        # everything deterministic. Nontrivial code reasoning cannot be safely
        # fabricated by PowerShell. Checkpoint instead of misclassifying this as
        # a scanner/project failure.
        $executorUsed = "DETERMINISTIC_ONLY"
        Write-Log "No independent semantic capability remains. Evidence collected and state checkpointed."

        $stateObj = [ordered]@{
            session_id = $SessionId
            cycle = $Cycle
            no_progress_count = $NoProgressCount
            last_fingerprint = $LastFingerprint
            goal = $rootGoal
            evidence_bundle = $evidenceBundle
            waiting_for = "CODE_REASONING"
            providers = @($Providers)
            attempted_routes = $AttemptedRoutes
            updated_at = (Get-Date -Format o)
        }
        Save-JsonFile $StatePath $stateObj
        Save-Knowledge $Providers $summary "WAITING_FOR_SEMANTIC_CAPABILITY" $SessionId

        if ($Mode -eq "MaximumAutonomy") {
            Write-Host "No semantic provider currently available. MaximumAutonomy will recheck in $ProviderRecheckMinutes minute(s)."
            Start-Sleep -Seconds ([math]::Max(60, $ProviderRecheckMinutes * 60))
            continue
        }

        Write-ConsoleStatus -SessionId $SessionId -Cycle $Cycle `
            -ReleaseState $summary.project_state `
            -BuildState $summary.unity_gate.report.buildResult `
            -EditMode "$($summary.tests.editmode.state)" `
            -PlayMode "$($summary.tests.playmode.state)" `
            -Action "RESUME_WHEN_SEMANTIC_CAPABILITY_RETURNS" -GoalText $rootGoal -Route "DETERMINISTIC_ONLY" -ChunkSize $ChunkProfile.Size
        Write-FinalStatus "WAITING_FOR_SEMANTIC_CAPABILITY"
        exit 0
    }

    Write-Log "Executor=$executorUsed exit=$execExit"

    # Provider failures are reclassified as routing events, not project failure.
    $update = Update-ProvidersFromFailureText -Providers $Providers -Text $execText
    $Providers = $update.Providers
    $failureClass = $update.Classification
    if ($failureClass -in @("PROVIDER_QUOTA_EXHAUSTED","PROVIDER_AUTH_FAILED","PROVIDER_FAILED")) {
        Write-Log "Executor provider event classified as $failureClass; refreshing routes."

        # A provider failure can change the live routing decision immediately.
        # Re-read the observed provider/quota state before selecting the next
        # route; never continue with the failed chunk's stale snapshot.
        $Providers = Get-ProviderMatrix -FreshRuntime
        $Providers = Refresh-QuotaBeforeSemanticSelection -Providers $Providers -RequiredCapabilities $requiredCapabilities
        $script:Providers = $Providers
        Save-JsonFile $ProviderStatePath @($Providers)
        Write-Log "RUNTIME PROVIDER REFRESH: after provider failure ($failureClass)."
        Write-Host "RUNTIME PROVIDER REFRESH: after provider failure ($failureClass)"
        Show-ProviderMatrix $Providers

        # Persist now so -Resume never loses the current objective.
        $stateObj = [ordered]@{
            session_id = $SessionId
            cycle = $Cycle
            no_progress_count = $NoProgressCount
            last_fingerprint = $LastFingerprint
            goal = $rootGoal
            evidence_bundle = $evidenceBundle
            last_provider_event = $failureClass
            providers = @($Providers)
            attempted_routes = $AttemptedRoutes
            updated_at = (Get-Date -Format o)
        }
        Save-JsonFile $StatePath $stateObj
        $State = $stateObj

        # Do NOT immediately stop. Next loop refreshes capability matrix and can
        # choose another provider or deterministic-only mode.
        continue
    }

    # Unknown/non-provider executor failures are safety-significant. Do not
    # continue optimistically or call the chunk complete. Preserve state and fail closed.
    if ($execExit -ne 0) {
        $stateObj = [ordered]@{
            session_id = $SessionId
            cycle = $Cycle
            no_progress_count = $NoProgressCount
            last_fingerprint = $LastFingerprint
            goal = $rootGoal
            evidence_bundle = $evidenceBundle
            executor = $executorUsed
            executor_exit = $execExit
            safety_status = "UNVERIFIED_EXECUTOR_FAILURE"
            providers = @($Providers)
            attempted_routes = $AttemptedRoutes
            updated_at = (Get-Date -Format o)
        }
        Save-JsonFile $StatePath $stateObj
        Save-Knowledge $Providers $summary "EXECUTOR_FAILED_CLOSED" $SessionId
        Write-Log "Executor failed without a safe provider-failover classification. Failing closed."
        Write-FinalStatus "EXECUTOR_FAILED_CLOSED"
        exit 1
    }

    # Blast-radius guard ------------------------------------------------------
    $gitAfter = Get-GitSnapshot
    $newPaths = Get-NewChangedPaths -Before $gitBefore -After $gitAfter

    # Governance is owner-controlled. Ordinary implementation chunks may not
    # silently modify it, regardless of provider or fallback route.
    $protectedPaths = @($newPaths | Where-Object { $_ -ieq "AGENTS.md" })
    if ($protectedPaths.Count -gt 0) {
        Write-Log "PROTECTED_CHANGE_DETECTED: $($protectedPaths -join ', ')"
        Save-Knowledge $Providers $summary "SAFETY_PROTECTED_CHANGE" $SessionId
        Write-Host "Protected owner-governance change detected:"
        $protectedPaths | ForEach-Object { Write-Host " - $_" }
        Write-FinalStatus "SAFETY_PROTECTED_CHANGE"
        exit 1
    }

    if ($ChunkProfile.MaxFiles -gt 0 -and $newPaths.Count -gt $ChunkProfile.MaxFiles) {
        Write-Log "CHANGE_SCOPE_EXCEEDED: $($newPaths.Count) new files > budget $($ChunkProfile.MaxFiles). Paths=$($newPaths -join ', ')"
        # No destructive rollback here. The underlying orchestrator must retain
        # ownership of isolated-workspace rollback. We stop before compounding.
        Save-Knowledge $Providers $summary "CHANGE_SCOPE_EXCEEDED" $SessionId
        Write-Host "New changed files:"
        $newPaths | ForEach-Object { Write-Host " - $_" }
        Write-FinalStatus "CHANGE_SCOPE_EXCEEDED"
        exit 1
    }

    # Verifier stage ----------------------------------------------------------
    # Do not trust executor success. Return to Release Gate on the next cycle.
    # Persist checkpoint first so crash/restart resumes from verified evidence.
    $stateObj = [ordered]@{
        session_id = $SessionId
        cycle = $Cycle
        no_progress_count = $NoProgressCount
        last_fingerprint = $LastFingerprint
        goal = $rootGoal
        evidence_bundle = $evidenceBundle
        executor = $executorUsed
        executor_exit = $execExit
        new_changed_paths = @($newPaths)
        providers = @($Providers)
        attempted_routes = $AttemptedRoutes
        next_stage = "RELEASE_VERIFY"
        verification_required = $true
        objective_completion_authority = "INDEPENDENT_RELEASE_GATE"
        safety_constitution_version = $SafetyConstitutionVersion
        safety_status = "PENDING_INDEPENDENT_VERIFICATION"
        updated_at = (Get-Date -Format o)
    }
    Save-JsonFile $StatePath $stateObj
    $State = $stateObj
    Save-Knowledge $Providers $summary "CHUNK_EXECUTED" $SessionId

    Write-Log "Chunk completed; verifier is independent Release Gate on next cycle."
}

Save-Knowledge $Providers $null "MAX_CYCLES_REACHED" $SessionId
Write-FinalStatus "MAX_CYCLES_REACHED"
exit 1

