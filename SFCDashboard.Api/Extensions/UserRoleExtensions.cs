using SFCDashboard.Api.Models;

public static class UserRoleExtensions
{
    public static bool HasPermission(this UserRole role, string permissionName)
    {
        if (role?.RolePermissions == null)
            return false;

        return role.RolePermissions.Any(rp => rp.Permission?.Name == permissionName);
    }
}
