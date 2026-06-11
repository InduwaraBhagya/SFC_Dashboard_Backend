using SFCDashboard.ApiClients;

public static class UserExtensions
{
    public static async Task<bool> HasAdminPermissionAsync(this HttpContext context, IPermissionsApiClient permissionsApiClient)
    {
        var serviceId = context.User?.Identity?.Name;
        if (string.IsNullOrEmpty(serviceId))
            return false;

        // Extract first 6 chars of service ID
        serviceId = serviceId.Length > 6 ? serviceId.Substring(0, 6) : serviceId;

        return await permissionsApiClient.IsUserAdminAsync(serviceId);
    }
}