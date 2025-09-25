using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumWebsite.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "標題為必填")]
        [StringLength(100, ErrorMessage = "標題不得超過 100 字元")]
        public string Title { get; set; }

        [Required(ErrorMessage = "內容為必填")]
        public string Content { get; set; }

        [ValidateNever]  // ✅ 不用對這個導覽屬性進行 Model 驗證
        public List<Reply> Replies { get; set; } = new List<Reply>(); // ✅ 保留這個就好

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? RegisterId { get; set; }
        public Register? Register { get; set; }

        public List<PostVote> PostVotes { get; set; } = new List<PostVote>();
    }
}
