using System;

namespace WCPS.WebApp.ViewModels
{
    public class AuditEntryViewModel
    {
        public string Action { get; set; } = null!;
        public string? PerformedById { get; set; }
        public string PerformedByName { get; set; } = "System";
        public DateTime Timestamp { get; set; }
    }
}
