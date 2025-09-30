using ForumWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ForumWebsite.Controllers.Api
{
    [ApiController]
    [Route("api/auth")] // ✅ 統一路徑
    public class AuthController : BaseApiController
    {
        private readonly ForumDbContext _context;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<Register> _passwordHasher;

        public AuthController(
            ForumDbContext context,
            IConfiguration config,
            IPasswordHasher<Register> passwordHasher)
        {
            _context = context;
            _config = config;
            _passwordHasher = passwordHasher;
        }

        // ============================
        // 註冊
        // ============================
        [HttpPost("register")] // ✅ 改為 /api/auth/register
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userName = request.UserName.Trim();
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _context.Register.AnyAsync(u => u.UserName == userName))
                ModelState.AddModelError(nameof(request.UserName), "使用者名稱已被使用");

            if (await _context.Register.AnyAsync(u => u.Email == email))
                ModelState.AddModelError(nameof(request.Email), "電子郵件已被使用");

            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = new Register
            {
                UserName = userName,
                Email = email,
                UserRole = PermissionLevel.User,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow,
                PasswordHash = _passwordHasher.HashPassword(new Register(), request.Password)
            };

            _context.Register.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(BuildAuthResponse(token, user));
        }

        // ============================
        // 登入
        // ============================
        [HttpPost("login")] // /api/auth/login
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _context.Register
                .SingleOrDefaultAsync(u => u.UserName == model.UserName.Trim());

            if (user == null ||
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { success = false, message = "帳號或密碼錯誤" });
            }

            if (user.Status != AccountStatus.Active)
                return StatusCode(StatusCodes.Status423Locked, new { success = false, message = "帳號目前未啟用，請聯絡客服" });

            user.LastLoginAt = DateTime.UtcNow;
            user.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(BuildAuthResponse(token, user));
        }

        // ============================
        // 取得個人資料
        // ============================
        [HttpGet("me")] // /api/auth/me
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var user = await _context.Register.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId.Value);

            if (user == null) return NotFound();

            return Ok(MapUserDetail(user));
        }

        // ============================
        // 更新個人資料
        // ============================
        [HttpPut("me")] // /api/auth/me
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var user = await _context.Register.FindAsync(userId.Value);
            if (user == null) return NotFound();

            var userName = request.UserName.Trim();
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _context.Register.AnyAsync(u => u.UserName == userName && u.Id != user.Id))
                ModelState.AddModelError(nameof(request.UserName), "使用者名稱已被使用");

            if (await _context.Register.AnyAsync(u => u.Email == email && u.Id != user.Id))
                ModelState.AddModelError(nameof(request.Email), "電子郵件已被使用");

            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            user.UserName = userName;
            user.Email = email;
            user.LastActiveAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "個人資料已更新", user = MapUserDetail(user) });
        }

        // ============================
        // 修改密碼
        // ============================
        [HttpPost("change-password")] // /api/auth/change-password
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var user = await _context.Register.FindAsync(userId.Value);
            if (user == null) return NotFound();

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (verification == PasswordVerificationResult.Failed)
                return BadRequest(new { message = "目前密碼不正確" });

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            user.LastActiveAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "密碼已更新" });
        }

        // ============================
        // Admin 專用
        // ============================
        [HttpGet] // /api/auth
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Register
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => MapUserDetail(u))
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")] // /api/auth/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Register.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            return Ok(MapUserDetail(user));
        }

        [HttpPut("{id}")] // /api/auth/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _context.Register.FindAsync(id);
            if (user == null) return NotFound();

            user.UserRole = request.UserRole;
            user.Status = request.Status;

            await _context.SaveChangesAsync();
            return Ok(new { message = "使用者資訊已更新", user = MapUserDetail(user) });
        }

        [HttpDelete("{id}")] // /api/auth/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Register.FindAsync(id);
            if (user == null) return NotFound();

            _context.Register.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "使用者已刪除" });
        }

        // ============================
        // Helpers
        // ============================
        private object BuildAuthResponse(string token, Register user) => new
        {
            message = $"歡迎 {user.UserName}",
            token,
            user = MapUserDetail(user)
        };

        private static object MapUserDetail(Register user) => new
        {
            user.Id,
            user.UserName,
            user.Email,
            role = user.UserRole,
            status = user.Status,
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt,
            lastActiveAt = user.LastActiveAt
        };

        private string GenerateJwtToken(Register user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.UserRole.ToString())
            };

            var key = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("JWT key is not configured.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
