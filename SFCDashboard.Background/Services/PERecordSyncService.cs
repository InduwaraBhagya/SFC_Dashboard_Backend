using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFCDashboard.Background.Models;
using SFCDashboard.Background.Data;

namespace SFCDashboard.Background.Services
{
    public class PERecordSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PERecordSyncService> _logger;
        private readonly TimeSpan[] _scheduledTimes = { 
            new TimeSpan(6, 0, 0),   // 6:00 AM
            new TimeSpan(13, 0, 0)   // 1:00 PM
        };

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
                    var nextRunTime = GetNextRunTime();
                    var delay = nextRunTime - DateTime.Now;

                    if (delay > TimeSpan.Zero)
                    {
                        _logger.LogInformation("Next PE records synchronization scheduled for {nextRun}", nextRunTime);
                        await Task.Delay(delay, stoppingToken);
                    }

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Starting PE records synchronization at {time}", DateTimeOffset.Now);

                        await SyncPERecordsAsync();

                        _logger.LogInformation("PE records synchronization completed at {time}", DateTimeOffset.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during PE records synchronization");
                    // Wait 1 hour before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private DateTime GetNextRunTime()
        {
            var now = DateTime.Now;
            var today = now.Date;

            // Check if any scheduled time today is still in the future
            foreach (var scheduledTime in _scheduledTimes.OrderBy(t => t))
            {
                var scheduledDateTime = today + scheduledTime;
                if (scheduledDateTime > now)
                {
                    return scheduledDateTime;
                }
            }

            // All times for today have passed, get the first time for tomorrow
            var tomorrow = today.AddDays(1);
            return tomorrow + _scheduledTimes.OrderBy(t => t).First();
        }

        public async Task SyncPERecordsAsync()
        {
            try
            {
                _logger.LogInformation("Starting PE records synchronization");

                // First, fix any existing tasks with null OLADateTime
                using (var scope = _serviceProvider.CreateScope())
                {
                    var fixingContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await FixExistingTaskOLADates(fixingContext);
                }

                // Load data with a dedicated context
                List<PERecord> sourceRecords;
                List<PlannedEvent> existingEvents;
                List<PETask> existingTasks;
                List<PETaskList> taskTemplates;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var loadingContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var dataLoadingTasks = await LoadDataAsync(loadingContext);
                    sourceRecords = dataLoadingTasks.sourceRecords;
                    existingEvents = dataLoadingTasks.existingEvents;
                    existingTasks = dataLoadingTasks.existingTasks;
                    taskTemplates = dataLoadingTasks.taskTemplates;
                }

                if (sourceRecords.Count == 0 || taskTemplates.Count == 0)
                {
                    _logger.LogWarning("Source records or task templates are empty. Cannot proceed with synchronization.");
                    return;
                }

                // Group records by PE_NUMBER to handle duplicates, filtering out null/empty PE_NUMBER
                var groupedRecords = sourceRecords
                    .Where(r => !string.IsNullOrEmpty(r.PE_NUMBER))
                    .GroupBy(r => r.PE_NUMBER!)
                    .ToDictionary(g => g.Key, g => g.ToList());

                _logger.LogInformation("Grouped {sourceCount} source records into {groupCount} unique PE numbers", 
                    sourceRecords.Count, groupedRecords.Count);

                // Create lookup dictionary for faster searching
                var existingEventsByPeNumber = existingEvents
                    .Where(pe => !string.IsNullOrEmpty(pe.PeNumber))
                    .ToDictionary(
                    pe => pe.PeNumber!, 
                    pe => pe, 
                    StringComparer.OrdinalIgnoreCase); // Case insensitive comparison

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    // Track metrics
                    int newEventCount = 0;
                    int updatedEventCount = 0;
                    int duplicateCount = 0;

                    // Process each unique PE_NUMBER group
                    foreach (var group in groupedRecords)
                    {
                        try
                        {
                            string peNumber = group.Key;
                            var records = group.Value;

                            if (string.IsNullOrEmpty(peNumber))
                            {
                                _logger.LogWarning("Skipping group with null or empty PE_NUMBER");
                                continue;
                            }

                            if (records.Count > 1)
                            {
                                _logger.LogWarning("Found {count} duplicate records for PE_NUMBER: {peNumber}", 
                                    records.Count, peNumber);
                                duplicateCount += records.Count - 1;
                                
                                // Use the most recent record in case of duplicates
                                // This assumes there's a timestamp field to determine which is most recent
                                // If not, you might need a different strategy
                                var mostRecentRecord = records.OrderByDescending(r => r.SO_CREATE_DATE ?? DateTime.MinValue).First();
                                records = new List<PERecord> { mostRecentRecord };
                            }

                            // Now process the single/most recent record
                            var record = records.First();
                            bool eventExists = existingEventsByPeNumber.TryGetValue(peNumber, out PlannedEvent? existingEvent);

                            if (eventExists)
                            {
                                _logger.LogDebug("Found existing event for PE_NUMBER: {peNumber}", peNumber);
                                
                                // Fetch the actual entity from database to update it
                                var trackedEvent = await dbContext.PlannedEvents
                                    .FirstOrDefaultAsync(pe => pe.PeNumber == peNumber);
                                    
                                if (trackedEvent != null)
                                {
                                    // Update existing entity
                                    UpdatePlannedEvent(trackedEvent, record);
                                    await dbContext.SaveChangesAsync();
                                    updatedEventCount++;
                                    
                                    // Check if tasks need to be created
                                    if (!existingTasks.Any(t => t.PENumber == peNumber))
                                    {
                                        await CreatePETasksForEvent(dbContext, trackedEvent, record, taskTemplates);
                                    }
                                    // Added line to update tasks for the existing event
                                    await UpdatePETasksForEvent(dbContext, trackedEvent, record, taskTemplates);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Creating new event for PE_NUMBER: {peNumber}", peNumber);
                                
                                // Do a final duplicate check to be safe
                                var duplicateCheck = await dbContext.PlannedEvents
                                    .FirstOrDefaultAsync(pe => pe.PeNumber == peNumber);
                                    
                                if (duplicateCheck == null)
                                {
                                    // Create new event
                                    var newEvent = CreatePlannedEventFromRecord(record);
                                    dbContext.PlannedEvents.Add(newEvent);
                                    
                                    // Save immediately to get the ID before creating tasks
                                    await dbContext.SaveChangesAsync();
                                    newEventCount++;
                                    
                                    // Create tasks for the new event
                                    await CreatePETasksForEvent(dbContext, newEvent, record, taskTemplates);
                                }
                                else
                                {
                                    _logger.LogWarning("Found duplicate for PE_NUMBER: {peNumber} despite not being in initial list", peNumber);
                                    // Update the existing record that was found
                                    UpdatePlannedEvent(duplicateCheck, record);
                                    await dbContext.SaveChangesAsync();
                                    updatedEventCount++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing record group with PE_NUMBER: {peNumber}", group.Key);
                            // Continue with next group
                        }
                    }

                    _logger.LogInformation("Sync completed. New events: {newCount}, Updated: {updatedCount}, Duplicates handled: {duplicateCount}",
                        newEventCount, updatedEventCount, duplicateCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PE records synchronization");
            }
        }

        private static async Task<(List<PERecord> sourceRecords, List<PlannedEvent> existingEvents, List<PETask> existingTasks, List<PETaskList> taskTemplates)> LoadDataAsync(ApplicationDbContext dbContext)
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
                    //Priority = record.WO_COMMENTS ?? string.Empty,
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

                        // Get workgroup from template or record
                        string workgroup = "NULL";  // Default value
                        
                        // If this is the current task, use the workgroup from the record/PlannedEvent
                        if (taskTemplate.Name == record.TASK_NAME && !string.IsNullOrEmpty(record.TASK_WG))
                        {
                            workgroup = record.TASK_WG;
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
                            TaskWorkGroup = workgroup,  // Set from determined workgroup
                            // Initialize optional fields to avoid database null constraint violations
                            ActualTaskCreatedDate = null,
                            ACtualTaskCompleteDate = null,
                            IsUrgent = false,
                            UrgentRequested = false, // Initialize UrgentRequested flag
                            Priority = string.Empty, // Initialize Priority field
                            OLADateTime = taskCompleteDate, // Set OLA datetime to the calculated complete date
                            IsOLAViolate = false, // Initialize OLA violation flag
                            ViolationStartTime = null, // Initialize violation time
                            EscalationsDisabled = false // Initialize escalations flag
                        };

                        // If this is the current task in the PE record, set actual dates and work group
                        if (taskTemplate.Name == record.TASK_NAME)
                        {
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
                    return;
                }

                // Create tasks without tracking the parent event
                var tasksToCreate = CreateTasksForEvent(plannedEvent, record, taskListTemplates);

                if (tasksToCreate.Count > 0)
                {
                    try
                    {
                        _logger.LogInformation("Adding {count} tasks for PE {peNumber} in batches", tasksToCreate.Count, plannedEvent.PeNumber);
                        
                        // Process in batches of 100
                        const int batchSize = 100;
                        for (int i = 0; i < tasksToCreate.Count; i += batchSize)
                        {
                            var batch = tasksToCreate.Skip(i).Take(batchSize).ToList();
                            
                            // Validate batch before adding
                            var validBatch = batch.Where(task => !string.IsNullOrEmpty(task.PENumber)).ToList();
                            
                            if (validBatch.Any())
                            {
                                dbContext.PETasks.AddRange(validBatch);
                                await dbContext.SaveChangesAsync();
                                
                                _logger.LogInformation("Saved batch of {count} tasks for PE {peNumber}", 
                                    validBatch.Count, plannedEvent.PeNumber);
                            }
                            
                            // Log any skipped tasks
                            var skippedCount = batch.Count - validBatch.Count;
                            if (skippedCount > 0)
                            {
                                _logger.LogWarning("Skipped {count} invalid tasks in batch", skippedCount);
                            }
                        }
                        
                        _logger.LogInformation("Successfully processed all tasks for PE {peNumber}", plannedEvent.PeNumber);
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
                throw;
            }
        }

        private async Task UpdatePETasksForEvent(
            ApplicationDbContext dbContext,
            PlannedEvent plannedEvent,
            PERecord record,
            List<PETaskList> taskTemplates)
        {
            try
            {
                var peNumber = plannedEvent.PeNumber;
                if (string.IsNullOrEmpty(peNumber))
                    return;

                // Get all existing tasks for this PE
                var existingTasks = await dbContext.PETasks
                    .Where(t => t.PENumber == peNumber)
                    .ToListAsync();

                // Build the latest set of tasks from templates
                var templateTasks = CreateTasksForEvent(plannedEvent, record, taskTemplates);

                foreach (var templateTask in templateTasks)
                {
                    var existingTask = existingTasks
                        .FirstOrDefault(t => t.TaskSeq == templateTask.TaskSeq && t.Task == templateTask.Task);

                    if (existingTask != null)
                    {
                        // Update fields from template/task record
                        existingTask.OLA = templateTask.OLA;
                        existingTask.OLADateTime = templateTask.OLADateTime; // Update OLA datetime
                        
                        // WORKGROUP HANDLING: Prioritize task-specific workgroups
                        // If this is the current task in the PE record, use record's workgroup
                        if (existingTask.Task == record.TASK_NAME && !string.IsNullOrEmpty(record.TASK_WG))
                        {
                            existingTask.TaskWorkGroup = record.TASK_WG;
                            _logger.LogDebug("Setting workgroup for current task {task} to {workgroup}", 
                                existingTask.Task, record.TASK_WG);
                        }
                        // Otherwise, don't override an existing valid workgroup with a null or empty one
                        else if (string.IsNullOrEmpty(existingTask.TaskWorkGroup) || existingTask.TaskWorkGroup == "NULL")
                        {
                            // Only update if template has a non-null workgroup
                            if (!string.IsNullOrEmpty(templateTask.TaskWorkGroup) && templateTask.TaskWorkGroup != "NULL")
                            {
                                existingTask.TaskWorkGroup = templateTask.TaskWorkGroup;
                                _logger.LogDebug("Setting workgroup for task {task} from template to {workgroup}", 
                                    existingTask.Task, templateTask.TaskWorkGroup);
                            }
                        }
                        
                        existingTask.TaskStatus = templateTask.TaskStatus;
                        existingTask.TaskCreatedDate = templateTask.TaskCreatedDate;
                        existingTask.TaskCompleteDate = templateTask.TaskCompleteDate;
                        
                        // Only set actual dates if not already set
                        if (!existingTask.ActualTaskCreatedDate.HasValue && templateTask.ActualTaskCreatedDate.HasValue)
                            existingTask.ActualTaskCreatedDate = templateTask.ActualTaskCreatedDate;
                        if (!existingTask.ACtualTaskCompleteDate.HasValue && templateTask.ACtualTaskCompleteDate.HasValue)
                            existingTask.ACtualTaskCompleteDate = templateTask.ACtualTaskCompleteDate;
                        // Preserve IsUrgent and UrgentRequested
                    }
                    else
                    {
                        // New task, add it
                        dbContext.PETasks.Add(templateTask);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PETasks for PE {peNumber}", plannedEvent.PeNumber);
            }
        }

        private async Task FixExistingTaskOLADates(ApplicationDbContext dbContext)
        {
            try
            {
                // Find tasks with null OLADateTime and set them based on TaskCompleteDate
                var tasksWithNullOLA = await dbContext.PETasks
                    .Where(t => t.OLADateTime == null)
                    .ToListAsync();

                if (tasksWithNullOLA.Any())
                {
                    _logger.LogInformation("Found {count} tasks with null OLADateTime, fixing them", tasksWithNullOLA.Count);
                    
                    foreach (var task in tasksWithNullOLA)
                    {
                        // Set OLADateTime to TaskCompleteDate
                        task.OLADateTime = task.TaskCompleteDate;
                    }
                    
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Fixed OLADateTime for {count} tasks", tasksWithNullOLA.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing existing task OLA dates");
            }
        }
    }
}
