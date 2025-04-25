using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFCDB.Models;
using SFCDashboard.Models;
using SFCDashboard.Data;
using NuGet.Packaging;

namespace SFCDashboard.Services
{
    public class PERecordSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PERecordSyncService> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromHours(24); // Run once per day

        public PERecordSyncService(IServiceProvider serviceProvider, ILogger<PERecordSyncService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting PE records synchronization at {time}", DateTimeOffset.Now);

                    await SyncPERecordsAsync();

                    _logger.LogInformation("PE records synchronization completed at {time}", DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during PE records synchronization");
                }

                await Task.Delay(_syncInterval, stoppingToken);
            }
        }

        public async Task SyncPERecordsAsync()
        {
            try
            {
                _logger.LogInformation("Starting PE records synchronization");

                // Load data with a dedicated context
                List<PERecord> sourceRecords;
                List<PlannedEvent> existingEvents;
                List<PETask> existingTasks;
                List<PETaskList> taskTemplates;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var loadingContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var dataLoadingTasks = await LoadDataAsync(loadingContext);
                    (sourceRecords, existingEvents, existingTasks, taskTemplates) = dataLoadingTasks;
                }

                if (sourceRecords.Count == 0 || taskTemplates.Count == 0)
                {
                    _logger.LogWarning("Required data missing. Aborting synchronization.");
                    return;
                }

                // Process records in batches
                var processedPENumbers = new HashSet<string>();
                const int batchSize = 100;

                foreach (var batch in sourceRecords.Where(r => !string.IsNullOrEmpty(r.PE_NUMBER))
                                                 .Where(r => r.PE_NUMBER != null && !processedPENumbers.Contains(r.PE_NUMBER))
                                                 .Chunk(batchSize))
                {
                    // Use a new context for each batch
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var newEvents = new List<PlannedEvent>();
                    var updatedEvents = new List<PlannedEvent>();
                    var newTasks = new List<PETask>();

                    foreach (var record in batch)
                    {
                        if (!string.IsNullOrEmpty(record.PE_NUMBER))
                        {
                            processedPENumbers.Add(record.PE_NUMBER);
                        }
                        var existingEvent = existingEvents.FirstOrDefault(pe => pe.PeNumber == record.PE_NUMBER);

                        if (existingEvent != null)
                        {
                            var trackedEvent = await dbContext.PlannedEvents
                                .FirstOrDefaultAsync(pe => pe.PeNumber == record.PE_NUMBER);
                            
                            if (trackedEvent != null)
                            {
                                UpdatePlannedEvent(trackedEvent, record);
                                updatedEvents.Add(trackedEvent);

                                if (!existingTasks.Any(t => t.PENumber == record.PE_NUMBER))
                                {
                                    var tasks = CreateTasksForEvent(trackedEvent, record, taskTemplates);
                                    if (tasks != null)
                                    {
                                        newTasks.AddRange(tasks);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var newEvent = CreatePlannedEventFromRecord(record);
                            if (newEvent != null)
                            {
                                newEvents.Add(newEvent);
                                var tasks = CreateTasksForEvent(newEvent, record, taskTemplates);
                                if (tasks != null)
                                {
                                    newTasks.AddRange(tasks);
                                }
                            }
                        }
                    }

                    // Save changes for this batch
                    if (newEvents.Count > 0)
                    {
                        await SaveEntitiesInBatches(dbContext, newEvents, "PlannedEvents");
                    }

                    if (updatedEvents.Count > 0)
                    {
                        await SaveEntitiesInBatches(dbContext, updatedEvents, "Updated PlannedEvents");
                    }

                    if (newTasks.Count > 0)
                    {
                        await SaveEntitiesInBatches(dbContext, newTasks, "PETasks");
                    }
                }

                // Update task phases with a new context
                using (var scope = _serviceProvider.CreateScope())
                {
                    var phaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await UpdateTaskPhasesEfficiently(phaseContext);
                }

                _logger.LogInformation("PE records synchronization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PE records synchronization");
                throw;
            }
        }

        private static async Task<(List<PERecord>, List<PlannedEvent>, List<PETask>, List<PETaskList>)> LoadDataAsync(ApplicationDbContext dbContext)
        {
            try 
            {
                // Execute queries sequentially to avoid concurrent operations
                var sourceRecords = await dbContext.PERecords.AsNoTracking().ToListAsync();
                var existingEvents = await dbContext.PlannedEvents.AsNoTracking().ToListAsync();
                var existingTasks = await dbContext.PETasks.AsNoTracking().ToListAsync();
                var taskTemplates = await dbContext.PETaskLists.AsNoTracking().ToListAsync();

                return (sourceRecords, existingEvents, existingTasks, taskTemplates);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error loading data from database", ex);
            }
        }

        private async Task SaveEntitiesInBatches<T>(DbContext context, List<T> entities, string entityType, int batchSize = 100) where T : class
        {
            try
            {
                var batches = entities.Chunk(batchSize);
                var totalSaved = 0;

                foreach (var batch in batches)
                {
                    if (context.Entry(batch.First()).State == EntityState.Added)
                        context.Set<T>().AddRange(batch);
                    else
                        context.Set<T>().UpdateRange(batch);

                    await context.SaveChangesAsync();
                    totalSaved += batch.Length;
                    _logger.LogDebug("Saved {count}/{total} {type}", totalSaved, entities.Count, entityType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving {type} batch", entityType);
                throw;
            }
        }

        private async Task UpdateTaskPhasesEfficiently(ApplicationDbContext dbContext)
        {
            var sql = @"
                WITH CurrentTasks AS (
                    SELECT PE_NUMBER as PENumber, TASK_NAME as TaskName, TASK_WG as TaskWg
                    FROM PlannedEvents
                    WHERE TASK_NAME IS NOT NULL
                )
                UPDATE pt
                SET TaskStatus = CASE
                        WHEN pt.Task = (SELECT TOP 1 TaskName 
                                       FROM CurrentTasks ct 
                                       WHERE ct.PENumber = pt.PENumber) 
                        THEN 'ONGOING'
                        WHEN pt.TaskSeq < (SELECT TOP 1 pt2.TaskSeq 
                                          FROM PETasks pt2 
                                          INNER JOIN CurrentTasks ct ON ct.PENumber = pt2.PENumber
                                          WHERE pt2.PENumber = pt.PENumber 
                                          AND pt2.Task = ct.TaskName) 
                        THEN 'COMPLETED'
                        ELSE 'WAITING'
                    END,
                    TaskWorkGroup = CASE
                        WHEN pt.Task = (SELECT TOP 1 TaskName 
                                       FROM CurrentTasks ct 
                                       WHERE ct.PENumber = pt.PENumber) 
                        THEN (SELECT TOP 1 TaskWg 
                              FROM CurrentTasks ct 
                              WHERE ct.PENumber = pt.PENumber)
                        ELSE pt.TaskWorkGroup
                    END,
                    ActualTaskCreatedDate = CASE
                        WHEN pt.Task = (SELECT TOP 1 TaskName 
                                       FROM CurrentTasks ct 
                                       WHERE ct.PENumber = pt.PENumber) 
                        AND pt.ActualTaskCreatedDate IS NULL 
                        THEN GETUTCDATE()
                        ELSE pt.ActualTaskCreatedDate
                    END,
                    ActualTaskCompleteDate = CASE
                        WHEN TaskStatus = 'COMPLETED' 
                        AND pt.ActualTaskCompleteDate IS NULL 
                        THEN GETUTCDATE()
                        ELSE pt.ActualTaskCompleteDate
                    END
                FROM PETasks pt
                WHERE EXISTS (SELECT 1 FROM CurrentTasks ct WHERE ct.PENumber = pt.PENumber)";

            await dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        private void UpdatePlannedEvent(PlannedEvent existingEvent, PERecord record)
        {
            try
            {
                // Map all properties from record to existingEvent with null handling
                existingEvent.Province = record.PROVINCE ?? existingEvent.Province ?? string.Empty;
                existingEvent.Region = record.REGION ?? existingEvent.Region ?? string.Empty;
                existingEvent.Rtom = record.RTOM ?? existingEvent.Rtom ?? string.Empty;
                existingEvent.RtomDescription = record.RTOM_DESCRIPTION ?? existingEvent.RtomDescription ?? string.Empty;
                existingEvent.JobReference = record.JOB_REFERENCE ?? existingEvent.JobReference ?? string.Empty;
                existingEvent.ContractorName = record.CONTRACTOR_NAME ?? existingEvent.ContractorName ?? string.Empty;
                existingEvent.PeActivity = record.PE_ACTIVITY ?? existingEvent.PeActivity ?? string.Empty;
                existingEvent.PeNature = record.PE_NATURE ?? existingEvent.PeNature ?? string.Empty;
                existingEvent.PeTitle = record.PE_TITLE ?? existingEvent.PeTitle ?? string.Empty;
                existingEvent.PeObjective = record.PE_OBJECTIVE ?? existingEvent.PeObjective ?? string.Empty;
                existingEvent.PeArea = record.PE_AREA ?? existingEvent.PeArea ?? string.Empty;
                existingEvent.SoNumber = record.SO_NUMBER ?? existingEvent.SoNumber ?? string.Empty;
                existingEvent.TaskSeq = record.TASK_SEQ ?? existingEvent.TaskSeq;
                existingEvent.TaskName = record.TASK_NAME ?? existingEvent.TaskName ?? string.Empty;
                existingEvent.TaskWg = record.TASK_WG ?? existingEvent.TaskWg ?? string.Empty;
                existingEvent.WoActualStartDate = record.WO_ACTUAL_START_DATE ?? existingEvent.WoActualStartDate ?? string.Empty;
                existingEvent.RequestReferenceNo = record.REQUEST_REFERENCE_NO ?? existingEvent.RequestReferenceNo ?? string.Empty;
                existingEvent.SoId = record.SO_ID ?? existingEvent.SoId ?? string.Empty;
                existingEvent.Region1 = record.REGION_1 ?? existingEvent.Region1 ?? string.Empty;
                existingEvent.Province1 = record.PROVINCE_1 ?? existingEvent.Province1 ?? string.Empty;
                existingEvent.Rtom1 = record.RTOM_1 ?? existingEvent.Rtom1 ?? string.Empty;
                existingEvent.Lea = record.LEA ?? existingEvent.Lea ?? string.Empty;
                existingEvent.CctId = record.CCT_ID ?? existingEvent.CctId ?? string.Empty;
                existingEvent.ServiceCategory = record.SERVICE_CATEGORY ?? existingEvent.ServiceCategory ?? string.Empty;
                existingEvent.ServiceType = record.SERVICE_TYPE ?? existingEvent.ServiceType ?? string.Empty;
                existingEvent.SoCreateDate = record.SO_CREATE_DATE ?? existingEvent.SoCreateDate;
                existingEvent.OrderType = record.ORDER_TYPE ?? existingEvent.OrderType ?? string.Empty;
                existingEvent.CrmOrder = record.CRM_ORDER ?? existingEvent.CrmOrder ?? string.Empty;
                existingEvent.WoId = record.WO_ID ?? existingEvent.WoId ?? string.Empty;
                existingEvent.PendingTaskName = record.PENDING_TASK_NAME ?? existingEvent.PendingTaskName ?? string.Empty;
                existingEvent.PendingWg = record.PENDING_WG ?? existingEvent.PendingWg ?? string.Empty;
                existingEvent.WoStatus = record.WO_STATUS ?? existingEvent.WoStatus ?? string.Empty;
                existingEvent.WoStartDate = record.WO_START_DATE ?? existingEvent.WoStartDate;
                existingEvent.ServiceSpeed = record.SERVICE_SPEED ?? existingEvent.ServiceSpeed ?? string.Empty;
                existingEvent.ServiceRequiredDate = record.SERVICE_REQUIRED_DATE ?? existingEvent.ServiceRequiredDate;
                existingEvent.FiberPeNo = record.FIBER_PE_NO ?? existingEvent.FiberPeNo ?? string.Empty;
                existingEvent.FiberSoId = record.FIBER_SO_ID ?? existingEvent.FiberSoId ?? string.Empty;
                existingEvent.ProductSoId = record.PRODUCT_SO_ID ?? existingEvent.ProductSoId ?? string.Empty;
                existingEvent.FiberPeTaskName = record.FIBER_PE_TASK_NAME ?? existingEvent.FiberPeTaskName ?? string.Empty;
                existingEvent.FiberPeTaskWg = record.FIBER_PE_TASK_WG ?? existingEvent.FiberPeTaskWg ?? string.Empty;
                existingEvent.PeWoComments = record.PE_WO_COMMENTS ?? existingEvent.PeWoComments ?? string.Empty;
                existingEvent.Customer = record.CUSTOMER ?? existingEvent.Customer ?? string.Empty;
                existingEvent.CusType = record.CUS_TYPE ?? existingEvent.CusType ?? string.Empty;
                existingEvent.AccountManager = record.ACCOUNT_MANAGER ?? existingEvent.AccountManager ?? string.Empty;
                existingEvent.SectionHandledBy = record.SECTION_HANDLED_BY ?? existingEvent.SectionHandledBy ?? string.Empty;
                existingEvent.LocationAAddress = record.LOCATION_A_ADDRESS ?? existingEvent.LocationAAddress ?? string.Empty;
                existingEvent.LocationBAddress = record.LOCATION_B_ADDRESS ?? existingEvent.LocationBAddress ?? string.Empty;
                existingEvent.NtuType = record.NTU_TYPE ?? existingEvent.NtuType ?? string.Empty;
                existingEvent.AccessMedium = record.ACCESS_MEDIUM ?? existingEvent.AccessMedium ?? string.Empty;
                existingEvent.AccessMediumAEnd = record.ACCESS_MEDIUM_A_END ?? existingEvent.AccessMediumAEnd ?? string.Empty;
                existingEvent.AccessMediumBEnd = record.ACCESS_MEDIUM_B_END ?? existingEvent.AccessMediumBEnd ?? string.Empty;
                existingEvent.Priority = record.WO_COMMENTS ?? existingEvent.Priority ?? string.Empty;
                
                // Keep PEStatus as is or set to default if null
                existingEvent.PEStatus = existingEvent.PEStatus ?? "ongoing";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating planned event for PE: {peNumber}", record.PE_NUMBER);
            }
        }

        private PlannedEvent CreatePlannedEventFromRecord(PERecord record)
        {
            try
            {
                _logger.LogDebug("Creating PlannedEvent from record with PE_NUMBER: {peNumber}", record.PE_NUMBER);

                // Create the event with null safety for all fields
                var plannedEvent = new PlannedEvent
                {
                    Province = record.PROVINCE ?? string.Empty,
                    Region = record.REGION ?? string.Empty,
                    Rtom = record.RTOM ?? string.Empty,
                    RtomDescription = record.RTOM_DESCRIPTION ?? string.Empty,
                    JobReference = record.JOB_REFERENCE ?? string.Empty,
                    ContractorName = record.CONTRACTOR_NAME ?? string.Empty,
                    PeNumber = record.PE_NUMBER ?? string.Empty, // This is critical - don't allow null
                    PeActivity = record.PE_ACTIVITY ?? string.Empty,
                    PeNature = record.PE_NATURE ?? string.Empty,
                    PeTitle = record.PE_TITLE ?? string.Empty,
                    PeObjective = record.PE_OBJECTIVE ?? string.Empty,
                    PeArea = record.PE_AREA ?? string.Empty,
                    SoNumber = record.SO_NUMBER ?? string.Empty,
                    TaskSeq = record.TASK_SEQ,
                    TaskName = record.TASK_NAME ?? string.Empty,
                    TaskWg = record.TASK_WG ?? string.Empty,
                    WoActualStartDate = record.WO_ACTUAL_START_DATE ?? string.Empty,
                    RequestReferenceNo = record.REQUEST_REFERENCE_NO ?? string.Empty,
                    SoId = record.SO_ID ?? string.Empty,
                    Region1 = record.REGION_1 ?? string.Empty,
                    Province1 = record.PROVINCE_1 ?? string.Empty,
                    Rtom1 = record.RTOM_1 ?? string.Empty,
                    Lea = record.LEA ?? string.Empty,
                    CctId = record.CCT_ID ?? string.Empty,
                    ServiceCategory = record.SERVICE_CATEGORY ?? string.Empty,
                    ServiceType = record.SERVICE_TYPE ?? string.Empty,
                    SoCreateDate = record.SO_CREATE_DATE,
                    OrderType = record.ORDER_TYPE ?? string.Empty,
                    CrmOrder = record.CRM_ORDER ?? string.Empty,
                    WoId = record.WO_ID ?? string.Empty,
                    PendingTaskName = record.PENDING_TASK_NAME ?? string.Empty,
                    PendingWg = record.PENDING_WG ?? string.Empty,
                    WoStatus = record.WO_STATUS ?? string.Empty,
                    WoStartDate = record.WO_START_DATE,
                    ServiceSpeed = record.SERVICE_SPEED ?? string.Empty,
                    ServiceRequiredDate = record.SERVICE_REQUIRED_DATE,
                    FiberPeNo = record.FIBER_PE_NO ?? string.Empty,
                    FiberSoId = record.FIBER_SO_ID ?? string.Empty,
                    ProductSoId = record.PRODUCT_SO_ID ?? string.Empty,
                    FiberPeTaskName = record.FIBER_PE_TASK_NAME ?? string.Empty,
                    FiberPeTaskWg = record.FIBER_PE_TASK_WG ?? string.Empty,
                    PeWoComments = record.PE_WO_COMMENTS ?? string.Empty,
                    Customer = record.CUSTOMER ?? string.Empty,
                    CusType = record.CUS_TYPE ?? string.Empty,
                    AccountManager = record.ACCOUNT_MANAGER ?? string.Empty,
                    SectionHandledBy = record.SECTION_HANDLED_BY ?? string.Empty,
                    LocationAAddress = record.LOCATION_A_ADDRESS ?? string.Empty,
                    LocationBAddress = record.LOCATION_B_ADDRESS ?? string.Empty,
                    NtuType = record.NTU_TYPE ?? string.Empty,
                    AccessMedium = record.ACCESS_MEDIUM ?? string.Empty,
                    AccessMediumAEnd = record.ACCESS_MEDIUM_A_END ?? string.Empty,
                    AccessMediumBEnd = record.ACCESS_MEDIUM_B_END ?? string.Empty,
                    Priority = record.WO_COMMENTS ?? string.Empty,
                    PEStatus = "ongoing", // Default status for new records
                    PECreatedDate = DateTime.UtcNow
                };

                return plannedEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PlannedEvent from record with PE_NUMBER: {peNumber}", record.PE_NUMBER);
                throw; // Rethrow to be caught by caller
            }
        }

        private List<PETask> CreateTasksForEvent(PlannedEvent plannedEvent, PERecord record, List<PETaskList> taskListTemplates)
        {
            var tasksToCreate = new List<PETask>();
            DateTime currentDate = DateTime.UtcNow;
            DateTime previousTaskCompleteDate = currentDate; // Start with the current date for the first task

            // Order task templates by sequence
            var orderedTaskTemplates = taskListTemplates
                .Where(t => !string.IsNullOrEmpty(t.OLA_Parameters)) // Only include templates with OLA values
                .OrderBy(t => t.TaskSeq)
                .ToList();

            _logger.LogInformation("Found {count} ordered task templates with OLA values for PE {peNumber}",
                orderedTaskTemplates.Count, plannedEvent.PeNumber);

            foreach (var taskTemplate in orderedTaskTemplates)
            {
                try
                {
                    // Calculate OLA close date - assume OLA_Parameters contains days
                    int olaDays = 0;
                    bool validOla = int.TryParse(taskTemplate.OLA_Parameters, out olaDays);

                    if (validOla)
                    {
                        _logger.LogDebug("Processing task template: {name} with OLA: {ola} days",
                            taskTemplate.Name, olaDays);

                        // Set up creation date (current date for first task, or previous task complete date for subsequent tasks)
                        DateTime taskCreatedDate = previousTaskCompleteDate;

                        // Calculate complete date based on OLA
                        DateTime taskCompleteDate = taskCreatedDate.AddDays(olaDays);

                        // For the next task, its created date will be this task's complete date
                        previousTaskCompleteDate = taskCompleteDate;

                        // Determine initial status based on current task
                        string initialStatus = "WAITING"; // Default status is waiting
                        
                        if (taskTemplate.Name == record.TASK_NAME)
                        {
                            initialStatus = "ONGOING"; // This is the current task
                        }
                        else if (orderedTaskTemplates.IndexOf(taskTemplate) < orderedTaskTemplates.FindIndex(t => t.Name == record.TASK_NAME))
                        {
                            initialStatus = "COMPLETED"; // This task comes before the current task
                        }

                        // Create task for this PE, safely handling nullable fields
                        var peTask = new PETask
                        {
                            PENumber = plannedEvent.PeNumber ?? string.Empty,
                            TaskSeq = taskTemplate.TaskSeq,
                            Task = taskTemplate.Name ?? string.Empty,
                            OLA = taskTemplate.OLA_Parameters ?? string.Empty,
                            TaskStatus = initialStatus,
                            TaskCreatedDate = taskCreatedDate,
                            TaskCompleteDate = taskCompleteDate,
                            TaskWorkGroup = "NULL", // Default value
                            // Initialize optional fields to avoid database null constraint violations
                            ActualTaskCreatedDate = null,
                            ACtualTaskCompleteDate = null,
                            IsUrgent = false
                        };

                        // If this is the current task in the PE record, set actual dates and work group
                        if (taskTemplate.Name == record.TASK_NAME)
                        {
                            peTask.TaskWorkGroup = record.TASK_WG ?? "NULL";
                            peTask.ActualTaskCreatedDate = currentDate;
                        }
                        // If this task is completed, set the actual complete date
                        else if (initialStatus == "COMPLETED")
                        {
                            peTask.ACtualTaskCompleteDate = currentDate;
                        }

                        tasksToCreate.Add(peTask);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid OLA parameter for task template {name}: {ola}",
                            taskTemplate.Name, taskTemplate.OLA_Parameters);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing task template {name} for PE {peNumber}",
                        taskTemplate.Name, plannedEvent.PeNumber);
                    // Continue processing other templates
                }
            }

            return tasksToCreate;
        }

        private async Task CreatePETasksForEvent(ApplicationDbContext dbContext, PlannedEvent plannedEvent, PERecord record, List<PETaskList> taskListTemplates)
        {
            try
            {
                // Check if templates exist
                if (taskListTemplates == null || taskListTemplates.Count == 0)
                {
                    _logger.LogWarning("No task list templates found. Cannot create tasks for PE Number: {peNumber}", plannedEvent.PeNumber);
                    return;
                }

                // Log how many templates we're working with
                _logger.LogInformation("Creating tasks for PE {peNumber} using {count} task templates", plannedEvent.PeNumber, taskListTemplates.Count);

                // First check if tasks already exist for this PE Number
                var existingTasks = await dbContext.PETasks
                    .Where(t => t.PENumber == plannedEvent.PeNumber)
                    .ToListAsync();

                if (existingTasks.Any())
                {
                    _logger.LogInformation("Tasks already exist for PE {peNumber}. Skipping task creation.", plannedEvent.PeNumber);
                    return; // Skip if tasks already exist
                }

                var tasksToCreate = CreateTasksForEvent(plannedEvent, record, taskListTemplates);

                if (tasksToCreate.Count > 0)
                {
                    try
                    {
                        _logger.LogInformation("Adding {count} tasks for PE {peNumber}", tasksToCreate.Count, plannedEvent.PeNumber);
                        
                        // Save tasks individually to identify problematic records
                        foreach (var task in tasksToCreate)
                        {
                            try
                            {
                                // Validate task before adding
                                if (string.IsNullOrEmpty(task.PENumber))
                                {
                                    _logger.LogWarning("Skipping task with null PENumber");
                                    continue;
                                }
                                
                                dbContext.PETasks.Add(task);
                                await dbContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to add task {taskName} for PE {peNumber}", 
                                    task.Task, task.PENumber);
                                
                                // Log detailed information about the failing record
                                _logger.LogError("Task data: PENumber={PENumber}, TaskSeq={TaskSeq}, Task={Task}, " +
                                    "TaskWorkGroup={TaskWorkGroup}, OLA={OLA}, TaskStatus={TaskStatus}, " +
                                    "TaskCreatedDate={TaskCreatedDate}, TaskCompleteDate={TaskCompleteDate}",
                                    task.PENumber, task.TaskSeq, task.Task, task.TaskWorkGroup, task.OLA,
                                    task.TaskStatus, task.TaskCreatedDate, task.TaskCompleteDate);
                                
                                // Continue with other tasks
                            }
                        }
                        
                        _logger.LogInformation("Successfully processed tasks for PE {peNumber}", plannedEvent.PeNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving tasks batch for PE {peNumber}", plannedEvent.PeNumber);
                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning("No tasks were created for PE {peNumber} - task templates may be missing OLA values", plannedEvent.PeNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tasks for PE {peNumber}", plannedEvent.PeNumber);
                throw; // Rethrow to be caught by the calling method
            }
        }

        private async Task UpdateTaskPhases(ApplicationDbContext dbContext)
        {
            try {
                // Get all planned events
                var plannedEvents = await dbContext.PlannedEvents.ToListAsync();
                var peTasks = await dbContext.PETasks.ToListAsync();
                DateTime currentDate = DateTime.UtcNow;

                foreach (var plannedEvent in plannedEvents)
                {
                    if (string.IsNullOrEmpty(plannedEvent.PeNumber))
                        continue;

                    var tasksForPE = peTasks
                        .Where(t => t.PENumber == plannedEvent.PeNumber)
                        .OrderBy(t => t.TaskSeq)
                        .ToList();

                    // Skip if no tasks found for this PE
                    if (!tasksForPE.Any())
                    {
                        _logger.LogWarning("No tasks found for PE {peNumber} during phase update", plannedEvent.PeNumber);
                        continue;
                    }

                    // Find the current task based on TaskName in PlannedEvent
                    var currentTaskName = plannedEvent.TaskName;

                    if (!string.IsNullOrEmpty(currentTaskName))
                    {
                        bool currentTaskFound = false;
                        bool hasChanges = false;

                        foreach (var task in tasksForPE)
                        {
                            try {
                                if (task.Task == currentTaskName)
                                {
                                    // This is the current task
                                    task.TaskStatus = "ONGOING";
                                    task.TaskWorkGroup = plannedEvent.TaskWg ?? task.TaskWorkGroup ?? "NULL";
                                    currentTaskFound = true;
                                    hasChanges = true;
                                    
                                    // Handle null ActualTaskCreatedDate
                                    if (!task.ActualTaskCreatedDate.HasValue)
                                    {
                                        task.ActualTaskCreatedDate = currentDate;
                                    }
                                }
                                else if (!currentTaskFound)
                                {
                                    // Tasks before the current task are finished
                                    task.TaskStatus = "COMPLETED";
                                    hasChanges = true;
                                    
                                    // Handle null ACtualTaskCompleteDate
                                    if (!task.ACtualTaskCompleteDate.HasValue)
                                    {
                                        task.ACtualTaskCompleteDate = currentDate;
                                    }
                                }
                                else
                                {
                                    // Tasks after the current task are waiting
                                    task.TaskStatus = "WAITING";
                                    hasChanges = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error updating task {taskId} for PE {peNumber}", task.Id, plannedEvent.PeNumber);
                            }
                        }

                        if (hasChanges)
                        {
                            try
                            {
                                // Save changes for this PE's tasks only
                                await dbContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error saving phase updates for PE {peNumber}", plannedEvent.PeNumber);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateTaskPhases");
            }
        }

        private void LogDbContextError(Exception ex, string contextMessage)
        {
            _logger.LogError(ex, "{contextMessage}: {message}", contextMessage, ex.Message);
            
            // Check for inner exception which often contains the real error
            if (ex is DbUpdateException dbEx && dbEx.InnerException != null)
            {
                _logger.LogError("Inner exception: {message}", dbEx.InnerException.Message);
                
                // Log entity validation errors if present
                var entries = (dbEx as DbUpdateException)?.Entries;
                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        _logger.LogError("Problem entity: {entity} State: {state}", 
                            entry.Entity.GetType().Name, entry.State);
                    }
                }
            }
        }

        public async Task<string> DiagnoseDatabaseIssues()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var diagnosticInfo = new System.Text.StringBuilder();
                    
                    // Check PERecord table
                    var recordCount = await dbContext.PERecords.CountAsync();
                    diagnosticInfo.AppendLine($"PERecord table count: {recordCount}");
                    
                    if (recordCount > 0)
                    {
                        var sampleRecord = await dbContext.PERecords.FirstOrDefaultAsync();
                        diagnosticInfo.AppendLine("Sample PERecord:");
                        diagnosticInfo.AppendLine($"- PE_NUMBER: {sampleRecord.PE_NUMBER ?? "NULL"}");
                        diagnosticInfo.AppendLine($"- TASK_NAME: {sampleRecord.TASK_NAME ?? "NULL"}");
                        diagnosticInfo.AppendLine($"- TASK_WG: {sampleRecord.TASK_WG ?? "NULL"}");
                    }
                    
                    // Check PlannedEvent table
                    var eventCount = await dbContext.PlannedEvents.CountAsync();
                    diagnosticInfo.AppendLine($"PlannedEvent table count: {eventCount}");
                    
                    // Check PETask table
                    var taskCount = await dbContext.PETasks.CountAsync();
                    diagnosticInfo.AppendLine($"PETask table count: {taskCount}");
                    
                    // Check PETaskList table (templates)
                    var templateCount = await dbContext.PETaskLists.CountAsync();
                    diagnosticInfo.AppendLine($"PETaskList table count: {templateCount}");
                    
                    if (templateCount > 0)
                    {
                        var sampleTemplate = await dbContext.PETaskLists.FirstOrDefaultAsync();
                        diagnosticInfo.AppendLine("Sample PETaskList:");
                        diagnosticInfo.AppendLine($"- Name: {sampleTemplate.Name ?? "NULL"}");
                        diagnosticInfo.AppendLine($"- TaskSeq: {sampleTemplate.TaskSeq}");
                        diagnosticInfo.AppendLine($"- OLA_Parameters: {sampleTemplate.OLA_Parameters ?? "NULL"}");
                    }
                    
                    return diagnosticInfo.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"Diagnostic error: {ex.Message}\n{ex.StackTrace}";
            }
        }
    }
}