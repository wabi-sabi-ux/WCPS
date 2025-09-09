using Microsoft.AspNetCore.Identity;

namespace WCPS.WebApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? EmployeeNo { get; set; }
        public string? FullName { get; set; }
    }
}
