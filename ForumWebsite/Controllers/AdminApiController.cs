using ForumWebsite.Filters;
using ForumWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ForumWebsite.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminApiController : BaseApiController
    {
        [HttpGet("dashboard")]
        [PermissionAuthorize(PermissionLevel.Admin)]
        public IActionResult GetDashboard()
        {
            var adminName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            return Ok(new
            {
                message = $"歡迎來到後台，{adminName} 管理員。",
                admin = adminName
            });
        }
        protected int? CurrentUserId()
        {
            var userId = User.FindFirst("userId")?.Value;
            if (int.TryParse(userId, out int id))
                return id;
            return null;
        }

    }

}

    
