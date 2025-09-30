using ForumWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ForumWebsite.Controllers.Api
{
    // API 專用的資料模型
    public class AddReplyModel
    {
        [Required]
        [StringLength(500, ErrorMessage = "留言不得超過 500 字")]
        public string Content { get; set; } = string.Empty;
    }

    public class PostQueryParameters
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
        public int? AuthorId { get; set; }
        public string SortBy { get; set; } = "createdAt";
        public string SortDirection { get; set; } = "desc";
    }

    public class CreatePostRequest
    {
        [Required]
        [StringLength(100, ErrorMessage = "標題不得超過 100 字元")]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }

    public class UpdatePostRequest : CreatePostRequest { }

    [ApiController]
    [Route("api/posts")] // ✅ 改成固定 /api/posts
    public class PostsApiController : BaseApiController
    {
        private readonly ForumDbContext _context;

        public PostsApiController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: /api/posts
        [HttpGet]
        public async Task<IActionResult> GetPosts([FromQuery] PostQueryParameters query)
        {
            var currentUserId = CurrentUserId();
            if (currentUserId.HasValue)
            {
                await TouchUserAsync(currentUserId.Value, saveImmediately: true);
            }

            var postsQuery = _context.Posts
                .AsNoTracking()
                .Include(p => p.Register)
                .Include(p => p.Replies)
                .Include(p => p.PostVotes)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var keyword = query.Search.Trim();
                postsQuery = postsQuery.Where(p =>
                    p.Title.Contains(keyword) ||
                    p.Content.Contains(keyword));
            }

            if (query.AuthorId.HasValue)
            {
                postsQuery = postsQuery.Where(p => p.RegisterId == query.AuthorId.Value);
            }

            postsQuery = ApplyPostSorting(postsQuery, query.SortBy, query.SortDirection);

            var totalCount = await postsQuery.CountAsync();
            var page = query.Page;
            var pageSize = query.PageSize;

            var items = await postsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    Author = p.Register == null ? null : new { p.Register.Id, p.Register.UserName },
                    p.CreatedAt,
                    CommentCount = p.Replies.Count,
                    Votes = new
                    {
                        Upvotes = p.PostVotes.Count(v => v.VoteType == VoteType.Upvote),
                        Downvotes = p.PostVotes.Count(v => v.VoteType == VoteType.Downvote),
                        Score = p.PostVotes.Sum(v => (int?)v.VoteType) ?? 0
                    }
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new
            {
                items,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages
                }
            });
        }

        // GET: /api/posts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var post = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Register)
                .Include(p => p.Replies.OrderByDescending(r => r.CreatedAt)).ThenInclude(r => r.Register)
                .Include(p => p.PostVotes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            var currentUserId = CurrentUserId();
            if (currentUserId.HasValue)
            {
                await TouchUserAsync(currentUserId.Value, saveImmediately: true);
            }

            var currentVote = currentUserId.HasValue
                ? post.PostVotes.FirstOrDefault(v => v.UserId == currentUserId.Value)?.VoteType
                : null;

            return Ok(new
            {
                post.Id,
                post.Title,
                post.Content,
                Author = post.Register == null ? null : new { post.Register.Id, post.Register.UserName },
                post.CreatedAt,
                Votes = new
                {
                    Upvotes = post.PostVotes.Count(v => v.VoteType == VoteType.Upvote),
                    Downvotes = post.PostVotes.Count(v => v.VoteType == VoteType.Downvote),
                    Score = post.PostVotes.Sum(v => (int)v.VoteType),
                    CurrentUserVote = currentVote
                },
                Replies = post.Replies
                    .Select(r => new
                    {
                        r.Id,
                        r.Content,
                        Author = r.Register == null ? null : new { r.Register.Id, r.Register.UserName },
                        r.CreatedAt
                    })
            });
        }

        // POST: /api/posts
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var authorId)) return Unauthorized();

            var post = new Post
            {
                Title = request.Title,
                Content = request.Content,
                RegisterId = authorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);

            var author = await _context.Register.FindAsync(authorId);
            if (author != null) author.LastActiveAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "文章建立成功", post.Id, post.Title });
        }

        // PUT: /api/posts/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostRequest updated)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userIdClaim = User.FindFirst("userId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var isOwner = userIdClaim != null && int.TryParse(userIdClaim, out var requesterId) && post.RegisterId == requesterId;

            if (!isOwner && role != "Admin" && role != "Moderator") return Forbid();

            post.Title = updated.Title;
            post.Content = updated.Content;

            await TouchUserAsync(post.RegisterId, saveImmediately: false);

            await _context.SaveChangesAsync();
            return Ok(new { message = "文章已更新", post.Id });
        }

        // DELETE: /api/posts/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var isOwner = userIdClaim != null && int.TryParse(userIdClaim, out var requesterId) && post.RegisterId == requesterId;

            if (!isOwner && role != "Admin" && role != "Moderator") return Forbid();

            _context.Posts.Remove(post);

            await TouchUserAsync(post.RegisterId, saveImmediately: false);

            await _context.SaveChangesAsync();
            return Ok(new { message = "文章已刪除" });
        }

        // POST: /api/posts/{postId}/replies
        [HttpPost("{postId}/replies")]
        [Authorize]
        public async Task<IActionResult> AddReply(int postId, [FromBody] AddReplyModel model)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var replierId)) return Unauthorized();

            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound(new { message = "找不到指定的文章" });

            var reply = new Reply
            {
                PostId = postId,
                RegisterId = replierId,
                CreatedAt = DateTime.UtcNow,
                Content = model.Content
            };

            _context.Replies.Add(reply);

            var replier = await _context.Register.FindAsync(replierId);
            if (replier != null) replier.LastActiveAt = DateTime.UtcNow;

            // 通知文章作者（排除自己）
            if (post.RegisterId != replierId)
            {
                var notification = new Notification
                {
                    RecipientId = post.RegisterId,
                    Message = $"有人回覆了您的文章: {post.Title}",
                    Link = $"/posts/{postId}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "留言新增成功", reply.Id, reply.Content });
        }

        // PUT: /api/posts/replies/{replyId}
        [HttpPut("replies/{replyId}")]
        [Authorize]
        public async Task<IActionResult> EditReply(int replyId, [FromBody] AddReplyModel updated)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userIdClaim = User.FindFirst("userId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var reply = await _context.Replies.FindAsync(replyId);
            if (reply == null) return NotFound();

            var isOwner = userIdClaim != null && int.TryParse(userIdClaim, out var requesterId) && reply.RegisterId == requesterId;

            if (!isOwner && role != "Admin" && role != "Moderator") return Forbid();

            reply.Content = updated.Content;

            await TouchUserAsync(reply.RegisterId, saveImmediately: false);

            await _context.SaveChangesAsync();
            return Ok(new { message = "留言已更新", reply.Id });
        }

        // DELETE: /api/posts/replies/{replyId}
        [HttpDelete("replies/{replyId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var reply = await _context.Replies.FindAsync(replyId);
            if (reply == null) return NotFound();

            var isOwner = userIdClaim != null && int.TryParse(userIdClaim, out var requesterId) && reply.RegisterId == requesterId;

            if (!isOwner && role != "Admin" && role != "Moderator") return Forbid();

            _context.Replies.Remove(reply);

            await TouchUserAsync(reply.RegisterId, saveImmediately: false);

            await _context.SaveChangesAsync();
            return Ok(new { message = "留言已刪除" });
        }

        // GET: /api/posts/all-comments (Admin 專用)
        [HttpGet("all-comments")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllComments()
        {
            var comments = await _context.Replies
                .Include(r => r.Register)
                .Include(r => r.Post)
                .Select(r => new
                {
                    r.Id,
                    r.Content,
                    Author = r.Register == null ? null : new { r.RegisterId, r.Register.UserName },
                    Post = r.Post == null ? null : new { r.PostId, r.Post.Title },
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }

        private static IQueryable<Post> ApplyPostSorting(IQueryable<Post> query, string? sortBy, string? sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy?.ToLowerInvariant() switch
            {
                "title" => descending ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title),
                "score" => descending
                    ? query.OrderByDescending(p => p.PostVotes.Sum(v => (int?)v.VoteType) ?? 0)
                    : query.OrderBy(p => p.PostVotes.Sum(v => (int?)v.VoteType) ?? 0),
                _ => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
            };
        }

        private async Task TouchUserAsync(int userId, bool saveImmediately)
        {
            var user = await _context.Register.FindAsync(userId);
            if (user == null) return;

            user.LastActiveAt = DateTime.UtcNow;
            if (saveImmediately) await _context.SaveChangesAsync();
        }
    }
}
