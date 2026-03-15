namespace Cutline.Api.Endpoints;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cutline.Core.Entities;
using Cutline.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        // POST /api/auth/register
        group.MapPost("/register", async (
            RegisterRequest req,
            CutlineDbContext db,
            IPasswordHasher<Manager> hasher,
            IConfiguration config,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.DisplayName) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "DisplayName, email, and password are required." });

            if (req.Password.Length < 8)
                return Results.BadRequest(new { error = "Password must be at least 8 characters." });

            var email = req.Email.Trim().ToLower();
            if (await db.Managers.AnyAsync(m => m.Email == email, ct))
                return Results.Conflict(new { error = "An account with that email already exists." });

            var manager = new Manager
            {
                Id          = Guid.NewGuid(),
                DisplayName = req.DisplayName.Trim(),
                Email       = email,
                CreatedAt   = DateTime.UtcNow,
            };
            manager.PasswordHash = hasher.HashPassword(manager, req.Password);

            db.Managers.Add(manager);
            await db.SaveChangesAsync(ct);

            var token = GenerateToken(manager, config);
            return Results.Ok(new { token, manager = MapManager(manager) });
        });

        // POST /api/auth/login
        group.MapPost("/login", async (
            LoginRequest req,
            CutlineDbContext db,
            IPasswordHasher<Manager> hasher,
            IConfiguration config,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Email and password are required." });

            var email   = req.Email.Trim().ToLower();
            var manager = await db.Managers.FirstOrDefaultAsync(m => m.Email == email, ct);

            if (manager is null || manager.PasswordHash is null)
                return Results.Unauthorized();

            var result = hasher.VerifyHashedPassword(manager, manager.PasswordHash, req.Password);
            if (result == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            // Rehash if needed (e.g. algorithm upgraded)
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                manager.PasswordHash = hasher.HashPassword(manager, req.Password);
                await db.SaveChangesAsync(ct);
            }

            var token = GenerateToken(manager, config);
            return Results.Ok(new { token, manager = MapManager(manager) });
        });

        // GET /api/auth/me
        group.MapGet("/me", async (
            HttpContext ctx,
            CutlineDbContext db,
            CancellationToken ct) =>
        {
            var managerId = GetManagerId(ctx);
            if (managerId == Guid.Empty) return Results.Unauthorized();

            var manager = await db.Managers.FirstOrDefaultAsync(m => m.Id == managerId, ct);
            return manager is null ? Results.NotFound() : Results.Ok(MapManager(manager));
        }).RequireAuthorization();
    }

    internal static Guid GetManagerId(HttpContext ctx)
    {
        var raw = ctx.User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private static string GenerateToken(Manager manager, IConfiguration config)
    {
        var secret = config["Jwt:Secret"]!;
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub",   manager.Id.ToString()),
                new Claim("email", manager.Email),
                new Claim("name",  manager.DisplayName),
            }),
            Expires            = DateTime.UtcNow.AddDays(30),
            SigningCredentials = creds,
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static object MapManager(Manager m) => new
    {
        id          = m.Id,
        displayName = m.DisplayName,
        email       = m.Email,
        avatarUrl   = m.AvatarUrl,
    };
}

public record RegisterRequest(string DisplayName, string Email, string Password);
public record LoginRequest(string Email, string Password);
