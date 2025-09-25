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

            public VoteType VoteType { get; set; } // 推 or 噓

            public DateTime CreatedAt { get; set; } = DateTime.Now;
        }

        // 推 or 噓的 enum
        public enum VoteType
        {
            Upvote = 1,
            Downvote = -1
        }

    }

