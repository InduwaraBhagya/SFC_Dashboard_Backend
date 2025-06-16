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
                        .Where(e => e.TaskId == task.Id && e.Level == level && !e.IsIgnored)
                        .FirstOrDefaultAsync();
                    
                    if (existingEscalation == null)
                    {
                        // Create new escalation
                        var escalation = new Escalation
                        {
                            TaskId = task.Id,
                            OLAViolationTime = task.OLADateTime,
                            CreatedAt = DateTime.Now,
                            Level = level,
                            IsRead = false,
                            IsIgnored = false,
                            IgnoreReason = ""
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
                .Where(e => e.RecipientId == userId && !e.IsRead && !e.IsIgnored)
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
        
        public async Task IgnoreEscalationAsync(int escalationId, string reason, int userId)
        {
            var escalation = await _context.Escalations.FindAsync(escalationId);
            
            if (escalation != null)
            {
                escalation.IsIgnored = true;
                escalation.IgnoreReason = reason;
                escalation.IgnoredAt = DateTime.Now;
                escalation.IgnoredById = userId;
                
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<Escalation> GetEscalationDetailsAsync(int id)
        {
            return await _context.Escalations
                .Include(e => e.PETask)
                .Include(e => e.Recipient)
                .Include(e => e.IgnoredBy)
                .FirstOrDefaultAsync(e => e.Id == id);
        }
    }
}