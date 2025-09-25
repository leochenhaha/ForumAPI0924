using ForumWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ForumWebsite.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistersApiController : ControllerBase
    {
        private readonly ForumDbContext _context;
        private readonly IConfiguration _config;

        public RegistersApiController(ForumDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: /api/registers
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] Register model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.UserRole = PermissionLevel.User;
            model.CreatedAt = DateTime.Now;

            _context.Register.Add(model);
            await _context.SaveChangesAsync();

            // ✅ 註冊後直接回傳 JWT Token
            var token = GenerateJwtToken(model);

            return Ok(new
            {
                message = $"註冊成功，歡迎 {model.UserName}",
                token,
                user = new { model.Id, model.UserName, model.Email, model.UserRole }
            });
        }

        // POST: /api/registers/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "登入資料無效" });

            var user = _context.Register.FirstOrDefault(u =>
                u.UserName == model.UserName &&
                u.PasswordHash == model.Password);

            if (user == null)
                return Unauthorized(new { success = false, message = "帳號或密碼錯誤" });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                success = true,
                token,
                user = new { user.Id, user.UserName, user.Email, user.UserRole }
            });
        }

        // GET: /api/registers/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId == null) return Unauthorized();

            var user = await _context.Register.FindAsync(int.Parse(userId));
            if (user == null) return NotFound();

            return Ok(new { user.Id, user.UserName, user.Email, user.UserRole });
        }

        // POST: /api/registers/make-me-admin
        [HttpPost("make-me-admin")]
        [Authorize]
        public async Task<IActionResult> MakeMeAdmin()
        {
            var userId = User.FindFirst("userId")?.Value;
            var username = User.Identity?.Name;

            if (userId == null) return Unauthorized();

            var user = await _context.Register.FindAsync(int.Parse(userId));
            if (user == null) return Unauthorized();

            if (username == "iver")
            {
                user.UserRole = PermissionLevel.Admin;
                await _context.SaveChangesAsync();
                return Ok($"你現在是管理員了！{user.UserRole}");
            }

            return Forbid("你沒有權限成為管理員");
        }

        // GET: /api/registers
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Register.ToListAsync();
            return Ok(users);
        }

        // GET: /api/registers/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Register.FindAsync(id);
            if (user == null) return NotFound();

            return Ok(user);
        }

        // PUT: /api/registers/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] Register updated)
        {
            if (id != updated.Id)
                return BadRequest("ID 不一致");

            var existing = await _context.Register.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return NotFound();

            updated.UserRole = existing.UserRole; // 保留原本權限

            _context.Entry(updated).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: /api/registers/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Register.FindAsync(id);
            if (user == null) return NotFound();

            _context.Register.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ 建立 JWT Token 的方法
        private string GenerateJwtToken(Register user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.UserRole.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
