namespace Cutline.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class CutlineDbContextFactory : IDesignTimeDbContextFactory<CutlineDbContext>
{
    public CutlineDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CutlineDbContext>()
            .UseNpgsql(
                Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                ?? "Host=localhost;Port=5432;Database=cutline;Username=cutline;Password=cutline_dev")
            .Options;

        return new CutlineDbContext(options);
    }
}
