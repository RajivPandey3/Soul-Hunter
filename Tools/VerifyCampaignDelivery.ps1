$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$deliveryRoot='D:\Downloads\Soul-Hunter-Assets\Campaign_Art_v01'
$baseline=Import-Csv (Join-Path $projectRoot 'Backups/AssetProduction/20260908-221112/manifest.csv')
$changed=@(foreach($item in $baseline){$path=Join-Path $projectRoot $item.Path;if(!(Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $item.SHA256){$item.Path}})
if($changed.Count){throw ('Baseline changed: '+($changed -join ', '))}
$rows=Get-Content (Join-Path $projectRoot 'ArtSource/SH10_v01/manifest.json') -Raw | ConvertFrom-Json
foreach($row in $rows){foreach($pair in @(@($row.source,'Blender'),@($row.model,'Models'))){$copy=Join-Path $deliveryRoot ($row.pack+'/'+$pair[1]+'/'+[IO.Path]::GetFileName($pair[0]));if((Get-FileHash -LiteralPath $copy).Hash -ne (Get-FileHash -LiteralPath $pair[0]).Hash){throw ('Copy mismatch: '+$copy)}}}
$exports=Get-Content (Join-Path $projectRoot 'Logs/SH10_exported.json') -Raw | ConvertFrom-Json
if($exports.Count -ne 11){throw 'Expected 11 packages'}
foreach($package in $exports){$entries= & tar -tf $package.output;if($LASTEXITCODE -ne 0 -or !($entries -match '/pathname$')){throw ('Invalid archive: '+$package.output)}}
$report=[pscustomobject]@{BaselineFilesUnchanged=$baseline.Count;VerifiedSourceCopies=($rows.Count*2);VerifiedUnityPackages=$exports.Count;GameplayTests='Deferred; art-only delivery'}
$reportPath=Join-Path $deliveryRoot 'Verification/DeliveryCheck.json'
if(Test-Path -LiteralPath $reportPath){throw 'Report already exists; refusing overwrite'}
$report | ConvertTo-Json | Set-Content -LiteralPath $reportPath -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $projectRoot 'Logs/SH10_exported.json') -Destination (Join-Path $deliveryRoot 'Verification/SH10_exported.json')
$hashRows=Get-ChildItem -LiteralPath $deliveryRoot -File -Recurse | Where-Object {$_.Name -ne 'SHA256.csv'} | ForEach-Object { [pscustomobject]@{Path=$_.FullName.Substring($deliveryRoot.Length+1);SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash;Bytes=$_.Length} }
$hashRows | Export-Csv -LiteralPath (Join-Path $deliveryRoot 'Verification/SHA256.csv') -NoTypeInformation
$report | ConvertTo-Json
