#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITRepairService.Data;
using ITRepairService.Models;
using ITRepairService.ViewModels.RepairTickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ITRepairService.Controllers;

[Authorize]
public class RepairTicketsController(AppDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<IActionResult> Index(string? status, string? sort, string? dir)
    {
        var query = _context.RepairTickets.AsNoTracking().AsQueryable();
        var canSeeInProgress = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ITSupport);

        var currentUser = await _userManager.GetUserAsync(User);
        var currentUserId = currentUser?.Id;
        var currentUserFullName = currentUser?.FullName?.Trim();
        var currentUserUserName = currentUser?.UserName?.Trim();
        var currentUserEmail = currentUser?.Email?.Trim();

        var isAdmin = User.IsInRole(AppRoles.Admin);
        var isApprove = User.IsInRole(AppRoles.Approve);

        if (!isAdmin)
        {
            // Show tickets where user is involved: as approver, requester, or assigned IT
            if (isApprove)
            {
                query = query.Where(ticket =>
                    (!string.IsNullOrWhiteSpace(currentUserId) && ticket.ApproverUserId == currentUserId)
                    || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.ApproverName == currentUserFullName)
                    || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.ApproverName == currentUserUserName)
                    || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.ApproverName == currentUserEmail)
                    || (!string.IsNullOrWhiteSpace(currentUserId) && ticket.SecondApproverUserId == currentUserId)
                    || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.SecondApproverName == currentUserFullName)
                    || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.SecondApproverName == currentUserUserName)
                    || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.SecondApproverName == currentUserEmail)
                    || (!string.IsNullOrWhiteSpace(currentUserId) && ticket.ThirdApproverUserId == currentUserId)
                    || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.ThirdApproverName == currentUserFullName)
                    || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.ThirdApproverName == currentUserUserName)
                    || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.ThirdApproverName == currentUserEmail)
                    || (!string.IsNullOrWhiteSpace(currentUserId) && ticket.NextApproverUserId == currentUserId)
                    || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.NextApproverName == currentUserFullName)
                    || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.NextApproverName == currentUserUserName)
                    || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.NextApproverName == currentUserEmail)
                    || (!string.IsNullOrWhiteSpace(currentUserId) && ticket.AssignedItUserId == currentUserId)
                    || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.AssignedItName == currentUserFullName)
                    || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.AssignedItName == currentUserUserName)
                    || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.AssignedItName == currentUserEmail)
                    || (!string.IsNullOrWhiteSpace(currentUserId) && ticket.RequesterUserId == currentUserId)
                    || (string.IsNullOrWhiteSpace(ticket.RequesterUserId)
                        && (
                            (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.RequesterName == currentUserFullName)
                            || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.RequesterName == currentUserUserName)
                            || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.RequesterName == currentUserEmail)
                            || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.CreatedByName == currentUserFullName)
                            || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.CreatedByName == currentUserUserName)
                            || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.CreatedByName == currentUserEmail)
                        )));
            }
            else
            {
                query = query.Where(ticket =>
                    (!string.IsNullOrWhiteSpace(currentUserId) && ticket.AssignedItUserId == currentUserId)
                    || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.AssignedItName == currentUserFullName)
                    || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.AssignedItName == currentUserUserName)
                    || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.AssignedItName == currentUserEmail)
                    || (!string.IsNullOrWhiteSpace(currentUserId) && ticket.RequesterUserId == currentUserId)
                    || (string.IsNullOrWhiteSpace(ticket.RequesterUserId)
                        && (
                            (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.RequesterName == currentUserFullName)
                            || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.RequesterName == currentUserUserName)
                            || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.RequesterName == currentUserEmail)
                            || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.CreatedByName == currentUserFullName)
                            || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.CreatedByName == currentUserUserName)
                            || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.CreatedByName == currentUserEmail)
                        )));
            }
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TicketStatus>(status, true, out var parsedStatus))
        {
            if (parsedStatus != TicketStatus.InProgress || canSeeInProgress)
            {
                query = query.Where(ticket => ticket.Status == parsedStatus);
                ViewData["CurrentStatus"] = parsedStatus.ToString();
            }
        }
        else
        {
            // No status filter selected: show all statuses, including Closed.
        }

        var currentSort = string.IsNullOrWhiteSpace(sort) ? "created" : sort.Trim().ToLowerInvariant();
        var currentDir = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        query = (currentSort, currentDir) switch
        {
            ("requester", "asc") => query.OrderBy(ticket => ticket.RequesterName).ThenByDescending(ticket => ticket.CreatedAt),
            ("requester", "desc") => query.OrderByDescending(ticket => ticket.RequesterName).ThenByDescending(ticket => ticket.CreatedAt),
            ("priority", "asc") => query.OrderBy(ticket => ticket.Priority).ThenByDescending(ticket => ticket.CreatedAt),
            ("priority", "desc") => query.OrderByDescending(ticket => ticket.Priority).ThenByDescending(ticket => ticket.CreatedAt),
            ("created", "asc") => query.OrderBy(ticket => ticket.CreatedAt),
            _ => query.OrderByDescending(ticket => ticket.CreatedAt)
        };

        ViewData["CurrentSort"] = currentSort;
        ViewData["CurrentDir"] = currentDir;

        ViewData["CurrentUserId"] = currentUserId;
        ViewData["CurrentUserFullName"] = currentUserFullName;
        ViewData["CurrentUserUserName"] = currentUserUserName;
        ViewData["CurrentUserEmail"] = currentUserEmail;

        var tickets = await query.ToListAsync();

        var latestStatusUpdatedBy = new Dictionary<int, string>();
        if (tickets.Count > 0)
        {
            var ticketIds = tickets.Select(ticket => ticket.Id).ToList();
            var timeline = await _context.RepairTicketStatusHistories
                .AsNoTracking()
                .Where(history => ticketIds.Contains(history.RepairTicketId))
                .OrderByDescending(history => history.ChangedAt)
                .ThenByDescending(history => history.Id)
                .Select(history => new { history.RepairTicketId, history.ChangedByName })
                .ToListAsync();

            foreach (var item in timeline)
            {
                if (!latestStatusUpdatedBy.ContainsKey(item.RepairTicketId)
                    && !string.IsNullOrWhiteSpace(item.ChangedByName))
                {
                    latestStatusUpdatedBy[item.RepairTicketId] = item.ChangedByName.Trim();
                }
            }
        }

        ViewData["LatestStatusUpdatedBy"] = latestStatusUpdatedBy;

        return View(tickets);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ITSupport)]
    public async Task<IActionResult> Report(string? status, string? keyword, bool print = false, int? ticketId = null)
    {
        RepairTicketReportViewModel model;
        if (ticketId.HasValue)
        {
            model = await BuildReportModelByIdAsync(ticketId.Value);

            if (print)
            {
                var printTimeline = await _context.RepairTicketStatusHistories
                    .AsNoTracking()
                    .Where(history => history.RepairTicketId == ticketId.Value)
                    .OrderBy(history => history.ChangedAt)
                    .ThenBy(history => history.Id)
                    .ToListAsync();

                ViewData["PrintTimeline"] = printTimeline;

                var itSupportUserIds = (await _userManager.GetUsersInRoleAsync(AppRoles.ITSupport))
                    .Select(user => user.Id)
                    .ToHashSet(StringComparer.Ordinal);
                var approveUserIds = (await _userManager.GetUsersInRoleAsync(AppRoles.Approve))
                    .Select(user => user.Id)
                    .ToHashSet(StringComparer.Ordinal);
                var itApproverUserIds = itSupportUserIds
                    .Intersect(approveUserIds)
                    .ToHashSet(StringComparer.Ordinal);

                if (itApproverUserIds.Count > 0)
                {
                    var itApproverEvent = printTimeline
                        .LastOrDefault(history =>
                            !string.IsNullOrWhiteSpace(history.ChangedByUserId)
                            && itApproverUserIds.Contains(history.ChangedByUserId)
                            && (history.Action == "Closed" || history.ToStatus == TicketStatus.Closed));

                    if (itApproverEvent is not null)
                    {
                        ViewData["PrintItApproverName"] = itApproverEvent.ChangedByName;
                        ViewData["PrintItApproverDate"] = itApproverEvent.ChangedAt;
                    }
                }
            }
        }
        else
        {
            model = await BuildReportModelAsync(status, keyword);
        }
        ViewData["PrintMode"] = print;

        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ITSupport)]
    public IActionResult ExportReportPdf(string? status, string? keyword)
    {
        // Keep existing button routes but open the report in preview mode instead of browser print dialog.
        return RedirectToAction(nameof(Report), new { status, keyword, print = true });
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ITSupport)]
    public IActionResult ExportTicketPdf(int id)
    {
        return RedirectToAction(nameof(Report), new { ticketId = id, print = true });
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ITSupport)]
    public async Task<IActionResult> PerformanceReport(DateTime? fromDate, DateTime? toDate, string? itName)
    {
        var model = await BuildPerformanceReportModelAsync(fromDate, toDate, itName);
        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ITSupport)]
    public async Task<IActionResult> ExportReportExcel()
    {
        var model = await BuildReportModelAsync(null, null);

        var csv = new StringBuilder();
        csv.AppendLine("Id,DocumentNo,RequesterName,Department,RepairType,Priority,Status,AssignedItName,ApproverName,CreatedAt,UpdatedAt,LastChangedAt,LastChangedByName,LastAction,LastRemark,IssueDescription");

        foreach (var item in model.Items)
        {
            csv.AppendLine(string.Join(",",
                CsvEscape(item.Id.ToString()),
                CsvEscape(item.DocumentNo),
                CsvEscape(item.RequesterName),
                CsvEscape(item.Department),
                CsvEscape(item.RepairType.ToString()),
                CsvEscape(item.Priority.ToString()),
                CsvEscape(item.Status.ToString()),
                CsvEscape(item.AssignedItName),
                CsvEscape(item.ApproverName),
                CsvEscape(item.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
                CsvEscape(item.UpdatedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty),
                CsvEscape(item.LastChangedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty),
                CsvEscape(item.LastChangedByName),
                CsvEscape(item.LastAction),
                CsvEscape(item.LastRemark),
                CsvEscape(item.IssueDescription)));
        }

        var content = "\uFEFF" + csv;
        var bytes = Encoding.UTF8.GetBytes(content);
        var fileName = $"RepairTickets_Report_All_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private async Task<RepairTicketPerformanceReportViewModel> BuildPerformanceReportModelAsync(DateTime? fromDate, DateTime? toDate, string? itName)
    {
        var query = _context.RepairTickets.AsNoTracking()
            .Where(ticket => ticket.Status == TicketStatus.Closed || ticket.Status == TicketStatus.InProgress)
            .AsQueryable();

        var trimmedItName = itName?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(trimmedItName))
        {
            query = query.Where(ticket =>
                EF.Functions.Like(ticket.AssignedItName, $"%{trimmedItName}%")
                || EF.Functions.Like(ticket.AssignedItUserId, $"%{trimmedItName}%"));
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync();

        var assigneeUserIds = tickets
            .Where(ticket => string.IsNullOrWhiteSpace(ticket.AssignedItName) && !string.IsNullOrWhiteSpace(ticket.AssignedItUserId))
            .Select(ticket => ticket.AssignedItUserId)
            .Distinct()
            .ToList();

        var assigneeNameByUserId = assigneeUserIds.Count > 0
            ? await _context.Users.AsNoTracking()
                .Where(user => assigneeUserIds.Contains(user.Id))
                .Select(user => new { user.Id, user.FullName, user.UserName, user.Email })
                .ToDictionaryAsync(
                    user => user.Id,
                    user => !string.IsNullOrWhiteSpace(user.FullName)
                        ? user.FullName
                        : (user.UserName ?? user.Email ?? user.Id))
            : new Dictionary<string, string>();

        var ticketIds = tickets.Select(ticket => ticket.Id).ToList();
        var histories = ticketIds.Count == 0
            ? new List<RepairTicketStatusHistory>()
            : await _context.RepairTicketStatusHistories.AsNoTracking()
                .Where(history => ticketIds.Contains(history.RepairTicketId))
                .OrderBy(history => history.ChangedAt)
                .ThenBy(history => history.Id)
                .ToListAsync();

        var historiesByTicketId = histories
            .GroupBy(history => history.RepairTicketId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var items = new List<RepairTicketPerformanceCaseViewModel>();
        var nowUtc = DateTime.UtcNow;
        foreach (var ticket in tickets)
        {
            if (!historiesByTicketId.TryGetValue(ticket.Id, out var ticketHistory))
            {
                continue;
            }

            var assignedEvent = ticketHistory
                .FirstOrDefault(history => history.ToStatus == TicketStatus.InProgress);
            var closedEvent = ticketHistory
                .LastOrDefault(history => history.ToStatus == TicketStatus.Closed || history.Action == "Closed");

            if (assignedEvent is null)
            {
                continue;
            }

            var endAtUtc = closedEvent?.ChangedAt ?? nowUtc;
            if (endAtUtc < assignedEvent.ChangedAt)
            {
                continue;
            }

            var assignedItName = !string.IsNullOrWhiteSpace(ticket.AssignedItName)
                ? ticket.AssignedItName
                : (!string.IsNullOrWhiteSpace(ticket.AssignedItUserId)
                    && assigneeNameByUserId.TryGetValue(ticket.AssignedItUserId, out var resolvedName)
                        ? resolvedName
                        : "-");

            var duration = endAtUtc - assignedEvent.ChangedAt;
            var referenceLocalDate = endAtUtc.ToLocalTime().Date;
            if (fromDate.HasValue && referenceLocalDate < fromDate.Value.Date)
            {
                continue;
            }

            if (toDate.HasValue && referenceLocalDate > toDate.Value.Date)
            {
                continue;
            }

            items.Add(new RepairTicketPerformanceCaseViewModel
            {
                TicketId = ticket.Id,
                DocumentNo = ticket.DocumentNo ?? string.Empty,
                RequesterName = ticket.RequesterName,
                Department = ticket.Department,
                AssignedItName = assignedItName,
                AssignedAt = assignedEvent.ChangedAt,
                ClosedAt = closedEvent?.ChangedAt,
                CurrentStatus = ticket.Status,
                Duration = duration
            });
        }

        var summary = items
            .GroupBy(item => string.IsNullOrWhiteSpace(item.AssignedItName) ? "-" : item.AssignedItName)
            .Select(group => new RepairTicketPerformanceStaffSummaryViewModel
            {
                ItStaffName = group.Key,
                ClosedCases = group.Count(),
                AverageHours = group.Average(item => item.Duration.TotalHours),
                MinimumHours = group.Min(item => item.Duration.TotalHours),
                MaximumHours = group.Max(item => item.Duration.TotalHours)
            })
            .OrderBy(summaryItem => summaryItem.AverageHours)
            .ThenBy(summaryItem => summaryItem.ItStaffName)
            .ToList();

        return new RepairTicketPerformanceReportViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            ItName = trimmedItName,
            Cases = items.OrderByDescending(item => item.ClosedAt).ToList(),
            StaffSummaries = summary
        };
    }

    private async Task<RepairTicketReportViewModel> BuildReportModelByIdAsync(int id)
    {
        var ticket = await _context.RepairTickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return new RepairTicketReportViewModel { Items = [], StatusCounts = [] };
        }

        var latestHistory = await _context.RepairTicketStatusHistories.AsNoTracking()
            .Where(h => h.RepairTicketId == id)
            .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
            .FirstOrDefaultAsync();

        var assignedItName = ticket.AssignedItName;
        if (string.IsNullOrWhiteSpace(assignedItName) && !string.IsNullOrWhiteSpace(ticket.AssignedItUserId))
        {
            var assignee = await _context.Users.AsNoTracking()
                .Where(user => user.Id == ticket.AssignedItUserId)
                .Select(user => new { user.FullName, user.UserName, user.Email })
                .FirstOrDefaultAsync();

            if (assignee is not null)
            {
                assignedItName = !string.IsNullOrWhiteSpace(assignee.FullName)
                    ? assignee.FullName
                    : (assignee.UserName ?? assignee.Email ?? ticket.AssignedItUserId);
            }
            else
            {
                assignedItName = ticket.AssignedItUserId;
            }
        }

        var item = new RepairTicketReportItemViewModel
        {
            Id = ticket.Id,
            DocumentNo = ticket.DocumentNo ?? string.Empty,
            RequesterName = ticket.RequesterName,
            Department = ticket.Department,
            DeviceName = ticket.DeviceName,
            IssueDescription = ticket.IssueDescription,
            RepairType = ticket.RepairType,
            Priority = ticket.Priority,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ApproverName = ticket.ApproverName,
            AssignedItName = assignedItName ?? string.Empty,
            LastChangedAt = latestHistory?.ChangedAt,
            LastChangedByName = latestHistory?.ChangedByName ?? string.Empty,
            LastAction = latestHistory?.Action ?? string.Empty,
            LastRemark = latestHistory?.Remark ?? string.Empty
        };

        return new RepairTicketReportViewModel
        {
            Items = [item],
            StatusCounts = Enum.GetValues<TicketStatus>().ToDictionary(s => s, s => 0)
        };
    }

    private async Task<RepairTicketReportViewModel> BuildReportModelAsync(string? status, string? keyword)
    {
        var query = _context.RepairTickets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TicketStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(ticket => ticket.Status == parsedStatus);
        }

        var trimmedKeyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(ticket =>
                EF.Functions.Like(ticket.RequesterName, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.Department, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.DeviceName, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.IssueDescription, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.ApproverName, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.AssignedItName, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.DocumentNo ?? string.Empty, $"%{trimmedKeyword}%"));
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync();

        var latestHistoryByTicketId = new Dictionary<int, RepairTicketStatusHistory>();
        if (tickets.Count > 0)
        {
            var ticketIds = tickets.Select(ticket => ticket.Id).ToList();
            var histories = await _context.RepairTicketStatusHistories
                .AsNoTracking()
                .Where(history => ticketIds.Contains(history.RepairTicketId))
                .OrderByDescending(history => history.ChangedAt)
                .ThenByDescending(history => history.Id)
                .ToListAsync();

            foreach (var history in histories)
            {
                if (!latestHistoryByTicketId.ContainsKey(history.RepairTicketId))
                {
                    latestHistoryByTicketId[history.RepairTicketId] = history;
                }
            }
        }

        var assigneeUserIds = tickets
            .Where(ticket => string.IsNullOrWhiteSpace(ticket.AssignedItName) && !string.IsNullOrWhiteSpace(ticket.AssignedItUserId))
            .Select(ticket => ticket.AssignedItUserId)
            .Distinct()
            .ToList();

        var assigneeNameByUserId = assigneeUserIds.Count > 0
            ? await _context.Users.AsNoTracking()
                .Where(user => assigneeUserIds.Contains(user.Id))
                .Select(user => new { user.Id, user.FullName, user.UserName, user.Email })
                .ToDictionaryAsync(
                    user => user.Id,
                    user => !string.IsNullOrWhiteSpace(user.FullName)
                        ? user.FullName
                        : (user.UserName ?? user.Email ?? user.Id))
            : new Dictionary<string, string>();

        return new RepairTicketReportViewModel
        {
            CurrentStatus = status,
            Keyword = trimmedKeyword,
            StatusCounts = Enum.GetValues<TicketStatus>()
                .ToDictionary(
                    ticketStatus => ticketStatus,
                    ticketStatus => tickets.Count(ticket => ticket.Status == ticketStatus)),
            Items = tickets.Select(ticket =>
            {
                latestHistoryByTicketId.TryGetValue(ticket.Id, out var latestHistory);
                var assignedItName = ticket.AssignedItName;
                if (string.IsNullOrWhiteSpace(assignedItName) && !string.IsNullOrWhiteSpace(ticket.AssignedItUserId))
                {
                    assignedItName = assigneeNameByUserId.TryGetValue(ticket.AssignedItUserId, out var resolvedName)
                        ? resolvedName
                        : ticket.AssignedItUserId;
                }

                return new RepairTicketReportItemViewModel
                {
                    Id = ticket.Id,
                    DocumentNo = ticket.DocumentNo ?? string.Empty,
                    RequesterName = ticket.RequesterName,
                    Department = ticket.Department,
                    DeviceName = ticket.DeviceName,
                    IssueDescription = ticket.IssueDescription,
                    RepairType = ticket.RepairType,
                    Priority = ticket.Priority,
                    Status = ticket.Status,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt,
                    ApproverName = ticket.ApproverName,
                    AssignedItName = assignedItName ?? string.Empty,
                    LastChangedAt = latestHistory?.ChangedAt,
                    LastChangedByName = latestHistory?.ChangedByName ?? string.Empty,
                    LastAction = latestHistory?.Action ?? string.Empty,
                    LastRemark = latestHistory?.Remark ?? string.Empty
                };
            }).ToList()
        };
    }

    public async Task<IActionResult> Details(int? id)
    {
        return await RenderDetailsViewAsync(id, "Details");
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ITSupport + "," + AppRoles.Approve)]
    public async Task<IActionResult> ReportDetails(int? id)
    {
        return await RenderDetailsViewAsync(id, "ReportDetails");
    }

    private async Task<IActionResult> RenderDetailsViewAsync(int? id, string viewName)
    {
        if (id is null)
        {
            return NotFound();
        }

        var ticket = await _context.RepairTickets
            .FirstOrDefaultAsync(ticket => ticket.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(ticket.AssignedItName) && !string.IsNullOrWhiteSpace(ticket.AssignedItUserId))
        {
            var assignee = await _context.Users.AsNoTracking()
                .Where(user => user.Id == ticket.AssignedItUserId)
                .Select(user => new { user.FullName, user.UserName, user.Email })
                .FirstOrDefaultAsync();

            ticket.AssignedItName = assignee is null
                ? ticket.AssignedItUserId
                : (!string.IsNullOrWhiteSpace(assignee.FullName)
                    ? assignee.FullName
                    : (assignee.UserName ?? assignee.Email ?? ticket.AssignedItUserId));
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (ticket.Status == TicketStatus.Rejected && currentUser is not null && IsOwnerTicket(ticket, currentUser))
        {
            var latestRejectedAt = await _context.RepairTicketStatusHistories
                .AsNoTracking()
                .Where(history => history.RepairTicketId == ticket.Id && history.ToStatus == TicketStatus.Rejected)
                .OrderByDescending(history => history.ChangedAt)
                .ThenByDescending(history => history.Id)
                .Select(history => (DateTime?)history.ChangedAt)
                .FirstOrDefaultAsync();

            if (latestRejectedAt.HasValue)
            {
                var hasReadAfterLatestReject = await _context.RepairTicketStatusHistories
                    .AsNoTracking()
                    .AnyAsync(history =>
                        history.RepairTicketId == ticket.Id
                        && history.Action == "RejectedRead"
                        && history.ChangedByUserId == currentUser.Id
                        && history.ChangedAt >= latestRejectedAt.Value);

                if (!hasReadAfterLatestReject)
                {
                    ticket.UpdatedAt = DateTime.UtcNow;
                    ticket.UpdatedByName = GetActorName(currentUser);
                    AddStatusHistory(ticket, ticket.Status, ticket.Status, currentUser, "RejectedRead");
                    await _context.SaveChangesAsync();
                }
            }
        }

        var canSeeInProgress = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ITSupport);
        var canEdit = User.IsInRole(AppRoles.ITSupport)
            || User.IsInRole(AppRoles.Admin)
            || User.IsInRole(AppRoles.Approve)
            || (ticket.Status == TicketStatus.Rejected && IsOwnerTicket(ticket, currentUser));
        if (!canSeeInProgress && ticket.Status == TicketStatus.InProgress)
        {
            canEdit = false;
        }

        ViewData["CanEditTicket"] = canEdit;

        var timeline = await _context.RepairTicketStatusHistories
            .AsNoTracking()
            .Where(history => history.RepairTicketId == ticket.Id)
            .OrderByDescending(history => history.ChangedAt)
            .ToListAsync();
        ViewData["StatusTimeline"] = timeline;

        return View(viewName, ticket);
    }

    public async Task<IActionResult> Create()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        ViewData["CurrentUserId"] = currentUser?.Id;
        var ticket = new RepairTicket
        {
            RequesterName = !string.IsNullOrWhiteSpace(currentUser?.FullName)
                ? currentUser.FullName
                : (currentUser?.UserName ?? User.Identity?.Name ?? string.Empty),
            Department = currentUser?.Department ?? string.Empty,
            Status = TicketStatus.Open
        };

        await PopulateApproverSelectionsAsync(ticket.ApproverDepartment, ticket.ApproverUserId);
        await PopulateDriveAccessDepartmentSelectionsAsync(ticket.DriveAccessDepartment);
        await PopulateThirdApproverSelectionsAsync(string.Empty);
        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("RequesterName,Department,DeviceName,IssueDescription,RepairType,DriveAccessDepartment,Priority,Status,AssignedItUserId")] RepairTicket ticket, IFormFile? pdfAttachment)
    {
        ticket.Priority = TicketPriority.Medium;

        var canSetStatus = User.IsInRole(AppRoles.ITSupport) || User.IsInRole(AppRoles.Admin);
        var isApproveCreator = User.IsInRole(AppRoles.Approve);
        if (isApproveCreator && !canSetStatus)
        {
            // Approver-created tickets move through Open -> Approved immediately.
            ticket.Status = TicketStatus.Approved;
        }
        else if (!canSetStatus)
        {
            ticket.Status = TicketStatus.Open;
        }

        var currentUser = await _userManager.GetUserAsync(User);
        ticket.RequesterName = !string.IsNullOrWhiteSpace(currentUser?.FullName)
            ? currentUser.FullName
            : (currentUser?.UserName ?? User.Identity?.Name ?? string.Empty);

        ticket.Department = ticket.Department?.Trim() ?? string.Empty;
        ticket.ApproverDepartment = ticket.ApproverDepartment?.Trim() ?? string.Empty;
        ticket.DriveAccessDepartment = ticket.DriveAccessDepartment?.Trim() ?? string.Empty;
        ticket.IssueDescription = ticket.IssueDescription?.Trim() ?? string.Empty;

        // Set approval level based on repair type
        if (ticket.RepairType == RepairType.DriveAccessPermission)
        {
            ticket.ApprovalLevel = 2; // Drive Access requires 2-level approval (first + drive dept SM/DM)
        }
        else
        {
            ticket.ApprovalLevel = 1; // Normal repair requires 1-level approval
        }

        var targetApproverDepartment = ticket.RepairType == RepairType.DriveAccessPermission
            ? ticket.DriveAccessDepartment
            : ticket.Department;

        // Set NextApprover fields for routing to the next approver in the workflow
        var approverUsers = await GetApproverUsersAsync();
        if (ticket.RepairType == RepairType.DriveAccessPermission)
        {
            // For Drive Access: First approver is from the requester's department
            // Next approver will be from DX department (SecondApprover)
            var firstApprover = approverUsers.FirstOrDefault(user => 
                !string.IsNullOrWhiteSpace(user.Department) && 
                user.Department.Trim().Equals(ticket.Department.Trim(), StringComparison.OrdinalIgnoreCase));
            
            if (firstApprover != null)
            {
                ticket.NextApproverUserId = firstApprover.Id;
                ticket.NextApproverName = !string.IsNullOrWhiteSpace(firstApprover.FullName)
                    ? firstApprover.FullName
                    : (firstApprover.UserName ?? firstApprover.Email ?? "Unknown");
                ticket.NextApproverDepartment = firstApprover.Department?.Trim() ?? string.Empty;
            }
        }
        else
        {
            // For normal repairs: Next approver is from the target department
            var nextApprover = approverUsers.FirstOrDefault(user => 
                !string.IsNullOrWhiteSpace(user.Department) && 
                user.Department.Trim().Equals(targetApproverDepartment.Trim(), StringComparison.OrdinalIgnoreCase));
            
            if (nextApprover != null)
            {
                ticket.NextApproverUserId = nextApprover.Id;
                ticket.NextApproverName = !string.IsNullOrWhiteSpace(nextApprover.FullName)
                    ? nextApprover.FullName
                    : (nextApprover.UserName ?? nextApprover.Email ?? "Unknown");
                ticket.NextApproverDepartment = nextApprover.Department?.Trim() ?? string.Empty;
            }
        }

        var selectedApprover = approverUsers.FirstOrDefault(user => user.Id == ticket.ApproverUserId);

        if (ticket.RepairType != RepairType.DriveAccessPermission)
        {
            ticket.DriveAccessDepartment = string.Empty;
            ticket.SecondApproverUserId = null;
            ticket.SecondApproverName = null;
        }

        if (ticket.RepairType == RepairType.DriveAccessPermission)
        {
            var driveDepartment = ticket.DriveAccessDepartment.Trim();
            var driveRemark = $"ฝ่ายที่ต้องการขอสิทธิ์ Drive: {driveDepartment}";
            if (string.IsNullOrWhiteSpace(ticket.IssueDescription))
            {
                ticket.IssueDescription = driveRemark;
            }
            else if (!ticket.IssueDescription.Contains(driveRemark, StringComparison.OrdinalIgnoreCase))
            {
                ticket.IssueDescription = $"{driveRemark}\n{ticket.IssueDescription}";
            }

            // The model binder may have added a required error before this auto-fill ran.
            ModelState.Remove(nameof(RepairTicket.IssueDescription));
        }

        // Don't set ApproverUserId and ApproverName during creation
        // These should only be set when someone actually approves the ticket
        // Clear any values that might have been set
        ticket.ApproverUserId = null;
        ticket.ApproverName = null;

        if (ticket.Status == TicketStatus.Rejected)
        {
            ModelState.AddModelError(nameof(RepairTicket.Status), "เจ้าของรายการไม่สามารถ Rejected รายการของตนเองได้");
        }

        if (ticket.IssueDescription.Length > 500)
        {
            ModelState.AddModelError(nameof(RepairTicket.IssueDescription), "รายละเอียดปัญหาต้องไม่เกิน 500 ตัวอักษร");
        }

        if (!ModelState.IsValid)
        {
            ViewData["CurrentUserId"] = currentUser?.Id;
            await PopulateApproverSelectionsAsync(ticket.ApproverDepartment, ticket.ApproverUserId);
            await PopulateDriveAccessDepartmentSelectionsAsync(ticket.DriveAccessDepartment);
            return View(ticket);
        }

        // Handle PDF attachment upload
        if (pdfAttachment is not null && pdfAttachment.Length > 0)
        {
            var allowedExtensions = new[] { ".pdf" };
            var fileExtension = Path.GetExtension(pdfAttachment.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("pdfAttachment", "Only PDF files are allowed.");
                ViewData["CurrentUserId"] = currentUser?.Id;
                await PopulateApproverSelectionsAsync(ticket.ApproverDepartment, ticket.ApproverUserId);
                await PopulateDriveAccessDepartmentSelectionsAsync(ticket.DriveAccessDepartment);
                return View(ticket);
            }

            var uploadsFolder = Path.Combine("wwwroot", "uploads", "pdfs");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await pdfAttachment.CopyToAsync(stream);
            }

            ticket.PdfAttachmentPath = $"/uploads/pdfs/{uniqueFileName}";
            ticket.PdfAttachmentFileName = pdfAttachment.FileName;
            ticket.PdfAttachmentFileSize = pdfAttachment.Length;
        }

        var actorName = GetActorName(currentUser);
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.CreatedByName = actorName;
        ticket.RequesterUserId = currentUser?.Id;
        ticket.DocumentNo = await GenerateDocumentNo(ticket.CreatedAt);
        _context.Add(ticket);

        if (isApproveCreator && !canSetStatus && ticket.Status == TicketStatus.Approved)
        {
            AddStatusHistory(ticket, null, TicketStatus.Open, currentUser, "Created");
            AddStatusHistory(ticket, TicketStatus.Open, TicketStatus.Approved, currentUser, "");
        }
        else
        {
            AddStatusHistory(ticket, null, ticket.Status, currentUser, "Created");
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var ticket = await _context.RepairTickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        ViewData["CurrentUserId"] = currentUser?.Id;
        ViewData["CurrentUserDisplayName"] = GetActorName(currentUser);
        ViewData["CurrentUserDepartment"] = currentUser?.Department?.Trim() ?? string.Empty;
        var canSeeInProgress = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ITSupport);
        var isPrivilegedUser = User.IsInRole(AppRoles.ITSupport)
            || User.IsInRole(AppRoles.Admin)
            || User.IsInRole(AppRoles.Approve);
        var isOwner = IsOwnerTicket(ticket, currentUser);

        if (!isPrivilegedUser)
        {
            if (!isOwner || (ticket.Status != TicketStatus.Rejected && ticket.Status != TicketStatus.Open && ticket.Status != TicketStatus.Complete))
            {
                return Forbid();
            }

            ViewData["RequesterEditMode"] = true;
        }

        if (!canSeeInProgress && ticket.Status == TicketStatus.InProgress)
        {
            return Forbid();
        }

        var timelineEntries = await _context.RepairTicketStatusHistories
            .AsNoTracking()
            .Where(history => history.RepairTicketId == ticket.Id)
            .OrderBy(history => history.ChangedAt)
            .ThenBy(history => history.Id)
            .Select(history => new { history.ToStatus, history.ChangedByUserId })
            .ToListAsync();

        var latestStatusEditorUserId = timelineEntries.LastOrDefault()?.ChangedByUserId;
        var canEditStatusByUser = string.IsNullOrWhiteSpace(latestStatusEditorUserId)
            || string.Equals(latestStatusEditorUserId, currentUser?.Id, StringComparison.Ordinal);
        var canMarkCompleteByAssignee = currentUser is not null
            && !string.IsNullOrWhiteSpace(ticket.AssignedItUserId)
            && string.Equals(ticket.AssignedItUserId, currentUser.Id, StringComparison.Ordinal)
            && ticket.Status == TicketStatus.InProgress;

        ViewData["CanEditStatusByUser"] = canEditStatusByUser;
        ViewData["CanMarkCompleteByAssignee"] = canMarkCompleteByAssignee;

        var timelineStatuses = CollapseConsecutiveStatuses(timelineEntries.Select(entry => entry.ToStatus));

        var statusFlow = timelineStatuses.Count > 0
            ? new List<TicketStatus>(timelineStatuses)
            : new List<TicketStatus> { ticket.Status };

        if (statusFlow[0] != TicketStatus.Open)
        {
            statusFlow.Insert(0, TicketStatus.Open);
        }

        if (statusFlow[^1] != ticket.Status)
        {
            statusFlow.Add(ticket.Status);
        }

        statusFlow = CollapseConsecutiveStatuses(statusFlow);

        ViewData["ShowInitialOpenStatus"] = statusFlow.Count > 1 && statusFlow[0] == TicketStatus.Open;
        var editableStatusFlow = GetEditableStatusFlow(statusFlow);
        ViewData["HasPriorStatusSteps"] = editableStatusFlow.Count > 1;
        ViewData["FlowPrimaryStatus"] = editableStatusFlow[0];
        ViewData["ApprovalStatuses"] = editableStatusFlow.Skip(1).ToList();

        await PopulateItSupportSelectionsAsync(ticket.AssignedItUserId);
        await PopulateApproverSelectionsAsync(ticket.ApproverDepartment, ticket.ApproverUserId);
        await PopulateDriveAccessDepartmentSelectionsAsync(ticket.DriveAccessDepartment);
        await PopulateThirdApproverSelectionsAsync(ticket.ThirdApproverUserId);
        
        // Pass NextApprover fields to the view
        ViewData["NextApproverUserId"] = ticket.NextApproverUserId;
        ViewData["NextApproverName"] = ticket.NextApproverName;
        ViewData["NextApproverDepartment"] = ticket.NextApproverDepartment;
        
        // Show DX approver dropdown for DriveAccessPermission tickets when:
        // 1. Ticket type is DriveAccessPermission
        // 2. User is not in requester edit mode
        // 3. Either: User is a DX approver OR ticket has been approved (ApproverUserId is set)
        var isDxApprover = currentUser != null 
            && !string.IsNullOrWhiteSpace(currentUser.Department) 
            && currentUser.Department.Trim().Equals("DX", StringComparison.OrdinalIgnoreCase)
            && User.IsInRole(AppRoles.Approve)
            && !User.IsInRole(AppRoles.ITSupport);
        
        var hasBeenApproved = !string.IsNullOrWhiteSpace(ticket.ApproverUserId);
        
        // Debug logging
        Console.WriteLine($"DEBUG EDIT: TicketId={ticket.Id}, RepairType={ticket.RepairType}, IsDxApprover={isDxApprover}, HasBeenApproved={hasBeenApproved}, RequesterEditMode={ViewData["RequesterEditMode"]}");
        
        // Show DX mode dropdown for DriveAccessPermission tickets when:
        // - User is DX approver, OR
        // - Ticket has been approved (so DX approver can select next IT approver)
        if (ticket.RepairType == RepairType.DriveAccessPermission 
            && !(ViewData["RequesterEditMode"] as bool? ?? false)
            && (isDxApprover || hasBeenApproved))
        {
            // Show only DX approvers for NextApprover selection
            var dxApprovers = await GetApproverUsersAsync();
            dxApprovers = dxApprovers
                .Where(user => !string.IsNullOrWhiteSpace(user.Department) 
                    && user.Department.Trim().Equals("DX", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // เพิ่ม user ปัจจุบันกลับเข้าไป ถ้ายังไม่มี
            if (!string.IsNullOrWhiteSpace(ticket.NextApproverUserId))
            {
                var currentNextApprover = await _userManager.FindByIdAsync(ticket.NextApproverUserId);

                if (currentNextApprover != null
                    && !dxApprovers.Any(x => x.Id == currentNextApprover.Id))
                {
                    dxApprovers.Add(currentNextApprover);
                }
            }

            // Filter to show only IT department users
            var itDepartmentUsers = dxApprovers
                .Where(user => !string.IsNullOrWhiteSpace(user.Department) 
                    && user.Department.Trim().Equals("IT", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // If no IT department users found, add current NextApprover if they exist
            if (!itDepartmentUsers.Any() && !string.IsNullOrWhiteSpace(ticket.NextApproverUserId))
            {
                var currentNextApprover = await _userManager.FindByIdAsync(ticket.NextApproverUserId);
                if (currentNextApprover != null)
                {
                    itDepartmentUsers.Add(currentNextApprover);
                }
            }
            
            ViewData["ApproverUsers"] = itDepartmentUsers;
            ViewData["NextApproverFilterMode"] = "DX";
            
            // Create independent list for direct DX approver select (users with both IT and Approve roles)
            var usersWithApproveRole = await _userManager.GetUsersInRoleAsync(AppRoles.Approve);
            var usersWithItRole = await _userManager.GetUsersInRoleAsync(AppRoles.ITSupport);
            
            // Find users who have BOTH IT and Approve roles
            var usersWithBothRoles = usersWithApproveRole
                .Where(approveUser => usersWithItRole.Any(itUser => itUser.Id == approveUser.Id))
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.UserName)
                .ToList();
            
            ViewData["DirectDxApproverUsers"] = usersWithBothRoles;
        }
        else
        {
            ViewData["NextApproverFilterMode"] = "All";
        }
        
        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,RequesterName,Department,DeviceName,IssueDescription,RepairType,Priority,Status,CreatedAt,ApproverDepartment,ApproverUserId,ApproverName,AssignedItUserId,AssignedItName,SecondApproverUserId,SecondApproverName,ThirdApproverUserId,ThirdApproverName,NextApproverUserId,NextApproverName,NextApproverDepartment,DriveAccessDepartment")] RepairTicket ticket, string? rejectRemark, List<TicketStatus>? approvalStatuses, string? thirdApproverUserId, IFormFile? pdfAttachment)
    {
        if (id != ticket.Id)
        {
            return NotFound();
        }

        var existingTicket = await _context.RepairTickets.FirstOrDefaultAsync(existing => existing.Id == id);
        if (existingTicket is null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var canSeeInProgress = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ITSupport);
        var isPrivilegedUser = User.IsInRole(AppRoles.ITSupport)
            || User.IsInRole(AppRoles.Admin)
            || User.IsInRole(AppRoles.Approve);
        var canEditStatus = User.IsInRole(AppRoles.Approve)
            || User.IsInRole(AppRoles.ITSupport)
            || User.IsInRole(AppRoles.Admin);
        var originalStatus = existingTicket.Status;
        var canMarkCompleteByAssignee = currentUser is not null
            && !string.IsNullOrWhiteSpace(existingTicket.AssignedItUserId)
            && string.Equals(existingTicket.AssignedItUserId, currentUser.Id, StringComparison.Ordinal)
            && originalStatus == TicketStatus.InProgress;
        var isOwner = IsOwnerTicket(existingTicket, currentUser);
        var persistedTimelineStatuses = await _context.RepairTicketStatusHistories
            .AsNoTracking()
            .Where(history => history.RepairTicketId == existingTicket.Id)
            .OrderBy(history => history.ChangedAt)
            .ThenBy(history => history.Id)
            .Select(history => history.ToStatus)
            .ToListAsync();
        persistedTimelineStatuses = CollapseConsecutiveStatuses(persistedTimelineStatuses);
        var latestStatusEditorUserId = await _context.RepairTicketStatusHistories
            .AsNoTracking()
            .Where(history => history.RepairTicketId == existingTicket.Id)
            .OrderByDescending(history => history.ChangedAt)
            .ThenByDescending(history => history.Id)
            .Select(history => history.ChangedByUserId)
            .FirstOrDefaultAsync();
        var canEditStatusByUser = string.IsNullOrWhiteSpace(latestStatusEditorUserId)
            || string.Equals(latestStatusEditorUserId, currentUser?.Id, StringComparison.Ordinal);
        var persistedStatusFlow = persistedTimelineStatuses.Count > 0
            ? new List<TicketStatus>(persistedTimelineStatuses)
            : new List<TicketStatus> { existingTicket.Status };

        if (persistedStatusFlow[0] != TicketStatus.Open)
        {
            persistedStatusFlow.Insert(0, TicketStatus.Open);
        }

        if (persistedStatusFlow[^1] != existingTicket.Status)
        {
            persistedStatusFlow.Add(existingTicket.Status);
        }

        var persistedEditableStatusFlow = GetEditableStatusFlow(persistedStatusFlow);
        var persistedStatusStepCount = persistedEditableStatusFlow.Count - 1;
        var submittedApprovalStatuses = (approvalStatuses ?? new List<TicketStatus>())
            .Where(status => status != TicketStatus.Closed && status != TicketStatus.Open)
            .ToList();

        var attemptedStatusChange = ticket.Status != originalStatus
            || submittedApprovalStatuses.Count > 0
            || !string.IsNullOrWhiteSpace(rejectRemark);

        var ownerCloseRequest = isOwner
            && originalStatus == TicketStatus.Complete
            && ticket.Status == TicketStatus.Closed
            && submittedApprovalStatuses.Count == 0
            && string.IsNullOrWhiteSpace(rejectRemark);

        var assigneeCompleteRequest = canMarkCompleteByAssignee
            && ticket.Status == TicketStatus.Complete
            && submittedApprovalStatuses.Count == 0
            && string.IsNullOrWhiteSpace(rejectRemark);

        // Prevent non-privileged users from changing status from Open
        if (!isPrivilegedUser && ticket.Status == TicketStatus.Open && originalStatus != TicketStatus.Open)
        {
            return Forbid();
        }

        if (!canEditStatus && attemptedStatusChange && !assigneeCompleteRequest && !ownerCloseRequest)
        {
            return Forbid();
        }

        if (!canEditStatus && !assigneeCompleteRequest && !ownerCloseRequest)
        {
            ticket.Status = originalStatus;
            submittedApprovalStatuses.Clear();
        }

        var statusFlow = CollapseConsecutiveStatuses(new[] { ticket.Status }.Concat(submittedApprovalStatuses));

        var effectiveStatus = statusFlow.Last();
        
        // Determine if this is a new approval (status changed from non-Approved to Approved)
        var isNewApproval = originalStatus != TicketStatus.Approved && effectiveStatus == TicketStatus.Approved;

        if (!isPrivilegedUser)
        {
            if (!isOwner || (existingTicket.Status != TicketStatus.Rejected && existingTicket.Status != TicketStatus.Open && existingTicket.Status != TicketStatus.Complete))
            {
                return Forbid();
            }

            existingTicket.Department = ticket.Department;
            existingTicket.DeviceName = ticket.DeviceName;
            existingTicket.IssueDescription = ticket.IssueDescription;
            existingTicket.RepairType = ticket.RepairType;
            
            // Allow owner to Close ticket when status is Complete, or Reopen when status is Rejected
            if (originalStatus == TicketStatus.Complete && effectiveStatus == TicketStatus.Closed)
            {
                existingTicket.Status = TicketStatus.Closed;
            }
            else if (originalStatus == TicketStatus.Rejected)
            {
                // When requester saves a rejected ticket, change status back to Open for re-submission
                existingTicket.Status = TicketStatus.Open;
            }
            else
            {
                existingTicket.Status = originalStatus;
            }
            
            existingTicket.UpdatedAt = DateTime.UtcNow;
            existingTicket.UpdatedByName = GetActorName(currentUser);

            if (!TryValidateModel(existingTicket))
            {
                ViewData["RequesterEditMode"] = true;
                await PopulateItSupportSelectionsAsync(existingTicket.AssignedItUserId);
                await PopulateApproverSelectionsAsync(existingTicket.ApproverDepartment, existingTicket.ApproverUserId);
                return View(existingTicket);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Auto-route to all IT staff with Approve role for Drive Access Permission when approved
        var currentUserDepartment = currentUser?.Department?.Trim() ?? string.Empty;
        var isDxDepartment = string.Equals(currentUserDepartment, "DX", StringComparison.OrdinalIgnoreCase);
        
        // // Set second approver for Drive Access Permission when current user is the NextApprover
        // // This runs when the NextApprover approves or edits an approved Drive Access ticket
        // if (existingTicket.RepairType == RepairType.DriveAccessPermission 
        //     && !string.IsNullOrWhiteSpace(existingTicket.NextApproverUserId)
        //     && currentUser != null
        //     && currentUser.Id == existingTicket.NextApproverUserId
        //     && canEditStatus
        //     && (effectiveStatus == TicketStatus.Approved || originalStatus == TicketStatus.Approved))
        // {
        //     // Set SecondApprover to the current user who is the NextApprover
        //     existingTicket.SecondApproverUserId = currentUser.Id;
        //     existingTicket.SecondApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
        //         ? currentUser.FullName
        //         : (currentUser.UserName ?? currentUser.Email ?? "Unknown");
        // }
        
        // Set SecondApprover for Drive Access Permission when DX approver approves
        // Only set when:
        // 1. Ticket is DriveAccessPermission
        // 2. Current user IS from DX department (second approver)
        // 3. SecondApproverUserId is not already set (first time setting)
        // 4. This is a new approval action (status changed from non-Approved to Approved)
        // Note: SecondApprover should be set by DX approver, NOT by the first approver
        if (existingTicket.RepairType == RepairType.DriveAccessPermission 
            && isDxDepartment 
            && canEditStatus
            && isNewApproval
            && string.IsNullOrWhiteSpace(existingTicket.SecondApproverUserId))
        {
            // Set SecondApprover to the DX approver who is performing the second approval
            // This ensures SecondApprover is only set once by the DX department approver
            existingTicket.SecondApproverUserId = currentUser.Id;
            existingTicket.SecondApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (currentUser.UserName ?? currentUser.Email ?? "Unknown");
        }
        
        // Update ApprovalLevel for Drive Access Permission based on who is approving
        if (existingTicket.RepairType == RepairType.DriveAccessPermission 
            && (effectiveStatus == TicketStatus.Approved || originalStatus == TicketStatus.Approved))
        {
            // First approver (non-DX) approved - set ApprovalLevel to 1
            if (!isDxDepartment && canEditStatus && !string.IsNullOrWhiteSpace(existingTicket.SecondApproverUserId))
            {
                existingTicket.ApprovalLevel = 1;
            }
            // Second approver (DX) approved - set ApprovalLevel to 2
            else if (isDxDepartment && canEditStatus && !string.IsNullOrWhiteSpace(existingTicket.SecondApproverUserId))
            {
                existingTicket.ApprovalLevel = 2;
            }
        }

        // Update NextApprover fields when status changes to Approved
        // Only auto-update on NEW approval (status changed from non-Approved to Approved) and if form didn't provide values
        if (effectiveStatus == TicketStatus.Approved && canEditStatus 
            && isNewApproval
            && string.IsNullOrWhiteSpace(ticket.NextApproverUserId))
        {
            var approverUsers = await GetApproverUsersAsync();
            
            if (existingTicket.RepairType == RepairType.DriveAccessPermission)
            {
                // For Drive Access: After first approval, next approver is DX department (SecondApprover)
                if (!isDxDepartment && !string.IsNullOrWhiteSpace(existingTicket.SecondApproverUserId))
                {
                    // First approval done, next is DX approver
                    var nextApprover = approverUsers.FirstOrDefault(user => 
                        !string.IsNullOrWhiteSpace(user.Department) && 
                        user.Department.Trim().Equals("DX", StringComparison.OrdinalIgnoreCase));
                    
                    if (nextApprover != null)
                    {
                        existingTicket.NextApproverUserId = nextApprover.Id;
                        existingTicket.NextApproverName = !string.IsNullOrWhiteSpace(nextApprover.FullName)
                            ? nextApprover.FullName
                            : (nextApprover.UserName ?? nextApprover.Email ?? "Unknown");
                        existingTicket.NextApproverDepartment = nextApprover.Department?.Trim() ?? string.Empty;
                    }
                }
                // Don't clear NextApprover when DX approves - let the manual form handling take care of it
                // The NextApprover should only be cleared when the workflow is truly complete
            }
            else
            {
                // For normal repairs: After approval, clear next approver (workflow complete)
                existingTicket.NextApproverUserId = null;
                existingTicket.NextApproverName = null;
                existingTicket.NextApproverDepartment = null;
            }
        }
        else if (effectiveStatus == TicketStatus.Open && originalStatus != TicketStatus.Open)
        {
            // When ticket is reopened (e.g., from Rejected), reset next approver
            var approverUsers = await GetApproverUsersAsync();
            var targetDepartment = existingTicket.RepairType == RepairType.DriveAccessPermission
                ? existingTicket.DriveAccessDepartment
                : existingTicket.Department;
            
            var nextApprover = approverUsers.FirstOrDefault(user => 
                !string.IsNullOrWhiteSpace(user.Department) && 
                user.Department.Trim().Equals(targetDepartment.Trim(), StringComparison.OrdinalIgnoreCase));
            
            if (nextApprover != null)
            {
                existingTicket.NextApproverUserId = nextApprover.Id;
                existingTicket.NextApproverName = !string.IsNullOrWhiteSpace(nextApprover.FullName)
                    ? nextApprover.FullName
                    : (nextApprover.UserName ?? nextApprover.Email ?? "Unknown");
                existingTicket.NextApproverDepartment = nextApprover.Department?.Trim() ?? string.Empty;
            }
        }

        if (!canSeeInProgress && (existingTicket.Status == TicketStatus.InProgress || statusFlow.Any(status => status == TicketStatus.InProgress)))
        {
            return Forbid();
        }

        existingTicket.RequesterName = ticket.RequesterName;
        existingTicket.Department = ticket.Department;
        existingTicket.DeviceName = ticket.DeviceName;
        existingTicket.IssueDescription = ticket.IssueDescription;
        // Don't update RepairType - preserve the original value from creation
        // existingTicket.RepairType = ticket.RepairType;
        existingTicket.Priority = ticket.Priority;
        existingTicket.Status = effectiveStatus;
        existingTicket.CreatedAt = ticket.CreatedAt;
        
        // Handle ApproverDepartment - ALWAYS set from RequesterUserId's department
        // This should NEVER be empty if RequesterUserId is set correctly
        var requesterUserId = existingTicket.RequesterUserId ?? string.Empty;
        
        if (!string.IsNullOrWhiteSpace(requesterUserId))
        {
            // Get requester user and their department
            var requesterUser = await _userManager.FindByIdAsync(requesterUserId);
            
            if (requesterUser != null && !string.IsNullOrWhiteSpace(requesterUser.Department))
            {
                // Set ApproverDepartment from requester's department
                existingTicket.ApproverDepartment = requesterUser.Department.Trim();
            }
            else
            {
                // Requester user not found or has no department - this is an error case
                // Use ticket's Department as last resort
                existingTicket.ApproverDepartment = existingTicket.Department?.Trim() ?? string.Empty;
            }
        }
        else
        {
            // No RequesterUserId - use ticket's Department
            existingTicket.ApproverDepartment = existingTicket.Department?.Trim() ?? string.Empty;
        }
        
        // Debug logging
        Console.WriteLine($"DEBUG ApproverDepartment: RequesterUserId='{requesterUserId}', Result='{existingTicket.ApproverDepartment}'");

        // Save approver fields when provided:
        // 1. If there's a new approval (status changed to Approved), set to current user
        // 2. If the form submitted approver fields (user clicked Approve button), save them
        //Flow การอนุมัติ

        if (currentUser != null && canEditStatus && isNewApproval)
        {
            // New approval: Set approver to the current user who performed the approval
            existingTicket.ApproverUserId = currentUser.Id;
            existingTicket.ApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (currentUser.UserName ?? currentUser.Email ?? "Unknown");
        }
        else if (!string.IsNullOrWhiteSpace(ticket.ApproverUserId) && canEditStatus)
        {
            // Approver was selected/form submitted (user clicked Approve button): save the values
            // This handles the case where user clicks Approve on an already-approved ticket
            existingTicket.ApproverUserId = ticket.ApproverUserId;
            
            // Look up the approver name from the user ID since we removed the hidden ApproverName field
            var approverUser = await _userManager.FindByIdAsync(ticket.ApproverUserId);
            existingTicket.ApproverName = !string.IsNullOrWhiteSpace(approverUser?.FullName)
                ? approverUser.FullName
                : (approverUser?.UserName ?? approverUser?.Email ?? "Unknown");
        }
        // Otherwise, preserve existing values (don't overwrite)
        
        existingTicket.AssignedItUserId = ticket.AssignedItUserId?.Trim() ?? string.Empty;
        // Save DriveAccessDepartment for DriveAccessPermission tickets
        if (existingTicket.RepairType == RepairType.DriveAccessPermission)
        {
            existingTicket.DriveAccessDepartment = ticket.DriveAccessDepartment?.Trim() ?? string.Empty;
        }
        else
        {
            existingTicket.DriveAccessDepartment = string.Empty;
        }

        //Level 2

        // Handle SecondApproverUserId (DX SM/DM for Drive Access)
        // IMPORTANT: SecondApproverUserId must ONLY be set when the first approver (ApproverUserId) has already approved.
        // When the first approver (ApproverUserId) approves (isNewApproval), SecondApproverUserId must NOT be saved.
        // When the SecondApprover saves (ApproverUserId already set from a previous approval), record them as the current user (CurrentUserId).
        if (existingTicket.RepairType == RepairType.DriveAccessPermission
            && canEditStatus
            && currentUser != null
            && !isNewApproval
            && !string.IsNullOrWhiteSpace(existingTicket.ApproverUserId)
            && (effectiveStatus == TicketStatus.Approved || originalStatus == TicketStatus.Approved))
        {
            // Save the current user who is the SecondApprover
            existingTicket.SecondApproverUserId = currentUser.Id;
            existingTicket.SecondApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (currentUser.UserName ?? currentUser.Email ?? "Unknown");
        }
        else if (!isNewApproval && !string.IsNullOrWhiteSpace(ticket.SecondApproverUserId) && !string.IsNullOrWhiteSpace(existingTicket.ApproverUserId))
        {
            existingTicket.SecondApproverUserId = ticket.SecondApproverUserId.Trim();
            existingTicket.SecondApproverName = ticket.SecondApproverName?.Trim() ?? string.Empty;
        }
        // If form didn't provide value, keep the existing value (set by auto-routing logic above)
        // If first approver (ApproverUserId) has not approved yet, do NOT save SecondApproverUserId from the form

        // Handle ThirdApproverUserId (SM/DM of DX for Drive Access)
        // Only set ThirdApprover when:
        // 1. Ticket is DriveAccessPermission AND status is Approved
        // 2. ThirdApproverUserId is not already set (first time setting)
        // 3. First approver (ApproverUserId) and SecondApprover (SecondApproverUserId) have already approved
        // 4. This is NOT a new approval by the first approver (isNewApproval = false)
        if (existingTicket.RepairType == RepairType.DriveAccessPermission 
            && currentUser != null 
            && canEditStatus
            && !isNewApproval
            && (effectiveStatus == TicketStatus.Approved || originalStatus == TicketStatus.Approved)
            && string.IsNullOrWhiteSpace(existingTicket.ThirdApproverUserId)
            && !string.IsNullOrWhiteSpace(existingTicket.ApproverUserId)
            && !string.IsNullOrWhiteSpace(existingTicket.SecondApproverUserId))
        {
            // Set ThirdApprover to the current user who is performing the third approval
            // This ensures ThirdApprover is only set once by the third approver
            existingTicket.ThirdApproverUserId = currentUser.Id;
            existingTicket.ThirdApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (currentUser.UserName ?? currentUser.Email ?? "Unknown");
        }
        // For non-DriveAccessPermission tickets or non-approved tickets, clear the fields
        else if (existingTicket.RepairType != RepairType.DriveAccessPermission || (effectiveStatus != TicketStatus.Approved && originalStatus != TicketStatus.Approved))
        {
            existingTicket.ThirdApproverUserId = null;
            existingTicket.ThirdApproverName = null;
        }

        // Handle NextApprover fields - allow manual override from form if provided
        // Only update if form provided values (auto-routing only happens on new approval without form values)
        var nextApproverUserIdFromForm = ticket.NextApproverUserId?.Trim();
        
        // Debug logging
        Console.WriteLine($"DEBUG: NextApproverUserId from form: '{nextApproverUserIdFromForm}'");
        Console.WriteLine($"DEBUG: Existing NextApproverUserId: '{existingTicket.NextApproverUserId}'");
        
        if (!string.IsNullOrWhiteSpace(nextApproverUserIdFromForm))
        {
            existingTicket.NextApproverUserId = nextApproverUserIdFromForm;
            
            // Look up the next approver from the user ID
            var nextApproverUser = await _userManager.FindByIdAsync(nextApproverUserIdFromForm);
            
            // Auto-populate NextApproverName from user database
            existingTicket.NextApproverName = !string.IsNullOrWhiteSpace(nextApproverUser?.FullName)
                ? nextApproverUser.FullName
                : (nextApproverUser?.UserName ?? nextApproverUser?.Email ?? "Unknown");
            
            // Auto-populate NextApproverDepartment from user's Department field
            existingTicket.NextApproverDepartment = nextApproverUser?.Department?.Trim() ?? string.Empty;
            
            Console.WriteLine($"DEBUG: Updated NextApproverName to: '{existingTicket.NextApproverName}'");
            Console.WriteLine($"DEBUG: Updated NextApproverDepartment to: '{existingTicket.NextApproverDepartment}'");
        }
        else if (!string.IsNullOrWhiteSpace(existingTicket.NextApproverUserId))
        {
            // If form didn't provide a value but there's an existing value, preserve it
            // This prevents clearing when the DX dropdown is not shown
            Console.WriteLine($"DEBUG: Preserving existing NextApproverUserId: '{existingTicket.NextApproverUserId}'");
        }

        // Check if current user is DX approver
        var isDxApprover = string.Equals(currentUserDepartment, "DX", StringComparison.OrdinalIgnoreCase)
            && User.IsInRole(AppRoles.Approve)
            && !User.IsInRole(AppRoles.ITSupport);

        if (effectiveStatus == TicketStatus.InProgress)
        {
            if (string.IsNullOrWhiteSpace(existingTicket.AssignedItUserId))
            {
                ModelState.AddModelError(nameof(RepairTicket.AssignedItUserId), "Please select IT assignee when status is InProgress.");
            }
            else
            {
                var assignedIt = await _userManager.FindByIdAsync(existingTicket.AssignedItUserId);
                if (assignedIt is not null)
                {
                    existingTicket.AssignedItName = !string.IsNullOrWhiteSpace(assignedIt.FullName)
                        ? assignedIt.FullName
                        : (assignedIt.UserName ?? assignedIt.Email ?? "Unknown");
                }
                else if (string.IsNullOrWhiteSpace(existingTicket.AssignedItName))
                {
                    existingTicket.AssignedItName = existingTicket.AssignedItUserId;
                }
            }
        }
        else if (effectiveStatus == TicketStatus.Approved && isDxApprover)
        {
            // DX approvers can assign IT staff when approving
            if (!string.IsNullOrWhiteSpace(existingTicket.AssignedItUserId))
            {
                var assignedIt = await _userManager.FindByIdAsync(existingTicket.AssignedItUserId);
                if (assignedIt is not null)
                {
                    existingTicket.AssignedItName = !string.IsNullOrWhiteSpace(assignedIt.FullName)
                        ? assignedIt.FullName
                        : (assignedIt.UserName ?? assignedIt.Email ?? "Unknown");
                }

                else if (string.IsNullOrWhiteSpace(existingTicket.AssignedItName))
                {
                    existingTicket.AssignedItName = existingTicket.AssignedItUserId;
                }
            }
        }
        else if (effectiveStatus == TicketStatus.Approved || effectiveStatus == TicketStatus.Complete || effectiveStatus == TicketStatus.Closed)
        {
            // Preserve existing assignment for Approved/Complete/Closed statuses
            // (IT Support/Admin may view/edit without changing status)
            if (!string.IsNullOrWhiteSpace(existingTicket.AssignedItUserId) && string.IsNullOrWhiteSpace(existingTicket.AssignedItName))
            {
                var assignedIt = await _userManager.FindByIdAsync(existingTicket.AssignedItUserId);
                if (assignedIt is not null)
                {
                    existingTicket.AssignedItName = !string.IsNullOrWhiteSpace(assignedIt.FullName)
                        ? assignedIt.FullName
                        : (assignedIt.UserName ?? assignedIt.Email ?? "Unknown");
                }
                else
                {
                    existingTicket.AssignedItName = existingTicket.AssignedItUserId;
                }
            }
        }
        else
        {
            existingTicket.AssignedItUserId = string.Empty;
            existingTicket.AssignedItName = string.Empty;
        }

        var trimmedRejectRemark = rejectRemark?.Trim() ?? string.Empty;

        // Allow owner to save a ticket that is already Rejected (will be changed to Open)
        // Only prevent owners from changing status TO Rejected from other statuses
        if (effectiveStatus == TicketStatus.Rejected && isOwner && originalStatus != TicketStatus.Rejected)
        {
            ModelState.AddModelError(nameof(RepairTicket.Status), "เจ้าของรายการไม่สามารถ Rejected รายการของตนเองได้");
        }

        if (effectiveStatus == TicketStatus.Rejected)
        {
            if (string.IsNullOrWhiteSpace(trimmedRejectRemark))
            {
                ModelState.AddModelError("rejectRemark", "Please provide a rejection remark when status is Rejected.");
            }
            else
            {
                var returnMessage = trimmedRejectRemark;
                var updatedIssueDescription = string.IsNullOrWhiteSpace(existingTicket.IssueDescription)
                    ? returnMessage
                    : $"{existingTicket.IssueDescription}\n\n{returnMessage}";

                if (updatedIssueDescription.Length > 500)
                {
                    ModelState.AddModelError(nameof(RepairTicket.IssueDescription), "Issue description is too long after adding rejection remark. Please shorten the remark.");
                }
                else
                {
                    existingTicket.IssueDescription = updatedIssueDescription;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            ViewData["RejectRemark"] = trimmedRejectRemark;
            ViewData["HasPriorStatusSteps"] = persistedStatusStepCount > 0;
            ViewData["ShowInitialOpenStatus"] = persistedStatusFlow.Count > 1 && persistedStatusFlow[0] == TicketStatus.Open;
            ViewData["CanEditStatusByUser"] = canEditStatusByUser;
            ViewData["CanMarkCompleteByAssignee"] = canMarkCompleteByAssignee;
            ViewData["ApprovalStatuses"] = statusFlow.Skip(1).ToList();
            ViewData["CurrentUserId"] = currentUser?.Id;
            ViewData["CurrentUserDisplayName"] = GetActorName(currentUser);
            await PopulateItSupportSelectionsAsync(existingTicket.AssignedItUserId);
            await PopulateApproverSelectionsAsync(existingTicket.ApproverDepartment, existingTicket.ApproverUserId);
            return View(existingTicket);
        }

        // Handle PDF attachment upload for Edit
        if (pdfAttachment is not null && pdfAttachment.Length > 0)
        {
            var allowedExtensions = new[] { ".pdf" };
            var fileExtension = Path.GetExtension(pdfAttachment.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("pdfAttachment", "Only PDF files are allowed.");
                ViewData["RejectRemark"] = trimmedRejectRemark;
                ViewData["HasPriorStatusSteps"] = persistedStatusStepCount > 0;
                ViewData["ShowInitialOpenStatus"] = persistedStatusFlow.Count > 1 && persistedStatusFlow[0] == TicketStatus.Open;
                ViewData["CanEditStatusByUser"] = canEditStatusByUser;
                ViewData["CanMarkCompleteByAssignee"] = canMarkCompleteByAssignee;
                ViewData["ApprovalStatuses"] = statusFlow.Skip(1).ToList();
                ViewData["CurrentUserId"] = currentUser?.Id;
                ViewData["CurrentUserDisplayName"] = GetActorName(currentUser);
                await PopulateItSupportSelectionsAsync(existingTicket.AssignedItUserId);
                await PopulateApproverSelectionsAsync(existingTicket.ApproverDepartment, existingTicket.ApproverUserId);
                return View(existingTicket);
            }

            var uploadsFolder = Path.Combine("wwwroot", "uploads", "pdfs");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await pdfAttachment.CopyToAsync(stream);
            }

            existingTicket.PdfAttachmentPath = $"/uploads/pdfs/{uniqueFileName}";
            existingTicket.PdfAttachmentFileName = pdfAttachment.FileName;
            existingTicket.PdfAttachmentFileSize = pdfAttachment.Length;
        }

        try
        {
            existingTicket.UpdatedAt = DateTime.UtcNow;
            existingTicket.UpdatedByName = GetActorName(currentUser);

            if (statusFlow.Count > 1)
            {
                var fromStatus = originalStatus;
                var hasFlowChange = false;

                foreach (var stepStatus in statusFlow)
                {
                    if (stepStatus == fromStatus)
                    {
                        continue;
                    }

                    hasFlowChange = true;
                    var stepRemark = stepStatus == TicketStatus.Rejected
                        ? trimmedRejectRemark
                        : null;
                    AddStatusHistory(existingTicket, fromStatus, stepStatus, currentUser, "", stepRemark);
                    fromStatus = stepStatus;
                }

                if (!hasFlowChange)
                {
                    AddStatusHistory(existingTicket, existingTicket.Status, existingTicket.Status, currentUser, "Saved", null);
                }
            }
            else if (originalStatus != existingTicket.Status)
            {
                var statusRemark = existingTicket.Status == TicketStatus.Rejected
                    ? trimmedRejectRemark
                    : null;
                AddStatusHistory(existingTicket, originalStatus, existingTicket.Status, currentUser, "", statusRemark);
            }
            else
            {
                // Record every save even when status is unchanged (e.g. multiple approval steps)
                AddStatusHistory(existingTicket, existingTicket.Status, existingTicket.Status, currentUser, "Saved", null);
            }

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await RepairTicketExists(existingTicket.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ITSupport)]
    public async Task<IActionResult> MyTasks(string? status, string? sort, string? dir)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var currentUserId = currentUser.Id;
        var currentUserFullName = currentUser.FullName?.Trim();
        var currentUserUserName = currentUser.UserName?.Trim();
        var currentUserEmail = currentUser.Email?.Trim();

        var query = _context.RepairTickets.AsNoTracking()
            .Where(ticket =>
                ticket.AssignedItUserId == currentUserId
                || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.AssignedItName == currentUserFullName)
                || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.AssignedItName == currentUserUserName)
                || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.AssignedItName == currentUserEmail));

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TicketStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(ticket => ticket.Status == parsedStatus);
            ViewData["CurrentStatus"] = parsedStatus.ToString();
        }

        var currentSort = string.IsNullOrWhiteSpace(sort) ? "created" : sort.Trim().ToLowerInvariant();
        var currentDir = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        query = (currentSort, currentDir) switch
        {
            ("requester", "asc") => query.OrderBy(ticket => ticket.RequesterName).ThenByDescending(ticket => ticket.CreatedAt),
            ("requester", "desc") => query.OrderByDescending(ticket => ticket.RequesterName).ThenByDescending(ticket => ticket.CreatedAt),
            ("created", "asc") => query.OrderBy(ticket => ticket.CreatedAt),
            _ => query.OrderByDescending(ticket => ticket.CreatedAt)
        };

        ViewData["CurrentSort"] = currentSort;
        ViewData["CurrentDir"] = currentDir;
        ViewData["CurrentUserId"] = currentUserId;
        ViewData["CurrentUserFullName"] = currentUserFullName;
        ViewData["CurrentUserUserName"] = currentUserUserName;
        ViewData["CurrentUserEmail"] = currentUserEmail;

        var tickets = await query.ToListAsync();

        var latestStatusUpdatedBy = new Dictionary<int, string>();
        if (tickets.Count > 0)
        {
            var ticketIds = tickets.Select(ticket => ticket.Id).ToList();
            var timeline = await _context.RepairTicketStatusHistories
                .AsNoTracking()
                .Where(history => ticketIds.Contains(history.RepairTicketId))
                .OrderByDescending(history => history.ChangedAt)
                .ThenByDescending(history => history.Id)
                .Select(history => new { history.RepairTicketId, history.ChangedByName })
                .ToListAsync();

            foreach (var item in timeline)
            {
                if (!latestStatusUpdatedBy.ContainsKey(item.RepairTicketId)
                    && !string.IsNullOrWhiteSpace(item.ChangedByName))
                {
                    latestStatusUpdatedBy[item.RepairTicketId] = item.ChangedByName.Trim();
                }
            }
        }

        ViewData["LatestStatusUpdatedBy"] = latestStatusUpdatedBy;
        ViewData["Title"] = "My Tasks";

        return View("Index", tickets);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var ticket = await _context.RepairTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(ticket => ticket.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ticket = await _context.RepairTickets.FindAsync(id);
        if (ticket is not null)
        {
            _context.RepairTickets.Remove(ticket);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private Task<bool> RepairTicketExists(int id)
    {
        return _context.RepairTickets.AnyAsync(ticket => ticket.Id == id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Approve)]
    public async Task<IActionResult> CloseTicket(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var ticket = await _context.RepairTickets.FindAsync(id);

        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.Status != TicketStatus.Complete)
        {
            return Forbid();
        }

        var previousStatus = ticket.Status;
        ticket.Status = TicketStatus.Closed;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.UpdatedByName = GetActorName(currentUser);
        AddStatusHistory(ticket, previousStatus, ticket.Status, currentUser, "Closed");
        _context.Update(ticket);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    

    private async Task<List<ApplicationUser>> GetApproverUsersAsync()
    {
        var approvers = await _userManager.GetUsersInRoleAsync(AppRoles.Approve);
        return approvers
            .OrderBy(user => user.Department)
            .ThenBy(user => user.FullName)
            .ThenBy(user => user.UserName)
            .ToList();
    }

    private async Task<List<ApplicationUser>> GetItSupportUsersAsync()
    {
        var itSupportUsers = await _userManager.GetUsersInRoleAsync(AppRoles.ITSupport);

        return itSupportUsers
            .OrderBy(user => user.Department)
            .ThenBy(user => user.FullName)
            .ThenBy(user => user.UserName)
            .ToList();
    }

    private async Task PopulateApproverSelectionsAsync(string? selectedDepartment, string? selectedApproverId)
    {
        var approverUsers = await GetApproverUsersAsync();
        var departments = (await _context.Users
                .AsNoTracking()
                .ToListAsync())
            .Select(user => user.Department?.Trim() ?? string.Empty)
            .Where(department => !string.IsNullOrWhiteSpace(department))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(department => department)
            .ToList();

        ViewData["ApproverDepartments"] = new SelectList(departments, selectedDepartment);
        ViewData["ApproverUsers"] = approverUsers;
        ViewData["SelectedApproverUserId"] = selectedApproverId;
    }

    private async Task PopulateDriveAccessDepartmentSelectionsAsync(string? selectedDepartment)
    {
        var departments = await _context.Users
            .AsNoTracking()
            .Select(user => user.Department)
            .Where(department => !string.IsNullOrWhiteSpace(department))
            .Select(department => department!.Trim())
            .Distinct()
            .OrderBy(department => department)
            .ToListAsync();

        ViewData["DriveAccessDepartments"] = new SelectList(departments, selectedDepartment);
    }

    private async Task PopulateThirdApproverSelectionsAsync(string? selectedThirdApproverId)
    {
        var approverUsers = await GetApproverUsersAsync();
        var dxApprovers = approverUsers
            .Where(user => !string.IsNullOrWhiteSpace(user.Department) 
                && user.Department.Trim().Equals("DX", StringComparison.OrdinalIgnoreCase))
            .ToList();
        ViewData["ThirdApproverUsers"] = dxApprovers;
        ViewData["SelectedThirdApproverUserId"] = selectedThirdApproverId;
    }

    private async Task PopulateItSupportSelectionsAsync(string? selectedItUserId)
    {
        var itSupportUsers = await GetItSupportUsersAsync();
        ViewData["ItSupportUsers"] = itSupportUsers;
        ViewData["SelectedItSupportUserId"] = selectedItUserId;
    }

    private static string GetActorName(ApplicationUser? user)
    {
        if (!string.IsNullOrWhiteSpace(user?.FullName))
        {
            return user.FullName;
        }

        return user?.UserName ?? user?.Email ?? "System";
    }

    private static bool IsOwnerTicket(RepairTicket ticket, ApplicationUser? user)
    {
        if (ticket.RequesterUserId == user?.Id)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(ticket.RequesterUserId))
        {
            return false;
        }

        var requesterName = ticket.RequesterName?.Trim();
        var createdByName = ticket.CreatedByName?.Trim();

        var candidates = new[]
        {
            user?.FullName,
            user?.UserName,
            user?.Email
        };

        return candidates.Any(candidate =>
            !string.IsNullOrWhiteSpace(candidate)
            && (
                (!string.IsNullOrWhiteSpace(requesterName)
                    && string.Equals(requesterName, candidate.Trim(), StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(createdByName)
                    && string.Equals(createdByName, candidate.Trim(), StringComparison.OrdinalIgnoreCase))
            ));
    }

    private async Task<string> GenerateDocumentNo(DateTime utcNow)
    {
        var thaiTime = utcNow.AddHours(7);
        var year = thaiTime.ToString("yy");
        var month = thaiTime.Month;
        var monthCode = month switch
        {
            10 => "A",
            11 => "B",
            12 => "C",
            _ => month.ToString()
        };

        var startOfMonthThai = new DateTime(thaiTime.Year, thaiTime.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var startOfMonthUtc = startOfMonthThai.AddHours(-7);
        var endOfMonthUtc = startOfMonthUtc.AddMonths(1);

        var requestsInMonth = await _context.RepairTickets
            .CountAsync(ticket => ticket.CreatedAt >= startOfMonthUtc && ticket.CreatedAt < endOfMonthUtc);
        var runningNumber = requestsInMonth + 1;

        return $"SR-{year}{monthCode}-{runningNumber:D3}";
    }

    private void AddStatusHistory(
        RepairTicket ticket,
        TicketStatus? fromStatus,
        TicketStatus toStatus,
        ApplicationUser? actor,
        string action,
        string? remark = null)
    {
        _context.AddRepairTicketStatusHistory(
            ticket,
            fromStatus,
            toStatus,
            action,
            remark,
            actor?.Id,
            GetActorName(actor));
    }

    private static List<TicketStatus> GetEditableStatusFlow(List<TicketStatus> statusFlow)
    {
        var editableFlow = statusFlow.Count > 0
            ? new List<TicketStatus>(statusFlow)
            : new List<TicketStatus> { TicketStatus.Open };

        if (editableFlow.Count > 1 && editableFlow[0] == TicketStatus.Open)
        {
            editableFlow.RemoveAt(0);
        }

        return editableFlow;
    }

    private static List<TicketStatus> CollapseConsecutiveStatuses(IEnumerable<TicketStatus> statuses)
    {
        var collapsed = new List<TicketStatus>();

        foreach (var status in statuses)
        {
            if (collapsed.Count == 0 || collapsed[^1] != status)
            {
                collapsed.Add(status);
            }
        }

        return collapsed;
    }
}
