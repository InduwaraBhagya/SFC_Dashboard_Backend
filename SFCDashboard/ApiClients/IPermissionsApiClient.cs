namespace SFCDashboard.ApiClients
{
    public interface IPermissionsApiClient
    {
        Task<bool> IsUserAdminAsync(string serviceId);
        Task<bool> HasPermissionAsync(string serviceId, string permissionName);
    }
}
