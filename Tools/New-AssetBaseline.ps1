$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$snapshotRoot = Join-Path $projectRoot ('Backups/AssetProduction/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
if (Test-Path -LiteralPath $snapshotRoot) { throw 'Snapshot already exists.' }
New-Item -ItemType Directory -Path $snapshotRoot | Out-Null
$manifest = foreach ($folder in @('Assets','Packages','ProjectSettings')) {
    $sourceRoot = Join-Path $projectRoot $folder
    foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File) {
        $relative = $file.FullName.Substring($projectRoot.Length + 1)
        $target = Join-Path $snapshotRoot $relative
        New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
        $before = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        Copy-Item -LiteralPath $file.FullName -Destination $target
        $after = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        if ($before -ne $after) { throw "Backup mismatch: $relative" }
        [pscustomobject]@{Path=$relative;SHA256=$after;Bytes=$file.Length}
    }
}
$manifest | Export-Csv -LiteralPath (Join-Path $snapshotRoot 'manifest.csv') -NoTypeInformation
[pscustomobject]@{Snapshot=$snapshotRoot;VerifiedFiles=$manifest.Count;Bytes=($manifest | Measure-Object Bytes -Sum).Sum} | ConvertTo-Json
