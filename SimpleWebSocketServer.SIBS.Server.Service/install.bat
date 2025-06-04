@echo off
setlocal enabledelayedexpansion

rem =======================
rem INITIAL CONFIGURATION
rem =======================
set "defaultPort=10005"
set "port="
set "servicePath=%~dp0"
set "configFile=%servicePath%Service\config.ini"
set "certFile=%servicePath%Service\certificate.p12"
set "expectedThumbprint=f1abc6eb32ef727d3a6e63b26c0b13a4ca81a281"
set "appId={01234567-89AB-CDEF-0123-456789ABCDEF}"

rem ========================
rem CHOOSE PORT IF NOT CONFIGURED
rem ========================
if exist "%configFile%" (
    echo Configuration file already exists. Skipping port setup.
    echo Reading port from config.ini...
    for /f "tokens=2 delims==" %%A in ('findstr /b /c:"Port=" "%configFile%"') do set "port=%%A"
    echo Port loaded from config.ini: !port!
) else (
:askPort
    set /p "userPort=Enter port number (default is %defaultPort%): "

    if "!userPort!"=="" (
        set "port=%defaultPort%"
        echo No input provided. Using default port: %defaultPort%
        goto checkInUse
    )

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

:checkInUse
    netstat -ano | findstr /r "TCP.*:!port! " >nul
    if not errorlevel 1 (
        echo Port !port! is already in use. Try another.
        goto askPort
    )

    echo Valid and available port selected: !port!
    echo [Settings]> "%configFile%"
    echo Port=!port!>> "%configFile%"
)

set "ipPort=0.0.0.0:!port!"

rem ===========================
rem CHECK CERTIFICATE EXISTENCE
rem ===========================
echo.
echo Checking for certificate with thumbprint: %expectedThumbprint%
echo Import path: %certFile%

set "certHash="
set "certInstalled=false"
for /f "tokens=2 delims=:" %%h in ('certutil -store My ^| findstr /i /c:"Cert Hash"') do (
    set "line=%%h"
    setlocal enabledelayedexpansion
    set "line=!line: =!"
    if /i "!line!"=="%expectedThumbprint%" (
        endlocal
        set "certInstalled=true"
        set "certHash=%expectedThumbprint%"
        goto certCheckDone
    )
    endlocal
)

:certCheckDone
if "%certInstalled%"=="true" (
    echo [INFO] Certificate already installed. Skipping import.
) else (
    echo [INFO] Certificate not found. Proceeding with import...

    echo Importing certificate...
    certutil -f -importpfx "%certFile%"
    if not "%ERRORLEVEL%"=="0" (
        echo [ERROR] Import failed. Certutil returned error code %ERRORLEVEL%.
        pause
        exit /b %ERRORLEVEL%
    )

    for /f "tokens=2 delims=:" %%h in ('certutil -store My ^| findstr /i /c:"Cert Hash"') do (
        set "line=%%h"
        setlocal enabledelayedexpansion
        set "line=!line: =!"
        if /i "!line!"=="%expectedThumbprint%" (
            endlocal
            set "certHash=%expectedThumbprint%"
            goto hashFound
        )
        endlocal
    )

    echo [ERROR] Imported certificate not found by thumbprint.
    pause
    exit /b 1
)

:hashFound

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
    rem echo [DEBUG] Found bound port: "!value!"
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
    netsh http add sslcert ipport=!ipPort! certhash=%certHash% appid=%appId%
    echo.
    echo SSL binding successfully added
) else (
    echo SSL binding already exists. No changes made.
)

echo.
echo =======================
echo INSTALLING SERVICE
set "serviceName=SmartCASLESS - Server"
set "serviceNameExe=SimpleWebSocketServer.SIBS.Server.Service"
cd /d "%servicePath%Service"
cd /d "C:\Windows\Microsoft.NET\Framework\v4.0.30319"
InstallUtil.exe "%servicePath%Service\%serviceNameExe%.exe"
sc config  "%serviceName%" start= auto
sc failure "%serviceName%" reset= 30 actions= restart/1000/restart/2000/restart/5000
net start "%serviceName%"

echo.
pause
endlocal