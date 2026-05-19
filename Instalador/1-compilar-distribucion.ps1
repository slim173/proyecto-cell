# ============================================================
#  CellShop ERP — Paso 1: Compilar distribución
#  Ejecutar desde PowerShell NORMAL (no necesita admin)
# ============================================================

$raiz  = Split-Path $PSScriptRoot -Parent
$dist  = "$PSScriptRoot\dist"

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  CellShop ERP — Compilando para dist " -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Limpiar distribución anterior
if (Test-Path $dist) {
    Write-Host "Limpiando dist anterior..." -ForegroundColor Yellow
    Remove-Item $dist -Recurse -Force
}
New-Item -ItemType Directory -Path "$dist\CellApi" | Out-Null
New-Item -ItemType Directory -Path "$dist\CellApp" | Out-Null
New-Item -ItemType Directory -Path "$dist\Database" | Out-Null

# ── Backend API ──────────────────────────────────────────────
Write-Host "[1/3] Compilando Backend API (self-contained)..." -ForegroundColor Green
dotnet publish "$raiz\Backend\CellApi\CellApi.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -o "$dist\CellApi" `
    --nologo -v quiet

if ($LASTEXITCODE -ne 0) { Write-Host "ERROR compilando API" -ForegroundColor Red; exit 1 }
Write-Host "   OK — Backend API compilado" -ForegroundColor DarkGreen

# ── Frontend App ─────────────────────────────────────────────
Write-Host "[2/3] Compilando Frontend Blazor (self-contained)..." -ForegroundColor Green
dotnet publish "$raiz\Frontend\CellApp\CellApp.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -o "$dist\CellApp" `
    --nologo -v quiet

if ($LASTEXITCODE -ne 0) { Write-Host "ERROR compilando App" -ForegroundColor Red; exit 1 }
Write-Host "   OK — Frontend App compilado" -ForegroundColor DarkGreen

# ── Scripts SQL ──────────────────────────────────────────────
Write-Host "[3/3] Copiando scripts de base de datos..." -ForegroundColor Green
Copy-Item "$raiz\Database\*.sql" "$dist\Database\" -Force
Write-Host "   OK — Scripts SQL copiados" -ForegroundColor DarkGreen

# ── Verificación de tamaño ───────────────────────────────────
$tamApi = (Get-ChildItem "$dist\CellApi" -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
$tamApp = (Get-ChildItem "$dist\CellApp" -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB

Write-Host ""
Write-Host "Distribución lista en: $dist" -ForegroundColor Cyan
Write-Host "  API:      $([math]::Round($tamApi,0)) MB" -ForegroundColor Gray
Write-Host "  App:      $([math]::Round($tamApp,0)) MB" -ForegroundColor Gray
Write-Host ""
Write-Host "Siguiente paso:" -ForegroundColor Yellow
Write-Host "  Abre Inno Setup y compila: CellShop-Setup.iss" -ForegroundColor Yellow
Write-Host ""
