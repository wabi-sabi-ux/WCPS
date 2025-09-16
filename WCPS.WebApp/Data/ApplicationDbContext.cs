using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WCPS.WebApp.Models;

namespace WCPS.WebApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }
        public DbSet<ClaimRequest> ClaimRequests { get; set; } //for claim requests
        public DbSet<AuditTrail> AuditTrails { get; set; } //for audittrail
    }
}
