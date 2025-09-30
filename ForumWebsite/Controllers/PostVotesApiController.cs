using ForumWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ForumWebsite.Controllers.Api
{
    [ApiController]
    [Route("api/posts/{postId:int}/votes")] // ✅ 路徑固定在某篇文章下
    public class PostVotesApiController : BaseApiController
    {
        private readonly ForumDbContext _context;

        public PostVotesApiController(ForumDbContext context)
        {
            _context = context;
        }

        public class VoteRequest
        {
            [Required]
            [EnumDataType(typeof(VoteType))]
            public VoteType VoteType { get; set; }
        }

        // GET: /api/posts/{postId}/votes
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetVoteSummary(int postId)
        {
            if (!await _context.Posts.AnyAsync(p => p.Id == postId))
                return NotFound();

            var currentUserId = CurrentUserId();
            var summary = await BuildVoteResponse(postId, currentUserId);
            return Ok(summary);
        }

        // POST: /api/posts/{postId}/votes
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpsertVote(int postId, [FromBody] VoteRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null) return NotFound();

            var existingVote = await _context.PostVotes
                .FirstOrDefaultAsync(v => v.PostId == postId && v.UserId == userId.Value);

            if (existingVote == null)
            {
                _context.PostVotes.Add(new PostVote
                {
                    PostId = postId,
                    UserId = userId.Value,
                    VoteType = request.VoteType,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingVote.VoteType = request.VoteType;
            }

            await TouchUserActivityAsync(userId.Value);
            await _context.SaveChangesAsync();

            var summary = await BuildVoteResponse(postId, userId.Value);
            return Ok(summary);
        }

        // DELETE: /api/posts/{postId}/votes
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> RemoveVote(int postId)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var vote = await _context.PostVotes
                .FirstOrDefaultAsync(v => v.PostId == postId && v.UserId == userId.Value);

            if (vote == null) return NotFound();

            _context.PostVotes.Remove(vote);

            await TouchUserActivityAsync(userId.Value);
            await _context.SaveChangesAsync();

            var summary = await BuildVoteResponse(postId, userId.Value);
            return Ok(summary);
        }

        private async Task<object> BuildVoteResponse(int postId, int? currentUserId)
        {
            var result = await _context.PostVotes
                .Where(v => v.PostId == postId)
                .GroupBy(v => v.PostId)
                .Select(g => new
                {
                    Upvotes = g.Count(v => v.VoteType == VoteType.Upvote),
                    Downvotes = g.Count(v => v.VoteType == VoteType.Downvote),
                    Score = g.Sum(v => (int)v.VoteType),
                    CurrentUserVote = currentUserId.HasValue
                        ? g.Where(v => v.UserId == currentUserId.Value)
                            .Select(v => (VoteType?)v.VoteType)
                            .FirstOrDefault()
                        : null
                })
                .FirstOrDefaultAsync();

            return new
            {
                PostId = postId,
                Upvotes = result?.Upvotes ?? 0,
                Downvotes = result?.Downvotes ?? 0,
                Score = result?.Score ?? 0,
                CurrentUserVote = result?.CurrentUserVote
            };
        }

        private async Task TouchUserActivityAsync(int userId)
        {
            var user = await _context.Register.FindAsync(userId);
            if (user != null)
                user.LastActiveAt = DateTime.UtcNow;
        }
    }
}
