using Microsoft.AspNetCore.Http;
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
            Console.WriteLine("UserId: " + (userId?.ToString() ?? "null"));
            Console.WriteLine("UserRole: " + role);

            // 未登入
            if (string.IsNullOrEmpty(role) || !Enum.TryParse(role, out PermissionLevel currentPermission))
            {
                context.Result = new JsonResult(new
                {
                    message = "Unauthorized",
                    detail = "請先登入後再存取此資源"
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            // 權限不足
            if (currentPermission < _requiredPermission)
            {
                context.Result = new JsonResult(new
                {
                    message = "Forbidden",
                    detail = "權限不足，無法存取此資源"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
