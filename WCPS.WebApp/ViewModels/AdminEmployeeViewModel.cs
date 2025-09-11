using System;

namespace WCPS.WebApp.ViewModels
{
    public class AdminEmployeeViewModel
    {
        public string Id { get; set; } = null!;
        public string? EmployeeNo { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = null!;
        public string Roles { get; set; } = null!; // comma-separated
        public DateTime? LastLoginAt { get; set; }

        public string? BankAccountNumber { get; set; }
    }
}
