@echo off
echo Starting SFC Dashboard Background Service...
sc start "SFC Dashboard Background Service"

echo Service Status:
sc query "SFC Dashboard Background Service"

pause
