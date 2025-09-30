using ForumWebsite.Filters;
using ForumWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ForumWebsite.Controllers.Api
{
    [ApiController]
    [Route("api/admin")] // ✅ 統一路徑：/api/admin
    [PermissionAuthorize(PermissionLevel.Admin)]
    public class AdminApiController : BaseApiController
    {
        private readonly ForumDbContext _context;

        private static readonly string[] ReportReasonPresets = new[]
        {
            "垃圾訊息或廣告",
            "仇恨或歧視言論",
            "騷擾或霸凌",
            "敏感或違法內容",
            "侵犯智慧財產權",
            "其他"
        };

        public AdminApiController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: /api/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardAsync()
        {
            var adminName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            var now = DateTime.UtcNow;
            var startOfWeek = now.Date.AddDays(-(int)now.Date.DayOfWeek);
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var activeThreshold = now.AddMinutes(-10);

            var totalMembers = await _context.Register.CountAsync();
            var totalPosts = await _context.Posts.CountAsync();
            var totalReplies = await _context.Replies.CountAsync();

            // ✅ 改成兩個報表相加
            var pendingReports = await _context.PostReports.CountAsync(r => r.Status == ReportStatus.Pending)
                                + await _context.ReplyReports.CountAsync(r => r.Status == ReportStatus.Pending);

            var onlineMembers = await _context.Register.CountAsync(u => u.LastActiveAt != null && u.LastActiveAt >= activeThreshold);
            var weeklyRegistrations = await _context.Register.CountAsync(u => u.CreatedAt >= startOfWeek);
            var monthlyRegistrations = await _context.Register.CountAsync(u => u.CreatedAt >= startOfMonth);

            return Ok(new
            {
                message = $"歡迎來到後台，{adminName} 管理員。",
                admin = adminName,
                totals = new
                {
                    members = totalMembers,
                    posts = totalPosts,
                    replies = totalReplies,
                    pendingReports
                },
                activity = new
                {
                    onlineMembers,
                    weeklyRegistrations,
                    monthlyRegistrations
                },
                reportReasons = ReportReasonPresets
            });
        }

        // 幫助取得當前管理員 Id
        protected new int? CurrentUserId()
        {
            var userId = User.FindFirst("userId")?.Value;
            if (int.TryParse(userId, out int id))
                return id;
            return null;
        }

        // 查文章的查詢參數
        public class AdminPostQuery
        {
            private const int MaxPageSize = 100;
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
            public int? AuthorId { get; set; }
        }

        // 查詢所有文章（支援分頁與搜尋）
        [HttpGet("posts")]
        public async Task<IActionResult> GetAllPosts([FromQuery] AdminPostQuery query)
        {
            var posts = _context.Posts
                .AsNoTracking()
                .Include(p => p.Register)
                .Include(p => p.Replies)
                .Include(p => p.PostVotes)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var keyword = query.Search.Trim();
                posts = posts.Where(p => p.Title.Contains(keyword) || p.Content.Contains(keyword));
            }

            if (query.AuthorId.HasValue)
            {
                posts = posts.Where(p => p.RegisterId == query.AuthorId.Value);
            }

            var totalCount = await posts.CountAsync();
            var page = query.Page;
            var pageSize = query.PageSize;

            var items = await posts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    Author = p.Register == null ? null : new { p.Register.Id, p.Register.UserName },
                    p.CreatedAt,
                    // ✅ 改成查 PostReports
                    reportCount = _context.PostReports.Count(r => r.PostId == p.Id),
                    replyCount = p.Replies.Count,
                    voteScore = p.PostVotes.Sum(v => (int?)v.VoteType) ?? 0
                })
                .ToListAsync();

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

        // 查會員的查詢參數
        public class MemberQuery
        {
            public AccountStatus? Status { get; set; }
            public PermissionLevel? Role { get; set; }
        }

        // 查詢會員列表
        [HttpGet("members")]
        public async Task<IActionResult> GetMembers([FromQuery] MemberQuery query)
        {
            var members = _context.Register
                .AsNoTracking()
                .Include(u => u.Posts)
                .Include(u => u.PostReportsFiled)   // ✅ 改
                .Include(u => u.ReplyReportsFiled)  // ✅ 改
                .AsQueryable();

            if (query.Status.HasValue)
            {
                members = members.Where(u => u.Status == query.Status.Value);
            }

            if (query.Role.HasValue)
            {
                members = members.Where(u => u.UserRole == query.Role.Value);
            }

            var results = await members
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.UserRole,
                    u.Status,
                    u.CreatedAt,
                    u.LastLoginAt,
                    u.LastActiveAt,
                    postCount = u.Posts.Count,
                    reportCount = u.PostReportsFiled.Count + u.ReplyReportsFiled.Count // ✅ 改
                })
                .ToListAsync();

            return Ok(results);
        }

        // 查檢舉
        public class ReportQuery
        {
            public ReportStatus? Status { get; set; }
            public ReportTargetType? TargetType { get; set; }
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] ReportQuery query)
        {
            // ✅ 查 PostReports
            var postReports = _context.PostReports
                .AsNoTracking()
                .Include(r => r.Reporter)
                .Include(r => r.Post).ThenInclude(p => p.Register)
                .AsQueryable();

            if (query.Status.HasValue)
                postReports = postReports.Where(r => r.Status == query.Status.Value);

            // ✅ 查 ReplyReports
            var replyReports = _context.ReplyReports
                .AsNoTracking()
                .Include(r => r.Reporter)
                .Include(r => r.Reply).ThenInclude(re => re.Register)
                .AsQueryable();

            if (query.Status.HasValue)
                replyReports = replyReports.Where(r => r.Status == query.Status.Value);

            var reports = await postReports.Select(r => new
            {
                r.Id,
                TargetType = ReportTargetType.Post,
                r.Status,
                r.Reason,
                r.Description,
                r.CreatedAt,
                Reporter = r.Reporter == null ? null : new { r.ReporterId, r.Reporter.UserName },
                Target = new
                {
                    Type = "Post",
                    Id = r.PostId,
                    Title = r.Post.Title,
                    Content = r.Post.Content,
                    Owner = r.Post.Register == null ? null : new { r.Post.Register.Id, r.Post.Register.UserName }
                }
            })
            .Union(replyReports.Select(r => new
            {
                r.Id,
                TargetType = ReportTargetType.Reply,
                r.Status,
                r.Reason,
                r.Description,
                r.CreatedAt,
                Reporter = r.Reporter == null ? null : new { r.ReporterId, r.Reporter.UserName },
                Target = new
                {
                    Type = "Reply",
                    Id = r.ReplyId,
                    Title = (string?)null,
                    Content = r.Reply.Content,
                    Owner = r.Reply.Register == null ? null : new { r.Reply.Register.Id, r.Reply.Register.UserName }
                }
            }))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

            return Ok(reports);
        }
    }
}
