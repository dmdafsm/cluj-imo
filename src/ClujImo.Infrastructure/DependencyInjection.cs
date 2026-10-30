using ClujImo.Infrastructure.Geocoding;
using ClujImo.Infrastructure.Ingestion;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Queries;
using ClujImo.Infrastructure.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenDataOptions>().Bind(configuration.GetSection(OpenDataOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<GeocodingOptions>().Bind(configuration.GetSection(GeocodingOptions.Section)).ValidateDataAnnotations().ValidateOnStart();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Default") ?? "Data Source=cluj-imo.db"));

        services.AddSingleton(TimeProvider.System);

        services.AddHttpClient<CkanClient>((sp, http) =>
            {
                http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<OpenDataOptions>>().Value.CkanApiUrl);
                http.Timeout = TimeSpan.FromMinutes(2);
            })
            .AddStandardResilienceHandler(o =>
            {
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
            });

        services.AddHttpClient<NominatimGeocoder>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value;
                http.BaseAddress = new Uri(options.NominatimUrl);
                http.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ro");
            });

        services.AddScoped<IngestionService>();
        services.AddScoped<GeocodingService>();
        services.AddScoped<PermitQueries>();
        return services;
    }
}
