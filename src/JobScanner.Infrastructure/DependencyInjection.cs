using System.Net;
using JobScanner.Application.Abstractions;
using JobScanner.Infrastructure.Cv;
using JobScanner.Infrastructure.Enrichment;
using JobScanner.Infrastructure.Extraction;
using JobScanner.Infrastructure.Liveness;
using JobScanner.Infrastructure.Llm;
using JobScanner.Infrastructure.Normalization;
using JobScanner.Infrastructure.Persistence;
using JobScanner.Infrastructure.Sources.Arbeitnow;
using JobScanner.Infrastructure.Sources.Jobicy;
using JobScanner.Infrastructure.Sources.RemoteOk;
using JobScanner.Infrastructure.Sources.Remotive;
using JobScanner.Infrastructure.Sources.WeWorkRemotely;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobScanner.Infrastructure;

/// <summary>Infrastructure katmani DI kaydi (Worker composition root'tan cagrilir).</summary>
public static class DependencyInjection
{
    private const string UserAgent = "JobScanner/1.0 (+https://github.com/EminBozkaya/RemoteJobFinder)";


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
        services.AddScoped<IApplicationMaterialRepository, EfApplicationMaterialRepository>();

        services.AddSingleton<IJobNormalizer, Normalizer>();
        services.AddSingleton<IJobEnricher, NoOpJobEnricher>();

        // Liveness checker — ilan URL'leri için HTTP HEAD, Polly + 10s timeout
        services.AddHttpClient<IJobLivenessChecker, HttpLivenessChecker>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            })
            .AddStandardResilienceHandler();
        // INotifier: Faz 1-2'de implementasyon YOK (kasitli olarak kaydedilmez).

        // LLM fact extraction (sağlayıcı-bağımsız IChatClient)
        AddLlm(services, config);

        // Kaynaklar (IJobSource) — toleransli HttpClient + Polly tabanli direnc
        services.Configure<JobicyOptions>(config.GetSection(JobicyOptions.SectionName));
        services.Configure<RemoteOkOptions>(config.GetSection(RemoteOkOptions.SectionName));
        services.Configure<WeWorkRemotelyOptions>(config.GetSection(WeWorkRemotelyOptions.SectionName));
        services.Configure<RemotiveOptions>(config.GetSection(RemotiveOptions.SectionName));
        services.Configure<ArbeitnowOptions>(config.GetSection(ArbeitnowOptions.SectionName));
        AddJobicy(services, config);
        AddRemoteOk(services, config);
        AddWeWorkRemotely(services, config);
        AddRemotive(services, config);
        AddArbeitnow(services, config);

        return services;
    }

    /// <summary>Tüm IJobSource HttpClient'ları için ortak ayar: UA + timeout + gzip + Polly resilience.</summary>
    private static void AddResilientSourceClient<TSource>(IServiceCollection services) where TSource : class
    {
        services.AddHttpClient<TSource>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
            })
            // gzip/brotli otomatik aç: RemoteOK gibi büyük (479KB→117KB) yanıtlarda
            // transfer süresini ~4x kısaltır, timeout riskini düşürür.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
            })
            .AddStandardResilienceHandler(o =>
            {
                // Bazı kaynaklar (örn. RemoteOK ~20s) default 10s attempt timeout altinda kalir.
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
            });
    }

    private static void AddLlm(IServiceCollection services, IConfiguration config)
    {
        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddSingleton<IExtractionVersion, ExtractionVersion>();
        services.AddSingleton<PromptRegistry>();

        // Materyal üretimi (Faz 4): ana CV kaynağı + prompt + generator
        services.Configure<CvOptions>(config.GetSection(CvOptions.SectionName));
        services.AddSingleton<ICvSource, FileCvSource>();
        services.AddSingleton<MaterialPromptRegistry>();

        var llm = config.GetSection(LlmOptions.SectionName).Get<LlmOptions>() ?? new LlmOptions();
        if (llm.Enabled)
        {
            services.AddSingleton(ChatClientFactory.Create(llm));
            services.AddSingleton<IEligibilityExtractor, LlmEligibilityExtractor>();
            services.AddSingleton<IApplicationMaterialGenerator, LlmApplicationMaterialGenerator>();
        }
        else
        {
            services.AddSingleton<IEligibilityExtractor, StubEligibilityExtractor>();
            services.AddSingleton<IApplicationMaterialGenerator, DisabledApplicationMaterialGenerator>();
        }
    }

    private static void AddJobicy(IServiceCollection services, IConfiguration config)
    {
        var opts = config.GetSection(JobicyOptions.SectionName).Get<JobicyOptions>() ?? new JobicyOptions();
        if (!opts.Enabled) return;

        AddResilientSourceClient<JobicyJobSource>(services);
        services.AddTransient<IJobSource>(sp => sp.GetRequiredService<JobicyJobSource>());
    }

    private static void AddRemoteOk(IServiceCollection services, IConfiguration config)
    {
        var opts = config.GetSection(RemoteOkOptions.SectionName).Get<RemoteOkOptions>() ?? new RemoteOkOptions();
        if (!opts.Enabled) return;

        AddResilientSourceClient<RemoteOkJobSource>(services);
        services.AddTransient<IJobSource>(sp => sp.GetRequiredService<RemoteOkJobSource>());
    }

    private static void AddWeWorkRemotely(IServiceCollection services, IConfiguration config)
    {
        var opts = config.GetSection(WeWorkRemotelyOptions.SectionName).Get<WeWorkRemotelyOptions>() ?? new WeWorkRemotelyOptions();
        if (!opts.Enabled) return;

        AddResilientSourceClient<WeWorkRemotelyJobSource>(services);
        services.AddTransient<IJobSource>(sp => sp.GetRequiredService<WeWorkRemotelyJobSource>());
    }

    private static void AddRemotive(IServiceCollection services, IConfiguration config)
    {
        var opts = config.GetSection(RemotiveOptions.SectionName).Get<RemotiveOptions>() ?? new RemotiveOptions();
        if (!opts.Enabled) return;

        AddResilientSourceClient<RemotiveJobSource>(services);
        services.AddTransient<IJobSource>(sp => sp.GetRequiredService<RemotiveJobSource>());
    }

    private static void AddArbeitnow(IServiceCollection services, IConfiguration config)
    {
        var opts = config.GetSection(ArbeitnowOptions.SectionName).Get<ArbeitnowOptions>() ?? new ArbeitnowOptions();
        if (!opts.Enabled) return;

        AddResilientSourceClient<ArbeitnowJobSource>(services);
        services.AddTransient<IJobSource>(sp => sp.GetRequiredService<ArbeitnowJobSource>());
    }
}
