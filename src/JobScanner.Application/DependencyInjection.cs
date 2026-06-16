using JobScanner.Application.Abstractions;
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

        // Saf is mantigi (yan etkisiz)
        services.AddSingleton<IRuleFilter, RuleFilter>();
        services.AddSingleton<IEligibilityDecider, StubEligibilityDecider>(); // Faz 1 stub
        services.AddSingleton<IScoringEngine, StubScoringEngine>();           // Faz 1 stub

        services.AddScoped<JobScanPipeline>();
        return services;
    }
}
