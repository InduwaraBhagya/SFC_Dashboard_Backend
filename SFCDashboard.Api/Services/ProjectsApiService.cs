using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;

namespace SFCDashboard.Api.Services
{
    public class ProjectsApiService : IProjectsApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPETasksApiService _peTasksService;
        private readonly ILogger<ProjectsApiService> _logger;

        public ProjectsApiService(ApplicationDbContext context, IPETasksApiService peTasksService, ILogger<ProjectsApiService> logger)
        {
            _context = context;
            _peTasksService = peTasksService;
            _logger = logger;
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            try
            {
                return await _context.Projects
                    .Include(p => p.ProjectPEs)
                    .ThenInclude(pe => pe.PlannedEvent)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all projects");
                return new List<Project>();
            }
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            try
            {
                return await _context.Projects
                    .Include(p => p.ProjectPEs)
                    .ThenInclude(pe => pe.PlannedEvent)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project {ProjectId}", id);
                return null;
            }
        }

        public async Task<ProjectDetailsDto?> GetProjectDetailsAsync(int id)
        {
            try
            {
                var project = await GetProjectByIdAsync(id);
                if (project == null)
                    return null;

                // Prepare PE numbers for all assigned PEs, filtering out nulls
                var peNumbers = project.ProjectPEs
                    .Select(pe => pe.PlannedEvent?.PeNumber)
                    .Where(peNum => !string.IsNullOrEmpty(peNum))
                    .ToList();

                var peTasksDict = await _peTasksService.GetTasksByPeNumbersAsync(peNumbers);

                var peViewModels = new List<ProjectPEViewModelDto>();
                foreach (var pe in project.ProjectPEs)
                {
                    var peNumber = pe.PlannedEvent?.PeNumber;
                    if (string.IsNullOrEmpty(peNumber)) continue;

                    var tasks = peTasksDict.TryGetValue(peNumber, out var tlist) && tlist != null ? tlist.ToList() : new List<PETask>();

                    // Get current task (first not completed by TaskSeq)
                    var currentTask = tasks
                        .Where(t => !string.Equals(t.TaskStatus, "completed", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(t => t.TaskSeq)
                        .FirstOrDefault();

                    peViewModels.Add(new ProjectPEViewModelDto
                    {
                        Id = pe.Id,
                        PlannedEventId = pe.PlannedEventId,
                        PlannedEvent = pe.PlannedEvent!,
                        CurrentTask = currentTask?.Task
                    });
                }

                return new ProjectDetailsDto
                {
                    Id = project.Id,
                    ProjectName = project.ProjectName,
                    CreatedDate = project.CreatedDate,
                    ProjectPEs = peViewModels
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project details for project {ProjectId}", id);
                return null;
            }
        }

        public async Task<List<PlannedEventDto>> SearchPlannedEventsAsync(string searchTerm, int projectId)
        {
            try
            {
                var currentProjectPEs = await _context.ProjectPEMappings
                    .Where(p => p.ProjectId == projectId)
                    .Select(p => p.PlannedEventId)
                    .ToListAsync();

                var results = await _context.PlannedEvents
                    .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                                (p.PeNumber != null && p.PeNumber.ToLower().Contains(searchTerm.ToLower())) ||
                                (p.Customer != null && p.Customer.ToLower().Contains(searchTerm.ToLower())))
                    .Where(p => !currentProjectPEs.Contains(p.Id))
                    .Select(p => new PlannedEventDto
                    {
                        Id = p.Id,
                        PeNumber = p.PeNumber ?? "",
                        Customer = p.Customer ?? "No Customer",
                        Region = p.Region ?? "",
                        WorkOrder = p.WoId ?? "",
                        DueDate = p.ServiceRequiredDate,
                        Priority = p.OrderType ?? ""
                    })
                    .Take(50)
                    .ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching planned events for project {ProjectId} with term {SearchTerm}", projectId, searchTerm);
                return new List<PlannedEventDto>();
            }
        }

        public async Task<bool> CreateProjectAsync(Project project)
        {
            try
            {
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project {ProjectName}", project.ProjectName);
                return false;
            }
        }

        public async Task<bool> AssignPEToProjectAsync(int plannedEventId, int projectId)
        {
            try
            {
                var mapping = new ProjectPEMapping
                {
                    ProjectId = projectId,
                    PlannedEventId = plannedEventId
                };
                _context.ProjectPEMappings.Add(mapping);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning PE {PlannedEventId} to project {ProjectId}", plannedEventId, projectId);
                return false;
            }
        }

        public async Task<bool> AssignMultiplePEsToProjectAsync(int projectId, List<int> plannedEventIds)
        {
            try
            {
                var mappings = plannedEventIds.Select(peId => new ProjectPEMapping
                {
                    ProjectId = projectId,
                    PlannedEventId = peId
                }).ToList();

                _context.ProjectPEMappings.AddRange(mappings);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning multiple PEs to project {ProjectId}", projectId);
                return false;
            }
        }

        public async Task<bool> RemovePEFromProjectAsync(int plannedEventId, int projectId)
        {
            try
            {
                var mapping = await _context.ProjectPEMappings
                    .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.PlannedEventId == plannedEventId);

                if (mapping == null)
                    return false;

                _context.ProjectPEMappings.Remove(mapping);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing PE {PlannedEventId} from project {ProjectId}", plannedEventId, projectId);
                return false;
            }
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null)
                    return false;

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", id);
                return false;
            }
        }
    }
}


