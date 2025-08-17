using System.Collections.Generic;

namespace SFCDashboard.Api.Models
{
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
}
