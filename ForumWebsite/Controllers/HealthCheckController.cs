using Microsoft.AspNetCore.Mvc;

namespace ForumWebsite.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                status = "ok",
                message = "Forum API 正常運作中 🚀",
                swagger = "/swagger",
                postsApi = "/api/posts",
                registersApi = "/api/registers",
                adminApi = "/api/admin/dashboard"
            });
        }
    }
}
