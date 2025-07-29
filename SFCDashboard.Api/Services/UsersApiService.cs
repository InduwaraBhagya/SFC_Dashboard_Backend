using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class UsersApiService : IUsersApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersApiService> _logger;

        public UsersApiService(ApplicationDbContext context, ILogger<UsersApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<SystemUser>> GetUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Include(u => u.UserRole)
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<SystemUser?> GetUserAsync(int id)
        {
            try
            {
                return await _context.Users.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with id {Id}", id);
                return null;
            }
        }

        public async Task<SystemUser?> GetUserWithRoleAndWorkGroupsAsync(int id)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with id {Id}", id);
                return null;
            }
        }

        public async Task<SystemUser?> GetUserByServiceIdAsync(string serviceId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with service id {ServiceId}", serviceId);
                return null;
            }
        }

        public async Task<SystemUser?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.ServiceId == ExtractServiceId(email));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with email {Email}", email);
                return null;
            }
        }

        public async Task<int> GetCurrentUserIdAsync(string userIdentity)
        {
            try
            {
                if (string.IsNullOrEmpty(userIdentity))
                    return 0;

                var serviceIdShort = ExtractServiceId(userIdentity);
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceIdShort);
                return user?.Id ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user id for {UserIdentity}", userIdentity);
                return 0;
            }
        }

        public async Task<bool> HasMultipleWorkgroupsAsync(string userIdentity)
        {
            try
            {
                if (string.IsNullOrEmpty(userIdentity))
                    return false;

                var email = userIdentity;
                if (string.IsNullOrEmpty(email))
                    return false;

                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == ExtractServiceId(email));

                if (user == null)
                    return false;

                bool canViewAll = user.UserRole?.HasPermission("ViewAll") == true;
                return !canViewAll && (user.UserWorkGroups?.Count ?? 0) > 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user has multiple workgroups for {UserIdentity}", userIdentity);
                return false;
            }
        }

        public async Task<bool> IsUserInSalesWorkgroupAsync(string userIdentity)
        {
            try
            {
                if (string.IsNullOrEmpty(userIdentity))
                    return false;

                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.ServiceId == ExtractServiceId(userIdentity));

                return user?.UserWorkGroups
                    ?.Any(uwg => uwg.WorkGroup.Name.Contains("SALES", StringComparison.OrdinalIgnoreCase))
                    ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is in sales workgroup for {UserIdentity}", userIdentity);
                return false;
            }
        }

        public async Task<bool> HasDrawFiberAccessAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return false;

                // Check if user is in NET-PROJ-ACC-CABLE workgroup
                return user.UserWorkGroups
                    ?.Any(uwg => uwg.WorkGroup.Name.Equals("NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase))
                    ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking draw fiber access for user {UserId}", userId);
                return false;
            }
        }

        public async Task<(int userWorkgroupId, string userWorkgroupName)> GetCurrentUserWorkGroupAsync(string userIdentity)
        {
            try
            {
                if (string.IsNullOrEmpty(userIdentity))
                    return (0, string.Empty);

                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.ServiceId == ExtractServiceId(userIdentity));

                var firstWorkgroup = user?.UserWorkGroups?.FirstOrDefault()?.WorkGroup;
                return (firstWorkgroup?.Id ?? 0, firstWorkgroup?.Name ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user workgroup for {UserIdentity}", userIdentity);
                return (0, string.Empty);
            }
        }

        public async Task<(List<int> userWorkgroupIds, List<string> userWorkgroupNames, bool canViewAll)> GetCurrentUserWorkGroupsAsync(string userIdentity)
        {
            try
            {
                if (string.IsNullOrEmpty(userIdentity))
                    return (new List<int>(), new List<string>(), false);

                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == ExtractServiceId(userIdentity));

                if (user == null)
                    return (new List<int>(), new List<string>(), false);

                bool canViewAll = user.UserRole?.HasPermission("ViewAll") == true;
                var workgroupIds = user.UserWorkGroups?.Select(uwg => uwg.WorkGroup.Id).ToList() ?? new List<int>();
                var workgroupNames = user.UserWorkGroups?.Select(uwg => uwg.WorkGroup.Name).ToList() ?? new List<string>();

                return (workgroupIds, workgroupNames, canViewAll);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user workgroups for {UserIdentity}", userIdentity);
                return (new List<int>(), new List<string>(), false);
            }
        }

        public async Task<(List<string> salesWorkgroups, bool canViewAll)> GetUserSalesWorkgroupsAsync(string userIdentity)
        {
            try
            {
                if (string.IsNullOrEmpty(userIdentity))
                    return (new List<string>(), false);

                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .Include(u => u.UserRole)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.ServiceId == ExtractServiceId(userIdentity));

                if (user == null)
                    return (new List<string>(), false);

                bool canViewAll = user.UserRole?.HasPermission("ViewAll") == true;
                var salesWorkgroups = user.UserWorkGroups?
                    .Where(uwg => uwg.WorkGroup.Name.Contains("SALES", StringComparison.OrdinalIgnoreCase))
                    .Select(uwg => uwg.WorkGroup.Name)
                    .ToList() ?? new List<string>();

                return (salesWorkgroups, canViewAll);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sales workgroups for {UserIdentity}", userIdentity);
                return (new List<string>(), false);
            }
        }

        public async Task<List<string>> GetUserAssignedCustomersAsync(int userId)
        {
            try
            {
                return await _context.CustomerUserAssignments
                    .Where(c => c.UserId == userId)
                    .Select(c => c.Customer)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned customers for user {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<SystemUser?> CreateUserAsync(SystemUser user)
        {
            try
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return null;
            }
        }

        public async Task<SystemUser?> UpdateUserAsync(SystemUser user)
        {
            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return null;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with id {Id}", id);
                return false;
            }
        }

        public async Task<UserLayoutDataDto?> GetUserLayoutDataAsync(string serviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(serviceId))
                    return null;

                var serviceIdShort = ExtractServiceId(serviceId);
                var userId = await GetCurrentUserIdAsync(serviceId);
                
                if (userId <= 0)
                    return null;

                var user = await GetUserWithRoleAndWorkGroupsAsync(userId);
                if (user == null)
                    return null;

                return new UserLayoutDataDto
                {
                    UserName = user.Name ?? "Guest",
                    IsAdmin = user.UserRole?.HasPermission("Admin") == true,
                    CanManageCustomerAssignments = user.UserRole?.HasPermission("ManageCustomerAssignments") == true,
                    CanManageDrawFiberPerms = user.UserRole?.HasPermission("ManageDrawFiberPerms") == true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user layout data for service ID {ServiceId}", serviceId);
                return null;
            }
        }

        public async Task<ProjectUserPermissionsDto?> GetProjectUserPermissionsAsync(string serviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(serviceId))
                    return null;

                var userId = await GetCurrentUserIdAsync(serviceId);
                if (userId <= 0)
                    return null;

                var user = await GetUserWithRoleAndWorkGroupsAsync(userId);
                if (user == null)
                    return null;

                var canManageProjects = user.UserRole?.HasPermission("ManageProjects") == true;

                return new ProjectUserPermissionsDto
                {
                    CurrentUser = user,
                    CanManageProjects = canManageProjects
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project user permissions for service ID {ServiceId}", serviceId);
                return null;
            }
        }

        public async Task<bool> EditSystemUserAsync(int id, string name, string serviceId, int? userRoleId, List<int> workGroupIds)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return false;
                }

                // Update basic user properties
                user.Name = name;
                user.ServiceId = serviceId;
                user.UserRoleId = userRoleId;

                // Update workgroups - remove existing and add new ones
                _context.UserWorkGroups.RemoveRange(user.UserWorkGroups);

                if (workGroupIds != null && workGroupIds.Any())
                {
                    foreach (var workGroupId in workGroupIds.Distinct())
                    {
                        _context.UserWorkGroups.Add(new UserWorkGroup
                        {
                            SystemUserId = id,
                            WorkGroupId = workGroupId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated user {Id} with {WorkGroupCount} workgroups", id, workGroupIds?.Count ?? 0);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system user {Id}", id);
                return false;
            }
        }

        public async Task<bool> SetUserWorkGroupsAsync(int userId, List<int> workGroupIds)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserWorkGroups)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return false;
                }

                // Remove all existing workgroup assignments
                _context.UserWorkGroups.RemoveRange(user.UserWorkGroups);

                // Add new assignments
                if (workGroupIds != null && workGroupIds.Any())
                {
                    foreach (var wgId in workGroupIds.Distinct())
                    {
                        _context.UserWorkGroups.Add(new UserWorkGroup
                        {
                            SystemUserId = userId,
                            WorkGroupId = wgId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting workgroups for user {UserId}", userId);
                return false;
            }
        }

        public async Task<List<SystemUser>> GetSalesUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Include(u => u.UserWorkGroups)
                    .ThenInclude(uwg => uwg.WorkGroup)
                    .Where(u => u.UserWorkGroups.Any(uwg => uwg.WorkGroup != null && uwg.WorkGroup.Name.ToLower().Contains("sales")))
                    .OrderBy(u => u.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales users");
                throw;
            }
        }

        private string ExtractServiceId(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            // Extract up to the first 6 characters of the email or service ID
            return email.Length > 6 ? email.Substring(0, 6) : email;
        }
    }
}


