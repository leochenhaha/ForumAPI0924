using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumWebsite.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RecipientId { get; set; }

        [ForeignKey("RecipientId")]
        public Register Recipient { get; set; }

        [Required]
        [StringLength(255)]
        public string Message { get; set; }

        [StringLength(500)]
        public string Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
