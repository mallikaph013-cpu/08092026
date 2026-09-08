#nullable enable

using ITRepairService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ITRepairService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RepairTicket> RepairTickets => Set<RepairTicket>();
    public DbSet<RepairTicketStatusHistory> RepairTicketStatusHistories => Set<RepairTicketStatusHistory>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();

    public void AddRepairTicketStatusHistory(
        RepairTicket ticket,
        TicketStatus? fromStatus,
        TicketStatus toStatus,
        string action,
        string? remark,
        string? changedByUserId,
        string changedByName)
    {
        RepairTicketStatusHistories.Add(new RepairTicketStatusHistory
        {
            RepairTicket = ticket,
            RepairTicketId = ticket.Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Action = action,
            Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim(),
            ChangedAt = DateTime.Now,
            ChangedByUserId = changedByUserId,
            ChangedByName = changedByName
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RepairTicket>()
            .Property(ticket => ticket.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<RepairTicket>()
            .Property(ticket => ticket.Status)
            .HasConversion<string>();

        modelBuilder.Entity<RepairTicket>()
            .Property(ticket => ticket.RepairType)
            .HasConversion<string>();

        modelBuilder.Entity<RepairTicketStatusHistory>()
            .Property(history => history.FromStatus)
            .HasConversion<string>();

        modelBuilder.Entity<RepairTicketStatusHistory>()
            .Property(history => history.ToStatus)
            .HasConversion<string>();

        modelBuilder.Entity<RepairTicketStatusHistory>()
            .HasOne(history => history.RepairTicket)
            .WithMany(ticket => ticket.StatusHistories)
            .HasForeignKey(history => history.RepairTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RepairTicketStatusHistory>()
            .HasIndex(history => new { history.RepairTicketId, history.ChangedAt });

        modelBuilder.Entity<NewsItem>()
            .Property(news => news.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
