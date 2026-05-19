@echo off
title CellShop ERP
color 0A

:: Matar instancias previas
taskkill /IM CellApi.exe /F >nul 2>&1
taskkill /IM CellApp.exe /F >nul 2>&1
powershell -Command "Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue; Start-Sleep 2" >nul 2>&1

:: Arrancar API en segundo plano
echo  Iniciando API...
start /B /MIN cmd /c "cd /d ""C:\Proyecto Cell\Backend\CellApi"" && dotnet run --no-build > ""C:\Proyecto Cell\logs\api.log"" 2>&1"

:: Esperar a que la API esté lista
echo  Esperando API...
timeout /t 8 /nobreak >nul

:: Arrancar Frontend en segundo plano
echo  Iniciando Frontend...
start /B /MIN cmd /c "cd /d ""C:\Proyecto Cell\Frontend\CellApp"" && dotnet run --no-build > ""C:\Proyecto Cell\logs\app.log"" 2>&1"

:: Esperar y abrir navegador
echo  Esperando Frontend...
timeout /t 10 /nobreak >nul

:: Abrir la app
start "" http://localhost:51011

echo.
echo  ================================================
echo   CellShop ERP en marcha
echo   Abre: http://localhost:51011
echo   Para cerrar: cierra esta ventana
echo  ================================================
echo.
pause

:: Al cerrar esta ventana, detener todo
taskkill /IM CellApi.exe /F >nul 2>&1
taskkill /IM CellApp.exe /F >nul 2>&1
powershell -Command "Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue" >nul 2>&1
