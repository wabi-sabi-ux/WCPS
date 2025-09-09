using System.ComponentModel.DataAnnotations;

namespace WCPS.WebApp.ViewModels
{
    public class AdminProcessViewModel
    {
        [Required]
        public int ClaimId { get; set; }

        public string ClaimRef { get; set; } = null!;
        public string Title { get; set; } = null!;
        public decimal AmountClaimed { get; set; }
        public string EmployeeName { get; set; } = null!;

        [Display(Name = "Action")]
        [Required]
        public string Action { get; set; } = "approve"; //"approve"/"reject"

        [Display(Name = "Approved Amount")]
        [Range(0, 100000)]
        public decimal? AmountApproved { get; set; }

        public string? Note { get; set; }
    }
}
