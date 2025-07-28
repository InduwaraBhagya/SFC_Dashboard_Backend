using Microsoft.AspNetCore.Mvc;
using SFCDashboard.Models;

namespace SFCDashboard.Controllers
{
    // This partial class contains refactored methods using API services
    public partial class PlannedEventsController
    {
        // Refactored helper methods using API services
        private async Task<(List<int> userWorkgroupIds, List<string> userWorkgroupNames, bool canViewAll)> GetCurrentUserWorkGroupsAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return (new List<int>(), new List<string>(), false);

            return await _usersApi.GetCurrentUserWorkGroupsAsync(serviceId);
        }

        private async Task<(int userWorkgroupId, string userWorkgroupName)> GetCurrentUserWorkGroupAsync()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return (0, string.Empty);

            return await _usersApi.GetCurrentUserWorkGroupAsync(serviceId);
        }

        private async Task<bool> HasMultipleWorkgroups()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return false;

            return await _usersApi.HasMultipleWorkgroupsAsync(serviceId);
        }

        private async Task<bool> IsUserInSalesWorkgroup()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return false;

            return await _usersApi.IsUserInSalesWorkgroupAsync(serviceId);
        }

        private async Task<bool> HasDrawFiberAccessAsync(int userId)
        {
            return await _usersApi.HasDrawFiberAccessAsync(userId);
        }

        private async Task<List<string>> GetUserAssignedCustomersAsync()
        {
            var currentUserId = await GetCurrentUserIdAsync();
            return await _usersApi.GetUserAssignedCustomersAsync(currentUserId);
        }

        private async Task<(List<string> salesWorkgroups, bool canViewAll)> GetUserSalesWorkgroups()
        {
            var serviceId = User.Identity?.Name;
            if (string.IsNullOrEmpty(serviceId))
                return (new List<string>(), false);

            return await _usersApi.GetUserSalesWorkgroupsAsync(serviceId);
        }

        // Refactored count methods using API services
        private async Task<int> GetUrgentCountForMultiWorkgroup(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            return await _plannedEventsApi.GetUrgentCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
        }

        private async Task<int> GetInProgressCountForMultiWorkgroup(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            return await _plannedEventsApi.GetInProgressCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
        }

        private async Task<int> GetOLAViolateCountForMultiWorkgroup(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            return await _plannedEventsApi.GetOLAViolateCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
        }

        private async Task<int> GetHoldCountForMultiWorkgroup(List<int> selectedWorkgroupIds, List<int> userWorkgroupIds)
        {
            int currentUserId = await GetCurrentUserIdAsync();
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);
            return await _plannedEventsApi.GetHoldCountForMultiWorkgroupAsync(selectedWorkgroupIds, userWorkgroupIds, hasDrawFiberAccess);
        }

        // Refactored CRUD operations using API services
        public async Task<IActionResult> CreateRefactored([Bind("Id,Province,Region,Rtom,RtomDescription,JobReference,ContractorName,PeNumber,PeActivity,PeNature,PeTitle,PeObjective,PeArea,SoNumber,TaskSeq,TaskName,TaskWg,WoActualStartDate,RequestReferenceNo,SoId,Region1,Province1,Rtom1,Lea,CctId,ServiceCategory,ServiceType,SoCreateDate,OrderType,CrmOrder,WoId,PendingTaskName,PendingWg,PEStatus,StartDate,ServiceSpeed,ServiceRequiredDate,FiberPeNo,FiberSoId,ProductSoId,FiberPeTaskName,FiberPeTaskWg,WoComments,Customer,CusType,AccountManager,SectionHandledBy,LocationAAddress,LocationBAddress,NtuType,AccessMedium,AccessMediumAEnd,AccessMediumBEnd")] PlannedEvent plannedEvent)
        {
            if (ModelState.IsValid)
            {
                var result = await _plannedEventsApi.CreatePlannedEventAsync(plannedEvent);
                if (result != null)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to create planned event");
            }
            return View(plannedEvent);
        }

        public async Task<IActionResult> EditRefactored(int id, [Bind("Id,Province,Region,Rtom,RtomDescription,JobReference,ContractorName,PeNumber,PeActivity,PeNature,PeTitle,PeObjective,PeArea,SoNumber,TaskSeq,TaskName,TaskWg,WoActualStartDate,RequestReferenceNo,SoId,Region1,Province1,Rtom1,Lea,CctId,ServiceCategory,ServiceType,SoCreateDate,OrderType,CrmOrder,WoId,PendingTaskName,PendingWg,PEStatus,StartDate,ServiceSpeed,ServiceRequiredDate,FiberPeNo,FiberSoId,ProductSoId,FiberPeTaskName,FiberPeTaskWg,WoComments,Customer,CusType,AccountManager,SectionHandledBy,LocationAAddress,LocationBAddress,NtuType,AccessMedium,AccessMediumAEnd,AccessMediumBEnd")] PlannedEvent plannedEvent)
        {
            if (id != plannedEvent.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _plannedEventsApi.UpdatePlannedEventAsync(plannedEvent);
                    if (result != null)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating planned event");
                    if (!await PlannedEventExists(plannedEvent.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to update planned event");
                    }
                }
            }
            return View(plannedEvent);
        }

        // Refactored Details method using API services
        public async Task<IActionResult> DetailsRefactored(int? id, string? returnUrl = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = await GetCurrentUserIdAsync();
            var currentUser = await _usersApi.GetUserAsync(currentUserId);

            if (currentUser == null)
            {
                return NotFound();
            }

            ViewData["CanMakeTasksUrgent"] = currentUser.UserRole?.HasPermission("CanMakeTasksUrgent") == true;
            ViewData["CanReportIssues"] = currentUser.UserRole?.HasPermission("CanReportIssues") == true;

            // Get the planned event
            var plannedEvent = await _plannedEventsApi.GetPlannedEventAsync(id.Value);
            if (plannedEvent == null)
            {
                return NotFound();
            }

            // Find the engineer by LEA code
            string? engineerName = null;
            if (!string.IsNullOrEmpty(plannedEvent.Lea))
            {
                engineerName = await _areaNetworkEngineersApi.GetEngineerNameByAreaAsync(plannedEvent.Lea);
            }
            ViewBag.NetworkEngineer = engineerName ?? "Not Assigned";

            // Check if current task is "Draw Fiber"
            bool isCurrentTaskDrawFiber = plannedEvent.TaskName?.Trim().ToLower() == "draw fiber";
            bool hasDrawFiberAccess = await HasDrawFiberAccessAsync(currentUserId);

            ViewData["CanManageEstimatedTime"] =
                currentUser.UserRole?.HasPermission("CanManageEstimatedTime") == true &&
                hasDrawFiberAccess &&
                isCurrentTaskDrawFiber;

            // Get related PE tasks for this event
            var peTasks = (await _peTasksApi.GetPETasksByPENumberAsync(plannedEvent.PeNumber ?? "")).ToList();
            if (!string.IsNullOrEmpty(plannedEvent.PeNumber))
            {
                SetTaskDatesFromPeNumber(plannedEvent.PeNumber, peTasks);
            }
            ViewBag.PETasks = peTasks;

            // Find PETaskListId for the current task name
            if (!string.IsNullOrEmpty(plannedEvent.TaskName))
            {
                var taskList = await _peTaskListsApi.GetPETaskListByNameAsync(plannedEvent.TaskName);
                ViewBag.CurrentTaskListId = taskList?.Id;
            }

            // Load issues for this PE
            var issues = await _peIssuesApi.GetPEIssueViewModelsByPlannedEventAsync(plannedEvent.Id);
            ViewBag.PEReportedIssues = issues;

            // Get escalations for PE tasks
            var peTaskIds = peTasks.Select(t => t.Id).ToList();
            var escalations = await _escalationsApi.GetEscalationsByTaskIdsAsync(peTaskIds);
            plannedEvent.Escalations = escalations.ToList();

            ViewBag.ReturnUrl = returnUrl;
            return View(plannedEvent);
        }
    }
}
