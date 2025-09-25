using System.ComponentModel.DataAnnotations;

namespace ForumWebsite.Models
{
    public class PostVote
    {
        [Key]
        public int Id { get; set; }

        public int PostId { get; set; }
        public Post Post { get; set; }

        public int UserId { get; set; }
        public Register User { get; set; }

        public VoteType VoteType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum VoteType
    {
        Upvote = 1,
        Downvote = -1
    }
}
