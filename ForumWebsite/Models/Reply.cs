using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ForumWebsite.Models
{
    public class Reply
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        [Required]
        public int RegisterId { get; set; }
        public Register Register { get; set; } = null!;

        // ❌ 舊的寫法：public ICollection<Report> Reports { get; set; } = new List<Report>();

        // ✅ 改成專屬於留言的檢舉
        public ICollection<ReplyReport> Reports { get; set; } = new List<ReplyReport>();
    }
}
