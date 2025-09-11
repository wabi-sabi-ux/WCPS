using Microsoft.AspNetCore.Identity;

namespace WCPS.WebApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? EmployeeNo { get; set; }
        public string? FullName { get; set; }

        public string? BankAccountNumber { get; set; } //for bank account
        public DateTime? LastLoginAt { get; set; } //track last login timestamp
    }
}
