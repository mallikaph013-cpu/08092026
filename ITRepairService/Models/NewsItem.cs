using System.ComponentModel.DataAnnotations;

namespace ITRepairService.Models;

public class NewsItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(120)]
    public string? CreatedByName { get; set; }

    [StringLength(120)]
    public string? UpdatedByName { get; set; }

    [StringLength(260)]
    public string? AttachmentFileName { get; set; }

    [StringLength(500)]
    public string? AttachmentUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}