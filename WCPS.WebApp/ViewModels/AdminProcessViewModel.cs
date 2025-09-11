using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WCPS.WebApp.ViewModels
{
    public class AdminProcessViewModel
    {
        [Required]
        public int ClaimId { get; set; }

        // Display-only fields (not required on POST)
        public string? ClaimRef { get; set; }
        public string? Title { get; set; }
        public decimal AmountClaimed { get; set; }
        public string? EmployeeName { get; set; }

        [Display(Name = "Action")]
        [Required]
        public string Action { get; set; } = "approve"; // "approve" or "reject"

        [Display(Name = "Approved Amount")]
        public decimal? AmountApproved { get; set; }

        public string? Note { get; set; }

        public List<AuditEntryViewModel>? AuditEntries { get; set; } = new List<AuditEntryViewModel>();
    }
}
