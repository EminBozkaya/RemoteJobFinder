using JobScanner.Application.Pipeline;
using JobScanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JobScanner.Worker;

/// <summary>
/// Arka plan tarayici host'u. Acilista DB migrate + seed; sonra pipeline'i calistirir.
/// RunOnce=true ise tek tarama yapip uygulamayi durdurur; aksi halde IntervalHours periyodu ile dongu.
/// </summary>
public sealed class ScannerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly PipelineOptions _options;
    private readonly ILogger<ScannerHostedService> _log;

    public ScannerHostedService(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        IOptions<PipelineOptions> options,
        ILogger<ScannerHostedService> log)
    {
        _scopeFactory = scopeFactory;
        _lifetime = lifetime;
        _options = options.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeDatabaseAsync(stoppingToken);

        if (_options.RunOnce)
        {
            await RunScanAsync(stoppingToken);
            _log.LogInformation("RunOnce=true — tarama tamamlandi, uygulama durduruluyor.");
            _lifetime.StopApplication();
            return;
        }

        await RunScanAsync(stoppingToken); // ilk tarama hemen
        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.IntervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunScanAsync(stoppingToken);
        }
    }

    private async Task InitializeDatabaseAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobScannerDbContext>();
        _log.LogInformation("Veritabani migrate ediliyor...");
        await db.Database.MigrateAsync(ct);
        await DbSeeder.SeedAsync(db, ct);
    }

    private async Task RunScanAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<JobScanPipeline>();
            await pipeline.RunAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // kapaniyor — normal
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Tarama sirasinda beklenmeyen hata; bir sonraki periyotta tekrar denenecek.");
        }
    }
}
