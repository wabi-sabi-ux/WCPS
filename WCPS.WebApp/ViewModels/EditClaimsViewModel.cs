using System.ComponentModel.DataAnnotations;

namespace WCPS.WebApp.ViewModels
{
    public class EditClaimViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        [Range(1, 100000)]
        public decimal AmountClaimed { get; set; }

        // read-only: path stored in DB for display/preview
        public string? ExistingReceiptPath { get; set; }
    }
}
