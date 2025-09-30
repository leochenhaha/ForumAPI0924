using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ForumWebsite.Models;

namespace ForumWebsite.Controllers.Api
{
    [ApiController] // ✅ 保留這個，讓繼承者自動套用 API 慣例
    public class BaseApiController : ControllerBase
    {
        // 取得目前使用者 Id（來自 JWT claim）
        protected int? CurrentUserId()
        {
            var userId = User.FindFirst("userId")?.Value;
            if (int.TryParse(userId, out int id))
                return id;
            return null;
        }

        // 取得目前使用者名稱
        protected string? CurrentUserName()
        {
            return User.Identity?.Name; // 對應 JWT 的 sub（使用者帳號）
        }

        // 取得目前使用者角色
        protected string? CurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        // 嘗試轉換成 PermissionLevel enum
        protected PermissionLevel? GetUserPermission()
        {
            var roleStr = CurrentUserRole();
            if (Enum.TryParse<PermissionLevel>(roleStr, out var roleEnum))
                return roleEnum;
            return null;
        }

        // 權限判斷
        protected bool IsAdmin() => GetUserPermission() == PermissionLevel.Admin;
        protected bool IsModerator() => GetUserPermission() is PermissionLevel.Moderator or PermissionLevel.Admin;
        protected bool IsUser() => GetUserPermission() == PermissionLevel.User;
        protected bool IsGuest() => GetUserPermission() == PermissionLevel.Guest;

        // 判斷能否編輯某資源
        protected bool CanEdit(int? resourceOwnerId)
        {
            var userId = CurrentUserId();
            var role = GetUserPermission();
            return userId == resourceOwnerId
                   || role == PermissionLevel.Admin
                   || role == PermissionLevel.Moderator;
        }
    }
}
