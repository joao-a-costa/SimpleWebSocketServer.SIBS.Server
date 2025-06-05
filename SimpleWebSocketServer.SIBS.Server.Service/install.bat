@echo off
setlocal enabledelayedexpansion

rem =======================
rem INITIAL CONFIGURATION
rem =======================
set "defaultPort=10005"
set "port="
set "servicePath=%~dp0"
set "configFile=%servicePath%Service\SimpleWebSocketServer.SIBS.Server.Service.ini"
set "certFile=%servicePath%Service\smartcashlessserver.p12"
set "appId={01234567-89AB-CDEF-0123-456789ABCDEF}"
set "ServiceName="
set "certHash="

rem ========================
rem CHOOSE PORT IF NOT CONFIGURED
rem ========================
if exist "%configFile%" (
    echo Configuration file already exists. Skipping port setup.
    echo Reading port and service name from SimpleWebSocketServer.SIBS.Server.Service.ini...

    for /f "tokens=2 delims==" %%A in ('findstr /b /c:"Port=" "%configFile%"') do set "port=%%A"
    for /f "tokens=2 delims==" %%A in ('findstr /b /c:"ServiceName=" "%configFile%"') do set "ServiceName=%%A"
    for /f "tokens=2 delims==" %%A in ('findstr /b /c:"Thumbprint=" "%configFile%"') do set "certHash=%%A"

    echo Port loaded from SimpleWebSocketServer.SIBS.Server.Service.ini: !port!
    echo Service name loaded from SimpleWebSocketServer.SIBS.Server.Service.ini: !ServiceName!
    echo Thumbprint loaded from SimpleWebSocketServer.SIBS.Server.Service.ini: !certHash!
) else (
:askPort
    set /p "userPort=Enter port number (default is %defaultPort%): "

    if "!userPort!"=="" (
        set "port=%defaultPort%"
        echo No input provided. Using default port: %defaultPort%
    ) else (
        set /a checkPort=!userPort!+0 >nul 2>nul
        if errorlevel 1 (
            echo Invalid input: not a number. Try again.
            goto askPort
        )
        if !checkPort! LSS 1 (
            echo Invalid port: must be >= 1. Try again.
            goto askPort
        )
        if !checkPort! GTR 65535 (
            echo Invalid port: must be <= 65535. Try again.
            goto askPort
        )
        set "port=!checkPort!"
    )

    netstat -ano | findstr /r "TCP.*:!port! " >nul
    if not errorlevel 1 (
        echo Port !port! is already in use. Try another.
        goto askPort
    )

    echo Valid and available port selected: !port!
    goto askServiceName

:askServiceName
    set "defaultServiceNameBase=SimpleWebSocketServer SIBS Server Service"
    set "defaultServiceName=!defaultServiceNameBase! !port!"
    set /p "userServiceName=Enter service name (default: !defaultServiceName!): "
    if "!userServiceName!"=="" (
        set "ServiceName=!defaultServiceName!"
        echo No input provided. Using default service name: !ServiceName!
    ) else (
        set "ServiceName=!userServiceName!"
    )

    rem Hardcoded thumbprint value
    set "certHash=f1abc6eb32ef727d3a6e63b26c0b13a4ca81a281"
)

echo [Settings]> "%configFile%"
echo Port=!port!>> "%configFile%"
echo ServiceName=!ServiceName!>> "%configFile%"
echo Thumbprint=!certHash!>> "%configFile%"

set "ipPort=0.0.0.0:!port!"

:: === Check if cert is already installed ===
set "certAlreadyInstalled=false"
echo [DEBUG] Checking installed certificate thumbprints...

for /f "tokens=2 delims=:" %%h in ('certutil -store My ^| findstr /i /c:"Cert Hash(sha1)"') do (
    set "candidate=%%h"
    setlocal enabledelayedexpansion
    set "cleaned=!Cert Hash(sha1):=!"
    echo [DEBUG] Found cert thumbprint: !cleaned!
    if /i "!cleaned!"=="!certHash!" (
        endlocal
        set "certAlreadyInstalled=true"
        echo [DEBUG] Match found: !cleaned!
        goto afterCheck
    )
    endlocal
)

:afterCheck
if "%certAlreadyInstalled%"=="true" (
    echo [INFO] Certificate with thumbprint %certHash% is already installed. Skipping import.
) else (
    echo Importing certificate...
    certutil -f -importpfx "%certFile%"
    if not "%ERRORLEVEL%"=="0" (
        echo [ERROR] Import failed. Certutil returned error code %ERRORLEVEL%.
        pause
        exit /b %ERRORLEVEL%
    )
)

rem =======================
rem SSL CERTIFICATE BINDING
rem =======================
echo.
echo Checking if port !ipPort! is already bound to an SSL certificate...
set "sslBound=false"
for /f "tokens=1,* delims=:" %%A in ('netsh http show sslcert ^| findstr /R /C:"IP:port"') do (
    set "key=%%A"
    set "value=%%B"
    setlocal enabledelayedexpansion
    set "value=!value: =!"
    if /i "!value!"=="port:!ipPort!" (
        echo [INFO] Port !ipPort! is already bound to an SSL certificate. Skipping binding.
        endlocal
        set "sslBound=true"
        goto sslCheckDone
    )
    endlocal
)

:sslCheckDone
if "%sslBound%"=="false" (
    echo Removing any existing SSL binding for port !ipPort!... 
    netsh http delete sslcert ipport=!ipPort! >nul 2>&1
    echo Adding new SSL binding...
    netsh http add sslcert ipport=!ipPort! certhash=!certHash! appid=%appId%
    echo.
    echo SSL binding successfully added
) else (
    echo SSL binding already exists. No changes made.
)

echo Adding firewall rule for port !port!...

netsh advfirewall firewall delete rule name="Allow !ServiceName! !port!" dir=in protocol=TCP localport=!port!
netsh advfirewall firewall delete rule name="Allow !ServiceName! !port!" dir=out protocol=TCP localport=!port!
netsh advfirewall firewall add rule name="Allow !ServiceName! !port!" dir=in action=allow protocol=TCP localport=!port!
netsh advfirewall firewall add rule name="Allow !ServiceName! !port!" dir=out action=allow protocol=TCP localport=!port!

echo Firewall rule added.

rem =======================
rem INSTALLING SERVICE
rem =======================
echo.

set "ServiceNameExe=SimpleWebSocketServer.SIBS.Server.Service"
cd /d "%servicePath%Service"
cd /d "C:\Windows\Microsoft.NET\Framework\v4.0.30319"
InstallUtil.exe "%servicePath%Service\%ServiceNameExe%.exe"
sc config  "!ServiceName!" start= auto
sc failure "!ServiceName!" reset= 30 actions= restart/1000/restart/2000/restart/5000
net start "!ServiceName!"

echo.
pause
endlocal