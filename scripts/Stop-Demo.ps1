$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$record=Join-Path $root 'artifacts/demo.pid'
if(-not(Test-Path -LiteralPath $record)){Write-Host 'Không có PID do script quản lý.';exit 0}
$serverPid=[int](Get-Content -LiteralPath $record)
$process=Get-CimInstance Win32_Process -Filter "ProcessId = $serverPid"
$expected=Join-Path $root 'artifacts/release/ChickenPos.Web.dll'
if($process -and $process.CommandLine.Contains($expected)){Stop-Process -Id $serverPid;Write-Host 'Đã dừng server demo.'}elseif($process){throw 'PID đã thuộc tiến trình khác; không dừng.'}else{Write-Host 'Server đã dừng.'}
