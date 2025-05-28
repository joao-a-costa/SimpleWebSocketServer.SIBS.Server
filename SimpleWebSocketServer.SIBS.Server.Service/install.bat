@echo off
setlocal

rem Set serviceName variable
set serviceName=SmartCASLESS - Server
set serviceNameExe=SimpleWebSocketServer.SIBS.Server.Service

rem Set servicePath variable
set servicePath=%~dp0

rem Navigate to the Service directory
cd %servicePath%

rem Navigate to the .NET Framework directory
cd C:\Windows\Microsoft.NET\Framework\v4.0.30319

rem Install the service
InstallUtil.exe "%servicePath%\%serviceNameExe%.exe"

set finalServiceName=%serviceName%

rem Set service failure options
sc failure "%finalServiceName%" reset= 30 actions= restart/1000/restart/2000/restart/5000

rem Start the service
net start "%finalServiceName%"

pause
endlocal