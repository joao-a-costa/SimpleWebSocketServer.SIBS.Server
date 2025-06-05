@echo off
setlocal enabledelayedexpansion

:: Kill MMC if it's running (useful if managing services or certificates)
taskkill /IM mmc.exe /F >nul 2>&1

:: === CONFIGURATION ===
set "servicePath=%~dp0"
set "configFile=%servicePath%Service\SimpleWebSocketServer.SIBS.Server.Service.ini"

:: === READ CONFIG FILE (.ini) ===
if not exist "%configFile%" (
    echo [ERROR] Configuration file not found: %configFile%
    pause
    exit /b 1
)

for /f "tokens=2 delims==" %%A in ('findstr /b /c:"ServiceName=" "%configFile%"') do set "serviceName=%%A"
for /f "tokens=2 delims==" %%A in ('findstr /b /c:"Port=" "%configFile%"') do set "port=%%A"

echo Service name loaded from config: !serviceName!
echo Port loaded from config: !port!

:: === STOP SERVICE IF RUNNING ===
net stop "!serviceName!" >nul 2>&1

:: === CREATE BACKUP FOLDER (uses date independent of system locale) ===
cd /d "%servicePath%"
for /f %%i in ('wmic os get LocalDateTime ^| find "."') do set dt=%%i
set "backupFolder=%servicePath%backup%dt:~6,2%%dt:~4,2%%dt:~0,4%"

mkdir "%backupFolder%" >nul 2>&1

echo Backing up current files to: %backupFolder%
xcopy "%servicePath%Service\" "%backupFolder%" /E /I /Y >nul

:: === DELETE ALL FILES EXCEPT .ini ===
set "target_dir=%servicePath%Service"
for /r "%target_dir%" %%f in (*) do (
    if /i not "%%~xf"==".ini" del "%%f"
)

echo Pre-installation complete.
pause
endlocal