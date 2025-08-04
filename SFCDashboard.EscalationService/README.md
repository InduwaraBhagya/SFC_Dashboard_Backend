# SFC Dashboard Escalation Service

A standalone Windows service for processing escalations in the SFC Dashboard system. This service runs independently from the main API and web applications.

## Features

- **Standalone Operation**: Runs independently from the main SFC Dashboard application
- **Configurable Intervals**: Check frequency can be configured (default: 30 minutes in production, 5 minutes in development)
- **Batch Processing**: Efficiently processes large numbers of tasks in batches
- **Windows Service**: Can be installed and run as a Windows service
- **Comprehensive Logging**: Uses Serilog for structured logging to console and files
- **Database Retry Logic**: Built-in retry logic for database connection failures
- **Graceful Shutdown**: Properly handles service stop requests

## Prerequisites

- .NET 9.0 Runtime
- SQL Server database (same as main SFC Dashboard)
- Windows (for service installation)

## Configuration

The service uses the same database as the main SFC Dashboard application. Configure the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(local);Database=SFCDashboard;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "EscalationService": {
    "CheckIntervalMinutes": 30,
    "BatchSize": 100
  }
}
```

### Configuration Options

- `CheckIntervalMinutes`: How often to check for new escalations (default: 30 minutes)
- `BatchSize`: Number of tasks to process in each batch (default: 100)

## Development Setup

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Run in development mode:**
   ```bash
   dotnet run --environment Development
   ```

3. **Test the service:**
   The service will check for escalations every 5 minutes in development mode.

## Production Deployment

### Option 1: Using the Build Script

1. **Run the build script:**
   ```bash
   Scripts\build-and-publish.bat
   ```

2. **Copy the published files** to your deployment location (e.g., `C:\Services\SFCEscalationService\`)

3. **Install as Windows Service:**
   ```bash
   # Navigate to deployment location
   cd C:\Services\SFCEscalationService\
   
   # Run as administrator
   Scripts\install-service.bat
   ```

### Option 2: Manual Installation

1. **Publish the application:**
   ```bash
   dotnet publish --configuration Release --output "C:\Services\SFCEscalationService" --self-contained false --runtime win-x64
   ```

2. **Install as Windows Service:**
   ```bash
   sc create "SFC Dashboard Escalation Service" binPath= "C:\Services\SFCEscalationService\SFCDashboard.EscalationService.exe" start= auto DisplayName= "SFC Dashboard Escalation Service"
   sc description "SFC Dashboard Escalation Service" "Processes escalations for SFC Dashboard system"
   sc start "SFC Dashboard Escalation Service"
   ```

## Service Management

### Start/Stop Service
```bash
# Start
sc start "SFC Dashboard Escalation Service"

# Stop
sc stop "SFC Dashboard Escalation Service"
```

### Uninstall Service
```bash
Scripts\uninstall-service.bat
```

### View Service Status
```bash
sc query "SFC Dashboard Escalation Service"
```

## Logging

The service creates logs in the following locations:

- **Console Output**: Real-time logging during development
- **Log Files**: `logs/escalation-service-YYYY-MM-DD.txt`
  - Rolling daily logs
  - Retained for 30 days
  - Includes timestamps, log levels, and structured data

### Log Levels

- **Information**: Normal operations, escalation counts, service lifecycle
- **Warning**: Non-critical issues, negative violation durations
- **Error**: Database errors, processing failures
- **Fatal**: Critical startup failures

## Integration with Main Application

The escalation service is designed to run alongside the main SFC Dashboard application:

1. **Database**: Uses the same database as the main application
2. **Configuration**: Reads from the `SystemConfigurations` table (can be enabled/disabled via API)
3. **Independence**: Can run on the same server or a different server
4. **No API Dependencies**: Does not depend on the main API being running

## Removing from Main Application

To completely separate escalation processing:

1. **Deploy this service** to production
2. **Remove EscalationBackgroundService** from the main API:
   ```csharp
   // Remove this line from Program.cs in SFCDashboard.Api
   // builder.Services.AddHostedService<EscalationBackgroundService>();
   ```
3. **Keep the escalation API endpoints** for management (enable/disable, manual checks)

## Monitoring

Monitor the service through:

1. **Windows Services Manager** (`services.msc`)
2. **Event Viewer** (Windows Logs > Application)
3. **Log Files** in the `logs` directory
4. **Database** - check `Escalations` table for new entries

## Troubleshooting

### Service Won't Start
1. Check Windows Event Log for errors
2. Verify database connection string
3. Ensure .NET 9.0 runtime is installed
4. Check file permissions in installation directory

### No Escalations Being Created
1. Check if escalation service is enabled in database (`SystemConfigurations` table)
2. Verify there are OLA violations in the `PETasks` table
3. Check log files for processing information
4. Ensure database connection is working

### Performance Issues
1. Adjust `BatchSize` in configuration (reduce for less memory usage)
2. Increase `CheckIntervalMinutes` if processing takes too long
3. Monitor database performance during escalation processing

## Security Considerations

- Service runs under Local System account by default
- Consider creating a dedicated service account with minimal permissions
- Database connection uses Windows Authentication (change as needed)
- Log files may contain sensitive information - secure accordingly

## Version History

- **v1.0**: Initial standalone escalation service
  - Extracted from main SFC Dashboard API
  - Windows service support
  - Configurable intervals and batch processing
  - Comprehensive logging
