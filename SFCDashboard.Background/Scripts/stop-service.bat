@echo off
echo Stopping SFC Dashboard Background Service...
sc stop "SFC Dashboard Background Service"

echo Service Status:
sc query "SFC Dashboard Background Service"

pause
