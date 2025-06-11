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
set "certFileName=smartcashlessserver"
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
)

echo [Settings]> "%configFile%"
echo Port=!port!>> "%configFile%"
echo ServiceName=!ServiceName!>> "%configFile%"

set "ipPort=0.0.0.0:!port!"

rem ============================
rem CHECK / IMPORT / GENERATE CERTIFICATE
rem ============================
echo.
echo Checking if certificate file exists: %certFile%

if not exist "%certFile%" (
    echo [INFO] Certificate file not found. Proceeding to generate and import a new one...

    set "OPENSSL_CMD=openssl"
    where openssl >nul 2>&1
    if errorlevel 1 (
        set "searchDir=c:\Program Files\OpenSSL-Win64"
        echo [DEBUG] OpenSSL fallback folder: !searchDir!
        echo [INFO] OpenSSL not found in PATH. Trying fallback: !searchDir!
        if exist "!searchDir!" (
            for /f "delims=" %%F in ('dir "!searchDir!" /s /b ^| findstr /i "bin\\openssl.exe"') do (
                set "OPENSSL_CMD=%%F"
                goto openssl_ready
            )
        )

        echo [INFO] OpenSSL not found. Attempting to run installer...
        if exist "%~dp0Service\Win64OpenSSL_Light-3_5_0.exe" (
            echo Running installer: Win64OpenSSL_Light-3_5_0.exe
            start /wait "" "%~dp0Service\Win64OpenSSL_Light-3_5_0.exe"
        ) else (
            echo [ERROR] Installer not found: %~dp0Service\Win64OpenSSL_Light-3_5_0.exe
            pause
            exit /b 1
        )

        where openssl >nul 2>&1
        if errorlevel 1 (
            if exist "!searchDir!" (
                for /f "delims=" %%F in ('dir "!searchDir!" /s /b ^| findstr /i "bin\\openssl.exe"') do (
                    set "OPENSSL_CMD=%%F"
                    goto openssl_ready
                )
            )
            echo [ERROR] OpenSSL still not found after installation "!searchDir!"
            pause
            exit /b 1
        )
    )
    :openssl_ready
    echo [INFO] OpenSSL path: %OPENSSL_CMD%

    pushd "%servicePath%Service"

    "%OPENSSL_CMD%" genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:4096 csr -out %certFileName%.key -aes256
    "%OPENSSL_CMD%" req -new -key %certFileName%.key -out %certFileName%.csr -subj "/C=PT/ST=Lisbon/L=Lisbon/O=Smartdigit/OU=Smartdigit/CN=SmartCASHLESS"
    "%OPENSSL_CMD%" x509 -req -days 365 -in %certFileName%.csr -signkey %certFileName%.key -out %certFileName%.crt
    "%OPENSSL_CMD%" pkcs12 -export -out %certFileName%.p12 -inkey %certFileName%.key -in %certFileName%.crt

    popd

    :: === Get current cert thumbprint from .p12 ===
    echo [INFO] Retrieving thumbprint from certificate file...

    pushd "%servicePath%Service"
    "%OPENSSL_CMD%" pkcs12 -in "%certFileName%.p12" -nokeys -nodes | "%OPENSSL_CMD%" x509 -noout -fingerprint -sha1 > tmp_fp.txt

    REM Ler linha do ficheiro e extrair fingerprint
    for /f "tokens=2 delims==" %%A in (tmp_fp.txt) do (
        set "certHash=%%A"
    )

    REM Remover os dois-pontos do fingerprint
    call set "certHash=%%certHash::=%%"

    echo [INFO] Thumbprint read: %certHash%
    echo Thumbprint=%certHash%>> "%configFile%"

    REM Limpar ficheiro temporário
    del tmp_fp.txt >nul 2>&1
    popd

    echo Importing certificate...
    certutil -f -importpfx "%certFile%"

    rem =======================
    rem SSL CERTIFICATE BINDING
    rem =======================
    echo.

    echo Removing any existing SSL binding for port !ipPort!...
    echo netsh http delete sslcert ipport=!ipPort!
    netsh http delete sslcert ipport=!ipPort! >nul 2>&1

    echo Adding new SSL binding...
    echo netsh http add sslcert ipport=!ipPort! certhash=!certHash! appid=%appId%
    netsh http add sslcert ipport=!ipPort! certhash=!certHash! appid=%appId%

    echo.
    echo SSL binding successfully added

    echo Adding firewall rule for port !port!...

    netsh advfirewall firewall delete rule name="Allow !ServiceName! !port!" dir=in protocol=TCP localport=!port!
    netsh advfirewall firewall delete rule name="Allow !ServiceName! !port!" dir=out protocol=TCP localport=!port!
    netsh advfirewall firewall add rule name="Allow !ServiceName! !port!" dir=in action=allow protocol=TCP localport=!port!
    netsh advfirewall firewall add rule name="Allow !ServiceName! !port!" dir=out action=allow protocol=TCP localport=!port!

    echo Firewall rule added.
)

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