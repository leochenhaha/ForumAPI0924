using ForumWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ForumWebsite.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsApiController : BaseApiController
    {
        private readonly ForumDbContext _context;

        public NotificationsApiController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: api/notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = CurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20) // Take latest 20 notifications
                .ToListAsync();

            return Ok(notifications);
        }

        // GET: api/notifications/unread
        [HttpGet("unread")]
        [Authorize]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = CurrentUserId();
            if (userId == null)
            {
                return Unauthorized(); // Should not happen due to [Authorize]
            }

            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        // GET: api/notifications/unread-count
        [HttpGet("unread-count")]
        [Authorize]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = CurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "���n�J��token�L��" });
                }

                var count = await _context.Notifications
                    .CountAsync(n => n.RecipientId == userId && !n.IsRead);

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "���A�����~",
                    detail = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }

        // POST: api/notifications/mark-as-read
        [HttpPost("mark-as-read")]
        public async Task<IActionResult> MarkAsRead()
        {
            var userId = CurrentUserId();
            if (userId == null)
            { 
                return Unauthorized();
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}
