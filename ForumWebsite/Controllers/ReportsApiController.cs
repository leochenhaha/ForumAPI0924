using ForumWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ForumWebsite.Controllers.Api
{
    [ApiController]
    [Route("api/reports")] // ✅ 固定路徑：/api/reports
    public class ReportsApiController : BaseApiController
    {
        private readonly ForumDbContext _context;

        private static readonly string[] DefaultReasons = new[]
        {
            "垃圾訊息或廣告",
            "仇恨或歧視言論",
            "騷擾或霸凌",
            "敏感或違法內容",
            "侵犯智慧財產權",
            "其他"
        };

        public ReportsApiController(ForumDbContext context)
        {
            _context = context;
        }

        // DTO
        public class CreateReportRequest
        {
            [Required]
            public ReportTargetType TargetType { get; set; } // Post 或 Reply

            [Required]
            public int TargetId { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "理由不得超過 100 字")]
            public string Reason { get; set; } = string.Empty;

            [StringLength(1000, ErrorMessage = "描述不得超過 1000 字")]
            public string? Description { get; set; }
        }

        // GET: /api/reports/reasons
        [HttpGet("reasons")]
        public IActionResult GetReasons() => Ok(DefaultReasons);

        // GET: /api/reports/mine
        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMyReports()
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var postReports = await _context.PostReports
                .AsNoTracking()
                .Where(r => r.ReporterId == userId.Value)
                .Select(r => new
                {
                    r.Id,
                    TargetType = "Post",
                    r.Reason,
                    r.Status,
                    r.CreatedAt,
                    r.ReviewNote,
                    r.ActionTaken
                })
                .ToListAsync();

            var replyReports = await _context.ReplyReports
                .AsNoTracking()
                .Where(r => r.ReporterId == userId.Value)
                .Select(r => new
                {
                    r.Id,
                    TargetType = "Reply",
                    r.Reason,
                    r.Status,
                    r.CreatedAt,
                    r.ReviewNote,
                    r.ActionTaken
                })
                .ToListAsync();

            var allReports = postReports.Concat(replyReports)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return Ok(allReports);
        }

        // POST: /api/reports
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var normalizedReason = string.IsNullOrWhiteSpace(request.Reason)
                ? DefaultReasons.Last()
                : request.Reason.Trim();

            if (request.TargetType == ReportTargetType.Post)
            {
                var post = await _context.Posts
                    .Include(p => p.Register)
                    .FirstOrDefaultAsync(p => p.Id == request.TargetId);

                if (post == null)
                    return NotFound(new { message = "找不到要檢舉的文章" });

                var duplicate = await _context.PostReports.AnyAsync(r =>
                    r.PostId == post.Id &&
                    r.ReporterId == userId.Value &&
                    r.Status == ReportStatus.Pending);

                if (duplicate)
                    return Conflict(new { message = "您已檢舉過這篇文章，請等待審核" });

                var report = new PostReport
                {
                    PostId = post.Id,
                    ReporterId = userId.Value,
                    Reason = normalizedReason,
                    Description = request.Description,
                    Status = ReportStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PostReports.Add(report);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    report.Id,
                    report.Status,
                    message = "文章檢舉已送出，管理員將盡快審核"
                });
            }
            else if (request.TargetType == ReportTargetType.Reply)
            {
                var reply = await _context.Replies
                    .Include(r => r.Post)
                    .Include(r => r.Register)
                    .FirstOrDefaultAsync(r => r.Id == request.TargetId);

                if (reply == null)
                    return NotFound(new { message = "找不到要檢舉的留言" });

                var duplicate = await _context.ReplyReports.AnyAsync(r =>
                    r.ReplyId == reply.Id &&
                    r.ReporterId == userId.Value &&
                    r.Status == ReportStatus.Pending);

                if (duplicate)
                    return Conflict(new { message = "您已檢舉過這則留言，請等待審核" });

                var report = new ReplyReport
                {
                    ReplyId = reply.Id,
                    ReporterId = userId.Value,
                    Reason = normalizedReason,
                    Description = request.Description,
                    Status = ReportStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReplyReports.Add(report);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    report.Id,
                    report.Status,
                    message = "留言檢舉已送出，管理員將盡快審核"
                });
            }

            return BadRequest(new { message = "未知的檢舉類型" });
        }
    }
}
