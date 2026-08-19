namespace ITRepairService.ViewModels.RepairTickets;

public class RepairTicketReportViewModel
{
    public List<RepairTicketReportItemViewModel> Items { get; set; } = new();

    public string? CurrentStatus { get; set; }

    public string Keyword { get; set; } = string.Empty;

    public Dictionary<ITRepairService.Models.TicketStatus, int> StatusCounts { get; set; } = new();
}

public class RepairTicketReportItemViewModel
{
    public int Id { get; set; }

    public string DocumentNo { get; set; } = string.Empty;

    public string RequesterName { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string DriveAccessDepartment { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public string IssueDescription { get; set; } = string.Empty;

    public ITRepairService.Models.RepairType RepairType { get; set; }

    public ITRepairService.Models.TicketPriority Priority { get; set; }

    public ITRepairService.Models.TicketStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string ApproverName { get; set; } = string.Empty;

    public string AssignedItName { get; set; } = string.Empty;

    public DateTime? LastChangedAt { get; set; }

    public string LastChangedByName { get; set; } = string.Empty;

    public string LastAction { get; set; } = string.Empty;

    public string LastRemark { get; set; } = string.Empty;
}
