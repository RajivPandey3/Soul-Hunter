$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$backupRoot='D:\Unity Projects\Soul-Hunter_Backup_20260909'
if(Test-Path -LiteralPath $backupRoot){throw 'Current backup target already exists; refusing overwrite.'}
New-Item -ItemType Directory -Path $backupRoot | Out-Null
$manifest=foreach($folder in @('Assets','Packages','ProjectSettings')){
 $sourceRoot=Join-Path $projectRoot $folder
 foreach($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File){
  $relative=$file.FullName.Substring($projectRoot.Length+1);$target=Join-Path $backupRoot $relative
  New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
  Copy-Item -LiteralPath $file.FullName -Destination $target
  $sourceHash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
  $copyHash=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
  if($sourceHash -ne $copyHash){throw ('Backup mismatch: '+$relative)}
  [pscustomobject]@{Path=$relative;SHA256=$copyHash;Bytes=$file.Length}
 }
}
$manifest | Export-Csv -LiteralPath (Join-Path $backupRoot 'manifest.csv') -NoTypeInformation
[pscustomobject]@{Backup=$backupRoot;VerifiedFiles=$manifest.Count;Bytes=($manifest|Measure-Object Bytes -Sum).Sum} | ConvertTo-Json
