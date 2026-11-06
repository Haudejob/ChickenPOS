$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$webRoot=Join-Path $projectRoot 'src/ChickenPos.Web'
$localDotnet=Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
if(Test-Path -LiteralPath $localDotnet){$dotnetPath=$localDotnet}else{$dotnetPath=(Get-Command dotnet -ErrorAction SilentlyContinue).Source}
if(-not $dotnetPath){throw 'Chưa có .NET SDK. Chạy scripts/Setup.ps1 trước.'}
$env:DOTNET_CLI_HOME=Join-Path $projectRoot '.tools'
$env:NUGET_PACKAGES=Join-Path $projectRoot '.tools/nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'artifacts') -Force | Out-Null
function Build-Project([string]$OutputName='release') {
 Push-Location $projectRoot
 try {
  & $dotnetPath restore $webRoot
  if($LASTEXITCODE -ne 0){throw 'Khôi phục phụ thuộc thất bại.'}
  & $dotnetPath build $webRoot -c Release --no-restore -o (Join-Path $projectRoot "artifacts/$OutputName")
  if($LASTEXITCODE -ne 0){throw 'Biên dịch thất bại.'}
 } finally {Pop-Location}
}
function Start-LocalDb {
 $localDbPath=(Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue).Source
 if(-not $localDbPath){$localDbPath='C:/Program Files/Microsoft SQL Server/170/Tools/Binn/SqlLocalDB.exe'}
 if(-not(Test-Path -LiteralPath $localDbPath)){throw 'Cần cài SQL Server Express LocalDB hoặc cấu hình ConnectionStrings__ChickenPos tới SQL Server.'}
 & $localDbPath start MSSQLLocalDB
 if($LASTEXITCODE -ne 0){throw 'Không thể khởi động LocalDB.'}
}
function Start-WebProcess([int]$Port,[string]$LogPrefix,[string]$OutputName='release'){
 $dll=Join-Path $projectRoot "artifacts/$OutputName/ChickenPos.Web.dll"
 if(-not(Test-Path -LiteralPath $dll)){throw 'Chưa có bản build. Chạy Setup.ps1 hoặc Start-Demo.ps1 không kèm NoBuild.'}
 $outLog=Join-Path $projectRoot "artifacts/$LogPrefix.stdout.log"
 $errLog=Join-Path $projectRoot "artifacts/$LogPrefix.stderr.log"
 Start-Process -FilePath $dotnetPath -ArgumentList @("`"$dll`"",'--contentRoot',"`"$webRoot`"",'--urls',"http://127.0.0.1:$Port") -WorkingDirectory $projectRoot -WindowStyle Hidden -RedirectStandardOutput $outLog -RedirectStandardError $errLog -PassThru
}
function Wait-ForServer([int]$Port){
 for($attempt=0;$attempt -lt 40;$attempt++){
  try{ $health=Invoke-RestMethod -Uri "http://127.0.0.1:$Port/api/health" -TimeoutSec 2; if($health.product -eq 'ChickenPOS'){return $health} }catch{}
  Start-Sleep -Milliseconds 250
 }
 throw "Ứng dụng chưa khởi động được. Đọc artifacts/*.log để tìm nguyên nhân (port $Port)."
}
