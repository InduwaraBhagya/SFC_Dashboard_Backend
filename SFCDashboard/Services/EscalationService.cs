using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFCDashboard.Services
{
    public class EscalationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EscalationService> _logger;

        public EscalationService(ApplicationDbContext context, ILogger<EscalationService> logger = null)
        {
            _context = context;
            _logger = logger;
        }

        // Helper method to log messages (works even if ILogger is null)
        private void Log(string message)
        {
            _logger?.LogInformation(message);
        }

        private void LogError(string message)
        {
            _logger?.LogError(message);
        }

        public async Task CheckAndCreateEscalationsAsync()
        {
            try
            {
                // Get all OLA violated tasks
                var violatedTasks = await _context.PETasks
                    .Where(t => t.TaskStatus != "Completed" && t.IsOLAViolate)
                    .ToListAsync();

                Log($"Found {violatedTasks.Count} violated tasks");

                // Get role IDs once to avoid multiple queries
                var allRoles = await _context.UserRoles
                    .Where(r => r.Name != null)
                    .ToListAsync();
                
                var engineerRoleId = allRoles
                    .FirstOrDefault(r => r.Name != null && r.Name.Trim().ToLower() == "engineer")?.Id ?? 0;

                var dgmRoleId = allRoles
                    .FirstOrDefault(r => r.Name != null && r.Name.Trim().ToLower() == "deputy general manager")?.Id ?? 0;

                var gmRoleId = allRoles
                    .FirstOrDefault(r => r.Name != null && r.Name.Trim().ToLower() == "general manager")?.Id ?? 0;

                foreach (var task in violatedTasks)
                {
                    // Calculate how long the task has been violated
                    var violationDuration = DateTime.Now - task.OLADateTime;
                    
                    // Determine escalation level based on violation duration
                    EscalationLevel level;
                    
                    if (violationDuration.TotalDays < 2)
                    {
                        level = EscalationLevel.Engineer;
                    }
                    else if (violationDuration.TotalDays < 4)
                    {
                        level = EscalationLevel.DGM;
                    }
                    else
                    {
                        level = EscalationLevel.GM;
                    }
                    
                    // Check if we already have an escalation for this task at this level
                    var existingEscalation = await _context.Escalations
                        .Where(e => e.TaskId == task.Id && e.Level == (int)level)
                        .FirstOrDefaultAsync();
                    
                    if (existingEscalation == null)
                    {
                        // Create new escalation
var escalation = new Escalation
{
    TaskId = task.Id,
    RecipientId = await GetRecipientIdForLevel(level, task),  // Await the async method to get the int value
    Title = $"Task {task.PENumber} violating SLA",
    Message = $"Task {task.PENumber} has been violating SLA since {task.ViolationStartTime:g}",
    CreatedAt = DateTime.Now,
    IsRead = false,
    IsResolved = false  // Changed from IsIgnored to IsResolved based on your model
};
                        
                        // Find the appropriate recipient
                        SystemUser recipient = null;
                        
                        // Get task's workgroup
                        string taskWorkGroupName = task.TaskWorkGroup?.ToLower()?.Trim();
                        
                        // Skip tasks without workgroup
                        if (string.IsNullOrEmpty(taskWorkGroupName))
                        {
                            continue; // Skip to next task
                        }
                        
var workGroup = await _context.WorkGroups
    .Include(wg => wg.UserWorkGroups)
    .ThenInclude(uwg => uwg.SystemUser)
    .FirstOrDefaultAsync(wg => wg.Name.ToLower().Contains(taskWorkGroupName) || 
                              taskWorkGroupName.Contains(wg.Name.ToLower()));
                        
                        if (workGroup != null)
                        {
                            // If we couldn't find role IDs above, try again within this specific context
                            if (engineerRoleId == 0 || dgmRoleId == 0 || gmRoleId == 0)
                            {
                                // Try more flexible matching
                                engineerRoleId = allRoles
                                    .FirstOrDefault(r => r.Name != null && (
                                        r.Name.Trim().ToLower() == "engineer" || 
                                        r.Name.Trim().ToLower().Contains("eng")))?.Id ?? 0;
                                
                                dgmRoleId = allRoles
                                    .FirstOrDefault(r => r.Name != null && (
                                        r.Name.Trim().ToLower() == "deputy general manager" || 
                                        r.Name.Trim().ToLower().Contains("dgm") ||
                                        r.Name.Trim().ToLower().Contains("deputy")))?.Id ?? 0;
                                
                                gmRoleId = allRoles
                                    .FirstOrDefault(r => r.Name != null && (
                                        r.Name.Trim().ToLower() == "general manager" || 
                                        r.Name.Trim().ToLower().Contains("gm") ||
                                        r.Name.Trim().ToLower().Contains("manager") && !r.Name.Trim().ToLower().Contains("deputy")))?.Id ?? 0;
                            }
                            
                            // First, check if the workGroup.UserWorkGroups collection is properly loaded
                            if (workGroup.UserWorkGroups == null || !workGroup.UserWorkGroups.Any())
                            {
                                // Try to reload the workgroup one more time
                                int workGroupId = workGroup != null ? workGroup.Id : 0;
                                workGroup = await _context.WorkGroups
                                    .AsNoTracking() // Avoid tracking issues
                                    .Include(wg => wg.UserWorkGroups)
                                        .ThenInclude(uwg => uwg.SystemUser)
                                    .FirstOrDefaultAsync(wg => wg.Id == workGroupId);
                                                            
                                if (workGroup == null || workGroup.UserWorkGroups == null || !workGroup.UserWorkGroups.Any())
                                {
                                    // Workgroup is empty or doesn't exist - find appropriate user based on role
                                    // Always set targetRoleId based on escalation level
                                    int targetRoleId = 0;
                                    switch (level)
                                    {
                                        case EscalationLevel.Engineer:
                                            targetRoleId = engineerRoleId;
                                            break;
                                        case EscalationLevel.DGM:
                                            targetRoleId = dgmRoleId;
                                            break;
                                        case EscalationLevel.GM:
                                            targetRoleId = gmRoleId;
                                            break;
                                    }

                                    // Try to find any user with the appropriate role in the system
                                    if (targetRoleId > 0)
                                    {
                                        recipient = await _context.Users
                                            .FirstOrDefaultAsync(u => u.UserRoleId == targetRoleId);

                                        if (recipient != null)
                                        {
                                            Log($"Workgroup '{taskWorkGroupName}' empty or not found. Using {recipient.Name} with matching role from system.");

                                            // Skip to creating the escalation
                                            escalation.RecipientId = recipient.Id;
                                            _context.Escalations.Add(escalation);
                                            await _context.SaveChangesAsync();

                                            // Skip to next task since we're done with this one
                                            continue;
                                        }
                                    }
                        
                                    // If no user with matching role, try to find any user in the system
                                    recipient = await _context.Users.FirstOrDefaultAsync();
                        
                                    if (recipient != null)
                                    {
                                        Log($"No user with appropriate role found. Using {recipient.Name} as fallback.");
                                        
                                        // Create escalation with this recipient
                                        escalation.RecipientId = recipient.Id;
                                        _context.Escalations.Add(escalation);
                                        await _context.SaveChangesAsync();
                                        
                                        // Skip to next task
                                        continue;
                                    }
                                    else
                                    {
                                        // No users in system at all
                                        Log($"No users found in the system. Escalation for task #{task.Id} not created.");
                                        continue;
                                    }
                                }
                            }

                            // Try a direct database query instead of using navigation properties
                            try {
                                // FIXED: Changed _context.SystemUsers to _context.Users
                                var usersInWorkgroup = await _context.Users
                                    .Join(
                                        _context.UserWorkGroups,
                                        user => user.Id,
                                        uwg => uwg.SystemUserId,
                                        (user, uwg) => new { User = user, WorkGroupId = uwg.WorkGroupId }
                                    )
                                    .Where(x => x.WorkGroupId == workGroup.Id)
                                    .Select(x => x.User)
                                    .ToListAsync();
                                
                                // Try to find a user with matching role based on level
                                if (level == EscalationLevel.Engineer && engineerRoleId > 0)
                                {
                                    recipient = usersInWorkgroup.FirstOrDefault(u => u.UserRoleId == engineerRoleId);
                                }
                                else if (level == EscalationLevel.DGM && dgmRoleId > 0)
                                {
                                    recipient = usersInWorkgroup.FirstOrDefault(u => u.UserRoleId == dgmRoleId);
                                }
                                else if (level == EscalationLevel.GM && gmRoleId > 0)
                                {
                                    recipient = usersInWorkgroup.FirstOrDefault(u => u.UserRoleId == gmRoleId);
                                }
                                
                                // If no matching role, use any user in workgroup
                                if (recipient == null && usersInWorkgroup.Any())
                                {
                                    recipient = usersInWorkgroup.First();
                                }
                            }
                            catch (Exception ex) 
                            {
                                LogError($"Error finding users in workgroup: {ex.Message}");
                                
                                // Continue with original approach if query fails
                                if (level == EscalationLevel.Engineer && engineerRoleId > 0)
                                {
                                    recipient = workGroup.UserWorkGroups
                                        .Where(uwg => uwg.SystemUser != null && uwg.SystemUser.UserRoleId == engineerRoleId)
                                        .Select(uwg => uwg.SystemUser)
                                        .FirstOrDefault();
                                }
                                else if (level == EscalationLevel.DGM && dgmRoleId > 0)
                                {
                                    recipient = workGroup.UserWorkGroups
                                        .Where(uwg => uwg.SystemUser != null && uwg.SystemUser.UserRoleId == dgmRoleId)
                                        .Select(uwg => uwg.SystemUser)
                                        .FirstOrDefault();
                                }
                                else if (level == EscalationLevel.GM && gmRoleId > 0)
                                {
                                    recipient = workGroup.UserWorkGroups
                                        .Where(uwg => uwg.SystemUser != null && uwg.SystemUser.UserRoleId == gmRoleId)
                                        .Select(uwg => uwg.SystemUser)
                                        .FirstOrDefault();
                                }
                            }

                            // If still no recipient found, use any valid user in the workgroup
                            if (recipient == null)
                            {
                                // Get the first valid user
                                recipient = workGroup.UserWorkGroups
                                    .Where(uwg => uwg.SystemUser != null)
                                    .Select(uwg => uwg.SystemUser)
                                    .FirstOrDefault();
                                
                                // If there are no valid users in this workgroup, find any user with Admin role
                                if (recipient == null)
                                {
                                    // FIXED: Changed _context.SystemUsers to _context.Users
                                    recipient = await _context.Users
                                        .FirstOrDefaultAsync(u => u.UserRoleId == 1); // Assuming Admin is role ID 1
                                    
                                    // If no admin, get any user in the system
                                    if (recipient == null)
                                    {
                                        // FIXED: Changed _context.SystemUsers to _context.Users
                                        recipient = await _context.Users.FirstOrDefaultAsync();
                                    }
                                }
                            }
                        }
                        
                        // IMPORTANT: Only create escalation if we have a recipient
                        if (recipient != null)
                        {
                            escalation.RecipientId = recipient.Id;

                            try
                            {
                                _context.Escalations.Add(escalation);
                                await _context.SaveChangesAsync();
                                Log($"Created escalation for task #{task.Id} assigned to {recipient.Name} (UserId: {recipient.Id}) at level {level}");
                            }
                            catch (DbUpdateException ex)
                            {
                                // Log error details
                                LogError($"Error saving escalation for task #{task.Id}: {ex.Message}");
                                if (ex.InnerException != null)
                                {
                                    LogError($"Inner exception: {ex.InnerException.Message}");
                                }
                            }
                        }
                        else
                        {
                            // Log that no recipient was found for this escalation
                            LogError($"No recipient found for task #{task.Id} in workgroup '{task.TaskWorkGroup}' at level {level}. Escalation not created.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in CheckAndCreateEscalationsAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LogError($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }
        
        public async Task<List<Escalation>> GetEscalationsForUserAsync(int userId)
        {
            return await _context.Escalations
                .Include(e => e.PETask)
                .Where(e => e.RecipientId == userId && !e.IsRead)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        
        public async Task MarkAsReadAsync(int escalationId)
        {
            var escalation = await _context.Escalations.FindAsync(escalationId);
            
            if (escalation != null)
            {
                escalation.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
        
        // public async Task IgnoreEscalationAsync(int escalationId, string reason, int userId)
        // {
        //     var escalation = await _context.Escalations.FindAsync(escalationId);
            
        //     if (escalation != null)
        //     {
        //         escalation.IsIgnored = true;
        //         escalation.IgnoreReason = reason;
        //         escalation.IgnoredAt = DateTime.Now;
        //         escalation.IgnoredById = userId;
                
        //         await _context.SaveChangesAsync();
        //     }
        // }
        
        public async Task<Escalation> GetEscalationDetailsAsync(int id)
        {
            return await _context.Escalations
                .Include(e => e.PETask)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task ProcessViolationEscalationsAsync()
        {
            // Get all violated tasks that need escalation
            var violatedTasks = await _context.PETasks
                .Where(p => p.IsOLAViolate && p.TaskStatus != "Completed" && p.TaskStatus != "Closed")
                .ToListAsync();

            foreach (var task in violatedTasks)
            {
                // Calculate violation duration
                DateTime violationStartTime = task.ViolationStartTime != null ? task.ViolationStartTime.Value :
                                             (task.OLADateTime != default(DateTime) ? task.OLADateTime : DateTime.UtcNow);
                TimeSpan violationDuration = DateTime.UtcNow - violationStartTime;
                
                // Determine recipient based on violation duration
                int recipientId = await DetermineRecipientAsync(task, violationDuration);
                
                // Check if we already created an escalation for this recipient and task
                bool escalationExists = await _context.Escalations
                    .AnyAsync(e => e.TaskId == task.Id && e.RecipientId == recipientId && !e.IsResolved);
                
                if (!escalationExists && recipientId > 0)
                {
                    // Get task name and recipient information
                    string taskName = task.Task ?? "Unknown Task";
                    
                    // Get recipient name and role
                    var recipient = await _context.Users
                        .Include(u => u.UserRole)
                        .FirstOrDefaultAsync(u => u.Id == recipientId);
                    
                    string recipientName = recipient?.Name ?? "Unknown";
                    string recipientRole = recipient?.UserRole?.Name ?? "Unknown Role";
                    
                    // Create the escalation
                    var escalation = new Escalation
                    {
                        TaskId = task.Id,
                        RecipientId = recipientId,
                        Title = $"Task {taskName} ({task.PENumber}) violating SLA",
                        Message = $"Task {taskName} ({task.PENumber}) has been violating SLA for {Math.Floor(violationDuration.TotalDays)} days and {violationDuration.Hours} hours. Assigned to {recipientName} ({recipientRole}).",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        IsResolved = false
                    };

                    _context.Escalations.Add(escalation);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Created escalation for task {TaskName} ({TaskId}) to recipient {RecipientName} ({RecipientRole}) after {ViolationDays} days violation", 
                        taskName, task.Id, recipientName, recipientRole, Math.Floor(violationDuration.TotalDays));
                }
            }
        }

        private async Task<int> DetermineRecipientAsync(PETask task, TimeSpan violationDuration)
        {
            // Get the task's workgroup to determine the hierarchy
            string taskWorkgroup = task.TaskWorkGroup;
            if (string.IsNullOrEmpty(taskWorkgroup))
            {
                _logger.LogWarning("Task {TaskId} has no workgroup assigned", task.Id);
                return 0;
            }

            // Immediately when violation occurs - send to engineer
            if (violationDuration.TotalHours < 24)
            {
                // Find engineer responsible for this task
                var engineer= await _context.Users
                    .Include(u => u.UserRole)
                    .Where(u => u.UserWorkGroups.Any(uwg => uwg.WorkGroup.Name == taskWorkgroup) && 
                           u.UserRole.Name == "Engineer")
                    .FirstOrDefaultAsync();
                var level = 1;
                
                return engineer?.Id ?? 0;
            }
            // After 1 day - send to DGM
            else if (violationDuration.TotalHours < 48)
            {
                // Find DGM for this workgroup
                var dgm = await _context.Users
                    .Include(u => u.UserRole)
                    .Where(u => u.UserWorkGroups.Any(uwg => uwg.WorkGroup.Name == taskWorkgroup) && 
                           u.UserRole.Name == "Deputy General Manager")
                    .FirstOrDefaultAsync();
                var level = 2;
                return dgm?.Id ?? 0;
            }
            // After 2 days - send to GM
            else
            {
                // Find GM
                var gm = await _context.Users
                    .Include(u => u.UserRole)
                    .Where(u => u.UserRole.Name == "General Manager")
                    .FirstOrDefaultAsync();
                var level = 3;
                return gm?.Id ?? 0;
            }
        }

        private async Task<int> GetRecipientIdForLevel(EscalationLevel level, PETask task)
        {
            // Get the task's workgroup to determine the hierarchy
            string taskWorkgroup = task.TaskWorkGroup;
            if (string.IsNullOrEmpty(taskWorkgroup))
            {
                _logger.LogWarning("Task {TaskId} has no workgroup assigned", task.Id);
                return 0;
            }

            switch (level)
            {
                case EscalationLevel.Engineer:
                    // Find engineer responsible for this task
                    var engineer = await _context.Users
                        .Include(u => u.UserRole)
                        .Where(u => u.UserWorkGroups.Any(uwg => uwg.WorkGroup.Name == taskWorkgroup) && 
                               u.UserRole.Name == "Engineer")
                        .FirstOrDefaultAsync();
                    return engineer?.Id ?? 0;
                    
                case EscalationLevel.DGM:
                    // Find DGM for this workgroup
                    var dgm = await _context.Users
                        .Include(u => u.UserRole)
                        .Where(u => u.UserWorkGroups.Any(uwg => uwg.WorkGroup.Name == taskWorkgroup) && 
                               u.UserRole.Name == "Deputy General Manager")
                        .FirstOrDefaultAsync();
                    return dgm?.Id ?? 0;
                    
                case EscalationLevel.GM:
                    // Find GM
                    var gm = await _context.Users
                        .Include(u => u.UserRole)
                        .Where(u => u.UserRole.Name == "General Manager")
                        .FirstOrDefaultAsync();
                    return gm?.Id ?? 0;
                    
                default:
                    _logger.LogWarning("Unknown escalation level: {Level} for task {TaskId}", level, task.Id);
                    return 0;
            }
        }
    }
}