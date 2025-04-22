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
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get all PE records from the source table
                var sourceRecords = await dbContext.PERecords.ToListAsync();

                // Get all existing planned events
                var existingPlannedEvents = await dbContext.PlannedEvents.ToListAsync();

                // Get all existing PE tasks
                var existingPETasks = await dbContext.PETasks.ToListAsync();

                // Get all task list templates
                var taskListTemplates = await dbContext.PETaskLists.ToListAsync();

                // Track processed PE numbers to avoid duplicates
                var processedPENumbers = new HashSet<string>();

                foreach (var record in sourceRecords)
                {
                    if (string.IsNullOrEmpty(record.PE_NUMBER) || processedPENumbers.Contains(record.PE_NUMBER))
                        continue;

                    processedPENumbers.Add(record.PE_NUMBER);

                    // Check if this PE number already exists
                    var existingEvent = existingPlannedEvents.FirstOrDefault(pe => pe.PeNumber == record.PE_NUMBER);

                    if (existingEvent != null)
                    {
                        // Update existing record with new data
                        UpdatePlannedEvent(existingEvent, record);
                        dbContext.PlannedEvents.Update(existingEvent);
                        await dbContext.SaveChangesAsync(); // Save to ensure changes are persisted

                        // Check if tasks exist for this event
                        bool tasksExist = existingPETasks.Any(t => t.PENumber == record.PE_NUMBER);

                        // Create tasks if they don't exist
                        if (!tasksExist)
                        {
                            await CreatePETasksForEvent(dbContext, existingEvent, record, taskListTemplates);
                        }
                    }
                    else
                    {
                        // Create new planned event
                        var newEvent = CreatePlannedEventFromRecord(record);
                        dbContext.PlannedEvents.Add(newEvent);
                        await dbContext.SaveChangesAsync(); // Save to generate ID

                        // Create associated tasks
                        await CreatePETasksForEvent(dbContext, newEvent, record, taskListTemplates);
                    }
                }

                // Update task phases based on current task status
                await UpdateTaskPhases(dbContext);

                await dbContext.SaveChangesAsync();
            }
        }


        private void UpdatePlannedEvent(PlannedEvent existingEvent, PERecord record)
        {
            // Map all properties from record to existingEvent
            existingEvent.Province = record.PROVINCE;
            existingEvent.Region = record.REGION;
            existingEvent.Rtom = record.RTOM;
            existingEvent.RtomDescription = record.RTOM_DESCRIPTION;
            existingEvent.JobReference = record.JOB_REFERENCE;
            existingEvent.ContractorName = record.CONTRACTOR_NAME;
            existingEvent.PeActivity = record.PE_ACTIVITY;
            existingEvent.PeNature = record.PE_NATURE;
            existingEvent.PeTitle = record.PE_TITLE;
            existingEvent.PeObjective = record.PE_OBJECTIVE;
            existingEvent.PeArea = record.PE_AREA;
            existingEvent.SoNumber = record.SO_NUMBER;
            existingEvent.TaskSeq = record.TASK_SEQ;
            existingEvent.TaskName = record.TASK_NAME;
            existingEvent.TaskWg = record.TASK_WG;
            existingEvent.WoActualStartDate = record.WO_ACTUAL_START_DATE;
            existingEvent.RequestReferenceNo = record.REQUEST_REFERENCE_NO;
            existingEvent.SoId = record.SO_ID;
            existingEvent.Region1 = record.REGION_1;
            existingEvent.Province1 = record.PROVINCE_1;
            existingEvent.Rtom1 = record.RTOM_1;
            existingEvent.Lea = record.LEA;
            existingEvent.CctId = record.CCT_ID;
            existingEvent.ServiceCategory = record.SERVICE_CATEGORY;
            existingEvent.ServiceType = record.SERVICE_TYPE;
            existingEvent.SoCreateDate = record.SO_CREATE_DATE;
            existingEvent.OrderType = record.ORDER_TYPE;
            existingEvent.CrmOrder = record.CRM_ORDER;
            existingEvent.WoId = record.WO_ID;
            existingEvent.PendingTaskName = record.PENDING_TASK_NAME;
            existingEvent.PendingWg = record.PENDING_WG;
            existingEvent.WoStatus = record.WO_STATUS;
            existingEvent.WoStartDate = record.WO_START_DATE;
            existingEvent.ServiceSpeed = record.SERVICE_SPEED;
            existingEvent.ServiceRequiredDate = record.SERVICE_REQUIRED_DATE;
            existingEvent.FiberPeNo = record.FIBER_PE_NO;
            existingEvent.FiberSoId = record.FIBER_SO_ID;
            existingEvent.ProductSoId = record.PRODUCT_SO_ID;
            existingEvent.FiberPeTaskName = record.FIBER_PE_TASK_NAME;
            existingEvent.FiberPeTaskWg = record.FIBER_PE_TASK_WG;
            existingEvent.PeWoComments = record.PE_WO_COMMENTS;
            existingEvent.Customer = record.CUSTOMER;
            existingEvent.CusType = record.CUS_TYPE;
            existingEvent.AccountManager = record.ACCOUNT_MANAGER;
            existingEvent.SectionHandledBy = record.SECTION_HANDLED_BY;
            existingEvent.LocationAAddress = record.LOCATION_A_ADDRESS;
            existingEvent.LocationBAddress = record.LOCATION_B_ADDRESS;
            existingEvent.NtuType = record.NTU_TYPE;
            existingEvent.AccessMedium = record.ACCESS_MEDIUM;
            existingEvent.AccessMediumAEnd = record.ACCESS_MEDIUM_A_END;
            existingEvent.AccessMediumBEnd = record.ACCESS_MEDIUM_B_END;
            existingEvent.WoComments = record.WO_COMMENTS;
            // Keep PEStatus as is or set to default if null
            existingEvent.PEStatus = existingEvent.PEStatus ?? "ongoing";
        }

        private PlannedEvent CreatePlannedEventFromRecord(PERecord record)
        {
            return new PlannedEvent
            {
                Province = record.PROVINCE,
                Region = record.REGION,
                Rtom = record.RTOM,
                RtomDescription = record.RTOM_DESCRIPTION,
                JobReference = record.JOB_REFERENCE,
                ContractorName = record.CONTRACTOR_NAME,
                PeNumber = record.PE_NUMBER,
                PeActivity = record.PE_ACTIVITY,
                PeNature = record.PE_NATURE,
                PeTitle = record.PE_TITLE,
                PeObjective = record.PE_OBJECTIVE,
                PeArea = record.PE_AREA,
                SoNumber = record.SO_NUMBER,
                TaskSeq = record.TASK_SEQ,
                TaskName = record.TASK_NAME,
                TaskWg = record.TASK_WG,
                WoActualStartDate = record.WO_ACTUAL_START_DATE,
                RequestReferenceNo = record.REQUEST_REFERENCE_NO,
                SoId = record.SO_ID,
                Region1 = record.REGION_1,
                Province1 = record.PROVINCE_1,
                Rtom1 = record.RTOM_1,
                Lea = record.LEA,
                CctId = record.CCT_ID,
                ServiceCategory = record.SERVICE_CATEGORY,
                ServiceType = record.SERVICE_TYPE,
                SoCreateDate = record.SO_CREATE_DATE,
                OrderType = record.ORDER_TYPE,
                CrmOrder = record.CRM_ORDER,
                WoId = record.WO_ID,
                PendingTaskName = record.PENDING_TASK_NAME,
                PendingWg = record.PENDING_WG,
                WoStatus = record.WO_STATUS,
                WoStartDate = record.WO_START_DATE,
                ServiceSpeed = record.SERVICE_SPEED,
                ServiceRequiredDate = record.SERVICE_REQUIRED_DATE,
                FiberPeNo = record.FIBER_PE_NO,
                FiberSoId = record.FIBER_SO_ID,
                ProductSoId = record.PRODUCT_SO_ID,
                FiberPeTaskName = record.FIBER_PE_TASK_NAME,
                FiberPeTaskWg = record.FIBER_PE_TASK_WG,
                PeWoComments = record.PE_WO_COMMENTS,
                Customer = record.CUSTOMER,
                CusType = record.CUS_TYPE,
                AccountManager = record.ACCOUNT_MANAGER,
                SectionHandledBy = record.SECTION_HANDLED_BY,
                LocationAAddress = record.LOCATION_A_ADDRESS,
                LocationBAddress = record.LOCATION_B_ADDRESS,
                NtuType = record.NTU_TYPE,
                AccessMedium = record.ACCESS_MEDIUM,
                AccessMediumAEnd = record.ACCESS_MEDIUM_A_END,
                AccessMediumBEnd = record.ACCESS_MEDIUM_B_END,
                WoComments = record.WO_COMMENTS,
                PEStatus = "ongoing", // Default status for new records
                TaskCreatedDate = DateTime.UtcNow
            };
        }

        private async Task CreatePETasksForEvent(ApplicationDbContext dbContext, PlannedEvent plannedEvent, PERecord record, List<PETaskList> taskListTemplates)
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

            List<PETask> tasksToCreate = new List<PETask>();

            foreach (var taskTemplate in taskListTemplates)
            {
                // Calculate OLA close date - assume OLA_Parameters contains days
                int olaDays = 0;
                if (!string.IsNullOrEmpty(taskTemplate.OLA_Parameters) &&
                    int.TryParse(taskTemplate.OLA_Parameters, out olaDays))
                {
                    // Create task for this PE
                    var peTask = new PETask
                    {
                        PENumber = plannedEvent.PeNumber,
                        TaskSeq = taskTemplate.TaskSeq,
                        Task = taskTemplate.Name,
                        OLA = taskTemplate.OLA_Parameters,
                        TaskStatus = "INPROGRESS",
                        TaskCreatedDate = DateTime.UtcNow,
                        TaskCloseDate = DateTime.UtcNow.AddDays(olaDays),
                        TaskPhase = "ONGOING"
                    };

                    // If this is the current task in the PE record, set the TaskWorkGroup
                    if (taskTemplate.Name == record.TASK_NAME)
                    {
                        peTask.TaskWorkGroup = record.TASK_WG;
                    }

                    tasksToCreate.Add(peTask);
                }
            }

            if (tasksToCreate.Count > 0)
            {
                await dbContext.PETasks.AddRangeAsync(tasksToCreate);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Created {count} tasks for PE {peNumber}", tasksToCreate.Count, plannedEvent.PeNumber);
            }
            else
            {
                _logger.LogWarning("No tasks were created for PE {peNumber} - task templates may be missing OLA values", plannedEvent.PeNumber);
            }
        }

        private async Task UpdateTaskPhases(ApplicationDbContext dbContext)
        {
            // Get all planned events
            var plannedEvents = await dbContext.PlannedEvents.ToListAsync();
            var peTasks = await dbContext.PETasks.ToListAsync();

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

                    foreach (var task in tasksForPE)
                    {
                        if (task.Task == currentTaskName)
                        {
                            // This is the current task
                            task.TaskPhase = "ONGOING";
                            task.TaskWorkGroup = plannedEvent.TaskWg ?? task.TaskWorkGroup;
                            currentTaskFound = true;
                        }
                        else if (!currentTaskFound)
                        {
                            // Tasks before the current task are finished
                            task.TaskPhase = "FINISH";
                            task.TaskStatus = "COMPLETED";
                        }
                        else
                        {
                            // Tasks after the current task are waiting
                            task.TaskPhase = "WAITING";
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}