using ForumWebsite;
using ForumWebsite.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<ForumDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<Register>, PasswordHasher<Register>>();

// API Controllers
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "請修正輸入的資料後再重試",
                Type = "https://httpstatuses.com/400"
            };

            return new BadRequestObjectResult(problemDetails);
        };
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Forum API", Version = "v1" });

    // ✅ JWT 認證支援
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "請輸入 JWT Token (格式: Bearer {token})",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// JWT 驗證
var jwtKey = builder.Configuration["Jwt:Key"] ??
    throw new InvalidOperationException("Jwt:Key 未設定，請於設定檔中配置安全的金鑰");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ??
    throw new InvalidOperationException("Jwt:Issuer 未設定，請於設定檔中配置發行者資訊");

var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = string.IsNullOrWhiteSpace(jwtAudience) ? null : jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// 健康檢查
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ForumDbContext>(name: "database");

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Forum API V1");
    });
}

// 全域錯誤處理
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "伺服器發生錯誤",
            Detail = app.Environment.IsDevelopment()
                ? exceptionFeature?.Error.Message
                : "系統發生未知錯誤，請稍後再試",
            Instance = context.Request.Path,
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

app.UseHttpsRedirection();
app.UseRouting();

// CORS
app.UseCors("AppCors");

// 驗證 & 授權
app.UseAuthentication();
app.UseAuthorization();

// API 控制器
app.MapControllers();
app.MapHealthChecks("/health");

// 測試首頁
app.MapGet("/", () => Results.Json(new
{
    message = "Forum API 正常運作中 🚀",
    docs = "/swagger"
}));

app.Run();
