@echo off
echo Installing SFC Dashboard Background Service...

REM Build the application
echo Building application...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)

REM Publish the application
echo Publishing application...
dotnet publish --configuration Release --output ".\publish"
if %errorlevel% neq 0 (
    echo Publish failed!
    pause
    exit /b %errorlevel%
)

REM Stop the service if it exists
echo Stopping existing service...
sc stop "SFC Dashboard Background Service" >nul 2>&1

REM Install/Create the service
echo Installing service...
sc create "SFC Dashboard Background Service" binPath= "%CD%\publish\SFCDashboard.Background.exe" start= auto
if %errorlevel% neq 0 (
    echo Service creation failed!
    pause
    exit /b %errorlevel%
)

REM Set service description
sc description "SFC Dashboard Background Service" "Handles background processing for SFC Dashboard including PE record sync, OLA violation monitoring, and hold task reminders"

REM Start the service
echo Starting service...
sc start "SFC Dashboard Background Service"
if %errorlevel% neq 0 (
    echo Service start failed!
    pause
    exit /b %errorlevel%
)

echo Service installed and started successfully!
echo Service Status:
sc query "SFC Dashboard Background Service"

pause
