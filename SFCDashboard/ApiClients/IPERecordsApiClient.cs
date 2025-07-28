using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SFCDB.Models;

namespace SFCDashboard.ApiClients
{
    public interface IPERecordsApiClient
    {
        Task<ApiResponse> ImportPERecordsAsync(IFormFile excelFile);
        Task<ApiResponse> ImportPERecordsFromJsonAsync(List<PERecord> peRecords);
        Task<ApiResponse<PaginatedResult<PERecord>>> GetPERecordsAsync(int page = 1, int pageSize = 1000);
        Task<ApiResponse<DatabaseStats>> GetDatabaseStatsAsync();
        Task<ApiResponse> SyncPERecordsAsync();
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
    }

    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class DatabaseStats
    {
        public int PeRecordsCount { get; set; }
        public int PlannedEventsCount { get; set; }
        public int PeTasksCount { get; set; }
        public int TaskTemplatesCount { get; set; }
    }
}
