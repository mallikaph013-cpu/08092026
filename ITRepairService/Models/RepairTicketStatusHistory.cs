#nullable enable

using System.ComponentModel.DataAnnotations;

namespace ITRepairService.Models;

public class RepairTicketStatusHistory
{
    public int Id { get; set; }

    [Required]
    public int RepairTicketId { get; set; }

    public RepairTicket? RepairTicket { get; set; }

    [Display(Name = "สถานะเดิม")]
    public TicketStatus? FromStatus { get; set; }

    [Required]
    [Display(Name = "สถานะใหม่")]
    public TicketStatus ToStatus { get; set; }

    [StringLength(50)]
    [Display(Name = "เหตุการณ์")]
    public string Action { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "หมายเหตุ")]
    public string? Remark { get; set; }

    [Display(Name = "บันทึกเมื่อ")]
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    [StringLength(450)]
    [Display(Name = "ผู้บันทึก (Id)")]
    public string? ChangedByUserId { get; set; }

    [StringLength(120)]
    [Display(Name = "ผู้บันทึก")]
    public string? ChangedByName { get; set; }
}
