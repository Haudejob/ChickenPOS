. (Join-Path $PSScriptRoot 'Common.ps1')
Build-Project 'test-release'
Start-LocalDb
$python=(Get-Command python -ErrorAction SilentlyContinue).Source
if(-not $python){throw 'Cần Python 3.10+ để chạy tests/integration.py.'}
try{Invoke-WebRequest -Uri 'http://127.0.0.1:5081/api/health' -TimeoutSec 1 | Out-Null;throw 'Port 5081 đang dùng. Hãy dừng phiên test cũ trước.'}catch{if($_.Exception.Message -like 'Port 5081*'){throw}}
$oldConnection=$env:ConnectionStrings__ChickenPos
$oldEnvironment=$env:ASPNETCORE_ENVIRONMENT
$dbName='ChickenPOS_Test_'+(Get-Date -Format 'yyyyMMdd_HHmmss')
try{
 $env:ConnectionStrings__ChickenPos="Server=(localdb)\MSSQLLocalDB;Database=$dbName;Integrated Security=true;TrustServerCertificate=true"
 $env:ASPNETCORE_ENVIRONMENT='Testing'
 $process=Start-WebProcess 5081 'test' 'test-release'
 $health=Wait-ForServer 5081
 if(-not $health.testInstance){throw 'Server 5081 không phải Testing, từ chối chạy.'}
 $folder=Join-Path $projectRoot 'docs/evidence';New-Item -ItemType Directory -Path $folder -Force | Out-Null
 @{database=$dbName;server='SQL Server LocalDB';environment='Testing';port=5081;startedAt=(Get-Date).ToUniversalTime().ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'test-environment.json') -Encoding utf8
 $env:PYTHONIOENCODING='utf-8'
 & $python (Join-Path $projectRoot 'tests/integration.py')
 $testExit=$LASTEXITCODE
}finally{
 if($process -and -not $process.HasExited){Stop-Process -Id $process.Id}
 $env:ConnectionStrings__ChickenPos=$oldConnection
 $env:ASPNETCORE_ENVIRONMENT=$oldEnvironment
}
if($testExit -ne 0){throw 'Có test FAIL. Đọc docs/evidence/integration-results.json.'}
Write-Host "Đã kiểm thử trên DB $dbName. DB được giữ để đối chiếu, không trộn vào DB demo."
