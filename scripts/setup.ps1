# setup.ps1 — Первоначальная настройка KescoBZ (.NET 10 + MudBlazor 9)
Write-Host "=== Kesco BZ Setup ===" -ForegroundColor Cyan

Write-Host "`n[1/3] Проверка .NET SDK..." -ForegroundColor Yellow
$v = dotnet --version 2>$null
if (-not $v) { Write-Host "  .NET SDK не найден! https://dot.net/download" -ForegroundColor Red; exit 1 }
Write-Host "  .NET SDK: $v" -ForegroundColor Green

Write-Host "`n[2/3] Restore..." -ForegroundColor Yellow
dotnet restore ..\KescoBZ.sln
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "`n[3/3] Build..." -ForegroundColor Yellow
dotnet build ..\KescoBZ.sln --configuration Debug
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "`nГотово! Запуск:" -ForegroundColor Cyan
Write-Host "  dotnet run --project src/Kesco.App.Web.BZ.MedicalTests --launch-profile Development"
