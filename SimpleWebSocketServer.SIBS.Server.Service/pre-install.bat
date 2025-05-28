@echo off
setlocal

taskkill /IM mmc.exe /F

rem Set the string "SmartCASLESS - Server" as a variable
set serviceName=SmartCASLESS - Server

rem Set service path
set servicePath=%~dp0

set finalServiceName=%serviceName%

rem Stop the service
net stop "%finalServiceName%"

rem Get the current date in DDMMYYYY format
for /f "tokens=1-3 delims=/" %%a in ("%date%") do (
  set "day=%%a"
  set "month=%%b"
  set "year=%%c"
)

rem Remove leading zeros from day and month
set "day=%day:~0,2%"
set "month=%month:~0,2%"

rem Create a new folder with the specified name
cd %servicePath%
set "backupFolder=%servicePath%backup%day%%month%%year%"
mkdir "%backupFolder%"

rem Copy the contents of the source folder to the new backup folder
xcopy "%servicePath%Service\" "%backupFolder%" /E /I /Y

rem Delete all content from folder except ver.ini
set "target_dir=%servicePath%"
for /r "%target_dir%" %%f in (*) do (
    if /i not "%%~nxf"=="%serviceName%.ini" del "%%f"
)

pause
endlocal