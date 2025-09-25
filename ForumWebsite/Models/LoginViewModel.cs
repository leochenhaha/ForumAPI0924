using System.ComponentModel.DataAnnotations;

namespace ForumWebsite.Models
{
    public class LoginViewModel
    {
        // 使用者輸入的帳號名稱（UserName）
        [Required(ErrorMessage = "請輸入使用者名稱")]
        public string UserName { get; set; }

        // 使用者輸入的密碼
        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)] // 告訴 Razor 這是密碼欄位（會顯示為 •••）
        public string Password { get; set; }

        // 是否記住登入狀態（可選）
        public bool RememberMe { get; set; } = false;
    }
}
