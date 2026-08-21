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

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(string? status, string? sort, string? dir, string? keyword)
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
        
        // DEBUG: Log user info and ticket count
        Console.WriteLine($"DEBUG Index: User={User.Identity?.Name}, IsAdmin={isAdmin}, IsApprove={isApprove}, UserId={currentUserId}");
        var totalTicketsBeforeFilter = await _context.RepairTickets.CountAsync();
        Console.WriteLine($"DEBUG Index: Total tickets in DB before filter: {totalTicketsBeforeFilter}");

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
                    (!string.IsNullOrWhiteSpace(currentUserId) && ticket.RequesterUserId == currentUserId)
                    || (string.IsNullOrWhiteSpace(ticket.RequesterUserId)
                        && (
                            (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.RequesterName == currentUserFullName)
                            || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.RequesterName == currentUserUserName)
                            || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.RequesterName == currentUserEmail)
                            || (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.CreatedByName == currentUserFullName)
                            || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.CreatedByName == currentUserUserName)
                            || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.CreatedByName == currentUserEmail)
                        ))
                    // Also show tickets where the user is the assigned IT person, so that when an
                    // IT Support user changes the status and saves, the record appears immediately
                    // in the Index list without requiring a manual page refresh.
                    || (!string.IsNullOrWhiteSpace(currentUserId) && ticket.AssignedItUserId == currentUserId)
                    || (string.IsNullOrWhiteSpace(ticket.AssignedItUserId)
                        && (
                            (!string.IsNullOrWhiteSpace(currentUserFullName) && ticket.AssignedItName == currentUserFullName)
                            || (!string.IsNullOrWhiteSpace(currentUserUserName) && ticket.AssignedItName == currentUserUserName)
                            || (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.AssignedItName == currentUserEmail)
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

        var trimmedKeyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(ticket =>
                EF.Functions.Like(ticket.RequesterName, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.Department, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.DocumentNo ?? string.Empty, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.DeviceName, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.IssueDescription, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.NextApproverName ?? string.Empty, $"%{trimmedKeyword}%")
                || EF.Functions.Like(ticket.NextApproverDepartment ?? string.Empty, $"%{trimmedKeyword}%"));
        }
        ViewData["Keyword"] = trimmedKeyword;

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
    public async Task<IActionResult> AssignedToMe(string? status, string? sort, string? dir)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var currentUserId = currentUser?.Id;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        var query = _context.RepairTickets
            .AsNoTracking()
            .Where(ticket => ticket.AssignedItUserId == currentUserId)
            .AsQueryable();

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
            ("priority", "asc") => query.OrderBy(ticket => ticket.Priority).ThenByDescending(ticket => ticket.CreatedAt),
            ("priority", "desc") => query.OrderByDescending(ticket => ticket.Priority).ThenByDescending(ticket => ticket.CreatedAt),
            ("created", "asc") => query.OrderBy(ticket => ticket.CreatedAt),
            _ => query.OrderByDescending(ticket => ticket.CreatedAt)
        };

        ViewData["CurrentSort"] = currentSort;
        ViewData["CurrentDir"] = currentDir;

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

        return View("Index", tickets);
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
            DriveAccessDepartment = ticket.DriveAccessDepartment ?? string.Empty,
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
                    DriveAccessDepartment = ticket.DriveAccessDepartment ?? string.Empty,
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
            ApproverDepartment = currentUser?.Department ?? string.Empty,
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
        // ApproverDepartment is readonly in Create form - always set from current user's department
        ticket.ApproverDepartment = currentUser?.Department?.Trim() ?? ticket.ApproverDepartment?.Trim() ?? string.Empty;
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
            // DriveAccessDepartment is required for Drive Access Permission
            if (string.IsNullOrWhiteSpace(ticket.DriveAccessDepartment))
            {
                ModelState.AddModelError(nameof(RepairTicket.DriveAccessDepartment), "กรุณาเลือกฝ่ายที่ต้องการขอสิทธิ์ Drive");
            }

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

        // Determine if the current user is the assigned IT person (CurrentUserId == AssignedItName).
        // Uses the same comprehensive comparison pattern as Index/MyTasks actions:
        // matches by AssignedItUserId, or by AssignedItName against FullName/UserName/Email.
        var isCurrentUserAssignedIt = currentUser is not null
            && !string.IsNullOrWhiteSpace(ticket.AssignedItUserId)
            && (
                string.Equals(ticket.AssignedItUserId, currentUser.Id, StringComparison.Ordinal)
                || (!string.IsNullOrWhiteSpace(currentUser.FullName)
                    && string.Equals(ticket.AssignedItName, currentUser.FullName, StringComparison.Ordinal))
                || (!string.IsNullOrWhiteSpace(currentUser.UserName)
                    && string.Equals(ticket.AssignedItName, currentUser.UserName, StringComparison.Ordinal))
                || (!string.IsNullOrWhiteSpace(currentUser.Email)
                    && string.Equals(ticket.AssignedItName, currentUser.Email, StringComparison.Ordinal))
            );
        ViewData["IsCurrentUserAssignedIt"] = isCurrentUserAssignedIt;

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
        // When requester is editing, use NextApproverDepartment as the selected department
        // so the dropdown shows the correct department for the NextApprover
        var selectedApproverDepartment = (ViewData["RequesterEditMode"] as bool? ?? false)
            ? ticket.NextApproverDepartment
            : ticket.ApproverDepartment;
        await PopulateApproverSelectionsAsync(selectedApproverDepartment, ticket.ApproverUserId);
        await PopulateDriveAccessDepartmentSelectionsAsync(ticket.DriveAccessDepartment);
        await PopulateThirdApproverSelectionsAsync(ticket.ThirdApproverUserId);
        
        // Pass NextApprover fields to the view
        ViewData["NextApproverUserId"] = ticket.NextApproverUserId;
        ViewData["NextApproverName"] = ticket.NextApproverName;
        ViewData["NextApproverDepartment"] = ticket.NextApproverDepartment;
        
        // Show DX approver dropdown for DriveAccessPermission tickets when:
        // 1. Ticket type is DriveAccessPermission
        // 2. User is not in requester edit mode
        // 3. Current user is NextApproverUserId AND first approval is done (ApproverUserId != null) AND second approval not yet done (SecondApproverUserId == null)
        // OR Current user is SecondApproverUserId (after second approval, can edit their selection)
        var currentUserId = currentUser?.Id;
        var ticketNextApproverId = ticket.NextApproverUserId?.Trim();
        var ticketSecondApproverId = ticket.SecondApproverUserId?.Trim();
        var ticketApproverId = ticket.ApproverUserId?.Trim();
        
        var isNextApprover = !string.IsNullOrWhiteSpace(ticketNextApproverId) 
            && ticketNextApproverId == currentUserId;
        var isSecondApprover = !string.IsNullOrWhiteSpace(ticketSecondApproverId) 
            && ticketSecondApproverId == currentUserId;
        
        // Show DX dropdown when:
        // - Current user is NextApprover AND first approval is done AND second approval not yet done
        // - OR Current user is SecondApprover (after second approval)
        // - OR Current user is a third-party viewer at Step 2 (ApproverUserId != null && SecondApproverUserId != null && Step == 2 && CurrentUserId != both)
        var isThirdPartyViewerStep2 = !string.IsNullOrWhiteSpace(ticketApproverId)
            && !string.IsNullOrWhiteSpace(ticketSecondApproverId)
            && ticket.Step == 2
            && ticketApproverId != currentUserId
            && ticketSecondApproverId != currentUserId;
        var shouldShowDxDropdown = ticket.RepairType == RepairType.DriveAccessPermission 
            && !(ViewData["RequesterEditMode"] as bool? ?? false)
            && (
                (isNextApprover && !string.IsNullOrWhiteSpace(ticketApproverId) && string.IsNullOrWhiteSpace(ticketSecondApproverId))
                || isSecondApprover
                || isThirdPartyViewerStep2
            );
        
        // Debug logging
        Console.WriteLine($"DEBUG EDIT: TicketId={ticket.Id}, RepairType={ticket.RepairType}");
        Console.WriteLine($"DEBUG EDIT: IsNextApprover={isNextApprover}, IsSecondApprover={isSecondApprover}");
        Console.WriteLine($"DEBUG EDIT: CurrentUserId='{currentUserId}'");
        Console.WriteLine($"DEBUG EDIT: NextApproverUserId='{ticketNextApproverId}'");
        Console.WriteLine($"DEBUG EDIT: SecondApproverUserId='{ticketSecondApproverId}'");
        Console.WriteLine($"DEBUG EDIT: ApproverUserId='{ticketApproverId}'");
        Console.WriteLine($"DEBUG EDIT: RequesterEditMode={ViewData["RequesterEditMode"]}");
        Console.WriteLine($"DEBUG EDIT: ShouldShowDX={shouldShowDxDropdown}");
        
        // Show DX mode dropdown when conditions are met
        if (shouldShowDxDropdown)
        {
            // Show only DX department approvers for NextApprover selection
            var dxDepartmentApprovers = await GetApproverUsersAsync();
            dxDepartmentApprovers = dxDepartmentApprovers
                .Where(user => !string.IsNullOrWhiteSpace(user.Department) 
                    && user.Department.Trim().Equals("DX", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // เพิ่ม user ปัจจุบันกลับเข้าไป ถ้ายังไม่มี
            if (!string.IsNullOrWhiteSpace(ticket.NextApproverUserId))
            {
                var currentNextApprover = await _userManager.FindByIdAsync(ticket.NextApproverUserId);

                if (currentNextApprover != null
                    && !dxDepartmentApprovers.Any(x => x.Id == currentNextApprover.Id))
                {
                    dxDepartmentApprovers.Add(currentNextApprover);
                }
            }

            // If no DX department users found, add current NextApprover if they exist
            if (!dxDepartmentApprovers.Any() && !string.IsNullOrWhiteSpace(ticket.NextApproverUserId))
            {
                var currentNextApprover = await _userManager.FindByIdAsync(ticket.NextApproverUserId);
                if (currentNextApprover != null)
                {
                    dxDepartmentApprovers.Add(currentNextApprover);
                }
            }
            
            ViewData["ApproverUsers"] = dxDepartmentApprovers;
            ViewData["NextApproverFilterMode"] = "DX";
            
            // Create independent list for direct DX approver select (users with Approve role in DX department)
            var usersWithApproveRole = await _userManager.GetUsersInRoleAsync(AppRoles.Approve);
            
            // Find users who have Approve role AND are in the DX department
            var usersWithBothRoles = usersWithApproveRole
                .Where(user => !string.IsNullOrWhiteSpace(user.Department)
                    && user.Department.Trim().Equals("DX", StringComparison.OrdinalIgnoreCase))
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.UserName)
                .ToList();
            
            // Ensure the current user is always in the list so the dropdown can default to them
            // Only add if the current user is in the DX department
            if (currentUser != null
                && !string.IsNullOrWhiteSpace(currentUser.Department)
                && currentUser.Department.Trim().Equals("DX", StringComparison.OrdinalIgnoreCase)
                && !usersWithBothRoles.Any(user => user.Id == currentUser.Id))
            {
                usersWithBothRoles.Add(currentUser);
                usersWithBothRoles = usersWithBothRoles
                    .OrderBy(user => user.FullName)
                    .ThenBy(user => user.UserName)
                    .ToList();
            }
            
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

    public async Task<IActionResult> Edit(int id, [Bind("Id,RequesterName,Department,DeviceName,IssueDescription,RepairType,DriveAccessDepartment,Priority,Status,CreatedAt,ApproverDepartment,ApproverUserId,ApproverName,AssignedItUserId,NextApproverUserId,NextApproverName,NextApproverDepartment,Step,ThirdApproverUserId,DxFinalApproverUserId")] RepairTicket ticket, string? rejectRemark, List<TicketStatus>? approvalStatuses, string? thirdApproverUserId, IFormFile? pdfAttachment)

    {
        if (id != ticket.Id)
        {
            return NotFound();
        }

        RepairTicket? existingTicket = await _context.RepairTickets.FirstOrDefaultAsync(existing => existing.Id == id);
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
        
        // Get current user department early (needed for DX checks)
        var currentUserDepartment = currentUser?.Department?.Trim() ?? string.Empty;
        var isDxDepartment = string.Equals(currentUserDepartment, "DX", StringComparison.OrdinalIgnoreCase);
        
        // Server-side validation: Prevent user from approving and routing to themselves
        // Skip validation when current user is a DIFFERENT approver at Step 1 (ApproverUserId already set by another user)
        var isDifferentApproverStep1 = !string.IsNullOrWhiteSpace(existingTicket.ApproverUserId)
            && string.IsNullOrWhiteSpace(existingTicket.SecondApproverUserId)
            && existingTicket.Step == 1
            && currentUser?.Id != existingTicket.ApproverUserId;

        if (currentUser != null && isPrivilegedUser && canEditStatus && !isDifferentApproverStep1)
        {
            var nextApproverUserId = ticket.NextApproverUserId?.Trim();
            var approverUserId = ticket.ApproverUserId?.Trim();
            
            // Check if Next Approver User is the same as current user
            if (!string.IsNullOrWhiteSpace(nextApproverUserId) && existingTicket.DxFinalApproverUserId != currentUser.Id &&
                string.Equals(nextApproverUserId, currentUser.Id, StringComparison.Ordinal))
            {
                ModelState.AddModelError("NextApproverUserId", 
                    "⚠️ ไม่สามารถบันทึกรายการได้ กรุณาเปลี่ยนให้เป็นฝ่ายและผู้อนุมัติที่ต่างจากของคุณ");
            }
            
            // Check if Approver User is the same as current user
            if (!string.IsNullOrWhiteSpace(approverUserId) && 
                string.Equals(approverUserId, currentUser.Id, StringComparison.Ordinal))
            {
                ModelState.AddModelError("ApproverUserId", 
                    "⚠️ ไม่สามารถบันทึกรายการได้ กรุณาเปลี่ยนผู้อนุมัติให้เป็นคนอื่น");
            }
        }
        var canMarkCompleteByAssignee = currentUser is not null
            && !string.IsNullOrWhiteSpace(existingTicket.AssignedItUserId)
            && string.Equals(existingTicket.AssignedItUserId, currentUser.Id, StringComparison.Ordinal)
            && originalStatus == TicketStatus.InProgress;
        var isOwner = IsOwnerTicket(existingTicket, currentUser);
        
        // Handle DxFinalApproverUserId from DX mode dropdown BEFORE clearing
        // In DX mode, the "เลือกผู้อนุมัติ DX (ขั้นตอนสุดท้าย)" dropdown saves to DxFinalApproverUserId
        var dxFinalApproverUserIdFromForm = ticket.DxFinalApproverUserId?.Trim();
        
        if (!string.IsNullOrWhiteSpace(dxFinalApproverUserIdFromForm))
        {
            existingTicket.DxFinalApproverUserId = dxFinalApproverUserIdFromForm;
            
            // Look up the DX final approver from the user ID
            var dxFinalApproverUser = await _userManager.FindByIdAsync(dxFinalApproverUserIdFromForm);
            
            // Auto-populate DxFinalApproverName from user database
            existingTicket.DxFinalApproverName = !string.IsNullOrWhiteSpace(dxFinalApproverUser?.FullName)
                ? dxFinalApproverUser.FullName
                : (dxFinalApproverUser?.UserName ?? dxFinalApproverUser?.Email ?? "Unknown");
            
            Console.WriteLine($"DEBUG: Updated DxFinalApproverUserId to: '{existingTicket.DxFinalApproverUserId}'");
            Console.WriteLine($"DEBUG: Updated DxFinalApproverName to: '{existingTicket.DxFinalApproverName}'");
            
            // When DX approver selects a user from "เลือกผู้อนุมัติ DX (ขั้นตอนสุดท้าย)" dropdown,
            // also update NextApprover fields with this value
            if (existingTicket.RepairType == RepairType.DriveAccessPermission
                && isDxDepartment
                && canEditStatus
                && !string.IsNullOrWhiteSpace(existingTicket.ApproverUserId))
            {
                existingTicket.NextApproverUserId = dxFinalApproverUserIdFromForm;
                existingTicket.NextApproverName = !string.IsNullOrWhiteSpace(dxFinalApproverUser?.FullName)
                    ? dxFinalApproverUser.FullName
                    : (dxFinalApproverUser?.UserName ?? dxFinalApproverUser?.Email ?? "Unknown");
                existingTicket.NextApproverDepartment = dxFinalApproverUser?.Department?.Trim() ?? string.Empty;
                
                Console.WriteLine($"DEBUG: Updated NextApprover from DX dropdown to: '{existingTicket.NextApproverUserId}' - '{existingTicket.NextApproverName}'");
            }
        }
        
        // Clear ThirdApprover fields from the bound model to prevent unwanted updates
        // ThirdApprover should ONLY be set by explicit form submission or controller logic
        ticket.ThirdApproverUserId = null;
        ticket.ThirdApproverName = null;
        // Clear DxFinalApprover fields from the bound model to prevent unwanted updates
        // DxFinalApprover should ONLY be set by explicit form submission or controller logic
        ticket.DxFinalApproverUserId = null;
        ticket.DxFinalApproverName = null;
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
            // Don't update RepairType - preserve the original value from creation
            // existingTicket.RepairType = ticket.RepairType;
            existingTicket.DriveAccessDepartment = ticket.DriveAccessDepartment?.Trim() ?? string.Empty;
            
            // Allow requester to edit NextApprover fields when status is Open
            if (originalStatus == TicketStatus.Open)
            {
                existingTicket.NextApproverUserId = ticket.NextApproverUserId?.Trim();
                existingTicket.NextApproverDepartment = ticket.NextApproverDepartment?.Trim();
                
                // Auto-populate NextApproverName from user database
                if (!string.IsNullOrWhiteSpace(existingTicket.NextApproverUserId))
                {
                    var nextApproverUser = await _userManager.FindByIdAsync(existingTicket.NextApproverUserId);
                    existingTicket.NextApproverName = !string.IsNullOrWhiteSpace(nextApproverUser?.FullName)
                        ? nextApproverUser.FullName
                        : (nextApproverUser?.UserName ?? nextApproverUser?.Email ?? "Unknown");
                }
                else
                {
                    existingTicket.NextApproverName = null;
                }
            }
            
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
        // IMPORTANT: Check if form provided NextApprover values FIRST, before auto-routing
        var nextApproverFromForm = ticket.NextApproverUserId?.Trim();
        
        // Only apply auto-routing if form didn't provide NextApprover values
        if (effectiveStatus == TicketStatus.Approved && canEditStatus 
            && isNewApproval
            && string.IsNullOrWhiteSpace(nextApproverFromForm))
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
                        Console.WriteLine($"DEBUG: Auto-routed NextApprover to DX: '{nextApprover.Id}'");
                    }
                }
                // Don't clear NextApprover when DX approves - let the manual form handling take care of it
            }
            else
            {
                // For normal repairs: After approval, clear next approver (workflow complete)
                existingTicket.NextApproverUserId = null;
                existingTicket.NextApproverName = null;
                existingTicket.NextApproverDepartment = null;
                Console.WriteLine($"DEBUG: Cleared NextApprover for normal repair");
            }
        }
        else if (effectiveStatus == TicketStatus.Approved && isNewApproval && !string.IsNullOrWhiteSpace(nextApproverFromForm))
        {
            // Form provided NextApprover value - preserve it (don't overwrite with auto-routing)
            Console.WriteLine($"DEBUG: Form provided NextApprover, preserving: '{nextApproverFromForm}'");
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
        // Preserve existing DriveAccessDepartment if form value is empty
        if (existingTicket.RepairType == RepairType.DriveAccessPermission)
        {
            var driveAccessDeptFromForm = ticket.DriveAccessDepartment?.Trim();
            if (!string.IsNullOrWhiteSpace(driveAccessDeptFromForm))
            {
                existingTicket.DriveAccessDepartment = driveAccessDeptFromForm;
            }
        }
        else
        {
            existingTicket.DriveAccessDepartment = string.Empty;
        }

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
        var originalStep = existingTicket.Step;



                if (originalStep != 1)
                    Console.WriteLine($"FAIL: originalStep = {originalStep}");

                if (string.IsNullOrWhiteSpace(existingTicket.ApproverUserId))
                    Console.WriteLine("FAIL: ApproverUserId");

                if (existingTicket.SecondApproverUserId != null)
                    Console.WriteLine($"FAIL: SecondApproverUserId = {existingTicket.SecondApproverUserId}");

                if (string.IsNullOrWhiteSpace(existingTicket.NextApproverUserId))
                    Console.WriteLine("FAIL: NextApproverUserId");

                if (currentUser?.Id == existingTicket.ApproverUserId)
                    Console.WriteLine("FAIL: currentUser == ApproverUserId");

                if (!string.Equals(
                        existingTicket.NextApproverUserId?.Trim(),
                        currentUser?.Id?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("FAIL: NextApproverUserId != currentUser.Id");
                }
    
         //Level 1

        if (currentUser != null && canEditStatus && isNewApproval)
        {
            // New approval: Set approver to the current user who performed the approval
            existingTicket.ApproverUserId = currentUser.Id;
            existingTicket.Step = 1;
            existingTicket.ApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (currentUser.UserName ?? currentUser.Email ?? "Unknown");



            // Form provided NextApproverUserId - use it
            existingTicket.NextApproverUserId = ticket.NextApproverUserId;
            existingTicket.NextApproverDepartment = ticket.NextApproverDepartment;

            // Look up the next approver from the user ID
            var nextApproverUser = await _userManager.FindByIdAsync(ticket.NextApproverUserId);

            // Auto-populate NextApproverName from user database
            existingTicket.NextApproverName = !string.IsNullOrWhiteSpace(nextApproverUser?.FullName)
                ? nextApproverUser.FullName
                : (nextApproverUser?.UserName ?? nextApproverUser?.Email ?? "Unknown");


        }
        else if (!string.IsNullOrWhiteSpace(ticket.ApproverUserId) && canEditStatus)
        {
            // Approver was selected/form submitted (user clicked Approve button): save the values
            // This handles the case where user clicks Approve on an already-approved ticket
            existingTicket.ApproverUserId = ticket.ApproverUserId;
            existingTicket.Step = 1;

            // Look up the approver name from the user ID since we removed the hidden ApproverName field
            var approverUser = await _userManager.FindByIdAsync(ticket.ApproverUserId);
            existingTicket.ApproverName = !string.IsNullOrWhiteSpace(approverUser?.FullName)
                ? approverUser.FullName
                : (approverUser?.UserName ?? approverUser?.Email ?? "Unknown");
        }
        else
        {
            // Preserve the user's Step value from the form when not in an approval flow
            existingTicket.Step = ticket.Step;
        }
        // Otherwise, preserve existing values (don't overwrite)
        
        existingTicket.AssignedItUserId = ticket.AssignedItUserId?.Trim() ?? string.Empty;
        // Save DriveAccessDepartment for DriveAccessPermission tickets
        if (existingTicket.RepairType == RepairType.DriveAccessPermission)
        {
            // Preserve existing value if form did not provide a value
            var driveAccessDeptFromForm = ticket.DriveAccessDepartment?.Trim();
            if (!string.IsNullOrWhiteSpace(driveAccessDeptFromForm))
            {
                existingTicket.DriveAccessDepartment = driveAccessDeptFromForm;
            }
        }
        else
        {
            existingTicket.DriveAccessDepartment = string.Empty;
        }

        //Level 2

        // Handle SecondApproverUserId (DX SM/DM for Drive Access)
        // Save SecondApproverUserId when:
        // 1. Current user is not null
        // 2. Current user is the NextApproverUserId (DX approver)
        // 3. Ticket is DriveAccessPermission type
        // 4. User has edit permission
        // 5. First approval is done (ApproverUserId is set)
        // 6. ApproverUserId is different from NextApproverUserId (to avoid setting during first approval)
        // This ensures SecondApproverUserId is saved when DX approver clicks Approve and Save
        
        if (existingTicket.RepairType == RepairType.DriveAccessPermission
            && canEditStatus
            && currentUser != null
            && originalStep == 1
            && !string.IsNullOrWhiteSpace(existingTicket.ApproverUserId)
            //&& existingTicket.SecondApproverUserId is null
            && !string.IsNullOrWhiteSpace(existingTicket.NextApproverUserId)
            //&& existingTicket.ApproverUserId != existingTicket.NextApproverUserId
            && currentUser.Id != existingTicket.ApproverUserId
            && string.Equals(existingTicket.NextApproverUserId, currentUser.Id, StringComparison.Ordinal))

        {
            // Set SecondApproverUserId to current user (DX approver) when they save
            // This runs when ApproverUserId is set and current user is the NextApproverUserId
            
            existingTicket.SecondApproverUserId = currentUser.Id;
            existingTicket.Step = 2;
            existingTicket.SecondApproverDepartment = currentUser.Department?.Trim() ?? string.Empty;
            existingTicket.SecondApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (currentUser.UserName ?? currentUser.Email ?? "Unknown");
            existingTicket.SecondApproverDepartment = currentUser.Department?.Trim() ?? string.Empty;

            if (existingTicket.RepairType == RepairType.DriveAccessPermission)
            {
                // Preserve existing value if form did not provide a value
                var driveAccessDeptFromForm = ticket.DriveAccessDepartment?.Trim();
                if (!string.IsNullOrWhiteSpace(driveAccessDeptFromForm))
                {
                    existingTicket.DriveAccessDepartment = driveAccessDeptFromForm;
                }
            }
            else
            {
                existingTicket.DriveAccessDepartment = string.Empty;
            }

            // Set DxFinalApprover fields from the "เลือกผู้อนุมัติ DX (ขั้นตอนสุดท้าย)" dropdown selection
            // Use the value from the form (dxFinalApproverUserIdFromForm) instead of current user
            if (!string.IsNullOrWhiteSpace(dxFinalApproverUserIdFromForm))
            {
                existingTicket.DxFinalApproverUserId = dxFinalApproverUserIdFromForm;
                
                // Look up the selected DX final approver from the user ID
                var dxFinalApproverUser = await _userManager.FindByIdAsync(dxFinalApproverUserIdFromForm);
                existingTicket.DxFinalApproverName = !string.IsNullOrWhiteSpace(dxFinalApproverUser?.FullName)
                    ? dxFinalApproverUser.FullName
                    : (dxFinalApproverUser?.UserName ?? dxFinalApproverUser?.Email ?? "Unknown");
                existingTicket.DxFinalApproverDepartment = dxFinalApproverUser?.Department?.Trim() ?? string.Empty;

                Console.WriteLine($"DEBUG: Set DxFinalApprover from dropdown - UserId: '{existingTicket.DxFinalApproverUserId}', Name: '{existingTicket.DxFinalApproverName}'");
            }
             else
            {
                // Fallback to current user if no dropdown selection was provided
                existingTicket.DxFinalApproverUserId = currentUser.Id;
                existingTicket.DxFinalApproverDepartment = currentUser.Department?.Trim() ?? string.Empty;
                existingTicket.DxFinalApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                    ? currentUser.FullName
                    : (currentUser.UserName ?? currentUser.Email ?? "Unknown");
                
                Console.WriteLine($"DEBUG: No dropdown selection, fallback DxFinalApprover to current user: '{existingTicket.DxFinalApproverUserId}'");
            }

            // Set NextApprover fields from DxFinalApprover values
            // (ฝ่ายที่อนุมัติลำดับถัดไป และ ผู้อนุมัติลำดับถัดไป)
            existingTicket.NextApproverUserId = existingTicket.DxFinalApproverUserId;
            existingTicket.NextApproverName = existingTicket.DxFinalApproverName;
            existingTicket.NextApproverDepartment = existingTicket.DxFinalApproverDepartment;
            
            Console.WriteLine($"DEBUG: Updated NextApprover from DxFinalApprover - UserId: '{existingTicket.NextApproverUserId}', Name: '{existingTicket.NextApproverName}', Dept: '{existingTicket.NextApproverDepartment}'");
            Console.WriteLine($"DEBUG: Set/Updated SecondApproverUserId to current user: '{existingTicket.SecondApproverUserId}'");
        }
        else if (existingTicket.RepairType != RepairType.DriveAccessPermission
            && canEditStatus
            && currentUser != null
            && originalStep == 1
            && !string.IsNullOrWhiteSpace(existingTicket.ApproverUserId)
            && !string.IsNullOrWhiteSpace(existingTicket.NextApproverUserId)
            && currentUser.Id != existingTicket.ApproverUserId
            && string.Equals(existingTicket.NextApproverUserId, currentUser.Id, StringComparison.Ordinal))
        {
            // Non-DriveAccess: When the NextApprover (DX user) approves, record DxFinalApprover
            // Use the form-provided DxFinalApprover if available, otherwise default to current user
            if (!string.IsNullOrWhiteSpace(dxFinalApproverUserIdFromForm))
            {
                existingTicket.DxFinalApproverUserId = dxFinalApproverUserIdFromForm;
                var dxFinalApproverUser = await _userManager.FindByIdAsync(dxFinalApproverUserIdFromForm);
                existingTicket.DxFinalApproverName = !string.IsNullOrWhiteSpace(dxFinalApproverUser?.FullName)
                    ? dxFinalApproverUser.FullName
                    : (dxFinalApproverUser?.UserName ?? dxFinalApproverUser?.Email ?? "Unknown");
                existingTicket.DxFinalApproverDepartment = dxFinalApproverUser?.Department?.Trim() ?? string.Empty;
            }
            else
            {
                existingTicket.DxFinalApproverUserId = currentUser.Id;
                existingTicket.DxFinalApproverDepartment = currentUser.Department?.Trim() ?? string.Empty;
                existingTicket.DxFinalApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                    ? currentUser.FullName
                    : (currentUser.UserName ?? currentUser.Email ?? "Unknown");
            }
            existingTicket.Step = 2;
            Console.WriteLine($"DEBUG: Non-DriveAccess NextApprover approved - DxFinalApprover UserId: '{existingTicket.DxFinalApproverUserId}', Name: '{existingTicket.DxFinalApproverName}', Dept: '{existingTicket.DxFinalApproverDepartment}'");
        }

        // If form didn't provide value, keep the existing value (set by auto-routing logic above)
        // If first approver (ApproverUserId) has not approved yet, do NOT save SecondApproverUserId from the form

        // Note: ThirdApproverUserId is NOT handled here
        // It should only be set when the workflow reaches the third approval stage
        // The dropdown is disabled and has no name attribute, so it won't be submitted
        // If ThirdApprover needs to be set in the future, it should be done explicitly
        // when the third approver performs their approval action

        // Handle NextApprover fields - allow manual override from form if provided
        // Only update if form provided values (auto-routing only happens on new approval without form values)
        var nextApproverUserIdFromForm = existingTicket.DxFinalApproverUserId;  //existingTicket.DxFinalApproverName
        var nextApproverNameFromForm = existingTicket.DxFinalApproverName;
        var nextApproverDepartmentFromForm = "DX";//ticket.NextApproverDepartment?.Trim();
        
        // Debug logging
        Console.WriteLine($"DEBUG: NextApproverUserId from form: '{nextApproverUserIdFromForm}'");
        Console.WriteLine($"DEBUG: NextApproverName from form: '{nextApproverNameFromForm}'");
        Console.WriteLine($"DEBUG: NextApproverDepartment from form: '{nextApproverDepartmentFromForm}'");
        Console.WriteLine($"DEBUG: Existing NextApproverUserId: '{existingTicket.NextApproverUserId}'");
        Console.WriteLine($"DEBUG: Existing NextApproverName: '{existingTicket.NextApproverName}'");
        Console.WriteLine($"DEBUG: Existing NextApproverDepartment: '{existingTicket.NextApproverDepartment}'");
        Console.WriteLine($"DEBUG: ApproverUserId: '{existingTicket.ApproverUserId}'");
        Console.WriteLine($"DEBUG: isNewApproval: {isNewApproval}");


        //Level 3
        
        if (!string.IsNullOrWhiteSpace(nextApproverUserIdFromForm) && canEditStatus && originalStep == 2 
            && currentUser.Id != existingTicket.ApproverUserId && currentUser.Id != existingTicket.SecondApproverUserId
            && string.Equals(existingTicket.DxFinalApproverUserId, currentUser.Id, StringComparison.Ordinal))
        {
            // Form provided NextApproverUserId - use it
            existingTicket.NextApproverUserId = nextApproverUserIdFromForm;
            
            // Look up the next approver from the user ID
            var nextApproverUser = await _userManager.FindByIdAsync(nextApproverUserIdFromForm);
            
            // Auto-populate NextApproverName from user database
            existingTicket.NextApproverName = !string.IsNullOrWhiteSpace(nextApproverUser?.FullName)
                ? nextApproverUser.FullName
                : (nextApproverUser?.UserName ?? nextApproverUser?.Email ?? "Unknown");


            existingTicket.ThirdApproverUserId = currentUser.Id;
            existingTicket.Step = 3;
            existingTicket.ThirdApproverDepartment = currentUser.Department?.Trim() ?? string.Empty;
            existingTicket.ThirdApproverName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (currentUser.UserName ?? currentUser.Email ?? "Unknown");
            existingTicket.ThirdApproverDepartment = currentUser.Department?.Trim() ?? string.Empty;


            if (existingTicket.RepairType == RepairType.DriveAccessPermission)
            {
                // Preserve existing value if form did not provide a value
                var driveAccessDeptFromForm = ticket.DriveAccessDepartment?.Trim();
                if (!string.IsNullOrWhiteSpace(driveAccessDeptFromForm))
                {
                    existingTicket.DriveAccessDepartment = driveAccessDeptFromForm;
                }
            }
            else
            {
                existingTicket.DriveAccessDepartment = string.Empty;
            }


            // Auto-populate NextApproverDepartment from user's Department field
            existingTicket.NextApproverDepartment = nextApproverUser?.Department?.Trim() ?? string.Empty;
            
            Console.WriteLine($"DEBUG: UPDATED NextApprover from form - UserId: '{existingTicket.NextApproverUserId}', Name: '{existingTicket.NextApproverName}', Dept: '{existingTicket.NextApproverDepartment}'");
        }
        else if (!string.IsNullOrWhiteSpace(nextApproverDepartmentFromForm) && canEditStatus)
        {
            // Form provided NextApproverDepartment but no user - update department only
            existingTicket.NextApproverDepartment = ticket.NextApproverDepartment;
            Console.WriteLine($"DEBUG: UPDATED NextApproverDepartment from form: '{existingTicket.NextApproverDepartment}'");
        }
        else if (!string.IsNullOrWhiteSpace(existingTicket.NextApproverUserId))
        {
            // If form didn't provide a value but there's an existing value, preserve it
            // This prevents clearing when the DX dropdown is not shown
            Console.WriteLine($"DEBUG: Preserving existing NextApproverUserId: '{existingTicket.NextApproverUserId}'");
        }
        else
        {
            Console.WriteLine($"DEBUG: No NextApproverUserId from form and no existing value");
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
            // Preserve existing assignment if form provided a value, otherwise clear
            if (string.IsNullOrWhiteSpace(ticket.AssignedItUserId))
            {
                existingTicket.AssignedItUserId = string.Empty;
                existingTicket.AssignedItName = string.Empty;
            }
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
    [Authorize]
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

        // Only IT with Approver role, Admin, or ticket owner (RequesterUserId) can close
        var isItApprover = User.IsInRole(AppRoles.ITSupport) && User.IsInRole(AppRoles.Approve);
        var isOwner = IsOwnerTicket(ticket, currentUser);
        if (!isItApprover && !User.IsInRole(AppRoles.Admin) && !isOwner)
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
        // Third approver (SM/DM of DX) should be users who have both ITSupport and Approve roles
        var itSupportUsers = await GetItSupportUsersAsync();
        var approverUsers = await GetApproverUsersAsync();

        var dxApprovers = approverUsers
            .Where(user => !string.IsNullOrWhiteSpace(user.Department) 
                && user.Department.Trim().Equals("DX", StringComparison.OrdinalIgnoreCase))
            .ToList();
        ViewData["ThirdApproverUsers"] = dxApprovers;

        
        // Filter to only users who have both ITSupport and Approve roles
        var thirdApproverUsers = itSupportUsers
            .Where(itUser => approverUsers.Any(approver => approver.Id == itUser.Id))
            .OrderBy(user => user.Department)
            .ThenBy(user => user.FullName)
            .ThenBy(user => user.UserName)
            .ToList();
        
        ViewData["ThirdApproverUsers"] = thirdApproverUsers;

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
