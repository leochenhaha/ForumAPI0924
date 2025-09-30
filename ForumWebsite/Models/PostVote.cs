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
        public VoteType VoteType { get; set; } // �� or �N

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // �@�Ϊ��벼����
    public enum VoteType
    {
        Upvote = 1,
        Downvote = -1
    }
}
