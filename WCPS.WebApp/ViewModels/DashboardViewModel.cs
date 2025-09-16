using System;
using System.Collections.Generic;
using WCPS.WebApp.Models;

namespace WCPS.WebApp.ViewModels
{
    public class DashboardViewModel
    {
        // generic
        public bool IsAdmin { get; set; }

        // employee stats
        public int MyTotalClaims { get; set; }
        public int MyPendingClaims { get; set; }
        public int MyApprovedClaims { get; set; }
        public int MyRejectedClaims { get; set; }
        public List<ClaimSummaryVm> RecentMyClaims { get; set; } = new();

        // admin stats
        public int PendingClaimsCount { get; set; }
        public int AdminApprovedCount { get; set; }
        public int AdminRejectedCount { get; set; }
        public int TotalEmployees { get; set; }
        public List<AuditSummaryVm> RecentActivity { get; set; } = new();
    }

    public class ClaimSummaryVm
    {
        public int Id { get; set; }
        public string ClaimRef { get; set; } = null!;
        public string Title { get; set; } = null!;
        public decimal AmountClaimed { get; set; }
        public decimal? AmountApproved { get; set; }
        public ClaimStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditSummaryVm
    {
        public string Entity { get; set; } = null!;
        public int EntityId { get; set; }
        public string Action { get; set; } = null!;
        public string? PerformedById { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
