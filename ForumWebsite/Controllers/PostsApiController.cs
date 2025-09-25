using ForumWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ForumWebsite.Controllers.Api
{
    // 為 API 請求建立一個專用的模型 (Model)
    public class AddReplyModel
    {
        public string Content { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class PostsApiController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public PostsApiController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: api/posts
        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            var posts = await _context.Posts
                .Include(p => p.Register)
                .Include(p => p.Replies)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    Author = new { p.RegisterId, p.Register.UserName },
                    p.CreatedAt,
                    CommentCount = p.Replies.Count
                })
                .ToListAsync();

            return Ok(posts);
        }

        // GET: api/posts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Register)
                .Include(p => p.Replies)
                    .ThenInclude(r => r.Register)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            return Ok(new
            {
                post.Id,
                post.Title,
                post.Content,
                Author = new { post.RegisterId, post.Register?.UserName },
                post.CreatedAt,
                Replies = post.Replies.Select(r => new
                {
                    r.Id,
                    r.Content,
                    Author = new { r.RegisterId, r.Register.UserName },
                    r.CreatedAt
                })
            });
        }

        // POST: api/posts
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] Post post)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId == null) return Unauthorized();

            post.RegisterId = int.Parse(userId);
            post.CreatedAt = DateTime.UtcNow;

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "文章建立成功", post.Id, post.Title });
        }

        // PUT: api/posts/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] Post updated)
        {
            var userId = User.FindFirst("userId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            if (post.RegisterId.ToString() != userId && role != "Admin" && role != "Moderator")
                return Forbid();

            post.Title = updated.Title;
            post.Content = updated.Content;
            await _context.SaveChangesAsync();

            return Ok(new { message = "文章已更新", post.Id });
        }

        // DELETE: api/posts/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            var userId = User.FindFirst("userId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            if (post.RegisterId.ToString() != userId && role != "Admin" && role != "Moderator")
                return Forbid();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "文章已刪除" });
        }

        // POST: api/posts/{postId}/replies
        [HttpPost("{postId}/replies")]
        [Authorize]
        public async Task<IActionResult> AddReply(int postId, [FromBody] AddReplyModel model)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId == null) return Unauthorized();

            var reply = new Reply
            {
                PostId = postId,
                RegisterId = int.Parse(userId),
                CreatedAt = DateTime.UtcNow,
                Content = model.Content
            };

            _context.Replies.Add(reply);

            // --- 通知功能 Start ---
            var post = await _context.Posts.FindAsync(postId);
            var replierId = int.Parse(userId);

            // Only notify if the replier is not the post owner
            if (post != null && post.RegisterId != replierId)
            {
                var notification = new Notification
                {
                    RecipientId = post.RegisterId.Value,
                    Message = $"有人回覆了您的文章: {post.Title}",
                    Link = $"/posts/{postId}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }
            // --- 通知功能 End ---

            await _context.SaveChangesAsync();

            return Ok(new { message = "留言新增成功", reply.Id, reply.Content });
        }

        // PUT: api/posts/replies/{replyId}
        [HttpPut("replies/{replyId}")]
        [Authorize]
        public async Task<IActionResult> EditReply(int replyId, [FromBody] AddReplyModel updated)
        {
            var userId = User.FindFirst("userId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var reply = await _context.Replies.FindAsync(replyId);
            if (reply == null) return NotFound();

            if (reply.RegisterId.ToString() != userId && role != "Admin" && role != "Moderator")
                return Forbid();

            reply.Content = updated.Content;
            await _context.SaveChangesAsync();

            return Ok(new { message = "留言已更新", reply.Id });
        }

        // DELETE: api/posts/replies/{replyId}
        [HttpDelete("replies/{replyId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            var userId = User.FindFirst("userId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var reply = await _context.Replies.FindAsync(replyId);
            if (reply == null) return NotFound();

            if (reply.RegisterId.ToString() != userId && role != "Admin" && role != "Moderator")
                return Forbid();

            _context.Replies.Remove(reply);
            await _context.SaveChangesAsync();

            return Ok(new { message = "留言已刪除" });
        }


        // GET: api/PostsApi/all-comments
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
                    Author = new
                    {
                        r.RegisterId,
                        r.Register.UserName
                    },
                    Post = new { r.PostId, r.Post.Title },
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }
    }
}