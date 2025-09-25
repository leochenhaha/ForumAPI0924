using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ForumWebsite.Models;
using System.Security.Claims;

namespace ForumWebsite.Filters
{
    // 權限驗證屬性
    public class PermissionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly PermissionLevel _requiredPermission;

        public PermissionAuthorizeAttribute(PermissionLevel requiredPermission)
        {
            _requiredPermission = requiredPermission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var role = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = context.HttpContext.User.FindFirst("userId")?.Value;
            int? userId = null;
            if (int.TryParse(userIdClaim, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            Console.WriteLine("=== PermissionAuthorize ===");
            Console.WriteLine("UserId: " + userId);
            Console.WriteLine("UserRole: " + role);

            if (string.IsNullOrEmpty(role) || !Enum.TryParse(role, out PermissionLevel currentPermission))
            {
                // 尚未登入 → 導回登入頁
                context.Result = new RedirectToActionResult("Login", "Registers", null);
                return;
            }

            if (currentPermission < _requiredPermission)
            {
                // 權限不足 → 導回首頁
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
