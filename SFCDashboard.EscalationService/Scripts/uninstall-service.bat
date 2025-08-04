@echo off
echo Uninstalling SFC Dashboard Escalation Service...

REM Stop service
sc stop "SFC Dashboard Escalation Service"

REM Delete service
sc delete "SFC Dashboard Escalation Service"

echo Service uninstallation completed.
pause
