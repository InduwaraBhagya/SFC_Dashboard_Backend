# SFC Dashboard Background Service

This is a standalone Windows service that handles all background processing for the SFC Dashboard application.

## Services Included

### 1. PERecordSyncService
- **Purpose**: Synchronizes PE records from the source database to PlannedEvents and PETasks
- **Schedule**: Runs twice daily at 6:00 AM and 1:00 PM
- **Functions**:
  - Syncs PE records to PlannedEvents table
  - Creates and updates PETasks based on task templates
  - Handles task phase management
  - Fixes OLA dates for existing tasks

### 2. OLAViolationService
- **Purpose**: Monitors and updates OLA violation status for tasks
- **Schedule**: Runs every 6 hours
- **Functions**:
  - Checks tasks for OLA violations
  - Updates IsOLAViolate flag
  - Sets ViolationStartTime when violations occur
  - Respects hold status (tasks on hold are not marked as violations)

### 3. HoldTaskReminderService
- **Purpose**: Sends reminders for tasks that have been on hold too long
- **Schedule**: Runs at 9 AM and 2 PM daily
- **Functions**:
  - Identifies tasks on hold for more than 24 hours
  - Sends reminder messages to workgroup users
  - Creates system-generated PEIssue records

### 4. EscalationWorkerService
- **Purpose**: Monitors OLA violations and creates escalations for overdue tasks
- **Schedule**: Runs at 9 AM daily (configurable)
- **Functions**:
  - Checks tasks with OLA violations
  - Creates escalations at appropriate levels (1, 2, or 3)
  - Level 1: Tasks violated for less than 24 hours
  - Level 2: Tasks violated for 1+ days
  - Level 3: Tasks violated for 3+ days
  - Respects escalation disabled flag on tasks

## Installation

### Prerequisites
- .NET 8.0 Runtime installed
- Access to the SFC Dashboard database
- Administrator privileges for service installation

### Quick Installation
1. Navigate to the `Scripts` folder
2. Run `install-service.bat` as Administrator
3. The service will be built, published, installed, and started automatically

### Manual Installation
```bash
# Build the application
dotnet build --configuration Release

# Publish the application
dotnet publish --configuration Release --output "./publish"

# Install as Windows service
sc create "SFC Dashboard Background Service" binPath= "[FULL_PATH_TO_EXE]" start= auto

# Start the service
sc start "SFC Dashboard Background Service"
```

## Configuration

### Database Connection
Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SFCDashboard;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

### Logging
Logging is configured in `appsettings.json` and can be adjusted for different environments:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SFCDashboard.Background": "Information"
    }
  }
}
```

### Escalation Service Configuration
Configure escalation service schedule in `appsettings.json`:
```json
{
  "EscalationService": {
    "ScheduledTimes": [ "09:00" ]
  }
}
```

- **ScheduledTimes**: Array of times (24-hour format) when escalation checks should run
- Default time is 9:00 AM if not configured
- The service can be enabled/disabled through the SystemConfigurations table

## Service Management

### Using Scripts
- **Install**: `Scripts\install-service.bat`
- **Uninstall**: `Scripts\uninstall-service.bat`
- **Start**: `Scripts\start-service.bat`
- **Stop**: `Scripts\stop-service.bat`

### Using Windows Services Console
1. Open Services (services.msc)
2. Find "SFC Dashboard Background Service"
3. Right-click for options (Start, Stop, Restart, Properties)

### Using Command Line
```cmd
# Check status
sc query "SFC Dashboard Background Service"

# Start service
sc start "SFC Dashboard Background Service"

# Stop service
sc stop "SFC Dashboard Background Service"

# Delete service
sc delete "SFC Dashboard Background Service"
```

## Monitoring and Troubleshooting

### Log Files
The service writes logs to the Windows Event Log and console output. Check:
- Windows Event Viewer > Windows Logs > Application
- Look for events from "SFC Dashboard Background Service"

### Common Issues

1. **Database Connection Failures**
   - Verify connection string in appsettings.json
   - Ensure service account has database access
   - Check network connectivity

2. **Service Won't Start**
   - Check Windows Event Log for specific errors
   - Verify .NET 8.0 Runtime is installed
   - Ensure database is accessible

3. **Performance Issues**
   - Monitor database performance during sync operations
   - Adjust sync intervals if needed
   - Check available disk space and memory

### Debugging
For development and debugging:
1. Set environment to Development
2. Run with: `dotnet run --environment Development`
3. Check logs for detailed information

## Architecture

The service is designed with separation of concerns:
- **Worker.cs**: Main service coordinator
- **Program.cs**: Service host configuration
- **Services/**: Individual background service implementations
- **Models/**: Database entity models
- **Data/**: Entity Framework DbContext

## Dependencies

- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Hosting.WindowsServices
- Microsoft.Extensions.Logging

## Integration with Main Application

The background service operates independently from the main SFC Dashboard application but shares the same database. This ensures:
- Data consistency across applications
- Improved performance by offloading background tasks
- Better scalability and maintenance
- Separate deployment and monitoring

## Updates and Maintenance

To update the service:
1. Stop the service: `sc stop "SFC Dashboard Background Service"`
2. Deploy new version to the same directory
3. Start the service: `sc start "SFC Dashboard Background Service"`

For major updates, consider using the uninstall/install scripts to ensure clean deployment.
