using System.ComponentModel.DataAnnotations;

namespace ForumWebsite.Models
{
    public class LoginViewModel
    {
        // 使用者輸入的帳號名稱（UserName）
        [Required(ErrorMessage = "請輸入使用者名稱")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "使用者名稱需介於 3 到 20 字元之間")]
        public string UserName { get; set; } = string.Empty;

        // 使用者輸入的密碼
        [Required(ErrorMessage = "請輸入密碼")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "密碼長度至少 8 個字元")]
        [DataType(DataType.Password)] // 告訴 Razor 這是密碼欄位（會顯示為 •••）
        public string Password { get; set; } = string.Empty;

        // 是否記住登入狀態（可選）
        public bool RememberMe { get; set; } = false;
    }
}
