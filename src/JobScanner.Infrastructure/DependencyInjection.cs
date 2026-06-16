using JobScanner.Application.Abstractions;
using JobScanner.Infrastructure.Enrichment;
using JobScanner.Infrastructure.Extraction;
using JobScanner.Infrastructure.Llm;
using JobScanner.Infrastructure.Normalization;
using JobScanner.Infrastructure.Persistence;
using JobScanner.Infrastructure.Sources.Jobicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobScanner.Infrastructure;

/// <summary>Infrastructure katmani DI kaydi (Worker composition root'tan cagrilir).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Persistence (PostgreSQL — Npgsql)
        var connectionString = config.GetConnectionString("Db")
            ?? throw new InvalidOperationException("ConnectionStrings:Db tanimli degil.");
        services.AddDbContext<JobScannerDbContext>(opt => opt.UseNpgsql(connectionString));

        services.AddScoped<IJobRepository, EfJobRepository>();
        services.AddScoped<IDeduplicator, Deduplicator>();
        services.AddScoped<IProfileRepository, EfProfileRepository>();
        services.AddScoped<IUserMatchRepository, EfUserMatchRepository>();
        services.AddScoped<IFactsCache, EfFactsCache>();

        services.AddSingleton<IJobNormalizer, Normalizer>();
        services.AddSingleton<IJobEnricher, NoOpJobEnricher>();
        // INotifier: Faz 1-2'de implementasyon YOK (kasitli olarak kaydedilmez).

        // LLM fact extraction (sağlayıcı-bağımsız IChatClient)
        AddLlm(services, config);

        // Kaynaklar (IJobSource) — toleransli HttpClient + Polly tabanli direnc
        services.Configure<JobicyOptions>(config.GetSection(JobicyOptions.SectionName));
        AddJobicy(services, config);

        return services;
    }

    private static void AddLlm(IServiceCollection services, IConfiguration config)
    {
        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddSingleton<IExtractionVersion, ExtractionVersion>();
        services.AddSingleton<PromptRegistry>();

        var llm = config.GetSection(LlmOptions.SectionName).Get<LlmOptions>() ?? new LlmOptions();
        if (llm.Enabled)
        {
            services.AddSingleton(ChatClientFactory.Create(llm));
            services.AddSingleton<IEligibilityExtractor, LlmEligibilityExtractor>();
        }
        else
        {
            services.AddSingleton<IEligibilityExtractor, StubEligibilityExtractor>();
        }
    }

    private static void AddJobicy(IServiceCollection services, IConfiguration config)
    {
        var jobicy = config.GetSection(JobicyOptions.SectionName).Get<JobicyOptions>() ?? new JobicyOptions();
        if (!jobicy.Enabled) return;

        services.AddHttpClient<JobicyJobSource>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("JobScanner/1.0 (+https://github.com)");
            })
            .AddStandardResilienceHandler(); // Polly v8: retry + circuit breaker + timeout

        services.AddTransient<IJobSource>(sp => sp.GetRequiredService<JobicyJobSource>());
    }
}
