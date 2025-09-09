using System.ComponentModel.DataAnnotations;

namespace WCPS.WebApp.ViewModels
{
    public class CreateClaimViewModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        [Range(1, 100000)]
        public decimal AmountClaimed { get; set; }
    }
}
