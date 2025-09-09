using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WCPS.WebApp.Models
{
    public enum ClaimStatus { Pending, Approved, Rejected }

    public class ClaimRequest
    {
        public int Id { get; set; }
        public string ClaimRef { get; set; } = Guid.NewGuid().ToString("N");

        [Required]
        public string EmployeeId { get; set; } = null!;
        public ApplicationUser? Employee { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Range(1, 100000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountClaimed { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountApproved { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
