using ForumWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForumWebsite.Controllers.Api
{
    public class NotificationQueryParameters
    {
        private const int MaxPageSize = 50;
        private int _page = 1;
        private int _pageSize = 20;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1) _pageSize = 1;
                else if (value > MaxPageSize) _pageSize = MaxPageSize;
                else _pageSize = value;
            }
        }

        public string? Search { get; set; }
        public bool? IsRead { get; set; }
    }

    public class MarkNotificationsRequest
    {
        public IEnumerable<int>? NotificationIds { get; set; }
    }

    [ApiController]
    [Route("api/notifications")] // ✅ 固定路徑，RESTful
    [Authorize]
    public class NotificationsApiController : BaseApiController
    {
        private readonly ForumDbContext _context;

        public NotificationsApiController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: /api/notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] NotificationQueryParameters query)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var notificationsQuery = _context.Notifications
                .AsNoTracking()
                .Where(n => n.RecipientId == userId);

            if (query.IsRead.HasValue)
            {
                notificationsQuery = notificationsQuery.Where(n => n.IsRead == query.IsRead.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var keyword = query.Search.Trim();
                notificationsQuery = notificationsQuery.Where(n => n.Message.Contains(keyword));
            }

            notificationsQuery = notificationsQuery.OrderByDescending(n => n.CreatedAt);

            var totalCount = await notificationsQuery.CountAsync();
            var page = query.Page;
            var pageSize = query.PageSize;

            var items = await notificationsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.Id,
                    n.Message,
                    n.Link,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            await TouchUserAsync(userId.Value, saveImmediately: true);

            return Ok(new
            {
                items,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }

        // GET: /api/notifications/unread
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications([FromQuery] NotificationQueryParameters query)
        {
            query.IsRead = false;
            return await GetNotifications(query);
        }

        // GET: /api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = CurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "請確認授權資訊", detail = "找不到使用者識別資訊" });
            }

            var count = await _context.Notifications
                .CountAsync(n => n.RecipientId == userId.Value && !n.IsRead);

            await TouchUserAsync(userId.Value, saveImmediately: true);

            return Ok(new { count });
        }

        // POST: /api/notifications/mark-as-read
        [HttpPost("mark-as-read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkNotificationsRequest? request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var targetQuery = _context.Notifications
                .Where(n => n.RecipientId == userId.Value);

            var targetIds = request?.NotificationIds?.Distinct().ToArray();

            if (targetIds != null && targetIds.Length > 0)
            {
                targetQuery = targetQuery.Where(n => targetIds.Contains(n.Id));
            }
            else
            {
                targetQuery = targetQuery.Where(n => !n.IsRead);
            }

            var notifications = await targetQuery.ToListAsync();
            if (!notifications.Any())
            {
                return Ok(new { updated = 0 });
            }

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await TouchUserAsync(userId.Value, saveImmediately: false);
            await _context.SaveChangesAsync();

            return Ok(new { updated = notifications.Count });
        }

        // DELETE: /api/notifications/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == userId.Value);

            if (notification == null)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);

            await TouchUserAsync(userId.Value, saveImmediately: false);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 更新使用者最後活躍時間
        private async Task TouchUserAsync(int userId, bool saveImmediately)
        {
            var user = await _context.Register.FindAsync(userId);
            if (user == null) return;

            user.LastActiveAt = DateTime.UtcNow;

            if (saveImmediately)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
