using JobScanner.Application.Abstractions;
using JobScanner.Application.Applications;
using JobScanner.Application.Deciding;
using JobScanner.Application.Filtering;
using JobScanner.Application.Pipeline;
using JobScanner.Application.Scoring;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobScanner.Application;

/// <summary>Application katmani DI kaydi (Worker composition root'tan cagrilir).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PipelineOptions>(config.GetSection(PipelineOptions.SectionName));
        services.Configure<RuleFilterOptions>(config.GetSection(RuleFilterOptions.SectionName));
        services.Configure<DeciderOptions>(config.GetSection(DeciderOptions.SectionName));

        // Saf is mantigi (yan etkisiz)
        services.AddSingleton<IRuleFilter, RuleFilter>();
        services.AddSingleton<IEligibilityDecider, EligibilityDecider>();
        services.AddSingleton<IScoringEngine, ScoringEngine>();

        services.AddScoped<JobScanPipeline>();
        services.AddScoped<MaterialService>();
        return services;
    }
}
