using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class UserPermissionService : IUserPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserPermissionService> _logger;
        private readonly IUsersApiService _usersApiService;

        public UserPermissionService(
            ApplicationDbContext context, 
            ILogger<UserPermissionService> logger,
            IUsersApiService usersApiService)
        {
            _context = context;
            _logger = logger;
            _usersApiService = usersApiService;
        }

        public async Task<UserPermissionInfo> GetUserPermissionInfoAsync(string serviceId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRole)
                        .ThenInclude(ur => ur!.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .Include(u => u.UserWorkGroups)
                        .ThenInclude(uwg => uwg.WorkGroup)
                    .FirstOrDefaultAsync(u => u.ServiceId == serviceId);

                if (user == null)
                {
                    _logger.LogWarning("User not found for service ID: {ServiceId}", serviceId);
                    return new UserPermissionInfo();
                }

                var workgroupNames = user.UserWorkGroups?.Select(uwg => uwg.WorkGroup.Name).ToList() ?? new List<string>();
                var workgroupIds = user.UserWorkGroups?.Select(uwg => uwg.WorkGroup.Id).ToList() ?? new List<int>();

                return new UserPermissionInfo
                {
                    UserId = user.Id,
                    UserName = user.Name,
                    ServiceId = user.ServiceId,
                    IsAdmin = HasUserPermission(user, "Admin"),
                    CanViewAll = HasUserPermission(user, "ViewAll"),
                    HasDrawFiberAccess = workgroupNames.Any(wg => wg.Equals("NET-PROJ-ACC-CABLE", StringComparison.OrdinalIgnoreCase)),
                    CanAcceptUrgentRequests = HasUserPermission(user, "CanAcceptUrgentRequests"),
                    CanManageCustomerAssignments = HasUserPermission(user, "ManageCustomerAssignments"),
                    CanManageDrawFiberPerms = HasUserPermission(user, "ManageDrawFiberPerms"),
                    WorkgroupNames = workgroupNames,
                    WorkgroupIds = workgroupIds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permission info for service ID: {ServiceId}", serviceId);
                return new UserPermissionInfo();
            }
        }

        public async Task<UserWorkgroupInfo> GetUserWorkgroupInfoAsync(string serviceId)
        {
            try
            {
                var userPermInfo = await GetUserPermissionInfoAsync(serviceId);
                var salesWorkgroups = await GetSalesWorkgroupsAsync();

                var result = new UserWorkgroupInfo();
                result.WorkgroupIds = userPermInfo.WorkgroupIds;
                result.WorkgroupNames = userPermInfo.WorkgroupNames;
                result.CanViewAll = userPermInfo.CanViewAll;
                result.IsInSalesWorkgroup = userPermInfo.WorkgroupNames.Any(wg => 
                    salesWorkgroups.Contains(wg, StringComparer.OrdinalIgnoreCase));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user workgroup info for service ID: {ServiceId}", serviceId);
                return new UserWorkgroupInfo();
            }
        }

        public async Task<bool> HasDrawFiberAccessAsync(string serviceId)
        {
            try
            {
                var userPermInfo = await GetUserPermissionInfoAsync(serviceId);
                return userPermInfo.HasDrawFiberAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking draw fiber access for service ID: {ServiceId}", serviceId);
                return false;
            }
        }

        public async Task<bool> IsUserInSalesWorkgroupAsync(string serviceId)
        {
            try
            {
                var workgroupInfo = await GetUserWorkgroupInfoAsync(serviceId);
                return workgroupInfo.IsInSalesWorkgroup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking sales workgroup for service ID: {ServiceId}", serviceId);
                return false;
            }
        }

        public async Task<List<string>> GetUserAssignedCustomersAsync(string serviceId)
        {
            try
            {
                // Note: This method may need adjustment based on the actual customer assignment model
                // For now, returning empty list as the CustomerUserAssignments relationship needs to be verified
                _logger.LogWarning("GetUserAssignedCustomersAsync not fully implemented - customer assignment model needs verification");
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned customers for service ID: {ServiceId}", serviceId);
                return new List<string>();
            }
        }

        public async Task<UserRedirectInfo> DetermineUserRedirectAsync(string serviceId, SearchCriteria searchCriteria)
        {
            try
            {
                var workgroupInfo = await GetUserWorkgroupInfoAsync(serviceId);

                // Check for sales workgroup redirect (only if user doesn't have ViewAll permission)
                if (workgroupInfo.IsInSalesWorkgroup && !workgroupInfo.CanViewAll)
                {
                    return new UserRedirectInfo
                    {
                        ShouldRedirect = true,
                        ActionName = "SalesView",
                        RouteValues = new
                        {
                            searchType = searchCriteria.SearchType,
                            peNumber = searchCriteria.PeNumber,
                            customer = searchCriteria.Customer,
                            jobReference = searchCriteria.JobReference,
                            soNumber = searchCriteria.SoNumber,
                            pageIndex = searchCriteria.PageIndex
                        }
                    };
                }

                // Check for multi-workgroup redirect (only if user doesn't have ViewAll and has multiple workgroups)
                if (!workgroupInfo.CanViewAll && workgroupInfo.WorkgroupIds.Count > 1)
                {
                    return new UserRedirectInfo
                    {
                        ShouldRedirect = true,
                        ActionName = "MultiWorkgroupView",
                        RouteValues = new
                        {
                            searchType = searchCriteria.SearchType,
                            peNumber = searchCriteria.PeNumber,
                            customer = searchCriteria.Customer,
                            jobReference = searchCriteria.JobReference,
                            soNumber = searchCriteria.SoNumber,
                            workgroupIds = searchCriteria.WorkgroupIds,
                            pageIndex = searchCriteria.PageIndex
                        }
                    };
                }

                return new UserRedirectInfo { ShouldRedirect = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining user redirect for service ID: {ServiceId}", serviceId);
                return new UserRedirectInfo { ShouldRedirect = false };
            }
        }

        private static bool HasUserPermission(SystemUser? user, string permissionName)
        {
            return user?.UserRole?.RolePermissions?
                .Any(rp => rp.Permission?.Name == permissionName) ?? false;
        }

        private async Task<List<string>> GetSalesWorkgroupsAsync()
        {
            try
            {
                return await _context.WorkGroups
                    .Where(wg => wg.Name.ToLower().Contains("sales") || 
                                wg.Name.ToLower().Contains("account") ||
                                wg.Name.ToLower().Contains("customer"))
                    .Select(wg => wg.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales workgroups");
                return new List<string>();
            }
        }
    }
}
