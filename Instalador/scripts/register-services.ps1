# ============================================================
#  CellShop ERP — Registrar servicios de Windows
#  Llamado automáticamente por el instalador (requiere admin)
# ============================================================
param(
    [string]$InstallDir = "C:\Program Files\CellShop"
)

function Register-CellService {
    param([string]$Nombre, [string]$Display, [string]$Exe, [string]$Desc)

    # Eliminar servicio anterior si existe
    $existe = Get-Service -Name $Nombre -ErrorAction SilentlyContinue
    if ($existe) {
        Write-Host "  Eliminando servicio anterior: $Nombre"
        Stop-Service  -Name $Nombre -Force -ErrorAction SilentlyContinue
        sc.exe delete $Nombre | Out-Null
        Start-Sleep -Seconds 1
    }

    # Crear servicio
    New-Service `
        -Name        $Nombre `
        -DisplayName $Display `
        -Description $Desc `
        -BinaryPathName "`"$Exe`"" `
        -StartupType Automatic `
        -ErrorAction Stop | Out-Null

    # Configurar recuperación automática ante fallos
    sc.exe failure $Nombre reset= 60 actions= restart/5000/restart/10000/restart/30000 | Out-Null

    Write-Host "  Servicio registrado: $Display" -ForegroundColor Green
}

Write-Host "Registrando servicios de Windows..." -ForegroundColor Cyan

Register-CellService `
    -Nombre  "CellShopAPI" `
    -Display "CellShop ERP — Backend API" `
    -Exe     "$InstallDir\API\CellApi.exe" `
    -Desc    "API REST del sistema CellShop ERP"

Register-CellService `
    -Nombre  "CellShopApp" `
    -Display "CellShop ERP — Aplicación Web" `
    -Exe     "$InstallDir\App\CellApp.exe" `
    -Desc    "Interfaz web del sistema CellShop ERP"

# Abrir puertos en el firewall de Windows
Write-Host "Configurando firewall..." -ForegroundColor Cyan

$reglas = @(
    @{ Nombre = "CellShop-App-51011"; Puerto = 51011; Desc = "CellShop App (web)" },
    @{ Nombre = "CellShop-API-51013"; Puerto = 51013; Desc = "CellShop API (backend)" }
)

foreach ($r in $reglas) {
    netsh advfirewall firewall delete rule name="$($r.Nombre)" | Out-Null
    netsh advfirewall firewall add rule `
        name="$($r.Nombre)" `
        dir=in action=allow protocol=TCP `
        localport=$($r.Puerto) `
        description="$($r.Desc)" | Out-Null
    Write-Host "  Puerto $($r.Puerto) abierto: $($r.Desc)" -ForegroundColor Green
}

# Arrancar servicios
Write-Host "Iniciando servicios..." -ForegroundColor Cyan
Start-Service -Name "CellShopAPI" -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Start-Service -Name "CellShopApp" -ErrorAction SilentlyContinue

Write-Host "Servicios registrados e iniciados correctamente." -ForegroundColor Green
