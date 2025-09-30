using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ForumWebsite.Models
{
    public class Register
    {
        [Key] // 主鍵
        public int Id { get; set; }

        [Required(ErrorMessage = "使用者名稱為必填")]
        [StringLength(20, ErrorMessage = "使用者名稱不能超過 20 字元")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "電子郵件為必填")]
        [EmailAddress(ErrorMessage = "請輸入正確的 Email 格式")]
        [StringLength(100, ErrorMessage = "Email 長度不得超過 100 字元")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "密碼為必填")]
        [JsonIgnore] // 確保回傳 API 不會洩漏
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public DateTime? LastActiveAt { get; set; }

        [Required]
        public PermissionLevel UserRole { get; set; } = PermissionLevel.User;

        [Required]
        public AccountStatus Status { get; set; } = AccountStatus.Active;

        // ✅ 一個會員可以有很多文章
        public ICollection<Post> Posts { get; set; } = new List<Post>();

        // ❌ 移除舊的 Report（因為已拆分）
        // public ICollection<Report> ReportsFiled { get; set; } = new List<Report>();

        // ✅ 一個會員可能檢舉多篇文章
        public ICollection<PostReport> PostReportsFiled { get; set; } = new List<PostReport>();

        // ✅ 一個會員可能檢舉多則留言
        public ICollection<ReplyReport> ReplyReportsFiled { get; set; } = new List<ReplyReport>();

        // ✅ 一個會員可能收到多個通知
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
