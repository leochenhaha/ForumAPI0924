using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ForumWebsite.Models;

namespace ForumWebsite.Controllers.Api
{
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        protected int? CurrentUserId()
        {
            var userId = User.FindFirst("userId")?.Value;
            if (int.TryParse(userId, out int id))
                return id;
            return null;
        }

        protected string? CurrentUserName()
        {
            return User.Identity?.Name; // 對應 JWT 的 sub（使用者帳號）
        }

        protected string? CurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        protected PermissionLevel? GetUserPermission()
        {
            var roleStr = CurrentUserRole();
            if (Enum.TryParse<PermissionLevel>(roleStr, out var roleEnum))
                return roleEnum;
            return null;
        }

        protected bool IsAdmin() => GetUserPermission() == PermissionLevel.Admin;
        protected bool IsModerator() => GetUserPermission() is PermissionLevel.Moderator or PermissionLevel.Admin;
        protected bool IsUser() => GetUserPermission() == PermissionLevel.User;
        protected bool IsGuest() => GetUserPermission() == PermissionLevel.Guest;

        protected bool CanEdit(int? resourceOwnerId)
        {
            var userId = CurrentUserId();
            var role = GetUserPermission();
            return userId == resourceOwnerId || role == PermissionLevel.Admin || role == PermissionLevel.Moderator;
        }
    }
}
