$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$tools=Join-Path $root '.tools'
New-Item -ItemType Directory -Path $tools -Force | Out-Null
$localSdk=Join-Path $tools 'dotnet/dotnet.exe'
$system=(Get-Command dotnet -ErrorAction SilentlyContinue).Source
$needInstall=-not(Test-Path -LiteralPath $localSdk)
if($needInstall -and $system){$sdks=& $system --list-sdks;if($sdks -match '^10\.0\.4'){$needInstall=$false}}
if($needInstall){
 $installer=Join-Path $tools 'dotnet-install.ps1'
 Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
 & $installer -Channel 10.0 -Quality GA -InstallDir (Join-Path $tools 'dotnet') -NoPath
 if($LASTEXITCODE -ne 0){throw 'Cài SDK thất bại.'}
}
. (Join-Path $PSScriptRoot 'Common.ps1')
Build-Project
Write-Host 'Đã chuẩn bị project. Cần SQL Server Express LocalDB để chạy mặc định.'
Write-Host 'Chạy: powershell -ExecutionPolicy Bypass -File scripts/Start-Demo.ps1'
