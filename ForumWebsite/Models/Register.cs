using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumWebsite.Models
{
    public class Register
    {
        [Key] // 主鍵
        public int Id { get; set; }

        [Required(ErrorMessage = "使用者名稱為必填")]
        [StringLength(20, ErrorMessage = "使用者名稱不能超過 20 字元")]
        public string UserName { get; set; }

        //[Required(ErrorMessage = "電子郵件為必填")]
        [EmailAddress(ErrorMessage = "請輸入正確的 Email 格式")]
        public string Email { get; set; }

        [Required(ErrorMessage = "密碼為必填")]
        public string PasswordHash { get; set; } // 將原本的 Password 改為加密版本

        public DateTime CreatedAt { get; set; }


        [Required]
        public PermissionLevel UserRole { get; set; } = PermissionLevel.User;

        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
