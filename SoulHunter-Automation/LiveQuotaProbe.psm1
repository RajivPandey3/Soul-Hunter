# Shared read-only live quota probe module v2.2.42
function ConvertTo-PercentRemaining {
    param([object]$Value)
    if ($null -eq $Value) { return $null }
    try {
        $n = [double]$Value
        if ($n -ge 0 -and $n -le 1) { $n = $n * 100.0 }
        if ($n -lt 0) { $n = 0 }
        if ($n -gt 100) { $n = 100 }
        return [math]::Round($n,2)
    } catch { return $null }
}

function Invoke-ReadOnlyJsonCommand {
    param(
        [string]$FileName,
        [string[]]$Arguments,
        [int]$TimeoutSeconds = 15
    )
    $result = [ordered]@{ Success=$false; Output=''; Error=''; ExitCode=-1; Command=$FileName }
    if ([string]::IsNullOrWhiteSpace($FileName)) { return [pscustomobject]$result }
    try {
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = $FileName
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $escaped = foreach ($arg in @($Arguments)) {
            $a = [string]$arg
            if ($a -match '[\s"]') { '"' + ($a -replace '(\*)"','$1$1\"' -replace '(\+)$','$1$1') + '"' } else { $a }
        }
        $psi.Arguments = ($escaped -join ' ')
        $proc = [System.Diagnostics.Process]::new()
        $proc.StartInfo = $psi
        [void]$proc.Start()
        $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
        $stderrTask = $proc.StandardError.ReadToEndAsync()
        if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
            try { $proc.Kill() } catch {}
            $result.Error = "read-only quota probe timed out"
            $result.ExitCode = 124
            return [pscustomobject]$result
        }
        $result.Output = $stdoutTask.Result
        $result.Error = $stderrTask.Result
        $result.ExitCode = $proc.ExitCode
        $result.Success = ($proc.ExitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace($result.Output))
    } catch {
        $result.Error = $_.Exception.Message
    }
    return [pscustomobject]$result
}

function Find-QuotaProbeCommand {
    param([string[]]$Names)
    foreach ($name in @($Names)) {
        $cmd = Get-Command $name -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($cmd -and $cmd.Source) { return [string]$cmd.Source }
    }
    return ''
}

function Get-AGYLocalServerCandidates {
    $rows = @()
    try {
        $procs = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
            $cmd = [string]$_.CommandLine
            $name = [string]$_.Name
            ($name -match '(?i)(agy|antigravity|language_server)') -or ($cmd -match '(?i)language_server')
        })
        foreach($p in $procs) {
            $processId = [int]$p.ProcessId
            $csrf = ''
            $cmd = [string]$p.CommandLine
            if($cmd -match '(?i)--csrf[_-]?token(?:=|\s+)"?(?<t>[^"\s]+)') { $csrf = [string]$Matches.t }
            elseif($cmd -match '(?i)csrfToken(?:=|\s+)"?(?<t>[^"\s]+)') { $csrf = [string]$Matches.t }
            try {
                $conns = @(Get-NetTCPConnection -State Listen -OwningProcess $processId -ErrorAction SilentlyContinue)
                foreach($c in $conns) {
                    if($c.LocalAddress -in @('127.0.0.1','::1','0.0.0.0','::')) {
                        $port=[int]$c.LocalPort
                        if($port -gt 0 -and -not ($rows | Where-Object port -eq $port)) {
                            $rows += [pscustomobject]@{ Port=$port; ProcessId=$processId; Process=$p.Name; CsrfToken=$csrf }
                        }
                    }
                }
            } catch {}
        }
    } catch {}
    return @($rows | Sort-Object Process,Port)
}

function Invoke-AGYConnectRpc {
    param([int]$Port,[string]$Rpc,[string]$CsrfToken,[int]$TimeoutSec=6)
    $body = (@{ metadata = @{ ideName='antigravity'; extensionName='antigravity'; ideVersion='unknown'; locale='en' } } | ConvertTo-Json -Compress)
    $headers=@{'Connect-Protocol-Version'='1';'Content-Type'='application/json'}
    if(-not [string]::IsNullOrWhiteSpace($CsrfToken)) { $headers['X-Codeium-Csrf-Token']=$CsrfToken }
    $paths=@(
        "https://127.0.0.1:$Port/$Rpc",
        "http://127.0.0.1:$Port/$Rpc"
    )
    foreach($uri in $paths) {
        try {
            if($uri.StartsWith('https://')) {
                try {
                    $old=[System.Net.ServicePointManager]::ServerCertificateValidationCallback
                    [System.Net.ServicePointManager]::ServerCertificateValidationCallback={ $true }
                    $resp=Invoke-WebRequest -UseBasicParsing -Uri $uri -Method Post -Headers $headers -Body $body -TimeoutSec $TimeoutSec -ErrorAction Stop
                } finally { [System.Net.ServicePointManager]::ServerCertificateValidationCallback=$old }
            } else {
                $resp=Invoke-WebRequest -UseBasicParsing -Uri $uri -Method Post -Headers $headers -Body $body -TimeoutSec $TimeoutSec -ErrorAction Stop
            }
            return [pscustomobject]@{Success=$true;Uri=$uri;Json=($resp.Content|ConvertFrom-Json -ErrorAction Stop);Error=''}
        } catch {}
    }
    return [pscustomobject]@{Success=$false;Uri='';Json=$null;Error='All AGY Connect RPC HTTPS/HTTP attempts failed.'}
}

function Get-AGYQuotaGroupRows {
    param([object]$Json,[string]$Source,[string]$FetchedAt)
    $groups=$null
    if($Json.response -and $Json.response.groups){$groups=@($Json.response.groups)}
    elseif($Json.groups){$groups=@($Json.groups)}
    if($null -eq $groups){return @()}
    $gem5=$null;$gemW=$null;$tp5=$null;$tpW=$null
    foreach($g in $groups){
        $gname=[string]$g.displayName
        foreach($b in @($g.buckets)){
            $id=[string]$b.bucketId;$win=[string]$b.window
            $rf=$null
            if($null -ne $b.remainingFraction){try{$rf=[double]$b.remainingFraction}catch{}}
            elseif($b.remaining -and $null -ne $b.remaining.remainingFraction){try{$rf=[double]$b.remaining.remainingFraction}catch{}}
            elseif($b.remaining -and $null -ne $b.remaining.remaining_fraction){try{$rf=[double]$b.remaining.remaining_fraction}catch{}}
            if($null -eq $rf -or $rf -lt 0 -or $rf -gt 1){continue}
            $row=[pscustomobject]@{RemainingPercent=[math]::Round($rf*100,2);ResetTime=[string]$b.resetTime;BucketId=$id;Window=$win}
            $isWeekly=($id -match 'weekly$' -or $win -eq 'weekly')
            $isGemini=($id -match '^gemini' -or $gname -match 'Gemini')
            if($isGemini){if($isWeekly){$gemW=$row}else{$gem5=$row}}
            elseif($id -match '^3p' -or $gname -match 'Claude|GPT') {if($isWeekly){$tpW=$row}else{$tp5=$row}}
        }
    }
    $rows=@()
    if($gem5 -or $gemW){
        $wins=@($gem5,$gemW)|Where-Object {$_ -ne $null};$rem=($wins|Measure-Object RemainingPercent -Minimum).Minimum
        $reset=($wins|Where-Object ResetTime|Sort-Object ResetTime|Select-Object -First 1).ResetTime
        $five=$null; $weekly=$null; if($gem5){$five=$gem5.RemainingPercent}; if($gemW){$weekly=$gemW.RemainingPercent}
        $rows += [pscustomobject]@{Family='Gemini';ModelId='';RemainingPercent=[double]$rem;FiveHour=$five;Weekly=$weekly;ResetTime=[string]$reset;QuotaScope='shared_pool';PoolId='AGY-GEMINI';Source=$Source;FetchedAt=$FetchedAt}
    }
    if($tp5 -or $tpW){
        $wins=@($tp5,$tpW)|Where-Object {$_ -ne $null};$rem=($wins|Measure-Object RemainingPercent -Minimum).Minimum
        $reset=($wins|Where-Object ResetTime|Sort-Object ResetTime|Select-Object -First 1).ResetTime
        $five=$null; $weekly=$null; if($tp5){$five=$tp5.RemainingPercent}; if($tpW){$weekly=$tpW.RemainingPercent}
        $rows += [pscustomobject]@{Family='ThirdParty';ModelId='';RemainingPercent=[double]$rem;FiveHour=$five;Weekly=$weekly;ResetTime=[string]$reset;QuotaScope='shared_pool';PoolId='AGY-CLAUDE-GPT';Source=$Source;FetchedAt=$FetchedAt}
    }
    return @($rows)
}

function Get-LiveAGYQuotaEvidence {
    # Authoritative AGY local quota source: RetrieveUserQuotaSummary.
    # Discover the running language-server port and CSRF token, then call the
    # Connect RPC read-only. Never invent quota when the source cannot answer.
    $rpc = 'exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary'
    $candidates = @(Get-AGYLocalServerCandidates)

    foreach ($candidate in $candidates) {
        $port = [int]$candidate.Port
        $token = [string]$candidate.CsrfToken
        $rpcResult = Invoke-AGYConnectRpc -Port $port -Rpc $rpc -CsrfToken $token

        if (-not $rpcResult.Success) { continue }

        $fetched = (Get-Date).ToUniversalTime().ToString('o')
        $source = ('AGY local RPC {0}' -f $rpcResult.Uri)
        $rows = @(Get-AGYQuotaGroupRows -Json $rpcResult.Json -Source $source -FetchedAt $fetched)

        if ($rows.Count -eq 0) { continue }

        $models = @()
        foreach ($row in $rows) {
            $models += [pscustomobject]@{
                family            = [string]$row.Family
                model_id          = [string]$row.ModelId
                remaining_percent = $row.RemainingPercent
                five_hour        = $row.FiveHour
                weekly            = $row.Weekly
                reset_time       = [string]$row.ResetTime
                quota_scope      = [string]$row.QuotaScope
                pool_id          = [string]$row.PoolId
                source           = [string]$row.Source
                fetched_at       = [string]$row.FetchedAt
            }
        }

        return [pscustomobject]@{
            Success   = $true
            Source    = $source
            Models    = @($models)
            Providers = @()
            FetchedAt = $fetched
            Error     = ''
        }
    }

    # Optional secondary machine-readable probe. It is never installed or
    # fabricated by Autopilot; absence means UNVERIFIED and therefore 0 usable.
    $probe = Find-QuotaProbeCommand @('agy-usage.exe','agy-usage.cmd','agy-usage','agy-quota.cmd','agy-quota')
    if (-not [string]::IsNullOrWhiteSpace($probe)) {
        $probeName = [System.IO.Path]::GetFileNameWithoutExtension($probe)
        $arguments = @('refresh','json')
        if ($probeName -like 'agy-quota*') { $arguments = @('--json') }

        $probeResult = Invoke-ReadOnlyJsonCommand -FileName $probe -Arguments $arguments
        if ($probeResult.Success) {
            try {
                $obj = $probeResult.Output | ConvertFrom-Json -ErrorAction Stop
                $models = @()
                foreach ($provider in @($obj.providers)) {
                    foreach ($model in @($provider.models)) {
                        $pct = ConvertTo-PercentRemaining $model.remaining_percent
                        if ($null -eq $pct) {
                            $pct = ConvertTo-PercentRemaining $provider.pool_remaining_percent
                        }
                        if ($null -eq $pct -or [string]::IsNullOrWhiteSpace([string]$model.model_id)) { continue }

                        $reset = [string]$provider.pool_reset_time
                        if ($model.reset_time) { $reset = [string]$model.reset_time }

                        $models += [pscustomobject]@{
                            model_id          = [string]$model.model_id
                            remaining_percent = $pct
                            reset_time        = $reset
                            quota_scope       = 'shared_pool'
                            pool_id           = [string]$provider.provider
                            source            = $probe
                            fetched_at        = (Get-Date).ToUniversalTime().ToString('o')
                        }
                    }
                }
                if ($models.Count -gt 0) {
                    return [pscustomobject]@{
                        Success   = $true
                        Source    = $probe
                        Models    = @($models)
                        Providers = @($obj.providers)
                        FetchedAt = (Get-Date).ToUniversalTime().ToString('o')
                        Error     = ''
                    }
                }
            } catch { }
        }
    }

    return [pscustomobject]@{
        Success   = $false
        Source    = ''
        Models    = @()
        Providers = @()
        FetchedAt = ''
        Error     = 'No live AGY quota source answered RetrieveUserQuotaSummary.'
    }
}

function Get-LiveCodexQuotaEvidence {
    param([string]$CodexExecutable)
    # Codex app-server exposes account/rateLimits/read without starting a model
    # turn. This is the authoritative read-only runtime quota path when ChatGPT
    # authentication is available to the installed Codex CLI.
    if ([string]::IsNullOrWhiteSpace($CodexExecutable) -or !(Test-Path $CodexExecutable)) {
        return [pscustomobject]@{ Success=$false; RemainingPercent=$null; ResetAt=''; FetchedAt=''; Error='Codex executable missing.' }
    }
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName=$CodexExecutable; $psi.Arguments='app-server --stdio'; $psi.UseShellExecute=$false
    $psi.CreateNoWindow=$true; $psi.RedirectStandardInput=$true; $psi.RedirectStandardOutput=$true; $psi.RedirectStandardError=$true
    try {
        $proc=[System.Diagnostics.Process]::new(); $proc.StartInfo=$psi; [void]$proc.Start()
        $init='{"jsonrpc":"2.0","method":"initialize","id":1,"params":{"clientInfo":{"name":"soul-hunter-quota-probe","title":"Soul Hunter Runtime Quota Probe","version":"2.2.21"}}}'
        $ready='{"jsonrpc":"2.0","method":"initialized","params":{}}'
        $req='{"jsonrpc":"2.0","method":"account/rateLimits/read","id":2,"params":{}}'
        $proc.StandardInput.WriteLine($init); $proc.StandardInput.WriteLine($ready); $proc.StandardInput.WriteLine($req); $proc.StandardInput.Close()
        $stdoutTask=$proc.StandardOutput.ReadToEndAsync(); $stderrTask=$proc.StandardError.ReadToEndAsync()
        if(-not $proc.WaitForExit(12000)){ try{$proc.Kill()}catch{}; return [pscustomobject]@{Success=$false;RemainingPercent=$null;ResetAt='';FetchedAt='';Error='Codex app-server quota probe timed out.'} }
        $out=$stdoutTask.Result; $err=$stderrTask.Result
        $response=$null
        foreach($line in ($out -split "`r?`n")) {
            if([string]::IsNullOrWhiteSpace($line)){continue}
            try { $o=$line|ConvertFrom-Json -ErrorAction Stop; if([string]$o.id -eq '2'){ $response=$o; break } } catch {}
        }
        if($null -eq $response -or $null -eq $response.result){
            return [pscustomobject]@{Success=$false;RemainingPercent=$null;ResetAt='';FetchedAt='';Error=('Codex rate-limit read returned no usable result. ' + $err).Trim()}
        }
        $rl=$response.result
        $candidates=@()
        if($rl.rateLimitsByLimitId -and $rl.rateLimitsByLimitId.codex){
            $cx=$rl.rateLimitsByLimitId.codex
            if($cx.primary){$candidates += $cx.primary}
            if($cx.secondary){$candidates += $cx.secondary}
        }
        if($rl.rateLimits){
            if($rl.rateLimits.primary){$candidates += $rl.rateLimits.primary}
            if($rl.rateLimits.secondary){$candidates += $rl.rateLimits.secondary}
        }
        $remaining=@()
        $resets=@()
        foreach($w in $candidates){
            if($null -ne $w.usedPercent){ $remaining += (100-[double]$w.usedPercent) }
            if($w.resetsAt){$resets += [DateTimeOffset]::FromUnixTimeSeconds([int64]$w.resetsAt).ToString('o')}
        }
        if($remaining.Count -eq 0){ return [pscustomobject]@{Success=$false;RemainingPercent=$null;ResetAt='';FetchedAt='';Error='Codex rate-limit response contained no usedPercent.'} }
        $pct=[math]::Round([math]::Max(0,[math]::Min(100,($remaining|Measure-Object -Minimum).Minimum)),2)
        $resetAt = ''
        if ($resets.Count) { $resetAt = ($resets | Sort-Object | Select-Object -First 1) }
        return [pscustomobject]@{Success=$true;RemainingPercent=$pct;ResetAt=$resetAt;FetchedAt=(Get-Date).ToUniversalTime().ToString('o');Error=''}
    } catch { return [pscustomobject]@{Success=$false;RemainingPercent=$null;ResetAt='';FetchedAt='';Error=('Codex quota probe failed: '+$_.Exception.Message)} }
}
Export-ModuleMember -Function ConvertTo-PercentRemaining,Invoke-ReadOnlyJsonCommand,Find-QuotaProbeCommand,Get-AGYLocalServerCandidates,Invoke-AGYConnectRpc,Get-AGYQuotaGroupRows,Get-LiveAGYQuotaEvidence,Get-LiveCodexQuotaEvidence
