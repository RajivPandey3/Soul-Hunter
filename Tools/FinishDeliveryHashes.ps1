$ErrorActionPreference='Stop'
$deliveryRoot='D:\Downloads\Soul-Hunter-Assets\Campaign_Art_v01'
$hashRows=Get-ChildItem -LiteralPath $deliveryRoot -File -Recurse | Where-Object {$_.Name -ne 'SHA256.csv'} | ForEach-Object { [pscustomobject]@{Path=$_.FullName.Substring($deliveryRoot.Length+1);SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash;Bytes=$_.Length} }
$hashRows | Export-Csv -LiteralPath (Join-Path $deliveryRoot 'Verification/SHA256.csv') -NoTypeInformation
Get-Content -LiteralPath (Join-Path $deliveryRoot 'Verification/DeliveryCheck.json')
'Hashed files: '+$hashRows.Count
