using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class initialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "UserRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.Id);
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
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserRoleId = table.Column<int>(type: "int", nullable: true),
                    WorkGroupId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_UserRole_UserRoleId",
                        column: x => x.UserRoleId,
                        principalTable: "UserRole",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_WorkGroups_WorkGroupId",
                        column: x => x.WorkGroupId,
                        principalTable: "WorkGroups",
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
                    SO_ID = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                name: "PETasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PENumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TaskSeq = table.Column<int>(type: "int", nullable: true),
                    Task = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskWorkGroup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OLA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskCompleteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualTaskCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ACtualTaskCompleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaskPhase = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "TaskEscalations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlannedEventId = table.Column<int>(type: "int", nullable: false),
                    EscalationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EscalationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EscalatedToUserId = table.Column<int>(type: "int", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskEscalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskEscalations_PlannedEvents_PlannedEventId",
                        column: x => x.PlannedEventId,
                        principalTable: "PlannedEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskEscalations_Users_EscalatedToUserId",
                        column: x => x.EscalatedToUserId,
                        principalTable: "Users",
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

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PlannedEventId",
                table: "Notifications",
                column: "PlannedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

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
                name: "IX_TaskEscalations_EscalatedToUserId",
                table: "TaskEscalations",
                column: "EscalatedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskEscalations_PlannedEventId",
                table: "TaskEscalations",
                column: "PlannedEventId");

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
                name: "IX_Users_UserRoleId",
                table: "Users",
                column: "UserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_WorkGroupId",
                table: "Users",
                column: "WorkGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PERecords");

            migrationBuilder.DropTable(
                name: "PETaskList");

            migrationBuilder.DropTable(
                name: "PETasks");

            migrationBuilder.DropTable(
                name: "TaskEscalations");

            migrationBuilder.DropTable(
                name: "TaskExtensionRequests");

            migrationBuilder.DropTable(
                name: "TaskHistories");

            migrationBuilder.DropTable(
                name: "PlannedEvents");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "WorkGroups");
        }
    }
}
