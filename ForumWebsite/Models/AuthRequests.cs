using System.ComponentModel.DataAnnotations;

namespace ForumWebsite.Models;

public class RegisterRequest
{
    [Required(ErrorMessage = "使用者名稱為必填")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "使用者名稱需介於 3 到 20 字元之間")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "電子郵件為必填")]
    [EmailAddress(ErrorMessage = "請輸入正確的 Email 格式")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "密碼為必填")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密碼長度至少 8 個字元")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "使用者名稱為必填")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "使用者名稱需介於 3 到 20 字元之間")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "電子郵件為必填")]
    [EmailAddress(ErrorMessage = "請輸入正確的 Email 格式")]
    public string Email { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "請輸入目前密碼")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入新密碼")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密碼長度至少 8 個字元")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "請再次輸入新密碼")]
    [Compare(nameof(NewPassword), ErrorMessage = "兩次輸入的密碼不一致")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class AdminUpdateUserRequest
{
    [Required]
    public PermissionLevel UserRole { get; set; }

    [Required]
    public AccountStatus Status { get; set; }
}