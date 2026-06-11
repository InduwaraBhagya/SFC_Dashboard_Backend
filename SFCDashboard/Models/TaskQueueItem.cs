namespace SFCDashboard.Models
{
    public class TaskQueueItem
    {
        public required PETask Task { get; set; }
        public double PriorityScore { get; set; }
        public int DaysUntilDue { get; set; }
        public DateTime? EffectiveDeadline { get; set; }
        public int OLAInDays { get; set; }
        public double OLAPercentRemaining { get; set; }
        public DateTime? UrgentMarkedDate => Task?.UrgentMarkedDate;

        // Helper properties for UI display
        public bool IsOverdue => DaysUntilDue < 0;
        
        public string DueStatus 
        {
            get
            {
                if (IsOverdue)
                    return $"Overdue by {Math.Abs(DaysUntilDue)} days";
                else if (DaysUntilDue == 0)
                    return "Due today";
                else if (DaysUntilDue == 1)
                    return "Due tomorrow";
                else
                    return $"Due in {DaysUntilDue} days";
            }
        }
        
        public string OLAStatus
        {
            get
            {
                if (Task.IsOLAViolate)
                    return "OLA Violated";
                else if (OLAPercentRemaining <= 10)
                    return "Critical (≤10% OLA left)";
                else if (OLAPercentRemaining <= 30)
                    return "Warning (≤30% OLA left)";
                else
                    return $"{OLAPercentRemaining:F0}% of OLA remains";
            }
        }
        
        public string PriorityLevel 
        {
            get 
            {
                if (Task.IsUrgent)
                {
                    if (Task.Priority?.Contains("Opening Ceremony") == true)
                        return "URGENT P1";
                    else if (Task.Priority?.Contains("Critical Customer") == true)
                        return "URGENT P2";
                    else
                        return "URGENT";
                }
                else if (Task.IsOLAViolate)
                    return "OLA VIOLATION";
                else if (DaysUntilDue >= 0 && DaysUntilDue <= Math.Min(2, Math.Ceiling(OLAInDays * 0.3)))
                    return "APPROACHING DEADLINE";
                else
                    return "REGULAR";
            }
        }
        
        public string UrgencyLevel
        {
            get
            {
                if (Task?.IsUrgent == true)
                    return "Urgent";
                else if (IsOverdue)
                    return "Overdue";
                else if (DaysUntilDue <= 1)
                    return "Due Soon";
                else
                    return "Normal";
            }
        }
    }
}
