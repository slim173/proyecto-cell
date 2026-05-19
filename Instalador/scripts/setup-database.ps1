# ============================================================
#  CellShop ERP — Configurar base de datos PostgreSQL
#  Llamado automáticamente por el instalador
# ============================================================
param(
    [string]$PgHost     = "localhost",
    [string]$PgPort     = "5432",
    [string]$PgPassword = "",
    [string]$InstallDir = "C:\Program Files\CellShop"
)

$env:PGPASSWORD = $PgPassword
$psql = $null

# Buscar psql.exe en rutas comunes
$rutas = @(
    "C:\Program Files\PostgreSQL\18\bin\psql.exe",
    "C:\Program Files\PostgreSQL\17\bin\psql.exe",
    "C:\Program Files\PostgreSQL\16\bin\psql.exe",
    "C:\Program Files\PostgreSQL\15\bin\psql.exe"
)
foreach ($r in $rutas) {
    if (Test-Path $r) { $psql = $r; break }
}
if (!$psql) {
    $psql = (Get-Command psql -ErrorAction SilentlyContinue)?.Source
}
if (!$psql) {
    Write-Error "No se encontró psql.exe. Asegúrate de que PostgreSQL está instalado."
    exit 1
}

$pgArgs = @("-h", $PgHost, "-p", $PgPort, "-U", "postgres")

Write-Host "Configurando base de datos CellShop..." -ForegroundColor Cyan

# 1. Verificar conexión
Write-Host "  [1/4] Comprobando conexión a PostgreSQL..."
$test = & $psql @pgArgs -c "SELECT 1;" postgres 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "No se pudo conectar a PostgreSQL. Revisa la contraseña y que el servicio esté en ejecución."
    exit 1
}

# 2. Crear base de datos si no existe
Write-Host "  [2/4] Creando base de datos 'db_cell'..."
$exists = & $psql @pgArgs -tAc "SELECT 1 FROM pg_database WHERE datname='db_cell';" postgres
if ($exists -ne "1") {
    & $psql @pgArgs -c "CREATE DATABASE db_cell ENCODING='UTF8';" postgres | Out-Null
    Write-Host "         Base de datos creada." -ForegroundColor Green
} else {
    Write-Host "         La base de datos ya existe." -ForegroundColor Yellow
}

# 3. Ejecutar scripts SQL en orden
Write-Host "  [3/4] Inicializando esquema y datos..."
$sqlDir = "$InstallDir\Database"
$scripts = Get-ChildItem "$sqlDir\*.sql" | Sort-Object Name
foreach ($script in $scripts) {
    Write-Host "         Ejecutando: $($script.Name)"
    & $psql @pgArgs -f $script.FullName db_cell | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Advertencia al ejecutar $($script.Name) — puede ser normal si los objetos ya existen."
    }
}

# 4. Actualizar cadena de conexión en appsettings.json
Write-Host "  [4/4] Configurando cadena de conexión..."
$connStr = "Host=$PgHost;Port=$PgPort;Database=db_cell;Username=postgres;Password=$PgPassword"

$apiSettings = "$InstallDir\API\appsettings.json"
if (Test-Path $apiSettings) {
    $json = Get-Content $apiSettings -Raw | ConvertFrom-Json
    $json.ConnectionStrings.DefaultConnection = $connStr
    $json | ConvertTo-Json -Depth 10 | Set-Content $apiSettings -Encoding UTF8
}

$appSettings = "$InstallDir\App\appsettings.json"
if (Test-Path $appSettings) {
    $json = Get-Content $appSettings -Raw | ConvertFrom-Json
    if ($json.PSObject.Properties['ApiBaseUrl']) {
        $json.ApiBaseUrl = "http://localhost:51013"
    }
    $json | ConvertTo-Json -Depth 10 | Set-Content $appSettings -Encoding UTF8
}

Write-Host "Base de datos configurada correctamente." -ForegroundColor Green
