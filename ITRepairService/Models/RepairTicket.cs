using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITRepairService.Models;

public class RepairTicket
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "ผู้แจ้งซ่อม")]
    public string RequesterName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "ฝ่าย")]
    public string Department { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    [Display(Name = "อุปกรณ์ / ทรัพย์สิน")]
    public string DeviceName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "รายละเอียดปัญหา")]
    public string IssueDescription { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ประเภทการแจ้งซ่อม")]
    public RepairType RepairType { get; set; } = RepairType.Hardware;

    [StringLength(100)]
    [Display(Name = "ต้องการขอสิทธิ์ Drive ของฝ่าย")]
    public string? DriveAccessDepartment { get; set; }

    
    [Display(Name = "ระดับความสำคัญ")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

   
    [Display(Name = "สถานะ")]
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    [Display(Name = "สร้างเมื่อ")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(120)]
    [Display(Name = "ผู้สร้าง")]
    public string? CreatedByName { get; set; }

    [Display(Name = "อัพเดตเมื่อ")]
    public DateTime? UpdatedAt { get; set; }

    [StringLength(120)]
    [Display(Name = "อัพเดตโดย")]
    public string? UpdatedByName { get; set; }

    [StringLength(100)]
    [Display(Name = "ฝ่ายที่อนุมัติ")]
    public string? ApproverDepartment { get; set; }

    [StringLength(450)]
    [Display(Name = "ผู้อนุมัติ (SM/DM ของฝ่ายผู้แจ้ง)")]
    public string? ApproverUserId { get; set; }

    [StringLength(150)]
    [Display(Name = "ชื่อผู้อนุมัติ")]
    public string? ApproverName { get; set; }

    [StringLength(450)]
    [Display(Name = "ผู้อนุมัติระดับ 2 (SM/DM ของ DX)")]
    public string? SecondApproverUserId { get; set; }

    [StringLength(150)]
    [Display(Name = "ชื่อผู้อนุมัติระดับ 2")]
    public string? SecondApproverName { get; set; }

     [StringLength(100)]
     [Display(Name = "ฝ่ายผู้อนุมัติระดับ 2")]
     public string? SecondApproverDepartment { get; set; }

     [StringLength(450)]
     [Display(Name = "ผู้อนุมัติระดับ 3 (SM/DM ของฝ่ายที่ขอสิทธิ์ Drive)")]
     public string? ThirdApproverUserId { get; set; }

     [StringLength(150)]
     [Display(Name = "ชื่อผู้อนุมัติระดับ 3")]
     public string? ThirdApproverName { get; set; }

     [StringLength(100)]
     [Display(Name = "ฝ่ายผู้อนุมัติระดับ 3")]
     public string? ThirdApproverDepartment { get; set; }

     [StringLength(450)]
     [Display(Name = "ผู้อนุมัติ DX (ขั้นตอนสุดท้าย)")]
     public string? DxFinalApproverUserId { get; set; }

     [StringLength(150)]
     [Display(Name = "ชื่อผู้อนุมัติ DX (ขั้นตอนสุดท้าย)")]
     public string? DxFinalApproverName { get; set; }

     [StringLength(100)]
     [Display(Name = "ฝ่ายผู้อนุมัติ DX (ขั้นตอนสุดท้าย)")]
     public string? DxFinalApproverDepartment { get; set; }

     [StringLength(450)]
     [Display(Name = "ผู้อนุมัติลำดับถัดไป")]
     public string? NextApproverUserId { get; set; }

     [StringLength(150)]
     [Display(Name = "ชื่อผู้อนุมัติลำดับถัดไป")]
     public string? NextApproverName { get; set; }

     [StringLength(100)]
    [Display(Name = "ฝ่ายที่อนุมัติลำดับถัดไป")]
    public string? NextApproverDepartment { get; set; }
     
 

     [Display(Name = "ระดับการอนุมัติ")]
     public int ApprovalLevel { get; set; } = 1;

     [Display(Name = "ขั้นตอนที่")]
     [Range(1, 100, ErrorMessage = "ขั้นตอนต้องอยู่ระหว่าง 1 ถึง 100")]
     public int Step { get; set; } = 1;

     [StringLength(450)]
     [Display(Name = "ผู้รับมอบหมายงาน (IT)")]
     public string? AssignedItUserId { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "ชื่อผู้รับมอบหมายงาน")]
    public string? AssignedItName { get; set; } = string.Empty;

    [StringLength(450)]
    [Display(Name = "รหัสผู้แจ้ง")]
    public string? RequesterUserId { get; set; }

    [StringLength(20)]
    [Display(Name = "เลขเอกสาร")]
    public string? DocumentNo { get; set; }

    [Display(Name = "ไฟล์แนบ PDF")]
    [StringLength(500)]
    public string? PdfAttachmentPath { get; set; }

    [Display(Name = "ชื่อไฟล์ PDF")]
    [StringLength(255)]
    public string? PdfAttachmentFileName { get; set; }

    [Display(Name = "ขนาดไฟล์ (bytes)")]
    public long? PdfAttachmentFileSize { get; set; }

    [Display(Name = "ขนาดไฟล์ (bytes)")]
    public string? pdfAttachment { get; set; }


    public ICollection<RepairTicketStatusHistory> StatusHistories { get; set; } = new List<RepairTicketStatusHistory>();
}

public enum RepairType
{
    [Display(Name = "PC LAN Account")]
    PcLanAccount = 5,

    [Display(Name = "Hardware")]
    Hardware = 0,

    [Display(Name = "Restore Backup")]
    RestoreBackup = 6,

    [Display(Name = "Lotus Note Account")]
    LotusNoteAccount = 4,

    [Display(Name = "Software")]
    Software = 1,

    /// <summary>Legacy value kept for existing database records. Not shown in new ticket forms.</summary>
    [Display(Name = "Network")]
    Network = 2,

    [Display(Name = "สิทธิ์การใช้งานเข้า Drive")]
    DriveAccessPermission = 3,

    [Display(Name = "Other")]
    Other = 7,
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TicketStatus
{
    [Display(Name = "Open (ผู้แจ้งสร้างรายการ)")]
    Open,

    [Display(Name = "InProgress (IT ดำเนินการ)")]
    InProgress,

    [Display(Name = "Approved (ผู้อนุมัติได้อนุมัติรายการ)")]
    Approved,

    [Display(Name = "Rejected (ผู้อนุมัติไม่อนุมัติรายการ)")]
    Rejected,

    [Display(Name = "Closed (ผู้แจ้งปิดงาน)")]
    Closed,

    [Display(Name = "Complete (IT ดำเนินการเสร็จ)")]
    Complete,

    [Display(Name = "Deleted")]
    Deleted
}