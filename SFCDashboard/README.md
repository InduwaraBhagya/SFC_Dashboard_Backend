# SFC Dashboard - Frontend API Endpoints Reference

This document provides a comprehensive overview of all API endpoints used by the SFC Dashboard frontend application.


## Table of Contents
- [Planned Events API](#planned-events-api) - Core PE management operations
- [Users API](#users-api) - User management and authentication
- [Permissions API](#permissions-api) - Authorization and access control
- [PE Tasks API](#pe-tasks-api) - Task management and tracking
- [Escalations API](#escalations-api) - Escalation processes and notifications
- [Projects API](#projects-api) - Project operations and PE assignments
- [Work Groups API](#work-groups-api) - Organizational work group management
- [PE Issues API](#pe-issues-api) - Issue tracking and resolution
- [PE Issue Resolutions API](#pe-issue-resolutions-api) - Issue resolution management
- [Area Network Engineers API](#area-network-engineers-api) - Engineer area assignments
- [User Roles API](#user-roles-api) - Role-based access control
- [PE Records API](#pe-records-api) - PE record operations
- [PE Task Lists API](#pe-task-lists-api) - Task list templates
- [Customer User Assignments API](#customer-user-assignments-api) - Customer-user mappings
- [Role Permissions API](#role-permissions-api) - Role-permission relationships
- [Task Queue API](#task-queue-api) - Task queue operations
- [Sub Task Lists API](#sub-task-lists-api) - Sub-task management
- [Error Handling](#error-handling) - HTTP status codes and error responses
- [Authentication](#authentication) - Azure AD integration details
- [Base URL Configuration](#base-url-configuration) - API configuration settings

---

## Planned Events API

The core API for managing planned events (PEs) in the system.

**Base Endpoint:** `/api/plannedevents`

**Related APIs:** [PE Tasks API](#pe-tasks-api), [PE Issues API](#pe-issues-api), [Projects API](#projects-api), [Users API](#users-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all planned events
- `GetByIdAsync(int id)` - Get a specific planned event by ID
- `CreateAsync(PlannedEvent)` - Create a new planned event
- `UpdateAsync(PlannedEvent)` - Update an existing planned event
- `DeleteAsync(int id)` - Delete a planned event

### Search & Filtering
- `SearchAsync(string searchTerm)` - Search planned events by term
- `SearchPlannedEventsAsync()` - Advanced search with pagination and filters
- `SearchPlannedEventsForUserAsync()` - User-specific search with access controls
- `GetPlannedEventsByWorkgroupAsync()` - Get events filtered by workgroup
- `GetDistinctCustomersAsync()` - Get list of unique customers
- `GetPlannedEventByPENumberAsync(string peNumber)` - Get PE by PE number

### Status-Based Retrieval
- `GetInProgressPlannedEventsAsync()` - Get events currently in progress
- `GetUrgentPlannedEventsAsync()` - Get urgent priority events
- `GetOLAViolatingPlannedEventsAsync()` - Get events violating OLA
- `GetHoldPlannedEventsAsync()` - Get events on hold
- `GetPendingUrgentRequestsAsync()` - Get pending urgent requests

### User-Specific Operations
- `GetInProgressPlannedEventsByUserIdAsync(int userId)` - In-progress events for specific user
- `GetUrgentPlannedEventsByUserIdAsync(int userId)` - Urgent events for specific user
- `GetOLAViolatingPlannedEventsByUserIdAsync(int userId)` - OLA violating events for specific user
- `GetHoldPlannedEventsByUserIdAsync(int userId)` - Hold events for specific user

### Count Operations
- `GetInProgressCountAsync()` - Count of in-progress events
- `GetUrgentCountAsync()` - Count of urgent events
- `GetOLAViolateCountAsync()` - Count of OLA violating events
- `GetHoldCountAsync()` - Count of events on hold
- `GetInProgressCountByUserIdAsync(int userId)` - User-specific in-progress count
- `GetUrgentCountByUserIdAsync(int userId)` - User-specific urgent count
- `GetOLAViolatingCountByUserIdAsync(int userId)` - User-specific OLA violating count
- `GetHoldCountByUserIdAsync(int userId)` - User-specific hold count

### Multi-Workgroup Operations
- `GetUrgentCountForMultiWorkgroupAsync()` - Urgent count across multiple workgroups
- `GetInProgressCountForMultiWorkgroupAsync()` - In-progress count across multiple workgroups
- `GetOLAViolateCountForMultiWorkgroupAsync()` - OLA violating count across multiple workgroups
- `GetHoldCountForMultiWorkgroupAsync()` - Hold count across multiple workgroups
- `GetInProgressPlannedEventsForMultiWorkgroupAsync()` - In-progress events across multiple workgroups
- `GetUrgentPlannedEventsForMultiWorkgroupAsync()` - Urgent events across multiple workgroups
- `GetHoldPlannedEventsForMultiWorkgroupAsync()` - Hold events across multiple workgroups
- `GetOLAViolatingPlannedEventsForMultiWorkgroupAsync()` - OLA violating events across multiple workgroups

### Sales-Specific Operations
- `GetSalesInProgressRecordsAsync()` - In-progress records for sales team
- `GetSalesHoldRecordsAsync()` - Hold records for sales team
- `GetSalesUrgentRecordsAsync()` - Urgent records for sales team
- `GetSalesOLAViolateRecordsAsync()` - OLA violating records for sales team
- `GetSalesInProgressCountAsync()` - Count of sales in-progress records
- `GetSalesHoldCountAsync()` - Count of sales hold records
- `GetSalesUrgentCountAsync()` - Count of sales urgent records
- `GetSalesOLAViolateCountAsync()` - Count of sales OLA violating records

---

## Users API

Manages system users, their roles, and permissions.

**Base Endpoint:** `/api/users`

**Related APIs:** [Permissions API](#permissions-api), [User Roles API](#user-roles-api), [Work Groups API](#work-groups-api), [Customer User Assignments API](#customer-user-assignments-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all system users
- `GetByIdAsync(int id)` - Get user by ID
- `GetByServiceIdAsync(string serviceId)` - Get user by service ID
- `CreateAsync(SystemUser)` - Create new user
- `UpdateAsync(SystemUser)` - Update existing user
- `EditSystemUserAsync(int id, EditSystemUserRequest)` - Edit user details
- `DeleteAsync(int id)` - Delete user

### User Authentication & Context
- `GetCurrentUserIdAsync(string serviceId)` - Get current user's ID
- `GetUserWithRoleAndWorkGroupsAsync(int userId)` - Get user with role and workgroup info
- `GetUserByServiceIdAsync(string serviceId)` - Get user by service ID

### Workgroup Operations
- `SetUserWorkGroupsAsync(int userId, List<int> workGroupIds)` - Assign workgroups to user
- `GetCurrentUserWorkGroupsAsync(string serviceId)` - Get current user's workgroups
- `GetCurrentUserWorkGroupAsync(string serviceId)` - Get current user's primary workgroup
- `HasMultipleWorkgroupsAsync(string serviceId)` - Check if user has multiple workgroups
- `IsUserInSalesWorkgroupAsync(string serviceId)` - Check if user is in sales workgroup
- `GetUserSalesWorkgroupsAsync(string serviceId)` - Get user's sales workgroups

### Permissions & Access
- `HasDrawFiberAccessAsync(int userId)` - Check if user has draw fiber access
- `GetUserAssignedCustomersAsync(int userId)` - Get customers assigned to user
- `GetUserLayoutDataAsync(string serviceId)` - Get user's layout configuration
- `GetProjectUserPermissionsAsync(string serviceId)` - Get user's project permissions

### Specialized Operations
- `GetSalesUsersAsync()` - Get all sales team users

---

## Permissions API

Manages system permissions and authorization.

**Base Endpoint:** `/api/permissions`

**Related APIs:** [Users API](#users-api), [User Roles API](#user-roles-api), [Role Permissions API](#role-permissions-api)

### Permission Checks
- `IsUserAdminAsync(string serviceId)` - Check if user has admin privileges
- `HasPermissionAsync(string serviceId, string permissionName)` - Check specific permission

### CRUD Operations
- `GetAllPermissionsAsync()` - Get all available permissions
- `GetPermissionByIdAsync(int id)` - Get specific permission
- `CreatePermissionAsync(Permission)` - Create new permission
- `UpdatePermissionAsync(Permission)` - Update existing permission
- `DeletePermissionAsync(int id)` - Delete permission
- `PermissionExistsAsync(int id)` - Check if permission exists

---

## PE Tasks API

Manages tasks associated with planned events.

**Base Endpoint:** `/api/petasks`

**Related APIs:** [Planned Events API](#planned-events-api), [Escalations API](#escalations-api), [PE Task Lists API](#pe-task-lists-api), [Task Queue API](#task-queue-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all PE tasks
- `GetByIdAsync(int id)` - Get specific task
- `CreateAsync(PETask)` - Create new task
- `UpdateAsync(PETask)` - Update existing task
- `DeleteAsync(int id)` - Delete task

### Task Retrieval
- `GetByPENumberAsync(string peNumber)` - Get tasks for specific PE number
- `GetPETasksByPENumberAsync(string peNumber)` - Get PE tasks by PE number
- `GetPETasksByPENumbersAsync(List<string> peNumbers)` - Get tasks for multiple PE numbers
- `GetTasksByPeNumbersAsync(List<string> peNumbers)` - Get tasks grouped by PE numbers

### Priority & Status Operations
- `GetUrgentRequestsAsync()` - Get urgent task requests
- `GetUrgentTasksAsync()` - Get urgent tasks
- `GetOLAViolationsAsync()` - Get OLA violating tasks
- `GetPendingTaskRequestsAsync(int limit)` - Get pending task requests
- `GetPendingUrgentTaskRequestsAsync(int limit)` - Get pending urgent task requests

### Task Management
- `MarkAsUrgentAsync(int id)` - Mark task as urgent
- `ProcessUrgentRequestAsync(int id, string urgentReason)` - Process urgent request
- `ProcessPEUrgentRequestAsync(string peNumber, string urgentReason)` - Process PE urgent request
- `CompleteViolatedTaskAsync(int id)` - Complete OLA violated task
- `RemoveUrgentStatusAsync(int id)` - Remove urgent status
- `UpdateEstimatedTimeAsync(int id, DateTime estimatedTime)` - Update estimated completion time
- `UpdatePETaskAsync(PETask)` - Update PE task

### Analytics & Reporting
- `GetEstimationHistoryAsync(int id)` - Get task estimation history
- `GetOLAViolatingPENumbersAsync()` - Get PE numbers with OLA violations
- `GetOLAViolationDetailsAsync(List<string> peNumbers)` - Get detailed OLA violation info

---

## Escalations API

Manages escalation processes and notifications.

**Base Endpoint:** `/api/escalations`

**Related APIs:** [PE Tasks API](#pe-tasks-api), [Users API](#users-api), [User Roles API](#user-roles-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all escalations
- `GetByIdAsync(int id)` - Get specific escalation
- `CreateAsync(Escalation)` - Create new escalation
- `UpdateAsync(Escalation)` - Update existing escalation
- `DeleteAsync(int id)` - Delete escalation

### Escalation Management
- `GetEscalationsByTaskIdsAsync(List<int> peTaskIds)` - Get escalations for specific tasks
- `GetEscalationsByUserRoleAsync(int userRoleLevel, List<string> userWorkgroupNames)` - Get escalations by user role
- `MarkAsReadAsync(int escalationId)` - Mark escalation as read

### System Operations
- `ManualEscalationCheckAsync()` - Trigger manual escalation check
- `GetOLAViolatedTasksDebugInfoAsync()` - Get debug info for OLA violated tasks
- `IsEscalationEnabledAsync()` - Check if escalation system is enabled
- `SetEscalationEnabledAsync(bool enabled)` - Enable/disable escalation system

---

## Projects API

Manages project operations and PE assignments.

**Base Endpoint:** `/api/projects`

**Related APIs:** [Planned Events API](#planned-events-api), [Users API](#users-api)

### Project Management
- `GetAllProjectsAsync()` - Get all projects
- `GetProjectByIdAsync(int id)` - Get specific project
- `GetProjectDetailsAsync(int id)` - Get detailed project information
- `CreateProjectAsync(Project)` - Create new project
- `DeleteProjectAsync(int id)` - Delete project

### PE Assignment Operations
- `SearchPlannedEventsAsync(string searchTerm, int projectId)` - Search PEs for project assignment
- `AssignPEToProjectAsync(int plannedEventId, int projectId)` - Assign single PE to project
- `AssignMultiplePEsToProjectAsync(int projectId, List<int> plannedEventIds)` - Assign multiple PEs to project
- `RemovePEFromProjectAsync(int plannedEventId, int projectId)` - Remove PE from project

---

## Work Groups API

Manages organizational work groups.

**Base Endpoint:** `/api/workgroups`

**Related APIs:** [Users API](#users-api), [Planned Events API](#planned-events-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all work groups
- `GetByIdAsync(int id)` - Get specific work group
- `CreateAsync(WorkGroup)` - Create new work group
- `UpdateAsync(WorkGroup)` - Update existing work group
- `DeleteAsync(int id)` - Delete work group

### Work Group Operations
- `GetWorkGroupsForUserAsync(List<int> userWorkgroupIds, bool canViewAll)` - Get work groups for user
- `GetWorkGroupNameAsync(int workgroupId)` - Get work group name
- `GetWorkGroupsByIdsAsync(List<int> workgroupIds)` - Get multiple work groups by IDs
- `GetWorkGroupAsync(int workgroupId)` - Get specific work group
- `GetWorkGroupsAsync()` - Get all work groups

---

## PE Issues API

Manages issues related to planned events.

**Base Endpoint:** `/api/peissues`

**Related APIs:** [Planned Events API](#planned-events-api), [PE Issue Resolutions API](#pe-issue-resolutions-api), [Users API](#users-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all PE issues
- `GetByIdAsync(int id)` - Get specific issue
- `CreateAsync(PEIssue)` - Create new issue
- `UpdateAsync(PEIssue)` - Update existing issue
- `DeleteAsync(int id)` - Delete issue

### Issue Management
- `GetInboxIssuesAsync(int userId, int limit)` - Get user's inbox issues
- `GetRemindersAsync(int userId, bool showAll)` - Get user's reminders
- `GetReminderCountAsync(int userId)` - Get count of user's reminders
- `MarkAllRemindersAsReadAsync(int userId)` - Mark all reminders as read
- `MarkReminderAsReadAsync(int reminderId)` - Mark specific reminder as read

### Issue Retrieval by PE
- `GetPEIssuesByPlannedEventAsync(int plannedEventId)` - Get issues for specific PE
- `GetPEIssueViewModelsByPlannedEventAsync(int plannedEventId)` - Get issue view models for PE
- `GetPEIssueViewModelsByPlannedEventIdsAsync(List<int> peIds)` - Get issue view models for multiple PEs
- `GetIssuesByPlannedEventIdsAsync(List<int> peIds)` - Get issues for multiple PEs

### User-Specific Operations
- `GetReceivedIssuesAsync(int userId)` - Get issues received by user
- `GetSentIssuesAsync(int userId)` - Get issues sent by user
- `GetUnreadInboxIssuesAsync(int userId)` - Get unread inbox issues
- `GetInboxViewModelsAsync(int userId)` - Get inbox view models
- `GetSentViewModelsAsync(int userId)` - Get sent issues view models

---

## PE Issue Resolutions API

Manages resolutions for PE issues.

**Base Endpoint:** `/api/peissueresolutions`

**Related APIs:** [PE Issues API](#pe-issues-api), [Users API](#users-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all issue resolutions
- `GetByIdAsync(int id)` - Get specific resolution
- `CreateAsync(PEIssueResolution)` - Create new resolution
- `UpdateAsync(PEIssueResolution)` - Update existing resolution
- `DeleteAsync(int id)` - Delete resolution

### Resolution Operations
- `GetResolutionsByIssueIdAsync(int issueId)` - Get resolutions for specific issue
- `GetPendingResolutionsAsync(int userId)` - Get pending resolutions for user
- `ConfirmResolutionAsync(int resolutionId, bool isConfirmed)` - Confirm or reject resolution

---

## Area Network Engineers API

Manages area network engineer assignments.

**Base Endpoint:** `/api/areanetworkengineers`

**Related APIs:** [Planned Events API](#planned-events-api), [Users API](#users-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all area network engineers
- `GetByIdAsync(int id)` - Get specific engineer mapping
- `CreateAsync(AreaNetworkEngineer)` - Create new engineer mapping
- `UpdateAsync(AreaNetworkEngineer)` - Update existing mapping
- `DeleteAsync(int id)` - Delete engineer mapping

### Engineer Lookup
- `GetEngineerNameByAreaAsync(string area)` - Get engineer name for specific area

---

## User Roles API

Manages user roles and role-based permissions.

**Base Endpoint:** `/api/userroles`

**Related APIs:** [Users API](#users-api), [Permissions API](#permissions-api), [Role Permissions API](#role-permissions-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all user roles
- `GetByIdAsync(int id)` - Get specific role
- `CreateAsync(UserRole)` - Create new role
- `UpdateAsync(UserRole)` - Update existing role
- `DeleteAsync(int id)` - Delete role
- `ExistsAsync(int id)` - Check if role exists

### Role Operations
- `GetWithPermissionsAsync(int id)` - Get role with associated permissions
- `IsUserAdminAsync(string serviceId)` - Check if user has admin role
- `GetRolePermissionIdsAsync(int roleId)` - Get permission IDs for role

---

## PE Records API

Manages PE record operations.

**Base Endpoint:** `/api/perecords`

**Related APIs:** [Planned Events API](#planned-events-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all PE records
- `GetByIdAsync(int id)` - Get specific record
- `CreateAsync(PERecord)` - Create new record
- `UpdateAsync(PERecord)` - Update existing record
- `DeleteAsync(int id)` - Delete record

### Record Management
- `GetRecordsByPENumberAsync(string peNumber)` - Get records for specific PE
- `GetLatestRecordByPENumberAsync(string peNumber)` - Get latest record for PE

---

## PE Task Lists API

Manages task list templates and configurations.

**Base Endpoint:** `/api/petasklists`

**Related APIs:** [PE Tasks API](#pe-tasks-api), [Sub Task Lists API](#sub-task-lists-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all task lists
- `GetByIdAsync(int id)` - Get specific task list
- `CreateAsync(PETaskList)` - Create new task list
- `UpdateAsync(PETaskList)` - Update existing task list
- `DeleteAsync(int id)` - Delete task list

### Task List Operations
- `GetTaskListsByTypeAsync(string type)` - Get task lists by type
- `GetActiveTaskListsAsync()` - Get currently active task lists

---

## Customer User Assignments API

Manages customer assignments to users.

**Base Endpoint:** `/api/customeruserassignments`

**Related APIs:** [Users API](#users-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all customer assignments
- `GetByIdAsync(int id)` - Get specific assignment
- `CreateAsync(CustomerUserAssignment)` - Create new assignment
- `UpdateAsync(CustomerUserAssignment)` - Update existing assignment
- `DeleteAsync(int id)` - Delete assignment

### Assignment Operations
- `GetAssignmentsByUserIdAsync(int userId)` - Get assignments for specific user
- `GetAssignmentsByCustomerAsync(string customer)` - Get assignments for specific customer
- `GetUsersForCustomerAsync(string customer)` - Get users assigned to customer

---

## Role Permissions API

Manages the relationship between roles and permissions.

**Base Endpoint:** `/api/rolepermissions`

**Related APIs:** [User Roles API](#user-roles-api), [Permissions API](#permissions-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all role permission mappings
- `GetByIdAsync(int id)` - Get specific mapping
- `CreateAsync(RolePermission)` - Create new mapping
- `UpdateAsync(RolePermission)` - Update existing mapping
- `DeleteAsync(int id)` - Delete mapping

### Permission Management
- `GetPermissionsByRoleIdAsync(int roleId)` - Get permissions for specific role
- `GetRolesByPermissionIdAsync(int permissionId)` - Get roles with specific permission
- `AssignPermissionToRoleAsync(int roleId, int permissionId)` - Assign permission to role
- `RemovePermissionFromRoleAsync(int roleId, int permissionId)` - Remove permission from role

---

## Task Queue API

Manages task queue operations.

**Base Endpoint:** `/api/taskqueue`

**Related APIs:** [PE Tasks API](#pe-tasks-api)

### Queue Operations
- `GetQueuedTasksAsync()` - Get all queued tasks
- `GetTasksByStatusAsync(string status)` - Get tasks by status
- `EnqueueTaskAsync(TaskQueueItem)` - Add task to queue
- `DequeueTaskAsync()` - Remove task from queue
- `UpdateTaskStatusAsync(int taskId, string status)` - Update task status

---

## Sub Task Lists API

Manages sub-task list operations.

**Base Endpoint:** `/api/subtasklists`

**Related APIs:** [PE Task Lists API](#pe-task-lists-api), [PE Tasks API](#pe-tasks-api)

### Basic CRUD Operations
- `GetAllAsync()` - Get all sub task lists
- `GetByIdAsync(int id)` - Get specific sub task list
- `CreateAsync(SubTaskList)` - Create new sub task list
- `UpdateAsync(SubTaskList)` - Update existing sub task list
- `DeleteAsync(int id)` - Delete sub task list

### Sub Task Operations
- `GetSubTasksByParentIdAsync(int parentId)` - Get sub tasks for parent task
- `GetSubTaskListsByTaskIdAsync(int taskId)` - Get sub task lists for specific task

---

## Error Handling

All API endpoints return appropriate HTTP status codes:
- `200 OK` - Successful operation
- `201 Created` - Resource created successfully
- `204 No Content` - Successful operation with no content
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## Authentication

All API endpoints require authentication through Azure AD integration. The application uses:
- OpenID Connect for user authentication
- JWT tokens for API authorization
- Role-based access control (RBAC)

## Base URL Configuration

The API base URL is configured in `appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5292"
  }
}
```

For production environments, update the BaseUrl to point to your deployed API server.

---

## Quick Navigation

**Core APIs:**
- [Planned Events API](#planned-events-api) | [PE Tasks API](#pe-tasks-api) | [PE Issues API](#pe-issues-api)

**User Management:**
- [Users API](#users-api) | [Permissions API](#permissions-api) | [User Roles API](#user-roles-api)

**System Management:**
- [Work Groups API](#work-groups-api) | [Projects API](#projects-api) | [Escalations API](#escalations-api)

**Specialized APIs:**
- [Area Network Engineers API](#area-network-engineers-api) | [Customer User Assignments API](#customer-user-assignments-api) | [Task Queue API](#task-queue-api)

[⬆️ Back to Top](#sfc-dashboard---frontend-api-endpoints-reference)