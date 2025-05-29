@echo off
setlocal enabledelayedexpansion

rem Default port
set "defaultPort=10005"
set "port="

rem Set paths
set "servicePath=%~dp0"
set "configFile=%servicePath%Service\config.ini"

rem Check if config.ini already exists
if exist "%configFile%" (
    echo Configuration file already exists. Skipping port setup.
    goto continueInstall
)

:askPort
set /p "userPort=Enter port number (default is %defaultPort%): "

rem If user presses enter without input, use default
if "!userPort!"=="" (
    set "port=%defaultPort%"
    echo No input provided. Using default port: %defaultPort%
    goto checkInUse
)

rem Try to evaluate input as number (set /a returns error if not numeric)
set "checkPort="
set /a checkPort=!userPort!+0 >nul 2>nul
if errorlevel 1 (
    echo Invalid input: not a number. Try again.
    goto askPort
)

rem Now check if it's within valid port range
if !checkPort! LSS 1 (
    echo Invalid port: must be >= 1. Try again.
    goto askPort
)
if !checkPort! GTR 65535 (
    echo Invalid port: must be <= 65535. Try again.
    goto askPort
)

set "port=!checkPort!"

:checkInUse
rem Check if port is already in use
netstat -ano | findstr /r "TCP.*:!port! " >nul
if not errorlevel 1 (
    echo Port !port! is already in use. Try another.
    goto askPort
)

echo Valid and available port selected: !port!

rem Write port to INI file
echo [Settings]> "%configFile%"
echo Port=%port%>> "%configFile%"

:continueInstall

rem Set serviceName variable
set "serviceName=SmartCASLESS - Server"
set "serviceNameExe=SimpleWebSocketServer.SIBS.Server.Service"

rem Navigate to the Service directory
cd /d "%servicePath%\Service"

rem Navigate to the .NET Framework directory
cd /d "C:\Windows\Microsoft.NET\Framework\v4.0.30319"

rem Install the service
InstallUtil.exe "%servicePath%\Service\%serviceNameExe%.exe"

rem Set service failure options
sc failure "%serviceName%" reset= 30 actions= restart/1000/restart/2000/restart/5000

rem Start the service
net start "%serviceName%"

pause
endlocal