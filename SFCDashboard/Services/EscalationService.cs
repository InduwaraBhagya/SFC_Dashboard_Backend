using Microsoft.EntityFrameworkCore;
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

        public EscalationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CheckAndCreateEscalationsAsync()
        {
            // Get all OLA violated tasks
            var violatedTasks = await _context.PETasks
                .Where(t => t.TaskStatus != "Completed" && t.IsOLAViolate)
                .ToListAsync();

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
                        IgnoreReason = "" // Initialize with empty string instead of null

                    };
                    
                    // Find the appropriate recipient
                    SystemUser recipient = null;
                    
                    // Get task's workgroup
// Get task's workgroup by matching name with case-insensitive comparison
var workGroup = await _context.WorkGroups
    .Include(wg => wg.UserWorkGroups)
    .ThenInclude(uwg => uwg.SystemUser)
    .FirstOrDefaultAsync(wg => wg.Name.ToLower().Trim() == task.TaskWorkGroup.ToLower().Trim());
                    
                    if (workGroup != null)
                    {
                        // Get role IDs once
                        var engineerRoleId = await _context.UserRoles.Where(r => r.Name == "Engineer").Select(r => r.Id).FirstOrDefaultAsync();
                        var dgmRoleId = await _context.UserRoles.Where(r => r.Name == "Deputy General Manager").Select(r => r.Id).FirstOrDefaultAsync();
                        var gmRoleId = await _context.UserRoles.Where(r => r.Name == "General Manager").Select(r => r.Id).FirstOrDefaultAsync();

                        if (level == EscalationLevel.Engineer)
                        {
                            // Find engineers in this workgroup
                            var engineer = workGroup.UserWorkGroups
                                .FirstOrDefault(uwg => uwg.SystemUser.UserRoleId == engineerRoleId)?.SystemUser;
                            recipient = engineer;
                        }
                        else if (level == EscalationLevel.DGM)
                        {
                            var dgm = workGroup.UserWorkGroups
                                .FirstOrDefault(uwg => uwg.SystemUser.UserRoleId == dgmRoleId)?.SystemUser;
                            recipient = dgm;
                        }
                        else if (level == EscalationLevel.GM)
                        {
                            var gm = workGroup.UserWorkGroups
                                .FirstOrDefault(uwg => uwg.SystemUser.UserRoleId == gmRoleId)?.SystemUser;
                            recipient = gm;
                        }
                    }
                    
                    if (recipient != null)
                    {
                        escalation.RecipientId = recipient.Id;
                    }
                    
                    _context.Escalations.Add(escalation);
                    await _context.SaveChangesAsync();
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