param([switch]$NoBuild)
. (Join-Path $PSScriptRoot 'Common.ps1')
try{$existing=Invoke-RestMethod -Uri 'http://127.0.0.1:5080/api/health' -TimeoutSec 2;if($existing.product -eq 'ChickenPOS'){Write-Host 'ChickenPOS đang chạy: http://127.0.0.1:5080';exit 0}}catch{}
if(-not $NoBuild){Build-Project}
Start-LocalDb
$env:ASPNETCORE_ENVIRONMENT='Development'
if($env:ConnectionStrings__ChickenPos -match 'ChickenPOS_Test_'){throw 'Đang cấu hình DB test; mở cửa sổ PowerShell mới để chạy demo.'}
$process=Start-WebProcess 5080 'demo'
Set-Content -LiteralPath (Join-Path $projectRoot 'artifacts/demo.pid') -Value $process.Id
$health=Wait-ForServer 5080
Write-Host 'ChickenPOS sẵn sàng: http://127.0.0.1:5080'
Write-Host 'Tài khoản: quanly / banhang / thungan / bep. Mật khẩu demo mặc định: Chicken@2026!'
Write-Host 'Đóng PowerShell không tắt server; dùng scripts/Stop-Demo.ps1 để dừng.'
