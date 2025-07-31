@echo off
echo Setting up environment files...

REM Check if .env files already exist
if exist "SFCDashboard\.env" (
    echo Warning: SFCDashboard\.env already exists
    set /p overwrite1="Do you want to overwrite it? (y/n): "
    if /i "%overwrite1%"=="y" (
        copy "SFCDashboard\.env.example" "SFCDashboard\.env"
        echo Created SFCDashboard\.env from example
    ) else (
        echo Skipped SFCDashboard\.env
    )
) else (
    copy "SFCDashboard\.env.example" "SFCDashboard\.env"
    echo Created SFCDashboard\.env from example
)

if exist "SFCDashboard.Api\.env" (
    echo Warning: SFCDashboard.Api\.env already exists
    set /p overwrite2="Do you want to overwrite it? (y/n): "
    if /i "%overwrite2%"=="y" (
        copy "SFCDashboard.Api\.env.example" "SFCDashboard.Api\.env"
        echo Created SFCDashboard.Api\.env from example
    ) else (
        echo Skipped SFCDashboard.Api\.env
    )
) else (
    copy "SFCDashboard.Api\.env.example" "SFCDashboard.Api\.env"
    echo Created SFCDashboard.Api\.env from example
)

echo.
echo Environment files setup complete!
echo.
echo IMPORTANT: Please edit the .env files and replace the placeholder values with your actual configuration:
echo   1. Azure AD credentials (Tenant ID, Client ID, Client Secret)
echo   2. Database connection string
echo   3. API URLs if different from defaults
echo.
echo See ENVIRONMENT_CONFIGURATION.md for detailed instructions.
pause
