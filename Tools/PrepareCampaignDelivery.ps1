$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$deliveryRoot='D:\Downloads\Soul-Hunter-Assets\Campaign_Art_v01'
if(Test-Path -LiteralPath $deliveryRoot){throw 'Delivery version already exists; refusing overwrite.'}
$rows=Get-Content -LiteralPath (Join-Path $projectRoot 'ArtSource/SH10_v01/manifest.json') -Raw | ConvertFrom-Json
if($rows.Count -ne 121){throw 'Generation incomplete.'}
foreach($row in $rows){if(-not (Test-Path -LiteralPath $row.source) -or -not (Test-Path -LiteralPath $row.model)){throw ('Missing asset: '+$row.name)}}
New-Item -ItemType Directory -Path $deliveryRoot | Out-Null
foreach($row in $rows){
 $packRoot=Join-Path $deliveryRoot $row.pack
 foreach($category in @('Blender','Models')){New-Item -ItemType Directory -Force -Path (Join-Path $packRoot $category) | Out-Null}
 Copy-Item -LiteralPath $row.source -Destination (Join-Path (Join-Path $packRoot 'Blender') ([IO.Path]::GetFileName($row.source)))
 Copy-Item -LiteralPath $row.model -Destination (Join-Path (Join-Path $packRoot 'Models') ([IO.Path]::GetFileName($row.model)))
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Delivery/SH10_README.md') -Destination (Join-Path $deliveryRoot 'README.md')
$verifyRoot=Join-Path $deliveryRoot 'Verification';New-Item -ItemType Directory -Path $verifyRoot | Out-Null
foreach($file in @('ArtSource/SH10_v01/manifest.json','Logs/SH10_validation.json','Logs/SH10_import_progress.json','Assets/Documentation/Technical/AssetProductionAudit.md')){
 Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination (Join-Path $verifyRoot ([IO.Path]::GetFileName($file)))
}
$scenes=Get-Content -LiteralPath (Join-Path $projectRoot 'Logs/SH10_scenes.json') -Raw | ConvertFrom-Json
for($i=0;$i -lt $scenes.Count;$i++){
 $preview=Join-Path $projectRoot ('Logs/SH10_L'+($i+1).ToString('00')+'_Preview.png')
 Copy-Item -LiteralPath $preview -Destination (Join-Path (Join-Path $deliveryRoot $scenes[$i].pack) 'Preview.png')
}
[pscustomobject]@{Delivery=$deliveryRoot;Assets=$rows.Count;LevelPreviews=$scenes.Count;Packages='pending Unity export'} | ConvertTo-Json
