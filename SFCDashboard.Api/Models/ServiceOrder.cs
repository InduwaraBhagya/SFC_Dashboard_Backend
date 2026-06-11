using System;

namespace SFCDashboard.Api.Models
{
    public class ServiceOrder
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Map additional columns from SOMS table as needed.
        // For example: public string CustomerName { get; set; } = string.Empty;
    }
}