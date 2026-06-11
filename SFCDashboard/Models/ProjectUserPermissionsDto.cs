namespace SFCDashboard.Models
{
    public class ProjectUserPermissionsDto
    {
        public SystemUser? CurrentUser { get; set; }
        public bool CanManageProjects { get; set; }
    }
}
