using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class initialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PEIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlannedEventId = table.Column<int>(type: "int", nullable: false),
                    PETaskId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    IssueText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IsReply = table.Column<bool>(type: "bit", nullable: false),
                    OriginalIssueId = table.Column<int>(type: "int", nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    IsResolutionRequest = table.Column<bool>(type: "bit", nullable: false),
                    IsHiddenFromInbox = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PERecords",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PROVINCE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REGION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RTOM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RTOM_DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JOB_REFERENCE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CONTRACTOR_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_NUMBER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_ACTIVITY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_NATURE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_TITLE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_OBJECTIVE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_AREA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SO_NUMBER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TASK_SEQ = table.Column<int>(type: "int", nullable: true),
                    TASK_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TASK_WG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_ACTUAL_START_DATE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REQUEST_REFERENCE_NO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SO_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REGION_1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PROVINCE_1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RTOM_1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LEA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CCT_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SERVICE_CATEGORY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SERVICE_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SO_CREATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ORDER_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CRM_ORDER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PENDING_TASK_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PENDING_WG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_STATUS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_START_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SERVICE_SPEED = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SERVICE_REQUIRED_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FIBER_PE_NO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FIBER_SO_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PRODUCT_SO_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FIBER_PE_TASK_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FIBER_PE_TASK_WG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_WO_COMMENTS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CUSTOMER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CUS_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACCOUNT_MANAGER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SECTION_HANDLED_BY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LOCATION_A_ADDRESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LOCATION_B_ADDRESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NTU_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACCESS_MEDIUM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACCESS_MEDIUM_A_END = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACCESS_MEDIUM_B_END = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_COMMENTS = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERecords", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PETaskList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskSeq = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OLA_Parameters = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PETaskList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UrgentReasons",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PERecordID = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrgentReasons", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UrgentReasons_PERecords_PERecordID",
                        column: x => x.PERecordID,
                        principalTable: "PERecords",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubTaskLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PETaskListId = table.Column<int>(type: "int", nullable: false),
                    SubTaskName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReported = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubTaskLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubTaskLists_PETaskList_PETaskListId",
                        column: x => x.PETaskListId,
                        principalTable: "PETaskList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_UserRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "UserRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserRoleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_UserRoles_UserRoleId",
                        column: x => x.UserRoleId,
                        principalTable: "UserRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlannedEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PROVINCE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REGION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RTOM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RTOM_DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JOB_REFERENCE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CONTRACTOR_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_NUMBER = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PE_ACTIVITY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_NATURE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_TITLE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_OBJECTIVE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_AREA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SO_NUMBER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TASK_SEQ = table.Column<int>(type: "int", nullable: true),
                    TASK_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TASK_WG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_ACTUAL_START_DATE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REQUEST_REFERENCE_NO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SO_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REGION_1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PROVINCE_1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RTOM_1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LEA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CCT_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SERVICE_CATEGORY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SERVICE_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SO_CREATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ORDER_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CRM_ORDER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PENDING_TASK_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PENDING_WG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_STATUS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_START_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SERVICE_SPEED = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SERVICE_REQUIRED_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FIBER_PE_NO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FIBER_SO_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PRODUCT_SO_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FIBER_PE_TASK_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FIBER_PE_TASK_WG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_WO_COMMENTS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CUSTOMER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CUS_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACCOUNT_MANAGER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SECTION_HANDLED_BY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LOCATION_A_ADDRESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LOCATION_B_ADDRESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NTU_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACCESS_MEDIUM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACCESS_MEDIUM_A_END = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACCESS_MEDIUM_B_END = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WO_COMMENTS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_STATUS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PRIORITY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PE_CREATED_DATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsHold = table.Column<bool>(type: "bit", nullable: false),
                    SystemUserId = table.Column<int>(type: "int", nullable: true),
                    WorkGroupId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedEvents", x => x.Id);
                    table.UniqueConstraint("AK_PlannedEvents_PE_NUMBER", x => x.PE_NUMBER);
                    table.ForeignKey(
                        name: "FK_PlannedEvents_Users_SystemUserId",
                        column: x => x.SystemUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlannedEvents_WorkGroups_WorkGroupId",
                        column: x => x.WorkGroupId,
                        principalTable: "WorkGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserWorkGroups",
                columns: table => new
                {
                    SystemUserId = table.Column<int>(type: "int", nullable: false),
                    WorkGroupId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    WorkGroupId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWorkGroups", x => new { x.SystemUserId, x.WorkGroupId });
                    table.ForeignKey(
                        name: "FK_UserWorkGroups_Users_SystemUserId",
                        column: x => x.SystemUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserWorkGroups_WorkGroups_WorkGroupId",
                        column: x => x.WorkGroupId,
                        principalTable: "WorkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserWorkGroups_WorkGroups_WorkGroupId1",
                        column: x => x.WorkGroupId1,
                        principalTable: "WorkGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    PlannedEventId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_PlannedEvents_PlannedEventId",
                        column: x => x.PlannedEventId,
                        principalTable: "PlannedEvents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PEIssueResolutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    ResolutionDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmationRequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEventId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEIssueResolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PEIssueResolutions_PEIssues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "PEIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PEIssueResolutions_PlannedEvents_PlannedEventId",
                        column: x => x.PlannedEventId,
                        principalTable: "PlannedEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PETasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PENumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaskSeq = table.Column<int>(type: "int", nullable: true),
                    Task = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskWorkGroup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OLA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskCompleteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualTaskCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ACtualTaskCompleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsUrgent = table.Column<bool>(type: "bit", nullable: false),
                    UrgentMarkedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UrgentRequested = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsOLAViolate = table.Column<bool>(type: "bit", nullable: false),
                    OLADateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViolationStartTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PETasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PETasks_PlannedEvents_PENumber",
                        column: x => x.PENumber,
                        principalTable: "PlannedEvents",
                        principalColumn: "PE_NUMBER",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPEMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    PlannedEventId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPEMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPEMappings_PlannedEvents_PlannedEventId",
                        column: x => x.PlannedEventId,
                        principalTable: "PlannedEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectPEMappings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskExtensionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlannedEventId = table.Column<int>(type: "int", nullable: false),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    RequestTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    RequestedExtension = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExtensionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskExtensionRequests_PlannedEvents_PlannedEventId",
                        column: x => x.PlannedEventId,
                        principalTable: "PlannedEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskExtensionRequests_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskExtensionRequests_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlannedEventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskHistories_PlannedEvents_PlannedEventId",
                        column: x => x.PlannedEventId,
                        principalTable: "PlannedEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Escalations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    RecipientId = table.Column<int>(type: "int", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    IgnoredById = table.Column<int>(type: "int", nullable: true),
                    PlannedEventId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Escalations_PETasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PETasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Escalations_PlannedEvents_PlannedEventId",
                        column: x => x.PlannedEventId,
                        principalTable: "PlannedEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskEstimationHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    EstimatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskEstimationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskEstimationHistory_PETasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PETasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Can manage estimated time for tasks", "CanManageEstimatedTime" },
                    { 2, "Can send \"Planned Event\" urgent requests", "CanSendPEUrgentRequests" },
                    { 3, "Can view and accept Planned Event urgent requests", "CanAcceptUrgentRequests" },
                    { 4, "Can mark a \"Task\" of a specific PE as Urgent", "CanMakeTasksUrgent" },
                    { 5, "Admin permissions", "Admin" },
                    { 6, "Can view all records", "ViewAll" },
                    { 7, "Can report issues on Planned Events", "CanReportIssues" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_PlannedEventId",
                table: "Escalations",
                column: "PlannedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_TaskId",
                table: "Escalations",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PlannedEventId",
                table: "Notifications",
                column: "PlannedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PEIssueResolutions_IssueId",
                table: "PEIssueResolutions",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_PEIssueResolutions_PlannedEventId",
                table: "PEIssueResolutions",
                column: "PlannedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_PETasks_PENumber",
                table: "PETasks",
                column: "PENumber");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedEvents_SystemUserId",
                table: "PlannedEvents",
                column: "SystemUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedEvents_WorkGroupId",
                table: "PlannedEvents",
                column: "WorkGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPEMappings_PlannedEventId",
                table: "ProjectPEMappings",
                column: "PlannedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPEMappings_ProjectId",
                table: "ProjectPEMappings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTaskLists_PETaskListId",
                table: "SubTaskLists",
                column: "PETaskListId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskEstimationHistory_TaskId",
                table: "TaskEstimationHistory",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExtensionRequests_ApprovedById",
                table: "TaskExtensionRequests",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExtensionRequests_PlannedEventId",
                table: "TaskExtensionRequests",
                column: "PlannedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExtensionRequests_RequestedById",
                table: "TaskExtensionRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_TaskHistories_PlannedEventId",
                table: "TaskHistories",
                column: "PlannedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskHistories_UserId",
                table: "TaskHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UrgentReasons_PERecordID",
                table: "UrgentReasons",
                column: "PERecordID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserRoleId",
                table: "Users",
                column: "UserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkGroups_WorkGroupId",
                table: "UserWorkGroups",
                column: "WorkGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkGroups_WorkGroupId1",
                table: "UserWorkGroups",
                column: "WorkGroupId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Escalations");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PEIssueResolutions");

            migrationBuilder.DropTable(
                name: "ProjectPEMappings");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SubTaskLists");

            migrationBuilder.DropTable(
                name: "TaskEstimationHistory");

            migrationBuilder.DropTable(
                name: "TaskExtensionRequests");

            migrationBuilder.DropTable(
                name: "TaskHistories");

            migrationBuilder.DropTable(
                name: "UrgentReasons");

            migrationBuilder.DropTable(
                name: "UserWorkGroups");

            migrationBuilder.DropTable(
                name: "PEIssues");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "PETaskList");

            migrationBuilder.DropTable(
                name: "PETasks");

            migrationBuilder.DropTable(
                name: "PERecords");

            migrationBuilder.DropTable(
                name: "PlannedEvents");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WorkGroups");

            migrationBuilder.DropTable(
                name: "UserRoles");
        }
    }
}
