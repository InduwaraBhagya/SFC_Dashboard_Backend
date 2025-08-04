@echo off
echo Building and publishing SFC Dashboard Escalation Service...

REM Build the project
dotnet build --configuration Release

REM Publish for deployment
dotnet publish --configuration Release --output ".\publish" --self-contained false --runtime win-x64

echo Build completed. Files are in the 'publish' folder.
echo Copy the 'publish' folder contents to your deployment location.
echo Then run 'install-service.bat' from the deployment location.
pause
