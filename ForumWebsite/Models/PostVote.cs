using System.ComponentModel.DataAnnotations;

namespace ForumWebsite.Models
{
    public class PostVote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        [Required]
        public int UserId { get; set; }
        public Register User { get; set; } = null!;

        [Required]
        public VoteType VoteType { get; set; } // 推 or 噓

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // 共用的投票類型
    public enum VoteType
    {
        Upvote = 1,
        Downvote = -1
    }
}
