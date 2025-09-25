using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ForumWebsite.Models
{
    public class Reply
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "留言內容為必填")]
        [StringLength(500, ErrorMessage = "留言不得超過 500 字")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now; // 預設值

        [Required]
        public int PostId { get; set; }                     // 外鍵：回覆對應的文章

        //[ValidateNever]
        public Post Post { get; set; }                      // 導覽屬性：EF用來串接文章

        public int? RegisterId { get; set; }                 // 可選的使用者外鍵（未登入可為 null）
        public Register? Register { get; set; }               // 導覽屬性：顯示回覆者
    }
}
