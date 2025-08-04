@echo off
echo Installing SFC Dashboard Escalation Service...

REM Stop service if running
sc stop "SFC Dashboard Escalation Service" 2>nul

REM Delete existing service
sc delete "SFC Dashboard Escalation Service" 2>nul

REM Create service
sc create "SFC Dashboard Escalation Service" binPath= "\"%~dp0SFCDashboard.EscalationService.exe\"" start= auto DisplayName= "SFC Dashboard Escalation Service"

REM Set service description
sc description "SFC Dashboard Escalation Service" "Processes escalations for SFC Dashboard system"

REM Set service to restart on failure
sc failure "SFC Dashboard Escalation Service" reset= 86400 actions= restart/30000/restart/60000/restart/120000

REM Start service
sc start "SFC Dashboard Escalation Service"

echo Service installation completed.
echo Use 'services.msc' to manage the service.
pause
