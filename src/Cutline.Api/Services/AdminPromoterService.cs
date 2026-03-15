namespace Cutline.Api.Services;

using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Runs once at startup. Reads the "Admin:Emails" config array and ensures every
/// listed email has IsAdmin = true on their Manager record.
///
/// Local dev: use POST /api/dev/seed-admin instead (creates the account + sets IsAdmin).
/// Production: set Admin:Emails in environment variables or appsettings.Production.json:
///
///   "Admin": { "Emails": ["you@example.com"] }
///
/// or as an env var:  Admin__Emails__0=you@example.com
/// </summary>
public class AdminPromoterService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<AdminPromoterService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var emails = config.GetSection("Admin:Emails").Get<string[]>();
        if (emails is null || emails.Length == 0) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CutlineDbContext>();

        var normalised = emails.Select(e => e.Trim().ToLower()).ToHashSet();
        var managers   = await db.Managers
            .Where(m => normalised.Contains(m.Email) && !m.IsAdmin)
            .ToListAsync(ct);

        if (managers.Count == 0) return;

        foreach (var m in managers)
            m.IsAdmin = true;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("AdminPromoter: promoted {Count} account(s) to admin", managers.Count);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
