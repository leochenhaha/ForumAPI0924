using System;
using System.ComponentModel.DataAnnotations;

namespace ForumWebsite.Models
{
    public class PostReport
    {
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        [Required]
        public int ReporterId { get; set; }
        public Register Reporter { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ReviewedById { get; set; }
        public Register? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? ReviewNote { get; set; }

        [StringLength(100)]
        public string? ActionTaken { get; set; }
    }
}
