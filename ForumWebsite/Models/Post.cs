using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ForumWebsite.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int RegisterId { get; set; }
        public Register Register { get; set; } = null!;

        // ✅ 一篇文章可以有很多留言
        public ICollection<Reply> Replies { get; set; } = new List<Reply>();

        // ✅ 修改：原本是 ICollection<Report> → 現在改成 ICollection<PostReport>
        public ICollection<PostReport> Reports { get; set; } = new List<PostReport>();

        // ✅ 一篇文章可以被很多人投票
        public ICollection<PostVote> PostVotes { get; set; } = new List<PostVote>();
    }
}
