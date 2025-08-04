@echo off
echo Uninstalling SFC Dashboard Background Service...

REM Stop the service
echo Stopping service...
sc stop "SFC Dashboard Background Service"

REM Delete the service
echo Removing service...
sc delete "SFC Dashboard Background Service"

if %errorlevel% equ 0 (
    echo Service uninstalled successfully!
) else (
    echo Failed to uninstall service. Error code: %errorlevel%
)

pause
