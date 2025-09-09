namespace WCPS.WebApp.Models
{
    public class AuditTrail
    {
        public int Id { get; set; }
        public string Entity { get; set; } = null!;
        public int EntityId { get; set; }
        public string Action { get; set; } = null!;
        public string? PerformedById { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
