using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobScanner.Infrastructure.Persistence;

/// <summary>
/// Tasarim-zamani (dotnet ef) DbContext fabrikasi. Migration uretimi/uygulamasi icin host'a
/// ihtiyac duymaz; baglanti dizesini ortam degiskeninden ya da varsayilandan alir.
/// </summary>
public sealed class JobScannerDbContextFactory : IDesignTimeDbContextFactory<JobScannerDbContext>
{
    public JobScannerDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Db")
            ?? "Host=localhost;Port=5432;Database=jobscanner;Username=jobscanner;Password=jobscanner";

        var options = new DbContextOptionsBuilder<JobScannerDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new JobScannerDbContext(options);
    }
}
