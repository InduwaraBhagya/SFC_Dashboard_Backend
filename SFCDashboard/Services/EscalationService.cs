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
                .Where(p => p.IsOLAViolate && p.TaskStatus != "COMPLETED" && p.ViolationStartTime.HasValue)
                .ToListAsync();

            foreach (var task in violatedTasks)
            {
                // Skip tasks with no violation start time
                if (!task.ViolationStartTime.HasValue) continue;

                // Calculate violation duration
                TimeSpan violationDuration = DateTime.UtcNow - task.ViolationStartTime.Value;
                
                // Determine escalation level based on duration
                EscalationLevel level;
                if (violationDuration.TotalHours < 24)
                {
                    level = EscalationLevel.Engineer;
                }
                else if (violationDuration.TotalHours < 48)
                {
                    level = EscalationLevel.DGM;
                }
                else
                {
                    level = EscalationLevel.GM;
                }
                
                // Get recipient ID for this escalation level
                int recipientId = await GetRecipientIdForLevel(level, task);
                
                // Check if we already have an active escalation at this level for this task
                bool existingEscalation = await _context.Escalations
                    .AnyAsync(e => e.TaskId == task.Id && !e.IsResolved && 
                              e.RecipientId == recipientId);
                
                if (!existingEscalation && recipientId > 0)
                {
                    // Create new escalation
                    var escalation = new Escalation
                    {
                        TaskId = task.Id,
                        RecipientId = recipientId,
                        Title = $"Task {task.Task} violating OLA",
                        Message = $"Task has been violating OLA for {Math.Floor(violationDuration.TotalDays)} days and {violationDuration.Hours} hours",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        IsResolved = false
                    };

                    _context.Escalations.Add(escalation);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Created escalation for task {TaskId} to recipient {RecipientId} at level {Level}", 
                        task.Id, recipientId, level);
                }
            }
        }

        private async Task<int> GetRecipientIdForLevel(EscalationLevel level, PETask task)
        {
            // Get the task's workgroup 
            string taskWorkgroup = task.TaskWorkGroup;
            if (string.IsNullOrEmpty(taskWorkgroup))
            {
                _logger.LogWarning("Task {TaskId} has no workgroup assigned", task.Id);
                return 0;
            }

            // Find recipient based on role
            int roleId = 0;
            
            switch (level)
            {
                case EscalationLevel.Engineer:
                    // Get Engineer role ID
                    var engineerRole = await _context.UserRoles
                        .FirstOrDefaultAsync(r => r.Name == "Engineer");
                    roleId = engineerRole?.Id ?? 0;
                    break;
                    
                case EscalationLevel.DGM:
                    // Get DGM role ID
                    var dgmRole = await _context.UserRoles
                        .FirstOrDefaultAsync(r => r.Name == "Deputy General Manager");
                    roleId = dgmRole?.Id ?? 0;
                    break;
                    
                case EscalationLevel.GM:
                    // Get GM role ID
                    var gmRole = await _context.UserRoles
                        .FirstOrDefaultAsync(r => r.Name == "General Manager");
                    roleId = gmRole?.Id ?? 0;
                    break;
            }
            
            if (roleId == 0)
            {
                _logger.LogWarning("Could not find role ID for level {Level}", level);
                return 0;
            }
            
            // Find user with this role in the workgroup
            var user = await _context.Users
                .Where(u => u.UserRoleId == roleId && 
                      (level == EscalationLevel.GM || // For GM, don't filter by workgroup
                       u.UserWorkGroups.Any(uwg => uwg.WorkGroup.Name == taskWorkgroup)))
                .FirstOrDefaultAsync();
                
            if (user == null)
            {
                _logger.LogWarning("No user found with role {RoleId} for workgroup {Workgroup}", 
                    roleId, taskWorkgroup);
                return 0;
            }
            
            _logger.LogInformation("Selected recipient {UserId} with role {RoleId} for level {Level}", 
                user.Id, roleId, level);
                
            return roleId;
        }
    }
}